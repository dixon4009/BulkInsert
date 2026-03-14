using JobHandling.Domain.Entities;
using JobHandling.Domain.Interfaces;
using Serilog;
using Serilog.Context;

namespace JobHandling.Application.Strategies
{
    /// <summary>
    /// Bulk job strategy processes all items regardless of individual failures.
    /// Logs all processing attempts and handles exceptions gracefully.
    /// </summary>
    public class BulkJobStrategy : IJobStrategy
    {
        private readonly IItemProcessingService _processor;

        public BulkJobStrategy(IItemProcessingService processor)
        {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        }

        /// <summary>
        /// Executes bulk processing strategy for all items.
        /// Continues processing even if individual items fail.
        /// </summary>
        /// <param name="job">The job to process</param>
        /// <param name="items">List of item IDs to process</param>
        /// <exception cref="ArgumentNullException">Thrown if job or items is null</exception>
        public async Task Execute(Job job, List<int> items)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            using (LogContext.PushProperty("JobId", job.Id))
            using (LogContext.PushProperty("JobType", "Bulk"))
            {
                Log.Information("Starting bulk processing of {ItemCount} items", items.Count);

                int successCount = 0;
                int failureCount = 0;

                foreach (var item in items)
                {
                    try
                    {
                        Log.Debug("Processing item {ItemId}", item);
                        var result = await _processor.ProcessItemAsync(item);

                        if (result.Success)
                        {
                            job.ProcessedItems++;
                            successCount++;
                            Log.Debug("Item {ItemId} processed successfully", item);
                        }
                        else
                        {
                            job.FailedItems++;
                            failureCount++;
                            Log.Warning("Item {ItemId} processing failed: {Description}", item, result.Description);
                        }

                        job.Logs.Add(new JobItemLog
                        {
                            ItemId = item,
                            Success = result.Success,
                            Description = result.Description,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    catch (Exception ex)
                    {
                        job.FailedItems++;
                        failureCount++;
                        
                        Log.Error(ex, "Exception processing item {ItemId}", item);

                        job.Logs.Add(new JobItemLog
                        {
                            ItemId = item,
                            Success = false,
                            Description = $"Exception: {ex.Message}",
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }

                Log.Information(
                    "Bulk processing completed. Success: {SuccessCount}, Failed: {FailureCount}, Total: {TotalCount}",
                    successCount,
                    failureCount,
                    items.Count);
            }
        }
    }
}
