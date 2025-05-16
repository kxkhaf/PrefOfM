using AuthService.Application.Settings;

namespace AuthService.Validators.Extensions;

public static class PasswordValidatorExtensions
{
    public static List<string> GetPasswordRequirements(this PasswordSettings settings)
    {
        var requirements = new List<string>();
        
        if (settings.RequireDigit)
            requirements.Add("at least one digit (0-9)");
        if (settings.RequireLowercase)
            requirements.Add("at least one lowercase letter (a-z)");
        if (settings.RequireUppercase)
            requirements.Add("at least one uppercase letter (A-Z)");
        if (settings.RequireNonAlphanumeric)
            requirements.Add("at least one special character");
        if (settings.RequiredUniqueChars > 0)
            requirements.Add($"at least {settings.RequiredUniqueChars} unique characters");
        
        requirements.Add($"minimum length of {settings.RequiredLength} characters");
        
        return requirements;
    }
}