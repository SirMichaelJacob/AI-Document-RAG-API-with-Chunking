using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RAG_Doc.Application.DTOs;
using RAG_Doc.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace RAG_Doc.Infrastructure.Services
{

    public class EmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly LlmSettings _settings;
        private int? _embeddingDimension;
        private readonly string _endPointUrl;


        public EmbeddingService( HttpClient httpClient, IOptions<LlmSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _httpClient.BaseAddress = new Uri(_settings.EndPointUrl);
            _httpClient.Timeout = new TimeSpan(0, 0, _settings.TimeoutSeconds);
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text,CancellationToken cancellationToken = default)
        {
            var request = new
            {
                input = text,
                model = _settings.EmbeddingModel
            };

            string jsonContent = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            


            var response = await _httpClient.PostAsync("/v1/embeddings", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Embedding API failed ({response.StatusCode}): {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken);

            if (result?.Data == null || result.Data.Count == 0)
                throw new InvalidOperationException("Embedding API returned empty result.");

            var embedding = result.Data[0].Embedding;

            if (embedding.Length == 0)
                throw new InvalidOperationException("Embedding vector is empty.");

            // Validate dimension consistency
            if (_embeddingDimension == null)
            {
                _embeddingDimension = embedding.Length;
            }
            else if (_embeddingDimension != embedding.Length)
            {
                throw new InvalidOperationException(
                    $"Embedding dimension mismatch. Expected {_embeddingDimension}, got {embedding.Length}.");
            }

            return embedding;
        }
    }

}
