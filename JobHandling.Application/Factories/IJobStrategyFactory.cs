using JobHandling.Application.Strategies;
using JobHandling.Domain.Enum;

namespace JobHandling.Application.Factories
{
    /// <summary>
    /// Factory interface for resolving job strategies by job type.
    /// Replaces fragile string-based strategy selection with type-safe factory pattern.
    /// </summary>
    public interface IJobStrategyFactory
    {
        IJobStrategy GetStrategy(JobType jobType);
    }
}