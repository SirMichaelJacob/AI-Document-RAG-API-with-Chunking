using Microsoft.Extensions.Options;
using RAG_Doc.Application.Commands;
using RAG_Doc.Application.DTOs;
using RAG_Doc.Application.Utilities;
using RAG_Doc.Domain.Entities;
using RAG_Doc.Domain.Interfaces;
using RAG_Doc.Domain.Utilities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace RAG_Doc.Application.Handlers
{

    public class DocumentHandler
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmbeddingService _embeddingService;
        private readonly IRagService _ragService;
        private readonly LlmSettings _settings;

        private const int ChunkSize = 500;
        private const int ChunkOverlap = 50;

        public DocumentHandler(
            IUnitOfWork unitOfWork,
            IEmbeddingService embeddingService,
            IRagService ragService,
            IOptions<LlmSettings> settings)
        {
            _unitOfWork = unitOfWork;
            _embeddingService = embeddingService;
            _ragService = ragService;
            _settings = settings.Value;
        }

        // ============================================================
        // INGEST DOCUMENT
        // ============================================================

        public async Task<CustomResult> Handle(CreateDocumentCommand command,CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.Content))
                return Fail("Content cannot be empty.");

            // 1️ Create Document
            var document = new Document
            {
                FileName = command.FileName ?? $"doc-{Guid.NewGuid()}",
                ContentType = command.ContentType,
                SizeInBytes = Encoding.UTF8.GetByteCount(command.Content),
                Status = DocumentStatus.Uploaded
            };

            await _unitOfWork.Document.AddAsync(document);

            // 2 Chunking
            var chunks = CreateChunks(command.Content, document.Id);

            foreach (var chunk in chunks)
                document.Chunks.Add(chunk);

            document.Status = DocumentStatus.Chunked;

            // 3️ Embedding per chunk
            foreach (var chunk in document.Chunks)
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Text);

                var chunkEmbedding = new ChunkEmbedding
                {
                    ChunkId = chunk.Id,
                    ModelName = _settings.EmbeddingModel,
                    VectorDimension = embedding.Length,
                    VectorData = Utility.ConvertFloatArrayToBytes(embedding)
                };

                await _unitOfWork.ChunkEmbedding.AddAsync(chunkEmbedding);
            }

            document.Status = DocumentStatus.Embedded;
            document.ProcessedAtUtc = DateTime.UtcNow;

            //await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 4️ Refresh FAISS index
            await _ragService.RefreshIndexAsync();

            document.Status = DocumentStatus.Completed;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Success("Document processed successfully.");
        }

        // ============================================================
        // QUERY
        // ============================================================

        public async Task<string> Handle(QueryDocumentsCommand command,CancellationToken cancellationToken = default)
        {
            return await _ragService.QueryAsync(command.Question, cancellationToken);
        }

        // ============================================================
        // CHUNKING
        // ============================================================

        private List<DocumentChunk> CreateChunks(string content, Guid documentId)
        {
            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var chunks = new List<DocumentChunk>();
            int index = 0;
            int sequence = 0;

            while (index < words.Length)
            {
                var chunkWords = words
                    .Skip(index)
                    .Take(ChunkSize)
                    .ToArray();

                var text = string.Join(" ", chunkWords);

                chunks.Add(new DocumentChunk
                {
                    DocumentId = documentId,
                    Text = text,
                    Sequence = sequence++,
                    TokenCount = chunkWords.Length,
                    ContentHash = Utility.CreateContentHash(text),
                    CreatedAtUtc = DateTime.UtcNow
                });

                index += ChunkSize - ChunkOverlap;
            }

            return chunks;
        }

        private static CustomResult Success(string message)
            => new CustomResult
            {
                IsSuccess = true,
                Message = message,
                HttpResponse = new HttpResponseMessage(HttpStatusCode.Created)
            };

        private static CustomResult Fail(string message)
            => new CustomResult
            {
                IsSuccess = false,
                Message = message,
                HttpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            };
    }

}
