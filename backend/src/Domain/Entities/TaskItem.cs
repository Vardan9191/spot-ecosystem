namespace Spot.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ListId { get; set; }
    public TaskList TaskList { get; set; } = null!;
    
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public string Quantity { get; set; } = "1";
}
