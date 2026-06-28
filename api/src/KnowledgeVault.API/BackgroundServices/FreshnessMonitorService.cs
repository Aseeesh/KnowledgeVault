using KnowledgeVault.Core.Interfaces.Repositories;

namespace KnowledgeVault.API.BackgroundServices;

public class FreshnessMonitorService(
    IServiceProvider serviceProvider,
    ILogger<FreshnessMonitorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Freshness monitor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var documentRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

                var staleDocuments = await documentRepo.GetStaleDocumentsAsync(
                    TimeSpan.FromHours(24), stoppingToken);

                if (staleDocuments.Count > 0)
                {
                    logger.LogInformation("Found {Count} stale documents for freshness check", staleDocuments.Count);

                    foreach (var doc in staleDocuments)
                    {
                        doc.FreshnessCheckedAt = DateTime.UtcNow;
                        await documentRepo.UpdateAsync(doc, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Freshness monitor error");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}
