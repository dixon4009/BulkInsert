namespace JobHandling.Domain.Exceptions
{
    public class JobHandlingException : Exception
    {
        public JobHandlingException(string message) : base(message) { }

        public JobHandlingException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}