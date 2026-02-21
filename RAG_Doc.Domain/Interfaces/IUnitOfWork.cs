using System;
using System.Collections.Generic;
using System.Text;

namespace RAG_Doc.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
         IDocumentRepo Document  { get; }
        IDocumentChunkRepo DocumentChunk { get; }
        IChunkEmbeddingRepo ChunkEmbedding { get; }
        IQueryLogRepo QueryLog { get; }

        Task SaveChangesAsync(CancellationToken ct = default);
        Task CommitAsync(CancellationToken ct = default); // Wolverine uses this for outbox
    }
}
