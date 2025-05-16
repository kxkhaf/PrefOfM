namespace EmailService.API.Requests;

public record EmailConfirmationRequest(
    string Email,
    string UserId,
    string Token);
