using JobHandling.Domain.Entities;

namespace JobHandling.Application.Strategies
{
    public interface IJobStrategy
    {
        Task Execute(Job job, List<int> items);
    }
}
