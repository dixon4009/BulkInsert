namespace JobHandling.Domain.Exceptions
{
    public class JobNotFoundException : JobHandlingException
    {
        public JobNotFoundException(Guid jobId)
            : base($"Job with ID '{jobId}' not found.") { }
    }
}