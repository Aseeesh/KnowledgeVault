# ADR-001: Use C# .NET 9 for REST API

**Status:** Accepted
**Date:** 2026-06-27

## Context

We need a backend framework for the REST API that handles multi-tenant document management, search orchestration, chat sessions, and async ingestion. Requirements include strong typing, mature ORM, CQRS support, and production middleware.

## Decision

Use C# .NET 9 with Clean Architecture (Core → Application → Infrastructure → API).

## Rationale

- **MediatR** provides native CQRS — commands and queries are separate concerns with pipeline behaviors for validation and logging
- **Entity Framework Core 9** is a mature ORM with PostgreSQL support, migrations, and excellent LINQ-to-SQL translation
- **FluentValidation** integrates into the MediatR pipeline for declarative input validation
- **ASP.NET middleware** pipeline gives fine-grained control over security headers, tenant resolution, rate limiting, and exception handling
- **Serilog** provides structured logging with minimal configuration
- **Performance**: .NET 9 achieves 32ms P50 for document CRUD operations in benchmarks

## Alternatives Considered

- **Node.js/Express**: Faster to prototype but weaker typing and limited CQRS ecosystem
- **Go**: Excellent performance but lacks mature ORM and CQRS patterns
- **Python/Django**: Strong ORM but would duplicate the AI engine's language unnecessarily

## Consequences

- Requires .NET SDK for development (9.0+)
- Docker image is larger (~200MB runtime) vs Go (~20MB)
- Team needs C# expertise
