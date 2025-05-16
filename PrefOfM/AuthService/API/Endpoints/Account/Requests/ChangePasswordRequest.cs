namespace AuthService.API.Endpoints.Account.Requests;

public record ChangePasswordRequest(
    string UserId,
    string CurrentPassword,
    string NewPassword
);