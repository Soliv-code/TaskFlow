namespace TaskFlow.Domain.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    // Навигационное свойство: одна роль -> много пользователей
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
