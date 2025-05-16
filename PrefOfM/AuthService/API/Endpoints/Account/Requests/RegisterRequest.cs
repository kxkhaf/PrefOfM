namespace AuthService.API.Endpoints.Account.Requests;

public record RegisterRequest(
    string UserName,
    string Email,
    string Password,
    DateOnly Birthday
);
