namespace TaskFlow.Domain.Entities;

public partial class ProjectMemberRole
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public DateTime? CreatedAt { get; set; }
    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
}
