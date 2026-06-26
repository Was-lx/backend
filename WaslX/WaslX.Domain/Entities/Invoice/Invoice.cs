using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class Invoice : BaseEntity
    {
        public int TenantId { get; set; }
        public decimal Amount { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidAt { get; set; }

        public Tenant Tenant { get; set; } = null!;
    }
}
