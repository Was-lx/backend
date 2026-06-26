using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class KnowledgeVector : BaseEntity
    {
        public int TenantId { get; set; }
        public int? CustomerId { get; set; }
        public KnowledgeSourceType SourceType { get; set; }
        public int SourceId { get; set; }
        public string TextContent { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = [];

        public Tenant Tenant { get; set; } = null!;
        public Customer? Customer { get; set; }
    }
}
