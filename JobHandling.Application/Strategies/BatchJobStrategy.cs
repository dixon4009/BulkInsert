using JobHandling.Domain.Entities;
using JobHandling.Domain.Interfaces;
using Serilog;
using Serilog.Context;

namespace JobHandling.Application.Strategies
{
    /// <summary>
    /// Batch job strategy processes items sequentially and stops on the first failure.
    /// Logs processing attempts and handles exceptions appropriately.
    /// </summary>
    public class BatchJobStrategy : IJobStrategy
    {
        private readonly IItemProcessingService _processor;

        public BatchJobStrategy(IItemProcessingService processor)
        {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        }

        /// <summary>
        /// Executes batch processing strategy, stopping on first failure.
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
            using (LogContext.PushProperty("JobType", "Batch"))
            {
                Log.Information("Starting batch processing of {ItemCount} items (will stop on first failure)", items.Count);

                foreach (var item in items)
                {
                    try
                    {
                        Log.Debug("Processing item {ItemId}", item);
                        var result = await _processor.ProcessItemAsync(item);

                        if (!result.Success)
                        {
                            job.FailedItems++;

                            Log.Warning(
                                "Batch processing stopped at item {ItemId}. Failure reason: {Description}",
                                item,
                                result.Description);

                            job.Logs.Add(new JobItemLog
                            {
                                ItemId = item,
                                Success = false,
                                Description = result.Description,
                                Timestamp = DateTime.UtcNow
                            });

                            break; // Stop on first failure
                        }

                        job.ProcessedItems++;
                        Log.Debug("Item {ItemId} processed successfully", item);

                        job.Logs.Add(new JobItemLog
                        {
                            ItemId = item,
                            Success = true,
                            Description = result.Description,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    catch (Exception ex)
                    {
                        job.FailedItems++;

                        Log.Error(
                            ex,
                            "Exception during batch processing at item {ItemId}. Batch stopped.",
                            item);

                        job.Logs.Add(new JobItemLog
                        {
                            ItemId = item,
                            Success = false,
                            Description = $"Exception: {ex.Message}",
                            Timestamp = DateTime.UtcNow
                        });

                        break; // Stop on exception
                    }
                }

                Log.Information(
                    "Batch processing completed. Processed: {ProcessedItems}, Failed: {FailedItems}",
                    job.ProcessedItems,
                    job.FailedItems);
            }
        }
    }
}