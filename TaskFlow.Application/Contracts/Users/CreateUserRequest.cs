namespace TaskFlow.Application.Contracts.Users;

public record CreateUserRequest(
    string Username,
    string Email,
    string Password,
    string Role = "User" // По умолчанию обычный пользователь
);