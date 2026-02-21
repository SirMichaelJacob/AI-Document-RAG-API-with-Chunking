using System;
using System.Collections.Generic;
using System.Text;

namespace RAG_Doc.Domain.Interfaces
{
    public interface ILMStudioService
    {
        Task<string> GenerateAsync(string prompt, int maxTokens = 500, CancellationToken cancellationToken = default);
    }
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    }
    public interface IRagService
    {
        Task<string> QueryAsync(string question, CancellationToken ct = default);
        Task RefreshIndexAsync();
    }
}
