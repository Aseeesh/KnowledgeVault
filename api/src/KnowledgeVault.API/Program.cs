using System.Text;
using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Qdrant.Client;
using Serilog;
using StackExchange.Redis;
using KnowledgeVault.API.BackgroundServices;
using KnowledgeVault.API.Filters;
using KnowledgeVault.API.Middleware;
using KnowledgeVault.Application.Behaviors;
using KnowledgeVault.Application.Common;
using KnowledgeVault.Application.Services.Citation;
using KnowledgeVault.Application.Services.Ingestion;
using KnowledgeVault.Application.Services.Search;
using KnowledgeVault.Core.Interfaces.Repositories;
using KnowledgeVault.Core.Interfaces.Services;
using KnowledgeVault.Infrastructure.Cache;
using KnowledgeVault.Infrastructure.Configuration;
using KnowledgeVault.Infrastructure.Data;
using KnowledgeVault.Infrastructure.MessageQueue;
using KnowledgeVault.Infrastructure.Repositories;
using KnowledgeVault.Infrastructure.Search;
using KnowledgeVault.Infrastructure.VectorStore;
using Microsoft.OpenApi.Models;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ─── Set port to 5001 ──────────────────────────────────────────
builder.WebHost.UseUrls("http://localhost:5001");

// ─── Load environment variables ──────────────────────────────
var env = builder.Environment;
if (env.IsDevelopment())
{
    // DotNetEnv.Env.Load();  // Uncomment if you have DotNetEnv installed
}

// ─── Database ──────────────────────────────────────────────────
var postgresConnectionString = AppConfig.GetConnectionString("PostgreSQL");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnectionString));

// ─── Redis ─────────────────────────────────────────────────────
var redisConnectionString = AppConfig.GetConnectionString("Redis");
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    Console.WriteLine($"Connecting to Redis: {redisConnectionString.Replace("password=", "password=***")}");
    return ConnectionMultiplexer.Connect(redisConnectionString);
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// ─── Qdrant ────────────────────────────────────────────────────
var qdrantHost = AppConfig.GetQdrantHost();
var qdrantPort = AppConfig.GetQdrantGrpcPort();
builder.Services.AddSingleton(_ =>
{
    Console.WriteLine($"Connecting to Qdrant: {qdrantHost}:{qdrantPort}");
    return new QdrantClient(qdrantHost, qdrantPort);
});
builder.Services.AddScoped<IVectorStoreService, QdrantVectorStoreService>();

// ─── RabbitMQ ──────────────────────────────────────────────────
builder.Services.AddSingleton<IMessageQueueService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMqService>>();
    var config = AppConfig.GetRabbitMqConfig();
    Console.WriteLine($"Connecting to RabbitMQ: {config.Host}:{config.Port}");
    return RabbitMqService.CreateAsync(config, logger).GetAwaiter().GetResult();
});

// ─── HTTP Clients ──────────────────────────────────────────────
var aiEngineUrl = AppConfig.GetAIEngineUrl();
builder.Services.AddHttpClient("AIEngine", client =>
{
    client.BaseAddress = new Uri(aiEngineUrl);
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

// ✅ Swagger with custom schema for file upload
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "KnowledgeVault API",
        Version = "v1",
        Description = "Production-grade RAG system with hybrid search, citation verification, and multi-tenant support"
    });

    // ✅ This maps IFormFile to binary format for Swagger
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });

    // Add security definition
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-Tenant-Id",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Tenant ID for multi-tenant isolation"
    });
});

// ─── CORS ───────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://127.0.0.1:5173"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ─── Debug Middleware ───────────────────────────────────────────
app.Use(async (context, next) =>
{
    Console.WriteLine($"📝 {context.Request.Method} {context.Request.Path}");
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 ERROR: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        throw;
    }
});

// ─── Middleware Pipeline ───────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseResponseCompression();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "KnowledgeVault API V1");
    c.RoutePrefix = "swagger";
});
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

// Create default tenant if none exists
using (var scope = app.Services.CreateScope())
{
    var tenantRepo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
    var tenants = await tenantRepo.GetAllAsync();
    if (!tenants.Any())
    {
        var defaultTenant = new KnowledgeVault.Core.Entities.Tenant
        {
            Id = Guid.Parse("a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"),
            Name = "Default Tenant",
            Slug = "default",
            ApiKey = "kv_test_key_2024",
            IsActive = true
        };
        await tenantRepo.CreateAsync(defaultTenant);
        Console.WriteLine("✅ Created default tenant");
    }
}

app.Run();