using KnowledgeVault.Core.Entities;

namespace KnowledgeVault.Application.Common;

public class TenantContext
{
    public Guid TenantId { get; set; }
    public string TenantSlug { get; set; } = "";
    public Tenant? Tenant { get; set; }
}
