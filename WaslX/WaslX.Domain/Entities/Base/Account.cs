using System;
using WaslX.Domain.Common;

namespace WaslX.Domain.Entities.Base
{
    public abstract class Account : BaseEntity
    {
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
