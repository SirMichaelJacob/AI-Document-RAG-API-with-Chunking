using Microsoft.EntityFrameworkCore;
using RAG_Doc.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RAG_Doc.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }
        public DbSet<ChunkEmbedding> ChunkEmbeddings { get; set; }
        public DbSet<QueryLog> QueryLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureDocument(modelBuilder);
            ConfigureDocumentChunk(modelBuilder);
            ConfigureChunkEmbedding(modelBuilder);
            ConfigureQueryLog(modelBuilder);
        }

        private static void ConfigureDocument(ModelBuilder builder)
        {
            builder.Entity<Document>(entity =>
            {
                entity.ToTable("Documents");

                entity.HasKey(d => d.Id);

                entity.Property(d => d.FileName)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(d => d.FilePath)
                      .HasMaxLength(500);

                entity.Property(d => d.ContentType)
                      .HasMaxLength(100);

                entity.Property(d => d.Status)
                      .IsRequired();

                entity.HasIndex(d => d.Status);
                entity.HasIndex(d => d.UploadedAtUtc);
            });
        }

        private static void ConfigureDocumentChunk(ModelBuilder builder)
        {
            builder.Entity<DocumentChunk>(entity =>
            {
                entity.ToTable("DocumentChunks");

                entity.HasKey(c => c.Id);

                entity.Property(c => c.Text)
                      .IsRequired()
                      .HasColumnType("nvarchar(max)");

                entity.Property(c => c.ContentHash)
                      .HasMaxLength(64);

                entity.HasIndex(c => c.DocumentId);
                entity.HasIndex(c => new { c.DocumentId, c.Sequence })
                      .IsUnique();

                entity.HasOne(c => c.Document)
                      .WithMany(d => d.Chunks)
                      .HasForeignKey(c => c.DocumentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureChunkEmbedding(ModelBuilder builder)
        {
            builder.Entity<ChunkEmbedding>(entity =>
            {
                entity.ToTable("ChunkEmbeddings");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.ModelName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.VectorData)
                      .IsRequired()
                      .HasColumnType("varbinary(max)");

                entity.HasIndex(e => e.ChunkId);
                entity.HasIndex(e => e.ModelName);

                entity.HasOne(e => e.Chunk)
                      .WithMany()
                      .HasForeignKey(e => e.ChunkId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureQueryLog(ModelBuilder builder)
        {
            builder.Entity<QueryLog>(entity =>
            {
                entity.ToTable("QueryLogs");

                entity.HasKey(q => q.Id);

                entity.Property(q => q.QueryText)
                      .IsRequired()
                      .HasMaxLength(1000);

                entity.HasIndex(q => q.CreatedAtUtc);
            });
        }

    }
}
