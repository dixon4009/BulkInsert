using JobHandling.Application.Factories;
using JobHandling.Domain.Entities;
using JobHandling.Domain.Enum;
using JobHandling.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Context;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace JobHandling.Infrastructure.Services
{
    /// <summary>
    /// Background service responsible for dequeuing and processing jobs asynchronously.
    /// Uses IJobStrategyFactory for type-safe strategy resolution.
    /// </summary>
    public class JobProcessingBackgroundService : BackgroundService, IJobProcessingQueue
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentQueue<(Guid JobId, List<int> Items, JobType JobType)> _jobQueue;

        public JobProcessingBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _jobQueue = new ConcurrentQueue<(Guid, List<int>, JobType)>();
        }

        public void QueueJob(Guid jobId, List<int> items, JobType jobType)
        {
            _jobQueue.Enqueue((jobId, items, jobType));
            Log.Debug("Job {JobId} queued for processing", jobId);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("Job processing background service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_jobQueue.TryDequeue(out var jobData))
                {
                    await ProcessJobAsync(jobData, stoppingToken);
                }
                else
                {
                    await Task.Delay(100, stoppingToken);
                }
            }

            Log.Information("Job processing background service stopped");
        }

        private async Task ProcessJobAsync(
            (Guid JobId, List<int> Items, JobType JobType) jobData,
            CancellationToken cancellationToken)
        {
            using (LogContext.PushProperty("JobId", jobData.JobId))
            {
                var stopwatch = Stopwatch.StartNew();

                Log.Information(
                    "Processing started for job of type {JobType} with {ItemCount} items",
                    jobData.JobType,
                    jobData.Items.Count);

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
                        var strategyFactory = scope.ServiceProvider.GetRequiredService<IJobStrategyFactory>();

                        var job = await repository.GetAsync(jobData.JobId);

                        job.Status = JobExecutionStatus.Running;
                        await repository.UpdateAsync(job);

                        Log.Debug("Job status changed to Running");

                        var strategy = strategyFactory.GetStrategy(jobData.JobType);

                        Log.Debug("Executing {StrategyType} for job type {JobType}", strategy.GetType().Name, jobData.JobType);

                        await strategy.Execute(job, jobData.Items);

                        job.Status = JobExecutionStatus.Completed;
                        await repository.UpdateAsync(job);

                        stopwatch.Stop();

                        Log.Information(
                            "Job completed successfully in {ElapsedMilliseconds}ms. Processed: {ProcessedItems}, Failed: {FailedItems}",
                            stopwatch.ElapsedMilliseconds,
                            job.ProcessedItems,
                            job.FailedItems);
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();

                    Log.Error(ex,
                        "Job failed after {ElapsedMilliseconds}ms",
                        stopwatch.ElapsedMilliseconds);

                    await HandleJobFailureAsync(jobData.JobId, ex);
                }
            }
        }

        private async Task HandleJobFailureAsync(Guid jobId, Exception ex)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
                    var job = await repository.GetAsync(jobId);

                    job.Status = JobExecutionStatus.Failed;
                    job.Logs.Add(new JobItemLog
                    {
                        ItemId = 0,
                        Success = false,
                        Description = $"Job processing failed: {ex.Message}"
                    });

                    await repository.UpdateAsync(job);

                    Log.Error("Job {JobId} marked as Failed and logged error message", jobId);
                }
            }
            catch (Exception updateEx)
            {
                Log.Error(updateEx, "Failed to update job {JobId} with error status", jobId);
            }
        }
    }
}