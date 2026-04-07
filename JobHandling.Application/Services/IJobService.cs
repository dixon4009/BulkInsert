using JobHandling.Application.DTOs;
using JobHandling.Domain.Entities;

namespace JobHandling.Application.Services
{
    /// <summary>
    /// Abstraction for the job orchestration service.
    /// Registered via DI to enable loose coupling between the API layer
    /// and application logic, and to simplify unit testing of controllers.
    /// </summary>
    public interface IJobService
    {
        Task<Guid> StartJob(StartJobRequest request);
        Task<JobStatusResponse> GetStatus(Guid id);
        Task<List<JobItemLog>> GetLogs(Guid id);
    }
}