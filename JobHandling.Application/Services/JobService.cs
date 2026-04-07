using JobHandling.Application.DTOs;
using JobHandling.Application.Strategies;
using JobHandling.Domain.Entities;
using JobHandling.Domain.Enum;
using JobHandling.Domain.Exceptions;
using JobHandling.Domain.Interfaces;
using Serilog;
using Serilog.Context;

namespace JobHandling.Application.Services
{
    public class JobService : IJobService
    {
        private readonly IJobRepository _repository;
        private readonly IJobProcessingQueue _processingQueue;

        public JobService(IJobRepository repository,
                          IJobProcessingQueue processingQueue)
        {
            _repository = repository;
            _processingQueue = processingQueue;
        }

        public async Task<Guid> StartJob(StartJobRequest request)
        {
            ValidateStartJobRequest(request);

            var job = new Job
            {
                Id = Guid.NewGuid(),
                JobType = request.JobType,
                TotalItems = request.Items.Count,
                Status = JobExecutionStatus.Pending
            };

            using (LogContext.PushProperty("JobId", job.Id))
            {
                try
                {
                    await _repository.SaveAsync(job);
                    Log.Information(
                        "Job saved to repository with status {Status}",
                        job.Status);

                    _processingQueue.QueueJob(job.Id, request.Items, request.JobType);
                    Log.Information(
                        "Job queued for processing with {ItemCount} items and type {JobType}",
                        request.Items.Count,
                        request.JobType);

                    return job.Id;
                }
                catch (Exception ex)
                {
                    Log.Error(ex,
                        "Error starting job of type {JobType}",
                        request.JobType);

                    throw new JobHandlingException("Failed to start job", ex);
                }
            }
        }

        public async Task<JobStatusResponse> GetStatus(Guid id)
        {
            using (LogContext.PushProperty("JobId", id))
            {
                try
                {
                    var job = await _repository.GetAsync(id);

                    return new JobStatusResponse
                    {
                        TotalItems = job.TotalItems,
                        ProcessedItems = job.ProcessedItems,
                        FailedItems = job.FailedItems,
                        Status = job.Status.ToString()
                    };
                }
                catch (KeyNotFoundException)
                {
                    Log.Warning("Job not found");
                    throw new JobNotFoundException(id);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error retrieving status");
                    throw;
                }
            }
        }

        public async Task<List<JobItemLog>> GetLogs(Guid id)
        {
            using (LogContext.PushProperty("JobId", id))
            {
                try
                {
                    var job = await _repository.GetAsync(id);
                    return job.Logs;
                }
                catch (KeyNotFoundException)
                {
                    Log.Warning("Job not found when retrieving logs");
                    throw new JobNotFoundException(id);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error retrieving logs");
                    throw;
                }
            }
        }

        private void ValidateStartJobRequest(StartJobRequest request)
        {
            if (request?.Items == null || request.Items.Count == 0)
            {
                Log.Warning("Invalid StartJobRequest: items are null or empty");
                throw new JobHandlingException("Items cannot be null or empty");
            }
        }
    }
}