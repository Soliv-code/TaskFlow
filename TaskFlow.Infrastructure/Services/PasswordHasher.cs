using System.Security.Cryptography;
using System.Text;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    // <-- НОВЫЙ МЕТОД: Генерация хеша для сохранения в БД
    public string Hash(string password)
    {
        // 1. Генерируем криптографически стойкую случайную соль
        byte[] salt = new byte[SaltSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);

        // 2. Генерируем хеш из пароля, соли и итераций
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize
        );

        // 3. Собираем всё в одну строку формата "iterations:salt:hash"
        return $"{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        // Наш формат в БД: итерации:base64_соль:base64_хеш
        var parts = passwordHash.Split(':');
        if (parts.Length != 3) return false;

        int iterations = int.Parse(parts[0]);
        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] expectedHash = Convert.FromBase64String(parts[2]);

        // Генерируем хеш из введенного пароля с теми же параметрами
        byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            HashSize
        );

        // Сравниваем хеши за фиксированное время (защита от timing attacks)
        return CryptographicOperations.FixedTimeEquals(expectedHash, inputHash);
    }
}