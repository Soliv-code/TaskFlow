namespace TaskFlow.Domain.Entities;

public partial class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    // Вместо строковой роли теперь связь через внешний ключ
    public int RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;
    
    // Флаг обязательной смены временного пароля
    public bool MustChangePassword { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}