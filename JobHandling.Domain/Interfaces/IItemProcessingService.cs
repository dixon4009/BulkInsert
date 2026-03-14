using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobHandling.Domain.Interfaces
{
    public interface IItemProcessingService
    {
        Task<(bool Success, string Description)> ProcessItemAsync(int item);
    }
}
