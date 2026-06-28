using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using KnowledgeVault.Core.Entities;

namespace KnowledgeVault.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<UserFeedback> UserFeedbacks => Set<UserFeedback>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        var jsonOptions = new JsonSerializerOptions();

        m.Entity<Tenant>(e =>
        {
            e.ToTable("tenants");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.Name).HasColumnName("name");
            e.Property(t => t.Slug).HasColumnName("slug");
            e.Property(t => t.ApiKey).HasColumnName("api_key");
            e.Property(t => t.RateLimitPerMinute).HasColumnName("rate_limit_per_minute").HasDefaultValue(60);
            e.Property(t => t.DenseSearchWeight).HasColumnName("dense_search_weight").HasDefaultValue(0.5);
            e.Property(t => t.SparseSearchWeight).HasColumnName("sparse_search_weight").HasDefaultValue(0.5);
            e.Property(t => t.Settings).HasColumnName("settings").HasColumnType("jsonb")
                .HasConversion(v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>());
            e.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(t => t.CreatedAt).HasColumnName("created_at");
            e.Property(t => t.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(t => t.Slug).IsUnique();
            e.HasIndex(t => t.ApiKey).IsUnique();
        });

        m.Entity<Document>(e =>
        {
            e.ToTable("documents");
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).HasColumnName("id");
            e.Property(d => d.TenantId).HasColumnName("tenant_id");
            e.Property(d => d.Title).HasColumnName("title");
            e.Property(d => d.SourceUrl).HasColumnName("source_url");
            e.Property(d => d.ContentType).HasColumnName("content_type");
            e.Property(d => d.Status).HasColumnName("status").HasConversion<string>();
            e.Property(d => d.Checksum).HasColumnName("checksum");
            e.Property(d => d.SizeBytes).HasColumnName("size_bytes");
            e.Property(d => d.Version).HasColumnName("version").HasDefaultValue(1);
            e.Property(d => d.ErrorMessage).HasColumnName("error_message");
            e.Property(d => d.ChunkCount).HasColumnName("chunk_count").HasDefaultValue(0);
            e.Property(d => d.Metadata).HasColumnName("metadata").HasColumnType("jsonb")
                .HasConversion(v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>());
            e.Property(d => d.CreatedAt).HasColumnName("created_at");
            e.Property(d => d.UpdatedAt).HasColumnName("updated_at");
            e.Property(d => d.IndexedAt).HasColumnName("indexed_at");
            e.Property(d => d.FreshnessCheckedAt).HasColumnName("freshness_checked_at");
            e.HasIndex(d => d.TenantId);
            e.HasIndex(d => d.Status);
            e.HasOne(d => d.Tenant).WithMany(t => t.Documents).HasForeignKey(d => d.TenantId);
        });

        m.Entity<DocumentChunk>(e =>
        {
            e.ToTable("document_chunks");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.DocumentId).HasColumnName("document_id");
            e.Property(c => c.TenantId).HasColumnName("tenant_id");
            e.Property(c => c.ChunkIndex).HasColumnName("chunk_index");
            e.Property(c => c.Content).HasColumnName("content");
            e.Property(c => c.TokenCount).HasColumnName("token_count");
            e.Property(c => c.VectorId).HasColumnName("vector_id");
            e.Property(c => c.SectionHeading).HasColumnName("section_heading");
            e.Property(c => c.Metadata).HasColumnName("metadata").HasColumnType("jsonb")
                .HasConversion(v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>());
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
            e.HasIndex(c => c.DocumentId);
            e.HasIndex(c => c.TenantId);
            e.HasOne(c => c.Document).WithMany(d => d.Chunks).HasForeignKey(c => c.DocumentId);
        });

        m.Entity<ChatSession>(e =>
        {
            e.ToTable("chat_sessions");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.TenantId).HasColumnName("tenant_id");
            e.Property(s => s.UserId).HasColumnName("user_id");
            e.Property(s => s.Title).HasColumnName("title");
            e.Property(s => s.CreatedAt).HasColumnName("created_at");
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(s => s.Tenant).WithMany(t => t.ChatSessions).HasForeignKey(s => s.TenantId);
        });

        m.Entity<ChatMessage>(e =>
        {
            e.ToTable("chat_messages");
            e.HasKey(cm => cm.Id);
            e.Property(cm => cm.Id).HasColumnName("id");
            e.Property(cm => cm.SessionId).HasColumnName("session_id");
            e.Property(cm => cm.Role).HasColumnName("role");
            e.Property(cm => cm.Content).HasColumnName("content");
            e.Property(cm => cm.Citations).HasColumnName("citations").HasColumnType("jsonb")
                .HasConversion(v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<List<CitationReference>>(v, jsonOptions) ?? new List<CitationReference>());
            e.Property(cm => cm.ConfidenceScore).HasColumnName("confidence_score");
            e.Property(cm => cm.TokensUsed).HasColumnName("tokens_used");
            e.Property(cm => cm.CreatedAt).HasColumnName("created_at");
            e.HasOne(cm => cm.Session).WithMany(s => s.Messages).HasForeignKey(cm => cm.SessionId);
        });

        m.Entity<UserFeedback>(e =>
        {
            e.ToTable("user_feedback");
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).HasColumnName("id");
            e.Property(f => f.SessionId).HasColumnName("session_id");
            e.Property(f => f.MessageId).HasColumnName("message_id");
            e.Property(f => f.Rating).HasColumnName("rating");
            e.Property(f => f.Comment).HasColumnName("comment");
            e.Property(f => f.CreatedAt).HasColumnName("created_at");
            e.HasOne(f => f.Message).WithOne(m => m.Feedback).HasForeignKey<UserFeedback>(f => f.MessageId);
        });

        m.Entity<IngestionJob>(e =>
        {
            e.ToTable("ingestion_jobs");
            e.HasKey(j => j.Id);
            e.Property(j => j.Id).HasColumnName("id");
            e.Property(j => j.TenantId).HasColumnName("tenant_id");
            e.Property(j => j.DocumentId).HasColumnName("document_id");
            e.Property(j => j.JobType).HasColumnName("job_type").HasConversion<string>();
            e.Property(j => j.Status).HasColumnName("status").HasConversion<string>();
            e.Property(j => j.ErrorMessage).HasColumnName("error_message");
            e.Property(j => j.RetryCount).HasColumnName("retry_count");
            e.Property(j => j.MaxRetries).HasColumnName("max_retries").HasDefaultValue(3);
            e.Property(j => j.StartedAt).HasColumnName("started_at");
            e.Property(j => j.CompletedAt).HasColumnName("completed_at");
            e.Property(j => j.CreatedAt).HasColumnName("created_at");
            e.HasIndex(j => j.Status);
            e.HasOne(j => j.Document).WithMany().HasForeignKey(j => j.DocumentId);
        });
    }
}
