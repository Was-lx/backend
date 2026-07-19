using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    /// <summary>
    /// One ingest unit for the RAG knowledge base — a FAQ, an uploaded document, or a website page.
    /// Owns the chunk lifecycle: a Hangfire job drives Pending → Processing → Indexed/Failed.
    /// </summary>
    public class KnowledgeDocument : BaseEntity
    {
        public int TenantId { get; set; }
        public KnowledgeSourceType SourceType { get; set; }

        /// <summary>Polymorphic reference to the origin row (e.g. FAQ.Id) — no FK, validated in code.</summary>
        public int? SourceRefId { get; set; }

        public string Title { get; set; } = string.Empty;
        public Language Language { get; set; } = Language.English;

        // Document source (Cloudinary)
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public string? MimeType { get; set; }

        // Website source
        public string? SourceUrl { get; set; }

        public KnowledgeDocumentStatus Status { get; set; } = KnowledgeDocumentStatus.Pending;
        public string? ErrorMessage { get; set; }
        public int ChunkCount { get; set; }
        public int Version { get; set; } = 1;

        public Tenant Tenant { get; set; } = null!;
        public ICollection<KnowledgeVector> Chunks { get; set; } = new HashSet<KnowledgeVector>();
    }
}
