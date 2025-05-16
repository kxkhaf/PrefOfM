namespace AuthService.API.Endpoints.Account.Requests;

public record ConfirmEmailChangeRequest(
    string UserId,
    string NewEmail, 
    string Token
);

