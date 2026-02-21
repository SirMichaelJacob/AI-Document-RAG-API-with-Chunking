using RAG_Doc.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RAG_Doc.Domain.Interfaces
{
    public interface IGenericRepo<T> where T : class
    {
        IQueryable<T> GetAll(CancellationToken cancellationToken = default);
        Task<T> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        void Delete(T entity);
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    }

    public interface IDocumentRepo : IGenericRepo<Document>
    {
        Task<List<string>> GetChunkContentsByIdsAsync(List<Guid> Ids, CancellationToken cancellationToken = default);

    }
    public interface IDocumentChunkRepo : IGenericRepo<DocumentChunk>
    {
        Task<bool> ExistsByHashAsync(string hash, CancellationToken cancellationToken = default);
        Task<DocumentChunk> GetByHashAsync(string hash, CancellationToken cancellationToken = default);
        Task<List<DocumentChunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    }
    public interface IChunkEmbeddingRepo : IGenericRepo<ChunkEmbedding>
    {
        Task<List<ChunkEmbedding>> GetByModelAsync(string modelName,CancellationToken cancellationToken = default);

        Task<ChunkEmbedding?> GetByChunkIdAsync(Guid chunkId,string modelName,CancellationToken cancellationToken = default);

        Task<List<(Guid ChunkId, byte[] VectorData)>> GetVectorsByModelAsync(string modelName,CancellationToken cancellationToken = default);

        Task DeleteByModelAsync(string modelName,CancellationToken cancellationToken = default);
        Task<List<ChunkEmbedding>> GetAllAsync(CancellationToken ct = default);

    }
    public interface IQueryLogRepo : IGenericRepo<QueryLog>
    {
        Task<List<QueryLog>> GetRecentAsync(int count,CancellationToken cancellationToken = default);

        Task<int> CountSinceAsync(DateTime sinceUtc,CancellationToken cancellationToken = default);
    }
}
