namespace TaskFlow.Application.Contracts.Authentication;
public record ChangePasswordRequest(
    string OldPassword,
    string NewPassword,
    string ConfirmPassword
);
