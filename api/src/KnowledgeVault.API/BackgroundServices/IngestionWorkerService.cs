using KnowledgeVault.Core.Enums;
using KnowledgeVault.Core.Interfaces.Repositories;
using KnowledgeVault.Core.Interfaces.Services;

namespace KnowledgeVault.API.BackgroundServices;

public class IngestionWorkerService(
    IServiceProvider serviceProvider,
    ILogger<IngestionWorkerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Ingestion worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var jobRepo = scope.ServiceProvider.GetRequiredService<IIngestionJobRepository>();
                var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();

                var pendingJobs = await jobRepo.GetByStatusAsync(IngestionJobStatus.Queued, 5, stoppingToken);

                // Also retry failed jobs that haven't exceeded max retries
                var failedJobs = await jobRepo.GetByStatusAsync(IngestionJobStatus.Failed, 3, stoppingToken);
                var retryableJobs = failedJobs.Where(j => j.RetryCount < j.MaxRetries).ToList();

                foreach (var job in pendingJobs.Concat(retryableJobs))
                {
                    try
                    {
                        logger.LogInformation("Processing ingestion job {JobId} for document {DocumentId}", job.Id, job.DocumentId);
                        await ingestionService.ProcessDocumentAsync(job.Id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process ingestion job {JobId}", job.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ingestion worker error");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
