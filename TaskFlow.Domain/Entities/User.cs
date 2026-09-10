namespace TaskFlow.Domain.Entities;

public partial class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int RoleId { get; set; }
    public bool MustChangePassword { get; set; }
    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual Role Role { get; set; } = null!;
}
