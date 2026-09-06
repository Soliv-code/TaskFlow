using System.Security.Cryptography;
using System.Text;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
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
            32);

        // Сравниваем хеши за фиксированное время (защита от timing attacks)
        return CryptographicOperations.FixedTimeEquals(expectedHash, inputHash);
    }
}
