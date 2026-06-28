using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Qdrant.Client;
using Serilog;
using StackExchange.Redis;
using KnowledgeVault.API.BackgroundServices;
using KnowledgeVault.API.Middleware;
using KnowledgeVault.Application.Behaviors;
using KnowledgeVault.Application.Common;
using KnowledgeVault.Application.Services.Citation;
using KnowledgeVault.Application.Services.Ingestion;
using KnowledgeVault.Application.Services.Search;
using KnowledgeVault.Core.Interfaces.Repositories;
using KnowledgeVault.Core.Interfaces.Services;
using KnowledgeVault.Infrastructure.Cache;
using KnowledgeVault.Infrastructure.Data;
using KnowledgeVault.Infrastructure.MessageQueue;
using KnowledgeVault.Infrastructure.Repositories;
using KnowledgeVault.Infrastructure.Search;
using KnowledgeVault.Infrastructure.VectorStore;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ─── Database ──────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

// ─── Redis ─────────────────────────────────────────────────────
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379"));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// ─── Qdrant ────────────────────────────────────────────────────
builder.Services.AddSingleton(_ =>
{
    var host = builder.Configuration["Qdrant:Host"] ?? "localhost";
    var port = int.Parse(builder.Configuration["Qdrant:Port"] ?? "6334");
    return new QdrantClient(host, port);
});
builder.Services.AddScoped<IVectorStoreService, QdrantVectorStoreService>();

// ─── RabbitMQ ──────────────────────────────────────────────────
builder.Services.AddSingleton<IMessageQueueService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMqService>>();
    return RabbitMqService.CreateAsync(builder.Configuration, logger).GetAwaiter().GetResult();
});

// ─── HTTP Clients ──────────────────────────────────────────────
builder.Services.AddHttpClient("AIEngine", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AIEngine:BaseUrl"] ?? "http://localhost:8000");
    client.Timeout = TimeSpan.FromMinutes(5);
});

// ─── MediatR + Validation ──────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<TenantContext>());
builder.Services.AddValidatorsFromAssemblyContaining<TenantContext>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// ─── Repositories ──────────────────────────────────────────────
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IIngestionJobRepository, IngestionJobRepository>();

// ─── Application Services ──────────────────────────────────────
builder.Services.AddScoped<ISearchService, HybridSearchService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<ICitationService, CitationVerificationService>();
builder.Services.AddScoped<IIngestionService, DocumentIngestionService>();
builder.Services.AddScoped<IChunkingService, ChunkingService>();
builder.Services.AddScoped<TenantContext>();

// ─── Background Services ──────────────────────────────────────
builder.Services.AddHostedService<IngestionWorkerService>();
builder.Services.AddHostedService<FreshnessMonitorService>();

// ─── API ───────────────────────────────────────────────────────
builder.Services.AddResponseCompression();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "KnowledgeVault API", Version = "v1",
        Description = "Production-grade RAG system with hybrid search, citation verification, and multi-tenant support" });
});

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ─── Middleware Pipeline ───────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseResponseCompression();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "api", version = "1.0" }));

// ─── Ensure Qdrant collection exists ───────────────────────────
using (var scope = app.Services.CreateScope())
{
    var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStoreService>();
    await vectorStore.EnsureCollectionAsync();
}

app.Run();
