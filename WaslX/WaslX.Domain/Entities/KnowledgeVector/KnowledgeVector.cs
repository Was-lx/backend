using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class KnowledgeVector : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid? CustomerId { get; set; }
        public KnowledgeSourceType SourceType { get; set; }
        public Guid SourceId { get; set; }
        public string TextContent { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = [];

        public Tenant Tenant { get; set; } = null!;
        public Customer? Customer { get; set; }
    }
}