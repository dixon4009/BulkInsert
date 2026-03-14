using JobHandling.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobHandling.Domain.Interfaces
{
    public interface IJobRepository
    {
        Task<Job> GetAsync(Guid id);

        Task SaveAsync(Job job);

        Task UpdateAsync(Job job);
    }
}
