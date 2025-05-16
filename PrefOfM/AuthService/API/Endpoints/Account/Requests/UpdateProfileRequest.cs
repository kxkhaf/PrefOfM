namespace AuthService.API.Endpoints.Account.Requests;

public record UpdateEmailRequest(string NewEmail, string CurrentPassword);
