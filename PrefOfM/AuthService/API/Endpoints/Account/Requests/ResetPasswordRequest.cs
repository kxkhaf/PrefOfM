namespace AuthService.API.Endpoints.Account.Requests;

public record ResetPasswordRequest(string UserId, string Token, string NewPassword);