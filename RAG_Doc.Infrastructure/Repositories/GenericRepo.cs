using Microsoft.EntityFrameworkCore;
using RAG_Doc.Domain.Entities;
using RAG_Doc.Domain.Interfaces;
using RAG_Doc.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace RAG_Doc.Infrastructure.Repositories
{
    public class GenericRepo<T> : IGenericRepo<T> where T : class
    {
        protected readonly AppDbContext _db;
        protected readonly DbSet<T> _dbSet;
        public GenericRepo(AppDbContext db)
        {
            _db = db;
            _dbSet = db.Set<T>();
        }
        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public IQueryable<T> GetAll(CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsNoTracking();
            return query;
        }

        public async Task<T> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);
        }
    }

    public class DocumentRepo : GenericRepo<Document>, IDocumentRepo
    {
        private const int SqlServerParameterLimit = 2000; // safety margin

        public DocumentRepo(AppDbContext db) : base(db)
        {
        }

        public async Task<List<string>> GetChunkContentsByIdsAsync(List<Guid> ids,CancellationToken cancellationToken = default)
        {
            if (ids == null || ids.Count == 0)
                return new List<string>();

            // Avoid SQL Server parameter limit issues
            var results = new List<DocumentChunk>();

            foreach (var batch in Batch(ids, SqlServerParameterLimit))
            {
                var chunkBatch = await _db.Set<DocumentChunk>()
                    .AsNoTracking()
                    .Where(dc => batch.Contains(dc.Id))
                    .Select(dc => new DocumentChunk
                    {
                        Id = dc.Id,
                        Text = dc.Text
                    })
                    .ToListAsync(cancellationToken);

                results.AddRange(chunkBatch);
            }

            // Preserve FAISS ranking order
            var lookup = results.ToDictionary(r => r.Id, r => r.Text);

            return ids
                .Where(id => lookup.ContainsKey(id))
                .Select(id => lookup[id])
                .ToList();
        }

        // ============================================================
        // INTERNAL BATCHING
        // ============================================================

        private static IEnumerable<List<Guid>> Batch(List<Guid> source, int size)
        {
            for (int i = 0; i < source.Count; i += size)
            {
                yield return source.GetRange(i, Math.Min(size, source.Count - i));
            }
        }
    }

    public class DocumentChunkRepo: GenericRepo<DocumentChunk>, IDocumentChunkRepo
    {
        public DocumentChunkRepo(AppDbContext db) : base(db)
        {
        }

        public async Task<bool> ExistsByHashAsync(string hash,CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .AnyAsync(x => x.ContentHash == hash, cancellationToken);
        }

        public async Task<DocumentChunk?> GetByHashAsync(string hash, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ContentHash == hash, cancellationToken);
        }

        public async Task<List<DocumentChunk>> GetByDocumentIdAsync(Guid documentId,CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(x => x.DocumentId == documentId)
                .OrderBy(x => x.Sequence)
                .ToListAsync(cancellationToken);
        }
    }

    public class ChunkEmbeddingRepo: GenericRepo<ChunkEmbedding>, IChunkEmbeddingRepo
    {
        public ChunkEmbeddingRepo(AppDbContext db) : base(db)
        {
        }

        public async Task<List<ChunkEmbedding>> GetByModelAsync(string modelName,CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(x => x.ModelName == modelName)
                .ToListAsync(cancellationToken);
        }

        public async Task<ChunkEmbedding?> GetByChunkIdAsync(Guid chunkId,string modelName,CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ChunkId == chunkId && x.ModelName == modelName,
                    cancellationToken);
        }

        public async Task<List<(Guid ChunkId, byte[] VectorData)>>GetVectorsByModelAsync(string modelName,CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(x => x.ModelName == modelName)
                .Select(x => new ValueTuple<Guid, byte[]>(
                    x.ChunkId,
                    x.VectorData))
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ChunkEmbedding>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.Set<ChunkEmbedding>()
                .AsNoTracking()
                .ToListAsync(ct);
        }
        public async Task DeleteByModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            var embeddings = await _dbSet
                .Where(x => x.ModelName == modelName)
                .ToListAsync(cancellationToken);

            _dbSet.RemoveRange(embeddings);
        }
    }

    public class QueryLogRepo: GenericRepo<QueryLog>, IQueryLogRepo
    {
        public QueryLogRepo(AppDbContext db) : base(db)
        {
        }

        public async Task<List<QueryLog>> GetRecentAsync(int count,CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountSinceAsync(DateTime sinceUtc,CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .CountAsync(x => x.CreatedAtUtc >= sinceUtc, cancellationToken);
        }
    }
}
