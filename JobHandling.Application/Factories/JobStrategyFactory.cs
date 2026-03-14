using JobHandling.Application.Strategies;
using JobHandling.Domain.Enum;
using JobHandling.Domain.Exceptions;

namespace JobHandling.Application.Factories
{
    /// <summary>
    /// Factory implementation for resolving job strategies.
    /// Uses enum-based lookup instead of string matching for type safety.
    /// </summary>
    public class JobStrategyFactory : IJobStrategyFactory
    {
        private readonly BulkJobStrategy _bulkStrategy;
        private readonly BatchJobStrategy _batchStrategy;

        public JobStrategyFactory(BulkJobStrategy bulkStrategy, BatchJobStrategy batchStrategy)
        {
            _bulkStrategy = bulkStrategy;
            _batchStrategy = batchStrategy;
        }

        public IJobStrategy GetStrategy(JobType jobType)
        {
            return jobType switch
            {
                JobType.Bulk => _bulkStrategy,
                JobType.Batch => _batchStrategy,
                _ => throw new JobHandlingException($"Unknown job type: {jobType}")
            };
        }
    }
}