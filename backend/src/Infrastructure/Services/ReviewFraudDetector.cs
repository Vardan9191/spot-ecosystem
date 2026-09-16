using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Spot.Infrastructure.Services;

public class ReviewFraudDetector : IReviewFraudDetector
{
    private const int MaxAllowedReviewsPerMinute = 5;
    private static readonly TimeSpan VelocityWindow = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _ipSubmissions = new();
    private readonly ConcurrentDictionary<Guid, List<DateTimeOffset>> _userSubmissions = new();
    private readonly HttpClient? _httpClient;
    private readonly string? _geminiApiKey;
    private readonly ILogger<ReviewFraudDetector> _logger;

    private static readonly Regex UrlRegex = new(@"https?:\/\/|www\.|bit\.ly|t\.me|wa\.me|\.xyz|\.club", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] SpamKeywords = 
    { 
        "crypto", "bitcoin", "free bonus", "whatsapp me", "telegram", 
        "earn $", "passive income", "free money", "invest now", "casino" 
    };

    private static readonly string[] PositiveServiceWords = { "friendly", "polite", "attentive", "fast", "helpful", "welcoming", "kind", "great service", "quick" };
    private static readonly string[] NegativeServiceWords = { "rude", "slow", "unhelpful", "ignored", "impatient", "bad service", "terrible service", "attitude" };

    private static readonly string[] PositiveQualityWords = { "fresh", "delicious", "tasty", "great", "excellent", "premium", "flavorful", "high quality", "clean", "authentic" };
    private static readonly string[] NegativeQualityWords = { "stale", "spoiled", "disgusting", "terrible", "poor quality", "defective", "broken", "cheap", "tasteless", "expired" };

    private static readonly string[] PositiveAtmosphereWords = { "cozy", "aesthetic", "vibe", "pleasant", "relaxing", "spacious", "modern", "beautiful", "ambient", "music", "clean" };
    private static readonly string[] NegativeAtmosphereWords = { "loud", "noisy", "dirty", "smelly", "cramped", "dark", "chaotic", "uncomfortable" };

    public ReviewFraudDetector(
        IConfiguration configuration,
        ILogger<ReviewFraudDetector> logger,
        HttpClient? httpClient = null)
    {
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
        _geminiApiKey = configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    }

    public void ResetVelocityTracking()
    {
        _ipSubmissions.Clear();
        _userSubmissions.Clear();
    }

    public bool CheckAndRecordVelocity(string? ipAddress, Guid userId, out string? reason)
    {
        var now = DateTimeOffset.UtcNow;
        reason = null;

        // 1. IP-based Velocity Check
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            var ipList = _ipSubmissions.GetOrAdd(ipAddress, _ => new List<DateTimeOffset>());
            lock (ipList)
            {
                ipList.RemoveAll(t => now - t > VelocityWindow);
                if (ipList.Count >= MaxAllowedReviewsPerMinute)
                {
                    reason = $"Velocity violation: IP '{ipAddress}' submitted {ipList.Count} reviews within 60 seconds (max {MaxAllowedReviewsPerMinute}).";
                    return false;
                }
                ipList.Add(now);
            }
        }

        // 2. User-based Velocity Check
        if (userId != Guid.Empty)
        {
            var userList = _userSubmissions.GetOrAdd(userId, _ => new List<DateTimeOffset>());
            lock (userList)
            {
                userList.RemoveAll(t => now - t > VelocityWindow);
                if (userList.Count >= MaxAllowedReviewsPerMinute)
                {
                    reason = $"Velocity violation: User '{userId}' submitted {userList.Count} reviews within 60 seconds (max {MaxAllowedReviewsPerMinute}).";
                    return false;
                }
                userList.Add(now);
            }
        }

        return true;
    }

    public async Task<ReviewScreeningResult> ScreenReviewAsync(ReviewScreeningRequest request, CancellationToken ct = default)
    {
        // Step 1: Check velocity
        bool velocityOk = CheckAndRecordVelocity(request.IpAddress, request.UserId, out var velocityReason);
        if (!velocityOk)
        {
            _logger.LogWarning("Fraud detected: {Reason}", velocityReason);
            return new ReviewScreeningResult(
                IsFlaggedAsSpam: true,
                SpamConfidence: 0.99,
                SpamReason: velocityReason,
                SentimentService: "Negative",
                SentimentQuality: "Negative",
                SentimentAtmosphere: "Negative",
                VelocityExceeded: true
            );
        }

        var comment = request.Comment ?? string.Empty;

        // Step 2: Content Rule / Heuristic Check
        if (UrlRegex.IsMatch(comment))
        {
            return new ReviewScreeningResult(
                IsFlaggedAsSpam: true,
                SpamConfidence: 0.95,
                SpamReason: "Promotional link or external redirect URL detected.",
                SentimentService: "Neutral",
                SentimentQuality: "Neutral",
                SentimentAtmosphere: "Neutral"
            );
        }

        foreach (var spamWord in SpamKeywords)
        {
            if (comment.Contains(spamWord, StringComparison.OrdinalIgnoreCase))
            {
                return new ReviewScreeningResult(
                    IsFlaggedAsSpam: true,
                    SpamConfidence: 0.90,
                    SpamReason: $"Suspicious commercial/scam keyword detected: '{spamWord}'.",
                    SentimentService: "Neutral",
                    SentimentQuality: "Neutral",
                    SentimentAtmosphere: "Neutral"
                );
            }
        }

        // Step 3: Call Gemini AI API if API key is configured
        if (!string.IsNullOrWhiteSpace(_geminiApiKey) && _httpClient != null && comment.Length >= 5)
        {
            try
            {
                var geminiResult = await ScreenWithGeminiApiAsync(request, ct);
                if (geminiResult != null)
                {
                    return geminiResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini AI review screening call failed or timed out. Falling back to rule-based sentiment engine.");
            }
        }

        // Step 4: Rule-based Semantic Sentiment Analysis Fallback
        var sentimentService = ClassifyDimensionSentiment(comment, request.RatingService, PositiveServiceWords, NegativeServiceWords);
        var sentimentQuality = ClassifyDimensionSentiment(comment, request.RatingQuality, PositiveQualityWords, NegativeQualityWords);
        var sentimentAtmosphere = ClassifyDimensionSentiment(comment, request.RatingAtmosphere, PositiveAtmosphereWords, NegativeAtmosphereWords);

        return new ReviewScreeningResult(
            IsFlaggedAsSpam: false,
            SpamConfidence: 0.05,
            SpamReason: null,
            SentimentService: sentimentService,
            SentimentQuality: sentimentQuality,
            SentimentAtmosphere: sentimentAtmosphere
        );
    }

    private string ClassifyDimensionSentiment(string comment, int rating, string[] positiveKeywords, string[] negativeKeywords)
    {
        int positiveHits = positiveKeywords.Count(k => comment.Contains(k, StringComparison.OrdinalIgnoreCase));
        int negativeHits = negativeKeywords.Count(k => comment.Contains(k, StringComparison.OrdinalIgnoreCase));

        if (negativeHits > positiveHits || rating <= 2)
        {
            return "Negative";
        }
        if (positiveHits > negativeHits || rating >= 4)
        {
            return "Positive";
        }
        return "Neutral";
    }

    private async Task<ReviewScreeningResult?> ScreenWithGeminiApiAsync(ReviewScreeningRequest request, CancellationToken ct)
    {
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_geminiApiKey}";

        var prompt = $@"
You are an expert fraud detection and sentiment analysis engine for local retail reviews.
Analyze the following review:
Comment: ""{request.Comment.Replace("\"", "\\\"")}""
Ratings: Service={request.RatingService}/5, Quality={request.RatingQuality}/5, Atmosphere={request.RatingAtmosphere}/5, Overall={request.RatingOverall}/5.

Respond ONLY with valid JSON conforming to this schema:
{{
  ""is_spam"": boolean,
  ""spam_confidence"": number (0.0 to 1.0),
  ""spam_reason"": string or null,
  ""sentiment_service"": ""Positive"" | ""Neutral"" | ""Negative"",
  ""sentiment_quality"": ""Positive"" | ""Neutral"" | ""Negative"",
  ""sentiment_atmosphere"": ""Positive"" | ""Neutral"" | ""Negative""
}}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = 0.1
            }
        };

        var response = await _httpClient!.PostAsync(
            endpoint, 
            new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"),
            ct
        );

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini API returned non-success code {StatusCode}", response.StatusCode);
            return null;
        }

        var jsonString = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(jsonString);
        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0) return null;

        var textContent = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(textContent)) return null;

        var parsed = JsonSerializer.Deserialize<GeminiAnalysisResponse>(textContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (parsed == null) return null;

        return new ReviewScreeningResult(
            IsFlaggedAsSpam: parsed.IsSpam,
            SpamConfidence: parsed.SpamConfidence,
            SpamReason: parsed.SpamReason,
            SentimentService: parsed.SentimentService ?? "Neutral",
            SentimentQuality: parsed.SentimentQuality ?? "Neutral",
            SentimentAtmosphere: parsed.SentimentAtmosphere ?? "Neutral"
        );
    }

    private class GeminiAnalysisResponse
    {
        [JsonPropertyName("is_spam")]
        public bool IsSpam { get; set; }

        [JsonPropertyName("spam_confidence")]
        public double SpamConfidence { get; set; }

        [JsonPropertyName("spam_reason")]
        public string? SpamReason { get; set; }

        [JsonPropertyName("sentiment_service")]
        public string? SentimentService { get; set; }

        [JsonPropertyName("sentiment_quality")]
        public string? SentimentQuality { get; set; }

        [JsonPropertyName("sentiment_atmosphere")]
        public string? SentimentAtmosphere { get; set; }
    }
}
