using JobHandling.Domain.Interfaces;
using Serilog;
using Serilog.Context;

namespace JobHandling.Infrastructure.Services
{
    /// <summary>
    /// Mock item processing service for testing and development purposes.
    /// Simulates processing delays and failures (items divisible by 5 fail).
    /// </summary>
    public class MockProcessingService : IItemProcessingService
    {
        private readonly int _delayMilliseconds;

        /// <summary>
        /// Initializes a new instance of the MockProcessingService.
        /// </summary>
        /// <param name="delayMilliseconds">Simulated processing delay in milliseconds (default: 500ms)</param>
        /// <exception cref="ArgumentException">Thrown when delayMilliseconds is negative</exception>
        public MockProcessingService(int delayMilliseconds = 500)
        {
            if (delayMilliseconds < 0)
            {
                throw new ArgumentException("Delay milliseconds cannot be negative", nameof(delayMilliseconds));
            }

            _delayMilliseconds = delayMilliseconds;
        }

        /// <summary>
        /// Processes an item with a simulated delay.
        /// Fails for items divisible by 5 to simulate realistic failure scenarios.
        /// </summary>
        /// <param name="item">The item ID to process</param>
        /// <returns>
        /// Tuple containing:
        /// - Success: True if item % 5 != 0, false otherwise
        /// - Description: Human-readable result message
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when item is negative</exception>
        public async Task<(bool Success, string Description)> ProcessItemAsync(int item)
        {
            using (LogContext.PushProperty("ItemId", item))
            {
                try
                {
                    // Input validation
                    if (item < 0)
                    {
                        Log.Warning("Attempted to process negative item ID: {ItemId}", item);
                        throw new ArgumentException("Item ID cannot be negative", nameof(item));
                    }

                    Log.Debug("Processing item {ItemId} with {DelayMilliseconds}ms delay", item, _delayMilliseconds);

                    await Task.Delay(_delayMilliseconds);

                    // Simulate failure for items divisible by 5
                    if (item % 5 == 0)
                    {
                        Log.Information("Item {ItemId} processing failed (divisible by 5)", item);
                        return (false, $"Item {item} processing failed: divisible by 5");
                    }

                    Log.Debug("Item {ItemId} processed successfully", item);
                    return (true, $"Item {item} processed successfully");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception during processing of item {ItemId}", item);
                    throw;
                }
            }
        }
    }   
}
