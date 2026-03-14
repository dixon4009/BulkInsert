using JobHandling.Domain.Interfaces;
using Polly;
using Polly.Retry;
using Serilog;
using Serilog.Context;


namespace JobHandling.Infrastructure.Services
{
    /// <summary>
    /// Decorator pattern: Wraps IItemProcessingService with resilience policy.
    /// Implements automatic retry with exponential backoff for failed items.
    /// </summary>
    public class ResilientProcessingService : IItemProcessingService
    {
        private readonly IItemProcessingService _innerService;
        private readonly AsyncRetryPolicy<(bool Success, string Description)> _retryPolicy;

        public ResilientProcessingService(IItemProcessingService innerService)
        {
            _innerService = innerService;

            _retryPolicy = Policy<(bool Success, string Description)>
                .HandleResult(r => !r.Success) // retry if failed
                .WaitAndRetryAsync(
                    retryCount: 3, // max 3 retries
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)), // 200ms, 400ms, 800ms backoff
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var itemId = context.ContainsKey("ItemId") ? context["ItemId"] : "Unknown";
                        
                        Log.Warning(
                            "Retry {RetryCount} for item {ItemId} after {DelayMilliseconds}ms. Failure: {Description}",
                            retryCount,
                            itemId,
                            timespan.TotalMilliseconds,
                            outcome.Result.Description);
                    });
        }

        /// <summary>
        /// Processes an item with automatic retry on failure.
        /// </summary>
        /// <param name="item">The item ID to process</param>
        /// <returns>Tuple containing success status and description</returns>
        public async Task<(bool Success, string Description)> ProcessItemAsync(int item)
        {
            using (LogContext.PushProperty("ItemId", item))
            {
                try
                {
                    Log.Debug("Processing item {ItemId} with resilience policy", item);

                    var context = new Polly.Context { { "ItemId", item } };
                    var result = await _retryPolicy.ExecuteAsync(
                        ctx => _innerService.ProcessItemAsync(item),
                        context);

                    if (result.Success)
                    {
                        Log.Debug("Item {ItemId} processed successfully", item);
                    }
                    else
                    {
                        Log.Warning(
                            "Item {ItemId} failed after all retries. Final result: {Description}",
                            item,
                            result.Description);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unexpected error processing item {ItemId}", item);
                    return (false, $"Unexpected error: {ex.Message}");
                }
            }
        }
    }
}