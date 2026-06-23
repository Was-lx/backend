using Microsoft.AspNetCore.Identity;

namespace WaslX.Infrastructure.Identity
{
    public class ApplicationRole : IdentityRole
    {
        public bool IsDefault { get; set; }
        public bool IsDeleted { get; set; }
    }
}
