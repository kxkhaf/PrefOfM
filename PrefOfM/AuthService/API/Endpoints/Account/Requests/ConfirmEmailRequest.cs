using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Endpoints.Account.Requests;

public record ConfirmEmailRequest(
    string UserId,
    string Token);