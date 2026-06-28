using FluentValidation;
using KnowledgeVault.Application.Commands.Documents;

namespace KnowledgeVault.Application.Validators;

public class CreateDocumentValidator : AbstractValidator<CreateDocumentCommand>
{
    private static readonly HashSet<string> SupportedContentTypes =
        ["text/plain", "text/html", "text/markdown", "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"];

    public CreateDocumentValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ContentType).NotEmpty().Must(ct => SupportedContentTypes.Contains(ct))
            .WithMessage("Unsupported content type. Supported: " + string.Join(", ", SupportedContentTypes));
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public class SearchRequestValidator : AbstractValidator<Queries.Search.HybridSearchQuery>
{
    public SearchRequestValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.TopK).InclusiveBetween(1, 100);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public class ChatCompletionValidator : AbstractValidator<Commands.Chat.ChatCompletionCommand>
{
    public ChatCompletionValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(10000);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
