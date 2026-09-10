namespace TaskFlow.Domain.Entities;

public partial class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public virtual User Owner { get; set; } = null!;
    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = [];
    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = [];
}
