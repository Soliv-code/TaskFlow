namespace TaskFlow.Application.Interfaces;

public interface IPasswordHasher
{
    // Генерирует хеш пароля для сохранения в БД
    string Hash(string password);
    // Проверяет, совпадает ли введенный пароль с хешем из БД
    bool Verify(string password, string passwordHash);
}
