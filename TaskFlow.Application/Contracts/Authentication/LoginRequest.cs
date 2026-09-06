namespace TaskFlow.Application.Contracts.Authentication;

public record LoginRequest(
    string Username,
    string Password
);
