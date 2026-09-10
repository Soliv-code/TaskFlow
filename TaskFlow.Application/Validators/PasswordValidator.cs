using System.Text.RegularExpressions;
using TaskFlow.Application.Exceptions;

namespace TaskFlow.Application.Validators;

public class PasswordValidator
{
    public static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new BusinessException("Пароль не может быть пустым");

        if (password.Length < 8)
            throw new BusinessException("Пароль должен содержать минимум 8 символов");

        if (!Regex.IsMatch(password, "[A-Z]"))
            throw new BusinessException("Пароль должен содержать хотя бы одну заглавную букву (A-Z)");

        if (!Regex.IsMatch(password, "[0-9]") && !Regex.IsMatch(password, "[!@#$%^&*()_+\\-=\\[\\]{};':\"\\\\|,.<>\\/?]"))
            throw new BusinessException("Пароль должен содержать хотя бы одну цифру (0-9) или спецсимвол (!@#$%^&*)");
    }
}
