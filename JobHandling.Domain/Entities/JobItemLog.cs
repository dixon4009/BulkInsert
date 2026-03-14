using JobHandling.Domain.Enum;

namespace JobHandling.Domain.Entities
{
    public class JobItemLog
    {
        public int ItemId { get; set; }

        public bool Success { get; set; }

        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when this item was processed.
        /// Useful for audit trails and performance analysis.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
