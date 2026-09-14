using NetTopologySuite.Geometries;

namespace Spot.Domain.Entities;

public class TaskList
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    /// <summary>
    /// Optional geofenced location trigger (WGS 84 SRID 4326).
    /// </summary>
    public Point? CustomLocation { get; set; }
    
    public int RadiusMeters { get; set; } = 150;
    public bool IsActive { get; set; } = true;

    public ICollection<TaskItem> Items { get; set; } = new List<TaskItem>();
}
