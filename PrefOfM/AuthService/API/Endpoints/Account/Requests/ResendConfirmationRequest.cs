namespace AuthService.API.Endpoints.Account.Requests;

public record ResendConfirmationRequest(
    string UserName,
    string Password,
    string Email);