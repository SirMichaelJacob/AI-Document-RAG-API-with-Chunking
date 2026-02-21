using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RAG_Doc.Domain.Entities
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? FilePath { get; set; }

        [MaxLength(100)]
        public string? ContentType { get; set; }

        public long SizeInBytes { get; set; }

        public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

        public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAtUtc { get; set; }

        public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }

    public class DocumentChunk
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid DocumentId { get; set; }

        public int Sequence { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        // Token count helps ranking and truncation
        public int TokenCount { get; set; }

        // Used to detect if re-embedding is required
        [MaxLength(64)]
        public string ContentHash { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public Document Document { get; set; } = null!;
    }

    public class ChunkEmbedding
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ChunkId { get; set; }

        [Required, MaxLength(100)]
        public string ModelName { get; set; } = string.Empty;

        public int VectorDimension { get; set; }
        public byte[] VectorData { get; set; } = Array.Empty<byte>();

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DocumentChunk Chunk { get; set; } = null!;
    }

    public class QueryLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(1000)]
        public string QueryText { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public enum DocumentStatus
    {
        Uploaded = 0,
        Chunked = 1,
        Embedded = 2,
        Completed = 3,
        Failed = 4
    }
}
