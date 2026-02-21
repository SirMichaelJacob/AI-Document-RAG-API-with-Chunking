using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RAG_Doc.Application.DTOs;
using RAG_Doc.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RAG_Doc.Infrastructure.Services
{

    public class LMStudioService : ILMStudioService
    {
        private readonly HttpClient _httpClient;
        private readonly LlmSettings _settings;
        private readonly string _endPointUrl;

        public LMStudioService(
            HttpClient httpClient,
            IOptions<LlmSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        public async Task<string> GenerateAsync(string prompt,int maxTokens = 500,CancellationToken cancellationToken = default)
        {
            var request = new
            {
                messages = new[]
                {
                new
                {
                    role = "system",
                    content = BuildSystemPrompt()
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
                model = _settings.LLMModel,
                temperature = _settings.Temperature,
                max_tokens = maxTokens,
                stream = false
            };

            using var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions",request,cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"LLM API failed ({response.StatusCode}): {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
                cancellationToken: cancellationToken);

            var answer = result?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(answer))
                throw new InvalidOperationException("LLM returned empty response.");

            return answer.Trim();
        }

        private static string BuildSystemPrompt()
        {
            return """
        You are a retrieval-augmented assistant.

        Use ONLY the information in the provided context documents.
        Do not use prior knowledge.
        Provide short, direct, factual answers.

        When answering:
        • Always mention the Title of the document used.
        • If multiple documents are used, list all titles.

        If the answer is not explicitly present in the context, respond exactly with:
        "The provided context does not contain enough information. Can I help you with something else?"
        """;
        }
    }

}
