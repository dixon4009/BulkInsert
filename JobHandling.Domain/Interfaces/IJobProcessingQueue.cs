using JobHandling.Domain.Enum;

namespace JobHandling.Domain.Interfaces
{
    public interface IJobProcessingQueue
    {
        void QueueJob(Guid jobId, List<int> items, JobType jobType);
    }
}