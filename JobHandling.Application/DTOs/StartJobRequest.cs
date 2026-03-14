using System.ComponentModel.DataAnnotations;
using JobHandling.Domain.Enum;

namespace JobHandling.Application.DTOs
{
    public class StartJobRequest
    {
        [Required(ErrorMessage = "JobType is required")]
        [EnumDataType(typeof(JobType))]
        public JobType JobType { get; set; }

        [Required(ErrorMessage = "Items list is required")]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        [MaxLength(1000, ErrorMessage = "Maximum 1000 items allowed")]
        public List<int> Items { get; set; }
    }
}
