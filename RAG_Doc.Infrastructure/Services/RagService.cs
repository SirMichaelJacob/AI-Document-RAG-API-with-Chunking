using FaissNet;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RAG_Doc.Application.DTOs;
using RAG_Doc.Domain.Entities;
using RAG_Doc.Domain.Interfaces;
using RAG_Doc.Domain.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RAG_Doc.Infrastructure.Services
{

    public class RagService : IRagService
    {
        private readonly IMemoryCache _cache;
        private readonly EmbeddingService _embeddingService;
        private readonly LMStudioService _llmClient;
        private readonly IUnitOfWork _unitOfWork;
        private readonly LlmSettings _settings;

        private FaissNet.Index _index;
        private List<Guid> _idMap = new();
        private readonly SemaphoreSlim _indexLock = new(1, 1);

        public RagService(
            IUnitOfWork unitOfWork,
            EmbeddingService embeddingService,
            LMStudioService llmClient,
            IOptions<LlmSettings> settings,
            IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _embeddingService = embeddingService;
            _llmClient = llmClient;
            _settings = settings.Value;
            _cache = cache;

            InitializeIndexAsync().GetAwaiter().GetResult();
        }

        // ============================================================
        // PUBLIC QUERY ENTRY
        // ============================================================

        public async Task<string> QueryAsync(string question, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question cannot be empty.");

            var stopwatch = Stopwatch.StartNew();

            string cacheKey = Utility.BuildDeterministicKey("answer", question);

            if (_cache.TryGetValue(cacheKey, out string cachedAnswer))
                return cachedAnswer;

            // 1️ Generate embedding
            float[] queryEmbedding = await GetOrCreateEmbeddingAsync(question, ct);

            // 2️ Search FAISS
            var relevantChunks = await SearchSimilarChunksAsync(queryEmbedding, _settings.TopK);

            if (!relevantChunks.Any())
                return "I couldn't find relevant information to answer your question.";

            // 3️ Build context safely
           string context = BuildContext(relevantChunks);
           //string context = string.Join("\n\n",relevantChunks.Select((doc, i) => $"Source {i + 1}: {doc}"));

            // 4️ Construct prompt

            string prompt = BuildPrompt(context, question);

            // 5️ Generate answer
            string answer = await _llmClient.GenerateAsync(prompt);
            answer = Utility.RemoveThinkBlocks(answer);

            stopwatch.Stop();

            // 6️ Log query
            await LogQueryAsync(question, answer, stopwatch.ElapsedMilliseconds);

            _cache.Set(cacheKey, answer, TimeSpan.FromMinutes(30));

            return answer;
        }

        // ============================================================
        // SEARCH
        // ============================================================

        private async Task<List<string>> SearchSimilarChunksAsync(float[] queryEmbedding, int topK)
        {
            if (_index == null || _idMap.Count == 0)
                return new List<string>();

            // 1️⃣ Normalize query embedding
            var normalizedQuery = Utility.Normalize(queryEmbedding);

            // 2️⃣ Search FAISS index
            var (distances, ids) = _index.Search(new[] { normalizedQuery }, topK);

            // 3️⃣ Filter invalid FAISS results (-1)
            var internalIds = ids[0].Where(id => id >= 0).Take(topK).ToList();
            if (!internalIds.Any())
                return new List<string>();

            // 4️⃣ Map FAISS internal IDs to chunk GUIDs
            var chunkIds = internalIds.Select(id => _idMap[(int)id]).ToList();

            // 5️⃣ Retrieve text from DB in same order
            return await _unitOfWork.Document.GetChunkContentsByIdsAsync(chunkIds);
        }

        // ============================================================
        // EMBEDDING
        // ============================================================

        private async Task<float[]> GetOrCreateEmbeddingAsync(string text, CancellationToken ct)
        {
            string key = Utility.BuildDeterministicKey("embed", text);

            if (_cache.TryGetValue(key, out float[] cached))
                return cached;

            var embedding = await _embeddingService.GenerateEmbeddingAsync(text);

            _cache.Set(key, embedding, TimeSpan.FromHours(6));

            return embedding;
        }

        // ============================================================
        // INDEX INITIALIZATION
        // ============================================================

        private async Task InitializeIndexAsync(CancellationToken ct = default)
        {
            
            // 1️⃣ Load all chunks with embeddings from DB
            var chunkEmbeddings =  await _unitOfWork.ChunkEmbedding.GetAllAsync(ct);
            if (!chunkEmbeddings.Any()) return;

            // 2️⃣ Map FAISS IDs to chunk IDs
            _idMap = chunkEmbeddings.Select(e => e.ChunkId).ToList();

            // 3️⃣ Convert byte[] embeddings to float[] and normalize
            var embeddingList = chunkEmbeddings
                .Select(e => Utility.Normalize(Utility.ConvertBytesToFloatArray(e.VectorData)))
                .ToArray();

            int dimension = embeddingList[0].Length;

            // 4️⃣ Create FAISS index for inner product (cosine similarity)
            _index = FaissNet.Index.CreateDefault(dimension, MetricType.METRIC_INNER_PRODUCT);

            // 5️⃣ Assign sequential IDs
            long[] faissIds = Enumerable.Range(0, embeddingList.Length).Select(i => (long)i).ToArray();

            // 6️⃣ Add vectors to FAISS
            _index.AddWithIds(embeddingList, faissIds);
        }

        public async Task RefreshIndexAsync()
        {
            await InitializeIndexAsync();
        }

        // ============================================================
        // PROMPT + CONTEXT
        // ============================================================

        private string BuildContext(List<string> chunks)
        {
            int maxPromptTokens = _settings.MaxTokens;
            int reservedForAnswer = 500;
            int maxContextTokens = maxPromptTokens - reservedForAnswer;
            int approxMaxWords = (int)(maxContextTokens / 1.3);

            int wordsPerChunk = approxMaxWords / chunks.Count;

            var truncated = chunks
                .Select(c => Utility.TruncateText(c, wordsPerChunk))
                .ToList();

            return string.Join("\n\n",
                truncated.Select((c, i) => $"Source {i + 1}:\n{c}"));
        }

        private static string BuildPrompt(string context, string question)
        {
            return $"""
            You are a precise AI assistant.

            Use ONLY the provided context to answer.
            If the answer is not contained in the context, say so clearly.

            Context:
            {context}

            Question:
            {question}

            Answer:
            """;
        }

        // ============================================================
        // LOGGING
        // ============================================================

        private async Task LogQueryAsync(string question, string answer, long latencyMs)
        {
            var log = new QueryLog
            {
                Id = Guid.NewGuid(),
                QueryText = question,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _unitOfWork.QueryLog.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }
    }

}
