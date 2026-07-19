using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    /// <summary>
    /// A single indexed chunk of a <see cref="KnowledgeDocument"/>. The embedding vector itself lives
    /// in Qdrant (see QdrantPointId) alongside a denormalized copy of TextContent — this row is the
    /// SQL-side source of truth for lineage, versioning and change-detection (ContentHash).
    /// </summary>
    public class KnowledgeVector : BaseEntity
    {
        public int TenantId { get; set; }
        public int? CustomerId { get; set; }
        public KnowledgeSourceType SourceType { get; set; }

        /// <summary>Polymorphic reference to the origin row (mirrors the parent document's SourceRefId).</summary>
        public int SourceId { get; set; }

        public string TextContent { get; set; } = string.Empty;

        public int DocumentId { get; set; }
        public int ChunkIndex { get; set; }
        public Guid QdrantPointId { get; set; }
        public string ContentHash { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public KnowledgeDocumentStatus Status { get; set; } = KnowledgeDocumentStatus.Pending;
        public int Version { get; set; } = 1;

        public Tenant Tenant { get; set; } = null!;
        public Customer? Customer { get; set; }
        public KnowledgeDocument Document { get; set; } = null!;
    }
}
