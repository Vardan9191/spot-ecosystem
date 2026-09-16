using NetTopologySuite.Geometries;

namespace Spot.Domain.Entities;

public class Store
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    /// <summary>
    /// Spatial Point representation with WGS 84 (SRID 4326).
    /// Note: Point.X = Longitude, Point.Y = Latitude.
    /// </summary>
    public Point Location { get; set; } = null!;
    
    public int RadiusMeters { get; set; } = 100;
    public string Address { get; set; } = string.Empty;
    public bool IsPartner { get; set; } = false;

    public ICollection<StoreReview> Reviews { get; set; } = new List<StoreReview>();
    public ICollection<StoreStory> Stories { get; set; } = new List<StoreStory>();
}
