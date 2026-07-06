using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// A tenant's saved card. SIMULATED for now — we only store display metadata
    /// (brand + last four + expiry). No real card data and no gateway integration yet;
    /// the real payment provider arrives in a later sprint and plugs in behind this.
    /// </summary>
    public class PaymentMethod : BaseEntity
    {
        public int TenantId { get; set; }
        public string Brand { get; set; } = string.Empty;   // Visa / Mastercard / …
        public string Last4 { get; set; } = string.Empty;
        public int ExpMonth { get; set; }
        public int ExpYear { get; set; }
        public string? HolderName { get; set; }
        public bool IsDefault { get; set; }

        public Tenant Tenant { get; set; } = null!;
    }
}
