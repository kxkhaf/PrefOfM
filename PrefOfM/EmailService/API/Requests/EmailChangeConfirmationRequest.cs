namespace EmailService.API.Requests;

public record EmailChangeConfirmationRequest(
    string NewEmail,
    string OldEmail,
    string UserId,
    string Token);