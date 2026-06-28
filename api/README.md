# KnowledgeVault API

REST API service for the KnowledgeVault platform. Handles document management, search orchestration, and ingestion pipeline coordination.

## Tech Stack

- **Runtime**: .NET 9 / C#
- **ORM**: Entity Framework Core 9 + PostgreSQL
- **Architecture**: Clean Architecture (Core → Infrastructure → API)

## Project Structure

```
api/
├── src/
│   ├── KnowledgeVault.Core/          # Domain entities, interfaces, DTOs
│   ├── KnowledgeVault.Infrastructure/ # EF Core, repositories, external clients
│   ├── KnowledgeVault.Ingestion/     # Async document processing workers
│   └── KnowledgeVault.API/           # Controllers, middleware, DI config
├── tests/
└── KnowledgeVault.sln
```

## Running Locally

```bash
# Requires PostgreSQL running on localhost:5432
dotnet restore
dotnet run --project src/KnowledgeVault.API
```

API available at `http://localhost:5000/swagger`

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Service health check |
| GET | `/api/tenants/{id}/documents` | List tenant documents |
| POST | `/api/tenants/{id}/documents` | Create document |
| GET | `/api/tenants/{id}/documents/{docId}` | Get document detail |
| DELETE | `/api/tenants/{id}/documents/{docId}` | Delete document |
| POST | `/api/tenants/{id}/search` | Proxy search to AI engine |
