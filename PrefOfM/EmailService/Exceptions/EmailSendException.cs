namespace EmailService.Exceptions;

public class EmailSendException : Exception
{
    public EmailSendException(string message) : base(message) { }
    public EmailSendException(string message, Exception innerException) 
        : base(message, innerException) { }
}