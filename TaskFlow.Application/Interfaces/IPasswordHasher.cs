namespace TaskFlow.Application.Interfaces;

public interface IPasswordHasher
{
    // Проверяет, совпадает ли введенный пароль с хешем из БД
    bool Verify(string password, string passwordHash);
}
