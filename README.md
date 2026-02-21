# 📄 AI Document RAG API

**ASP.NET Core + Wolverine + LM Studio + Vector Embeddings**

A Retrieval-Augmented Generation (RAG) API that allows users to:

* Upload `.txt`, `.pdf`, and `.docx` documents
* Automatically chunk and embed document content
* Store vector embeddings
* Ask questions against uploaded documents
* Retrieve contextually relevant answers using a local LLM (LM Studio)

---

# 🏗 Architecture Overview

```
Client (Swagger / Frontend)
        │
        ▼
ASP.NET Core Web API
        │
        ▼
Wolverine Message Bus
        │
        ├── Document Processing Handler
        │        ├── Extract Text
        │        ├── Chunk Text
        │        ├── Generate Embeddings
        │        └── Store Vectors
        │
        └── Query Handler
                 ├── Embed Question
                 ├── Vector Similarity Search
                 ├── Build Prompt
                 └── Call LLM (LM Studio)
```

---

# 🚀 Features

* ✔ Document upload (`.txt`, `.pdf`, `.docx`)
* ✔ Automatic chunking strategy
* ✔ Vector embedding via local embedding model
* ✔ Top-K semantic search
* ✔ LLM answer generation
* ✔ Fully asynchronous processing using Wolverine
* ✔ Configurable via `appsettings.json`
* ✔ Swagger support

---

# 🧠 What is RAG?

Retrieval-Augmented Generation (RAG) improves LLM answers by:

1. Retrieving relevant document chunks using embeddings
2. Injecting them into the LLM prompt
3. Generating grounded answers

Instead of relying on model memory, the system uses **your documents as external knowledge**.

---

# 📦 Tech Stack

| Component        | Technology                     |
| ---------------- | ------------------------------ |
| Backend          | ASP.NET Core                   |
| Messaging        | Wolverine                      |
| LLM Runtime      | LM Studio                      |
| Embeddings       | Nomic Embed                    |
| Document Parsing | PdfPig + OpenXML               |
| Vector Storage   | Database (Your implementation) |
| API Testing      | Swagger                        |

---

# 📁 Project Structure

```
/Controllers
    DocumentController.cs
    QueryController.cs

/Services
    LMStudioService.cs
    EmbeddingService.cs
    ChunkingService.cs
    VectorSearchService.cs

/Handlers
    ProcessDocumentHandler.cs
    AskQuestionHandler.cs

/Models
    DocumentChunk.cs
    EmbeddingResponse.cs

Program.cs
appsettings.json
```

---

# ⚙️ Configuration

### `appsettings.json`

```json
"LLMS": {
  "EndPointUrl": "http://localhost:1234",
  "LLMModel": "qwen3-14b-claude-4.5-opus-high-reasoning-distill",
  "EmbeddingModel": "text-embedding-nomic-embed-text-v1.5-embedding",
  "MaxRetries": 3,
  "TimeoutSeconds": 1200,
  "Temperature": 0.5,
  "MaxTokens": 2048,
  "TopK": 3
}
```

---

# 🖥 Running LM Studio

1. Install LM Studio
2. Download:

   * LLM Model (e.g. Qwen3)
   * Embedding Model (e.g. nomic embed)
3. Start Local Server
4. Ensure server is running at:

```
http://localhost:1234
```

---

# 🔌 HttpClient Setup

```csharp
builder.Services.AddHttpClient<LMStudioService>(client =>
{
    var endpointUrl = builder.Configuration["LLMS:EndPointUrl"] ?? "http://localhost:1234";
    client.BaseAddress = new Uri(endpointUrl);

    var timeoutSeconds = builder.Configuration.GetValue<int>("LLMS:TimeoutSeconds", 600);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});
```

---

# 📤 Document Upload Flow

### Endpoint

```
POST /api/documents/upload
```

### Supports

* `.txt`
* `.pdf`
* `.docx`

### Processing Steps

1. Validate file
2. Extract text
3. Chunk text (e.g., 500–1000 tokens per chunk)
4. Generate embedding per chunk
5. Store chunk + vector in database

---

# ❓ Ask Question Flow

### Endpoint

```
POST /api/query/ask
```

### Steps

1. Embed question
2. Retrieve TopK similar chunks
3. Build prompt:

```
Use the context below to answer:

[chunk1]
[chunk2]
[chunk3]

Question:
...
```

4. Send to LLM
5. Return generated answer

---

# 🔍 Why Chunking?

### ❌ Embedding entire document:

* Vector too dense
* Loses granularity
* Poor retrieval precision
* Context window overflow

### ✅ Chunking advantages:

* Higher semantic precision
* Smaller embeddings
* Faster similarity search
* Better contextual grounding
* Scales to large documents

Typical chunk size:

```
500–1000 tokens
```

---

# 🧪 Testing with Swagger

1. Run the project
2. Navigate to:

```
https://localhost:{port}/swagger
```

3. Upload document
4. Ask question

---

# 🛠 Common Issues & Fixes

## 1️⃣ BaseAddress is null

Ensure HttpClient is registered:

```csharp
builder.Services.AddHttpClient<EmbeddingService>();
```

---

## 2️⃣ Project crashes on upload

Common causes:

* Large file
* Missing OpenXML package
* LM Studio not running
* Timeout too low

Increase timeout:

```json
"TimeoutSeconds": 1200
```

---

## 3️⃣ Slow Response (>5 mins)

Causes:

* Large context
* Too many chunks
* Heavy model (14B+)
* CPU inference

Solutions:

* Reduce TopK to 2–3
* Reduce MaxTokens
* Use smaller model
* Enable streaming

---

# 📊 Performance Optimization Tips

* Cache embeddings
* Persist vectors
* Pre-chunk at ingestion
* Use cosine similarity index
* Avoid embedding same text twice
* Truncate context before LLM call

---

# 🔐 Security Considerations

* Validate file size
* Restrict file types
* Sanitize text input
* Add authentication
* Limit max upload size

---

# 📈 Future Improvements

* Persistent vector DB (Qdrant / PgVector)
* Background document processing queue
* Streaming LLM responses
* Conversation memory
* Metadata filtering
* Hybrid search (keyword + vector)

---

# 📜 License

MIT License

---

# 👨‍💻 Author

Michael Jacob
Senior Software Engineer
AI + RAG Systems

---
