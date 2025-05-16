namespace MusicService.Domain.Exceptions;

public abstract class DomainException(string message) : System.Exception(message);

public class NotFoundException(string entityName, object key)
    : DomainException($"Entity \"{entityName}\" ({key}) was not found.");

public abstract class ValidationException(IDictionary<string, string[]> errors)
    : DomainException("Validation errors occurred")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}