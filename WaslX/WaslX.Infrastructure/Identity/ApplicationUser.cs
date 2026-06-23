using Microsoft.AspNetCore.Identity;

namespace WaslX.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsDisabled { get; set; }
    public bool IsForgetPasswordOtpConfirmed { get; set; }
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}