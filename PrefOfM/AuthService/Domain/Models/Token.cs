using AuthService.Domain.Enums;

namespace AuthService.Domain.Models;

public record Token(string Value, DateTime Expiration);