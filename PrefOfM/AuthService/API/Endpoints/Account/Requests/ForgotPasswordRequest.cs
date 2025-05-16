namespace AuthService.API.Endpoints.Account.Requests;

public record ForgotPasswordRequest(string UserName, string Email);