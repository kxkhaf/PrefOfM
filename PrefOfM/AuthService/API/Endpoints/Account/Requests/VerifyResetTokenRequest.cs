namespace AuthService.API.Endpoints.Account.Requests;

public record VerifyResetTokenRequest(string UserId, string Token);