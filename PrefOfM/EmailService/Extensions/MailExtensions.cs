namespace EmailService.Extensions;

public static class MailExtensions
{
    public static T Also<T>(this T obj, Action<T> action)
    {
        action(obj);
        return obj;
    }
}