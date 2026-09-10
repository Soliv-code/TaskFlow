namespace TaskFlow.Domain.Entities;

public partial class ProjectMember
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime? JoinedAt { get; set; }
    public virtual Project Project { get; set; } = null!;
    public virtual ProjectMemberRole Role { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
