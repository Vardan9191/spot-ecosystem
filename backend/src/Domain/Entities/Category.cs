namespace Spot.Domain.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    public ICollection<Store> Stores { get; set; } = new List<Store>();
    public ICollection<TaskList> TaskLists { get; set; } = new List<TaskList>();
}
