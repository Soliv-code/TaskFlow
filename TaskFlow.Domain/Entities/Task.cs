namespace TaskFlow.Domain.Entities;

public partial class Task
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int StatusId { get; set; }

    public int PriorityId { get; set; }

    public int ProjectId { get; set; }

    public int? AssigneeId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? DueDate { get; set; }

    public virtual User? Assignee { get; set; }

    public virtual TaskPriority Priority { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual TaskStatus Status { get; set; } = null!;
}
