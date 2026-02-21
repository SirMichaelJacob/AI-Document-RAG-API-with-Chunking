using Microsoft.EntityFrameworkCore;
using RAG_Doc.Domain.Interfaces;
using RAG_Doc.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace RAG_Doc.Infrastructure.Repositories
{
    public class UnitOfWork: IUnitOfWork
    {
        private readonly AppDbContext _db;
        public IDocumentRepo Document { get; }
        public IDocumentChunkRepo DocumentChunk { get; }
        public IChunkEmbeddingRepo ChunkEmbedding { get; }
        public IQueryLogRepo QueryLog { get; }
        public UnitOfWork(AppDbContext db)
        {
            _db = db;
            Document = new DocumentRepo(db);
            DocumentChunk = new DocumentChunkRepo(db);
            ChunkEmbedding = new ChunkEmbeddingRepo(db);
            QueryLog = new QueryLogRepo(db);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
        public async Task CommitAsync(CancellationToken ct = default)
        {
            await SaveChangesInternalAsync(ct);
        }

        

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            //await _db.SaveChangesAsync(ct);
            await SaveChangesInternalAsync(ct);
        }

        private async Task SaveChangesInternalAsync(CancellationToken ct = default)
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(ct);

                try
                {
                    await _db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync(ct);

                    // You can inspect which entities were affected
                    var entries = ex.Entries;

                    foreach (var entry in entries)
                    {
                        //  Refresh entity values from the database
                        await entry.ReloadAsync(ct);//Optional
                    }

                    throw new DbUpdateConcurrencyException(
                        "A concurrency conflict occurred. The data may have been modified or deleted since it was loaded.",
                        ex
                    );
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    throw new Exception("An error occurred while saving changes.", ex);
                }
            });
        }
    }
}
