namespace Spot.Domain.Entities;

public class StoreReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;
    public Guid UserId { get; set; }
    
    public int RatingService { get; set; }
    public int RatingQuality { get; set; }
    public int RatingOverall { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
