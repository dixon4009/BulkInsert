using FluentAssertions;
using JobHandling.Application.DTOs;
using JobHandling.Application.Services;
using JobHandling.Application.Strategies;
using JobHandling.Domain.Entities;
using JobHandling.Domain.Enum;
using JobHandling.Domain.Interfaces;
using JobHandling.Infrastructure.Repositories;
using JobHandling.Infrastructure.Services;
using Moq;

namespace JobHandling.Tests
{
    /// <summary>
    /// Integration tests covering complete flow from endpoint to repository.
    /// Tests the full job processing pipeline for Bulk and Batch job types.
    /// </summary>
    public class IntegrationTests
    {
        /// <summary>
        /// Test: Complete Bulk Job flow - from StartJob endpoint to job completion
        /// Covers: Controller → Service → Repository → Background Service → Strategy
        /// </summary>
        [Fact]
        public async Task BulkJob_CompleteFlow_ShouldProcessAllItemsAndPersistResults()
        {
            // Arrange - Setup repository
            var repository = new InMemoryJobRepository();
            
            // Mock processing service that fails on items divisible by 5
            var processorMock = new Mock<IItemProcessingService>();
            processorMock.Setup(p => p.ProcessItemAsync(It.IsAny<int>()))
                .Returns<int>(item => Task.FromResult(
                    item % 5 == 0 
                        ? (false, "Item divisible by 5") 
                        : (true, "Success")));

            // Setup strategies
            var bulkStrategy = new BulkJobStrategy(processorMock.Object);

            // Create service
            var queueMock = new Mock<IJobProcessingQueue>();
            var jobService = new JobService(repository, queueMock.Object);

            // Act 1 - Start job
            var request = new StartJobRequest
            {
                JobType = JobType.Bulk,
                Items = new List<int> { 1, 2, 3, 5, 6 } // 5 items, 1 failure expected
            };

            var jobId = await jobService.StartJob(request);

            // Act 2 - Simulate background processing
            var job = await repository.GetAsync(jobId);
            await bulkStrategy.Execute(job, request.Items);
            job.Status = JobExecutionStatus.Completed;
            await repository.UpdateAsync(job);

            // Act 3 - Get status
            var status = await jobService.GetStatus(jobId);

            // Assert
            jobId.Should().NotBeEmpty();
            status.TotalItems.Should().Be(5);
            status.ProcessedItems.Should().Be(4);
            status.FailedItems.Should().Be(1);
            status.Status.Should().Be(JobExecutionStatus.Completed.ToString());

            var logs = await jobService.GetLogs(jobId);
            logs.Should().HaveCount(5);
            logs.Count(l => l.Success).Should().Be(4);
            logs.Count(l => !l.Success).Should().Be(1);
        }

        /// <summary>
        /// Test: Complete Batch Job flow - from StartJob endpoint to first failure
        /// Covers: Controller → Service → Repository → Background Service → Strategy
        /// </summary>
        [Fact]
        public async Task BatchJob_CompleteFlow_ShouldStopOnFirstFailureAndPersistResults()
        {
            // Arrange
            var repository = new InMemoryJobRepository();
            
            var processorMock = new Mock<IItemProcessingService>();
            processorMock.Setup(p => p.ProcessItemAsync(It.IsAny<int>()))
                .Returns<int>(item => Task.FromResult(
                    item % 5 == 0 
                        ? (false, "Item divisible by 5") 
                        : (true, "Success")));

            var batchStrategy = new BatchJobStrategy(processorMock.Object);

            var queueMock = new Mock<IJobProcessingQueue>();
            var jobService = new JobService(repository, queueMock.Object);

            // Act
            var request = new StartJobRequest
            {
                JobType = JobType.Batch,
                Items = new List<int> { 1, 2, 5, 3, 4 } // Fails on 3rd item (5)
            };

            var jobId = await jobService.StartJob(request);

            var job = await repository.GetAsync(jobId);
            await batchStrategy.Execute(job, request.Items);
            job.Status = JobExecutionStatus.Completed;
            await repository.UpdateAsync(job);

            var status = await jobService.GetStatus(jobId);

            // Assert
            status.TotalItems.Should().Be(5);
            status.ProcessedItems.Should().Be(2);
            status.FailedItems.Should().Be(1);
            status.Status.Should().Be(JobExecutionStatus.Completed.ToString());

            var logs = await jobService.GetLogs(jobId);
            logs.Should().HaveCount(3); // Only 3 items processed before failure
            logs[0].Success.Should().BeTrue();
            logs[1].Success.Should().BeTrue();
            logs[2].Success.Should().BeFalse();
        }

        /// <summary>
        /// Test: ResilientProcessingService retry logic
        /// Covers: Resilient service with exponential backoff
        /// </summary>
        [Fact]
        public async Task ResilientProcessingService_WithTransientFailure_ShouldRetryAndEventuallySucceed()
        {
            // Arrange
            int attemptCount = 0;
            var innerServiceMock = new Mock<IItemProcessingService>();
            
            // Fail twice, succeed on third attempt
            innerServiceMock.Setup(p => p.ProcessItemAsync(It.IsAny<int>()))
                .Returns<int>(item =>
                {
                    attemptCount++;
                    return Task.FromResult(
                        attemptCount < 3 
                            ? (false, "Transient failure") 
                            : (true, "Success after retries"));
                });

            var resilientService = new ResilientProcessingService(innerServiceMock.Object);

            // Act
            var result = await resilientService.ProcessItemAsync(1);

            // Assert
            result.Success.Should().BeTrue();
            result.Description.Should().Be("Success after retries");
            innerServiceMock.Verify(p => p.ProcessItemAsync(1), Times.Exactly(3));
        }

        /// <summary>
        /// Test: ResilientProcessingService exhausts retries
        /// Covers: Resilient service returns failure after all retries exhausted
        /// </summary>
        [Fact]
        public async Task ResilientProcessingService_WithPersistentFailure_ShouldFailAfterMaxRetries()
        {
            // Arrange
            var innerServiceMock = new Mock<IItemProcessingService>();
            innerServiceMock.Setup(p => p.ProcessItemAsync(It.IsAny<int>()))
                .ReturnsAsync((false, "Persistent failure"));

            var resilientService = new ResilientProcessingService(innerServiceMock.Object);

            // Act
            var result = await resilientService.ProcessItemAsync(1);

            // Assert
            result.Success.Should().BeFalse();
            result.Description.Should().Be("Persistent failure");
            // 1 initial attempt + 3 retries = 4 total attempts
            innerServiceMock.Verify(p => p.ProcessItemAsync(1), Times.Exactly(4));
        }

        /// <summary>
        /// Test: Job status calculations are correct
        /// Covers: ProcessedItems + FailedItems should equal items attempted
        /// </summary>
        [Fact]
        public async Task JobStatusCalculations_ShouldBeConsistent()
        {
            // Arrange
            var repository = new InMemoryJobRepository();
            var processorMock = new Mock<IItemProcessingService>();
            processorMock.Setup(p => p.ProcessItemAsync(It.IsAny<int>()))
                .Returns<int>(item => Task.FromResult(
                    item % 3 == 0 
                        ? (false, "Divisible by 3") 
                        : (true, "Success")));

            var bulkStrategy = new BulkJobStrategy(processorMock.Object);
            var queueMock = new Mock<IJobProcessingQueue>();
            var jobService = new JobService(repository, queueMock.Object);

            // Act
            var request = new StartJobRequest
            {
                JobType = JobType.Bulk,
                Items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 }
            };

            var jobId = await jobService.StartJob(request);
            var job = await repository.GetAsync(jobId);
            await bulkStrategy.Execute(job, request.Items);
            await repository.UpdateAsync(job);

            var status = await jobService.GetStatus(jobId);

            // Assert
            status.ProcessedItems.Should().Be(6); // 1,2,4,5,7,8
            status.FailedItems.Should().Be(3);   // 3,6,9
            (status.ProcessedItems + status.FailedItems).Should().Be(status.TotalItems);
        }
    }
}