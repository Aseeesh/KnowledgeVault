using FluentAssertions;
using KnowledgeVault.Application.Commands.Chat;
using KnowledgeVault.Application.Commands.Documents;
using KnowledgeVault.Application.Queries.Search;
using KnowledgeVault.Application.Validators;

namespace KnowledgeVault.Tests.Unit.Validators;

public class ValidatorTests
{
    [Fact]
    public async Task CreateDocument_ValidRequest_Passes()
    {
        var validator = new CreateDocumentValidator();
        var command = new CreateDocumentCommand(Guid.NewGuid(), "Test Doc", "text/plain", null, null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "text/plain")]
    [InlineData("Title", "")]
    [InlineData("Title", "application/zip")]
    public async Task CreateDocument_InvalidRequest_Fails(string title, string contentType)
    {
        var validator = new CreateDocumentValidator();
        var command = new CreateDocumentCommand(Guid.NewGuid(), title, contentType, null, null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateDocument_EmptyTenant_Fails()
    {
        var validator = new CreateDocumentValidator();
        var command = new CreateDocumentCommand(Guid.Empty, "Test", "text/plain", null, null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task SearchQuery_ValidRequest_Passes()
    {
        var validator = new SearchRequestValidator();
        var query = new HybridSearchQuery(Guid.NewGuid(), "What is RAG?", 10);

        var result = await validator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("query", 0)]
    [InlineData("query", 101)]
    public async Task SearchQuery_InvalidRequest_Fails(string queryText, int topK)
    {
        var validator = new SearchRequestValidator();
        var query = new HybridSearchQuery(Guid.NewGuid(), queryText, topK);

        var result = await validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ChatCompletion_EmptyMessage_Fails()
    {
        var validator = new ChatCompletionValidator();
        var command = new ChatCompletionCommand(Guid.NewGuid(), "", null, null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ChatCompletion_TooLongMessage_Fails()
    {
        var validator = new ChatCompletionValidator();
        var command = new ChatCompletionCommand(Guid.NewGuid(), new string('a', 10001), null, null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateDocument_AllSupportedTypes_Pass()
    {
        var validator = new CreateDocumentValidator();
        var types = new[] { "text/plain", "text/html", "text/markdown", "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };

        foreach (var ct in types)
        {
            var result = await validator.ValidateAsync(new CreateDocumentCommand(Guid.NewGuid(), "Doc", ct, null, null));
            result.IsValid.Should().BeTrue($"content type '{ct}' should be valid");
        }
    }
}
