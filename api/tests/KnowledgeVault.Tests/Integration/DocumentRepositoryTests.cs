using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using KnowledgeVault.Core.Entities;
using KnowledgeVault.Core.Enums;
using KnowledgeVault.Infrastructure.Data;
using KnowledgeVault.Infrastructure.Repositories;

namespace KnowledgeVault.Tests.Integration;

public class DocumentRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly DocumentRepository _repo;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DocumentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repo = new DocumentRepository(_db);

        _db.Tenants.Add(new Tenant { Id = _tenantId, Name = "Test", Slug = "test" });
        _db.SaveChanges();
    }

    [Fact]
    public async Task Create_ReturnsDocumentWithId()
    {
        var doc = new Document { TenantId = _tenantId, Title = "Test Doc", ContentType = "text/plain" };

        var result = await _repo.CreateAsync(doc);

        result.Id.Should().NotBeEmpty();
        result.Status.Should().Be(DocumentStatus.Pending);
    }

    [Fact]
    public async Task GetById_ExistingDoc_ReturnsDoc()
    {
        var doc = await _repo.CreateAsync(new Document { TenantId = _tenantId, Title = "Test", ContentType = "text/plain" });

        var result = await _repo.GetByIdAsync(_tenantId, doc.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test");
    }

    [Fact]
    public async Task GetById_WrongTenant_ReturnsNull()
    {
        var doc = await _repo.CreateAsync(new Document { TenantId = _tenantId, Title = "Test", ContentType = "text/plain" });

        var result = await _repo.GetByIdAsync(Guid.NewGuid(), doc.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTenant_ReturnsPaginated()
    {
        for (int i = 0; i < 5; i++)
            await _repo.CreateAsync(new Document { TenantId = _tenantId, Title = $"Doc {i}", ContentType = "text/plain" });

        var (items, total) = await _repo.GetByTenantAsync(_tenantId, 1, 3);

        items.Should().HaveCount(3);
        total.Should().Be(5);
    }

    [Fact]
    public async Task Delete_RemovesDocument()
    {
        var doc = await _repo.CreateAsync(new Document { TenantId = _tenantId, Title = "Delete Me", ContentType = "text/plain" });

        await _repo.DeleteAsync(_tenantId, doc.Id);

        var result = await _repo.GetByIdAsync(_tenantId, doc.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByStatus_FiltersCorrectly()
    {
        await _repo.CreateAsync(new Document { TenantId = _tenantId, Title = "Pending", ContentType = "text/plain", Status = DocumentStatus.Pending });
        await _repo.CreateAsync(new Document { TenantId = _tenantId, Title = "Indexed", ContentType = "text/plain", Status = DocumentStatus.Indexed });

        var pending = await _repo.GetByStatusAsync(DocumentStatus.Pending);
        var indexed = await _repo.GetByStatusAsync(DocumentStatus.Indexed);

        pending.Should().ContainSingle(d => d.Title == "Pending");
        indexed.Should().ContainSingle(d => d.Title == "Indexed");
    }

    public void Dispose() => _db.Dispose();
}
