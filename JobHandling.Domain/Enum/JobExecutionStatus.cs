using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobHandling.Domain.Enum
{
    public enum JobExecutionStatus
    {
        Pending,
        Running,
        Completed,
        Failed
    }
}
