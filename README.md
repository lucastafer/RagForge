# RagForge API

A compact Retrieval-Augmented Generation (RAG) API built with .NET, PostgreSQL, pgvector, and any provider that implements the OpenAI embeddings and chat-completions formats.

RagForge ingests text or Markdown files, splits them into overlapping chunks, creates embeddings, stores vectors in PostgreSQL, retrieves the most relevant evidence for a question, and produces a grounded answer with the exact source chunks used.

## Why this project

This repository demonstrates practical backend and AI integration skills without hiding the core RAG flow behind a framework. It uses async I/O, dependency injection, EF Core, vector similarity search, configuration validation, API error responses, automated tests, and CI while keeping the architecture intentionally small.

**Portfolio description:** Built a retrieval-augmented generation API in ASP.NET Core that ingests Markdown/text documents, generates configurable OpenAI-compatible embeddings, performs cosine similarity search with PostgreSQL/pgvector, and returns grounded LLM answers with auditable source chunks.

## Architecture

```text
HTTP request
    │
    ▼
RagForge.Api              Controllers, validation, configuration
    │
    ▼
RagForge.Application      DTOs, interfaces, use cases, chunking
    │
    ▼
RagForge.Domain           Entities and business invariants
    ▲
    │
RagForge.Infrastructure   EF Core/pgvector store, OpenAI-compatible HTTP client
    │
    ├── PostgreSQL + pgvector
    └── Embeddings and chat provider
```

The layout follows the same pattern as HookForge: `Api`, `Application`, `Domain`, and `Infrastructure`. API composes the application, Application orchestrates use cases, Domain owns entities and invariants, and Infrastructure implements external integrations. There is no MediatR, Redis, message queue, or background worker in the MVP.

## RAG flow

1. Upload a `.txt`, `.md`, or `.markdown` file.
2. Split the text into configurable overlapping chunks.
3. Send chunks to the configured embeddings endpoint in one batch.
4. Persist document metadata, chunks, and vectors through EF Core.
5. Embed a question and run exact cosine-distance search in pgvector.
6. Send the retrieved evidence to the chat model with grounding instructions.
7. Return the answer plus document IDs, chunk IDs, content, and similarity scores.

Exact search is a good fit for this small demo and supports embedding models with different dimensions. For a larger corpus, choose one embedding dimension, change the column to `vector(n)`, and add an HNSW cosine index.

## Stack

- .NET 10 and ASP.NET Core Web API
- Entity Framework Core with Npgsql
- PostgreSQL 17 with pgvector
- OpenAI-compatible `/embeddings` and `/chat/completions` endpoints
- xUnit and GitHub Actions

## Run locally

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), Docker, and an API key for an OpenAI-compatible provider.

```bash
cp .env.example .env
```

Edit `.env` and replace the sample PostgreSQL password and `OpenAI__ApiKey`. If using another provider, also update the base URL and model names. Keep the embedding model unchanged after ingesting documents; vectors from different models may have incompatible dimensions or semantics.

PostgreSQL is exposed on host port `5433` by default so it can coexist with another local PostgreSQL on `5432`. Change both `POSTGRES_PORT` and the connection-string port if needed.

Start PostgreSQL:

```bash
docker compose --env-file .env up -d
```

Load the local environment, restore dependencies, and start the API:

```bash
set -a
source .env
set +a
dotnet restore RagForge.sln
dotnet run --project src/RagForge.Api
```

The API listens on `http://localhost:5190`. On first startup, EF Core creates the pgvector extension and demo schema automatically.

In Development, the generated OpenAPI document is available at `http://localhost:5190/openapi/v1.json`. You can also execute the ready-made requests in `RagForge.http` from Rider, Visual Studio, or VS Code with a REST Client extension.

Alternatively, keep the API key in .NET user secrets instead of `.env`:

```bash
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key" --project src/RagForge.Api
```

## Endpoints

### Health

```http
GET /health
```

### Ingest a document

```http
POST /api/documents
Content-Type: multipart/form-data
```

```bash
curl -X POST http://localhost:5190/api/documents \
  -F "file=@samples/rag-overview.md;type=text/markdown"
```

Example response:

```json
{
  "documentId": "4a7cf45d-6641-4dc6-bab2-599fc9a1950d",
  "name": "rag-overview.md",
  "chunkCount": 2
}
```

### Ask a question

```http
POST /api/questions
Content-Type: application/json
```

```bash
curl -X POST http://localhost:5190/api/questions \
  -H "Content-Type: application/json" \
  -d '{"question":"What are the main stages of the RAG pipeline?","topK":5}'
```

The response contains `answer` and `sources`. Each source includes the document and chunk identifiers, document name, chunk index, retrieved content, and cosine similarity score.

## How to verify the RAG flow

1. Start PostgreSQL and the API.
2. Call the document endpoint with `samples/rag-overview.md`.
3. Confirm the response has a non-zero `documentId` and `chunkCount`.
4. Ask `What are the main stages of the RAG pipeline?`.
5. Confirm the answer references `[Source 1]` (or another returned source number).
6. Confirm `sources` contains text from `rag-overview.md` and a similarity score.

That validates the complete path: file upload → chunking → embeddings → PostgreSQL/pgvector → vector retrieval → grounded chat answer → source response.

## Does it require an OpenAI API key?

With the default configuration, yes. Both ingestion and questions call OpenAI-compatible APIs: ingestion needs an embedding model, and questions need the same embedding model plus a chat model.

The application is provider-agnostic at the HTTP boundary. You can point `OpenAI__BaseUrl`, `OpenAI__EmbeddingModel`, and `OpenAI__ChatModel` to another service that implements compatible `/embeddings` and `/chat/completions` endpoints. For a local provider that does not validate bearer credentials, use any non-empty development value for `OpenAI__ApiKey`.

An OpenAI API key belongs to the API Platform and must never be committed. A ChatGPT subscription does not automatically include API usage; API billing is managed separately.

## Configuration

ASP.NET Core environment variables use double underscores for nested keys.

| Setting | Purpose | Default |
| --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection | Required |
| `OpenAI__BaseUrl` | OpenAI-compatible API root | `https://api.openai.com/v1/` |
| `OpenAI__ApiKey` | Provider credential | Required |
| `OpenAI__EmbeddingModel` | Embedding model | `text-embedding-3-small` |
| `OpenAI__ChatModel` | Chat model | `gpt-4o-mini` |
| `Rag__Chunking__ChunkSize` | Maximum chunk characters | `1000` |
| `Rag__Chunking__Overlap` | Overlap characters | `150` |
| `DocumentUpload__MaxBytes` | Maximum upload size | `5242880` |

The client intentionally targets the widely supported OpenAI request/response shape rather than a provider-specific SDK. A compatible local service can be used by changing only configuration; if it ignores authentication, set the API key to any non-empty local value.

## Tests

```bash
dotnet test RagForge.sln
```

The tests cover chunk sizing and validation plus the complete ingestion and question-answering orchestration using deterministic test doubles. Database and live provider integration tests are natural next additions when the project grows.

## Secret safety

- `.env`, local appsettings overrides, private keys, and certificate files are ignored by Git.
- Tracked `appsettings.json` contains no credential.
- `.env.example` contains placeholders only.
- .NET user secrets are supported for local development.
- The CI workflow receives no provider secret and only builds/tests code.

Before pushing, run `git diff --cached` and confirm that no real token appears in the staged changes.

## Reset the demo database

This deletes only the Docker volume created by this Compose project:

```bash
docker compose down -v
docker compose --env-file .env up -d
```

## Push to GitHub

With the [GitHub CLI](https://cli.github.com/) authenticated:

```bash
git init
git add .
git commit -m "feat: add RagForge RAG API"
git branch -M main
gh repo create RagForge --public --source=. --remote=origin --push
```

Without GitHub CLI, create an empty `RagForge` repository on GitHub, then run:

```bash
git init
git add .
git commit -m "feat: add RagForge RAG API"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/RagForge.git
git push -u origin main
```

## Possible next steps

- Add PDF extraction behind an `IDocumentTextExtractor` implementation.
- Add HNSW indexing after locking the embedding dimension.
- Add document listing/deletion and re-indexing.
- Add Testcontainers integration tests for PostgreSQL.
- Stream chat responses for a richer client experience.

## References

- [pgvector-dotnet](https://github.com/pgvector/pgvector-dotnet)
- [Npgsql EF Core provider](https://www.npgsql.org/efcore/)
- [ASP.NET Core configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)
