namespace Spot.Domain.Entities;

public class StoreReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;
    public Guid UserId { get; set; }
    
    public int RatingService { get; set; }
    public int RatingQuality { get; set; }
    public int RatingAtmosphere { get; set; } = 5;
    public int RatingOverall { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // AI Screening & Fraud Prevention Metadata
    public bool IsFlaggedAsSpam { get; set; } = false;
    public double SpamConfidence { get; set; } = 0.0;
    public string? SpamReason { get; set; }
    public string SentimentService { get; set; } = "Neutral";
    public string SentimentQuality { get; set; } = "Neutral";
    public string SentimentAtmosphere { get; set; } = "Neutral";
    public string? AuthorIpAddress { get; set; }
}
