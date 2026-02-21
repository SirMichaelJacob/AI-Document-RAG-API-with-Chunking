using System;
using System.Collections.Generic;
using System.Text;

namespace RAG_Doc.Application.Commands
{
    public sealed class QueryDocumentsCommand
    {
        public string Question { get; init; } = string.Empty;

        // Optional overrides
        public int? TopK { get; init; }
        public bool BypassCache { get; init; } = false;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Question))
                throw new ArgumentException("Question cannot be empty.");

            if (Question.Length < 3)
                throw new ArgumentException("Question is too short.");
        }
    }

    public sealed class CreateDocumentCommand
    {
        public string FileName { get; init; } = string.Empty;

        public string? ContentType { get; init; }

        public string Content { get; init; } = string.Empty;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(FileName))
                throw new ArgumentException("FileName is required.");

            if (string.IsNullOrWhiteSpace(Content))
                throw new ArgumentException("Content cannot be empty.");
        }
    }
}
