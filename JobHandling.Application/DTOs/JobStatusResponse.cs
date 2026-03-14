namespace JobHandling.Application
{
    public class JobStatusResponse
    {
        public int TotalItems { get; set; }

        public int ProcessedItems { get; set; }

        public int FailedItems { get; set; }

        public string Status { get; set; }
    }
}
