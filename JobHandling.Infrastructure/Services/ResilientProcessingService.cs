using JobHandling.Domain.Interfaces;
using Polly;
using Polly.Retry;
using Serilog;
using Serilog.Context;
using System.Net.Http;

namespace JobHandling.Infrastructure.Services
{
    /// <summary>
    /// Decorator that wraps an IItemProcessingService with resilience policies.
    /// 
    /// Design decision: Two retry triggers are configured intentionally —
    ///   1. Result-based: retries when the inner service returns Success=false (graceful failures).
    ///   2. Exception-based: retries on transient exceptions (network timeouts, HTTP 503, etc.).
    /// This ensures the system is fault-tolerant against both logical failures and infrastructure errors,
    /// which is critical for real-world services where processing calls involve I/O.
    /// </summary>
    public class ResilientProcessingService : IItemProcessingService
    {
        private readonly IItemProcessingService _innerService;
        private readonly AsyncRetryPolicy<(bool Success, string Description)> _retryPolicy;

        public ResilientProcessingService(IItemProcessingService innerService)
        {
            _innerService = innerService;

            _retryPolicy = Policy<(bool Success, string Description)>
                .HandleResult(r => !r.Success)
                .Or<HttpRequestException>()
                .Or<TimeoutException>()
                .Or<InvalidOperationException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var itemId = context.ContainsKey("ItemId") ? context["ItemId"] : "Unknown";

                        if (outcome.Exception != null)
                        {
                            Log.Warning(
                                outcome.Exception,
                                "Retry {RetryCount} for item {ItemId} after {DelayMilliseconds}ms due to exception: {ExceptionMessage}",
                                retryCount,
                                itemId,
                                timespan.TotalMilliseconds,
                                outcome.Exception.Message);
                        }
                        else
                        {
                            Log.Warning(
                                "Retry {RetryCount} for item {ItemId} after {DelayMilliseconds}ms. Failure: {Description}",
                                retryCount,
                                itemId,
                                timespan.TotalMilliseconds,
                                outcome.Result.Description);
                        }
                    });
        }

        /// <summary>
        /// Processes an item with automatic retry on both result-based failures and exceptions.
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
                    Log.Error(ex, "Item {ItemId} failed after all retries with unhandled exception", item);
                    return (false, $"Failed after retries: {ex.Message}");
                }
            }
        }
    }
}