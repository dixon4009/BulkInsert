using FluentAssertions;
using JobHandling.Application.DTOs;
using JobHandling.Application.Services;
using JobHandling.Domain.Entities;
using JobHandling.Domain.Enum;
using JobHandling.Domain.Exceptions;
using JobHandling.Domain.Interfaces;
using Moq;

namespace JobHandling.Tests
{
    public class JobServiceTests
    {
        [Fact]
        public async Task StartJob_ShouldReturnJobId()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3 };
            var request = new StartJobRequest
            {
                JobType = JobType.Bulk,
                Items = items
            };

            var repoMock = new Mock<IJobRepository>();
            repoMock.Setup(r => r.SaveAsync(It.IsAny<Job>())).Returns(Task.CompletedTask);

            var queueMock = new Mock<IJobProcessingQueue>();

            var service = new JobService(repoMock.Object, queueMock.Object);

            // Act
            var jobId = await service.StartJob(request);

            // Assert
            jobId.Should().NotBeEmpty();
        }

        [Fact]
        public async Task StartJob_ShouldSaveJobWithPendingStatus()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3 };
            var request = new StartJobRequest
            {
                JobType = JobType.Bulk,
                Items = items
            };

            var repoMock = new Mock<IJobRepository>();
            repoMock.Setup(r => r.SaveAsync(It.IsAny<Job>())).Returns(Task.CompletedTask);

            var queueMock = new Mock<IJobProcessingQueue>();

            var service = new JobService(repoMock.Object, queueMock.Object);

            // Act
            var jobId = await service.StartJob(request);

            // Assert
            repoMock.Verify(r => r.SaveAsync(It.Is<Job>(j =>
                j.TotalItems == 3 &&
                j.JobType == JobType.Bulk &&
                j.Status == JobExecutionStatus.Pending)), Times.Once);
        }

        [Fact]
        public async Task StartJob_ShouldQueueJobForProcessing()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3 };
            var request = new StartJobRequest
            {
                JobType = JobType.Bulk,
                Items = items
            };

            var repoMock = new Mock<IJobRepository>();
            repoMock.Setup(r => r.SaveAsync(It.IsAny<Job>())).Returns(Task.CompletedTask);

            var queueMock = new Mock<IJobProcessingQueue>();

            var service = new JobService(repoMock.Object, queueMock.Object);

            // Act
            var jobId = await service.StartJob(request);

            // Assert
            queueMock.Verify(q => q.QueueJob(jobId, items, JobType.Bulk), Times.Once);
        }

        [Fact]
        public async Task GetStatus_ShouldReturnJobStatus()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var job = new Job
            {
                Id = jobId,
                JobType = JobType.Bulk,
                TotalItems = 3,
                ProcessedItems = 2,
                FailedItems = 1,
                Status = JobExecutionStatus.Running
            };

            var repoMock = new Mock<IJobRepository>();
            repoMock.Setup(r => r.GetAsync(jobId)).ReturnsAsync(job);

            var queueMock = new Mock<IJobProcessingQueue>();

            var service = new JobService(repoMock.Object, queueMock.Object);

            // Act
            var status = await service.GetStatus(jobId);

            // Assert
            status.TotalItems.Should().Be(3);
            status.ProcessedItems.Should().Be(2);
            status.FailedItems.Should().Be(1);
            status.Status.Should().Be(JobExecutionStatus.Running.ToString());
        }

        [Fact]
        public async Task GetLogs_ShouldReturnJobLogs()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var logs = new List<JobItemLog>
            {
                new JobItemLog { ItemId = 1, Success = true, Description = "Success" },
                new JobItemLog { ItemId = 2, Success = false, Description = "Failed" }
            };

            var job = new Job
            {
                Id = jobId,
                JobType = JobType.Bulk,
                TotalItems = 2,
                ProcessedItems = 2,
                FailedItems = 1,
                Status = JobExecutionStatus.Completed,
                Logs = logs
            };

            var repoMock = new Mock<IJobRepository>();
            repoMock.Setup(r => r.GetAsync(jobId)).ReturnsAsync(job);

            var queueMock = new Mock<IJobProcessingQueue>();

            var service = new JobService(repoMock.Object, queueMock.Object);

            // Act
            var result = await service.GetLogs(jobId);

            // Assert
            result.Should().HaveCount(2);
            result[0].Success.Should().BeTrue();
            result[1].Success.Should().BeFalse();
        }

        [Fact]
        public async Task StartJob_BulkJob_ShouldQueueWithBulkJobType()
        {
            // Arrange
            var items = new List<int> { 1, 5 };
            var request = new StartJobRequest
            {
                JobType = JobType.Bulk,
                Items = items
            };

            var repoMock = new Mock<IJobRepository>();
            repoMock.Setup(r => r.SaveAsync(It.IsAny<Job>())).Returns(Task.CompletedTask);

            var queueMock = new Mock<IJobProcessingQueue>();

            var service = new JobService(repoMock.Object, queueMock.Object);

            // Act
            var jobId = await service.StartJob(request);

            // Assert
            queueMock.Verify(q => q.QueueJob(
                It.IsAny<Guid>(),
                It.Is<List<int>>(l => l.Count == 2),
                JobType.Bulk), Times.Once);
        }

        [Fact]
        public async Task StartJob_BatchJob_ShouldQueueWithBatchJobType()
        {
            // Arrange
            var items = new List<int> { 1, 5, 3 };
            var request = new StartJobRequest
            {
                JobType = JobType.Batch,
                Items = items
            };

            var repoMock = new Mock<IJobRepository>();
            repoMock.Setup(r => r.SaveAsync(It.IsAny<Job>())).Returns(Task.CompletedTask);

            var queueMock = new Mock<IJobProcessingQueue>();

            var service = new JobService(repoMock.Object, queueMock.Object);

            // Act
            var jobId = await service.StartJob(request);

            // Assert
            queueMock.Verify(q => q.QueueJob(
                It.IsAny<Guid>(),
                It.Is<List<int>>(l => l.Count == 3),
                JobType.Batch), Times.Once);
        }

        [Fact]
        public async Task StartJob_WithEmptyItems_ShouldThrowJobHandlingException()
        {
            // Arrange
            var request = new StartJobRequest
            {
                JobType = JobType.Bulk,
                Items = new List<int>()
            };

            var repoMock = new Mock<IJobRepository>();
            var queueMock = new Mock<IJobProcessingQueue>();

            var service = new JobService(repoMock.Object, queueMock.Object);

            // Act & Assert
            await service.Invoking(s => s.StartJob(request))
                .Should()
                .ThrowAsync<JobHandlingException>();
        }

        [Fact]
        public async Task GetStatus_WithNonExistentJob_ShouldThrowJobNotFoundException()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var repoMock = new Mock<IJobRepository>();
            repoMock.Setup(r => r.GetAsync(jobId))
                .ThrowsAsync(new KeyNotFoundException());

            var queueMock = new Mock<IJobProcessingQueue>();

            var service = new JobService(repoMock.Object, queueMock.Object);

            // Act & Assert
            await service.Invoking(s => s.GetStatus(jobId))
                .Should()
                .ThrowAsync<JobNotFoundException>();
        }

        [Fact]
        public async Task GetLogs_WithNonExistentJob_ShouldThrowJobNotFoundException()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var repoMock = new Mock<IJobRepository>();
            repoMock.Setup(r => r.GetAsync(jobId))
                .ThrowsAsync(new KeyNotFoundException());

            var queueMock = new Mock<IJobProcessingQueue>();

            var service = new JobService(repoMock.Object, queueMock.Object);

            // Act & Assert
            await service.Invoking(s => s.GetLogs(jobId))
                .Should()
                .ThrowAsync<JobNotFoundException>();
        }
    }
}