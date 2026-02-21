using System;
using System.Collections.Generic;
using System.Text;

namespace RAG_Doc.Application.DTOs
{

    public class LlmSettings
    {
        public string EmbeddingModel { get; set; }
        public string LLMModel { get; set; }
        public string EndPointUrl { get; set; } = "http://localhost:1234";
        public double Temperature { get; set; }
        public int TopK { get; set; } = 3;
        public int MaxTokens { get; set; }
        public int TimeoutSeconds { get; set; }
        public int MaxRetries { get; set; }
    }

    public sealed class EmbeddingResponse
    {
        public List<EmbeddingData> Data { get; set; } = new();
    }

    public sealed class EmbeddingData
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }

    public sealed class ChatCompletionResponse
    {
        public List<Choice> Choices { get; set; } = new();
    }

    public sealed class Choice
    {
        public Message Message { get; set; } = new();
    }

    public sealed class Message
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
