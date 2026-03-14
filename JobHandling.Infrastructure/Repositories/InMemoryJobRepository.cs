using JobHandling.Domain.Entities;
using JobHandling.Domain.Interfaces;
using System.Collections.Concurrent;

namespace JobHandling.Infrastructure.Repositories
{
    public class InMemoryJobRepository : IJobRepository
    {
        private static readonly ConcurrentDictionary<Guid, Job> _jobs = new();

        public Task<Job> GetAsync(Guid id)
        {
            if (!_jobs.TryGetValue(id, out var job))
            {
                throw new KeyNotFoundException($"Job with ID {id} not found.");
            }

            return Task.FromResult(job);
        }

        public Task SaveAsync(Job job)
        {
            var storedJob = new Job
            {
                Id = job.Id,
                JobType = job.JobType,
                TotalItems = job.TotalItems,
                ProcessedItems = job.ProcessedItems,
                FailedItems = job.FailedItems,
                Status = job.Status,
                Logs = new List<JobItemLog>(job.Logs)
            };
            _jobs[job.Id] = storedJob;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Job job)
        {
            if (_jobs.TryGetValue(job.Id, out var existingJob))
            {
                existingJob.ProcessedItems = job.ProcessedItems;
                existingJob.FailedItems = job.FailedItems;
                existingJob.Status = job.Status;
                existingJob.TotalItems = job.TotalItems;
                existingJob.JobType = job.JobType;
                existingJob.Logs = new List<JobItemLog>(job.Logs);
            }
            else
            {
                _jobs[job.Id] = job;
            }

            return Task.CompletedTask;
        }
    }
}   