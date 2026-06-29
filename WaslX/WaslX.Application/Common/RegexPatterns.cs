namespace WaslX.Application.Common;

public static class RegexPatterns
{
    // At least 8 chars, one lowercase, one uppercase, one non-alphanumeric.
    public const string Password = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$";
}
