using JobHandling.Domain.Enum;

namespace JobHandling.Domain.Entities
{
    public class Job
    {
        public Guid Id { get; set; }

        public JobType JobType { get; set; }

        public int TotalItems { get; set; }

        public int ProcessedItems { get; set; } = 0;

        public int FailedItems { get; set; } = 0;

        public JobExecutionStatus Status { get; set; }

        public List<JobItemLog> Logs { get; set; } = new();
    }
}
