namespace EmailService.API.Requests;

public record PasswordResetRequest(
    string Email,
    string UserId,
    string Token);