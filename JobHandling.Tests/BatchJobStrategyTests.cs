using FluentAssertions;
using JobHandling.Application.Strategies;
using JobHandling.Domain.Entities;
using JobHandling.Domain.Interfaces;
using Moq;

namespace JobHandling.Tests
{
    /// <summary>
    /// Unit tests for BatchJobStrategy which processes items sequentially and stops on first failure.
    /// </summary>
    public class BatchJobStrategyTests
    {
        [Fact]
        public async Task Execute_StopsOnFirstFailure_LogsCorrectly()
        {
            // Arrange
            var items = new List<int> { 1, 5, 3 };
            var job = CreateJob();

            var processorMock = new Mock<IItemProcessingService>();
            processorMock.Setup(p => p.ProcessItemAsync(1))
                .ReturnsAsync((true, "Success"));
            processorMock.Setup(p => p.ProcessItemAsync(5))
                .ReturnsAsync((false, "Failed"));
            processorMock.Setup(p => p.ProcessItemAsync(3))
                .ReturnsAsync((true, "Success"));

            var strategy = new BatchJobStrategy(processorMock.Object);

            // Act
            await strategy.Execute(job, items);

            // Assert
            job.ProcessedItems.Should().Be(1);
            job.FailedItems.Should().Be(1);
            job.Logs.Should().HaveCount(2);

            job.Logs.Should().Contain(l => l.ItemId == 1 && l.Success);
            job.Logs.Should().Contain(l => l.ItemId == 5 && !l.Success && l.Description == "Failed");

            processorMock.Verify(p => p.ProcessItemAsync(3), Times.Never);
        }

        [Fact]
        public async Task Execute_ProcessesAllItemsWhenAllSucceed()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3 };
            var job = CreateJob();

            var processorMock = new Mock<IItemProcessingService>();
            processorMock.Setup(p => p.ProcessItemAsync(It.IsAny<int>()))
                .ReturnsAsync((true, "Success"));

            var strategy = new BatchJobStrategy(processorMock.Object);

            // Act
            await strategy.Execute(job, items);

            // Assert
            job.ProcessedItems.Should().Be(3);
            job.FailedItems.Should().Be(0);
            job.Logs.Should().HaveCount(3);
            job.Logs.Should().AllSatisfy(log => log.Success.Should().BeTrue());

            processorMock.Verify(p => p.ProcessItemAsync(It.IsAny<int>()), Times.Exactly(3));
        }

        [Fact]
        public async Task Execute_StopsImmediatelyOnFirstFailure()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3, 4, 5 };
            var job = CreateJob();

            var processorMock = new Mock<IItemProcessingService>();
            processorMock.Setup(p => p.ProcessItemAsync(1))
                .ReturnsAsync((true, "Success"));
            processorMock.Setup(p => p.ProcessItemAsync(2))
                .ReturnsAsync((false, "Failed on item 2"));
            processorMock.Setup(p => p.ProcessItemAsync(It.IsIn(3, 4, 5)))
                .ReturnsAsync((true, "Should not reach here"));

            var strategy = new BatchJobStrategy(processorMock.Object);

            // Act
            await strategy.Execute(job, items);

            // Assert
            job.ProcessedItems.Should().Be(1);
            job.FailedItems.Should().Be(1);
            job.Logs.Should().HaveCount(2);

            processorMock.Verify(p => p.ProcessItemAsync(1), Times.Once);
            processorMock.Verify(p => p.ProcessItemAsync(2), Times.Once);
            processorMock.Verify(p => p.ProcessItemAsync(It.IsIn(3, 4, 5)), Times.Never);
        }

        [Fact]
        public async Task Execute_LogsIncludeCorrectSuccessStatusAndDescription()
        {
            // Arrange
            var items = new List<int> { 1, 2 };
            var job = CreateJob();

            var processorMock = new Mock<IItemProcessingService>();
            processorMock.Setup(p => p.ProcessItemAsync(1))
                .ReturnsAsync((true, ""));
            processorMock.Setup(p => p.ProcessItemAsync(2))
                .ReturnsAsync((false, "Item 2 failed due to validation error"));

            var strategy = new BatchJobStrategy(processorMock.Object);

            // Act
            await strategy.Execute(job, items);

            // Assert
            job.Logs.Should().HaveCount(2);

            var log1 = job.Logs[0];
            log1.ItemId.Should().Be(1);
            log1.Success.Should().BeTrue();

            var log2 = job.Logs[1];
            log2.ItemId.Should().Be(2);
            log2.Success.Should().BeFalse();
            log2.Description.Should().Be("Item 2 failed due to validation error");
        }

        [Fact]
        public async Task Execute_FailOnFirstItem_NoOtherItemsProcessed()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3 };
            var job = CreateJob();

            var processorMock = new Mock<IItemProcessingService>();
            processorMock.Setup(p => p.ProcessItemAsync(1))
                .ReturnsAsync((false, "First item failed"));
            processorMock.Setup(p => p.ProcessItemAsync(It.IsIn(2, 3)))
                .ReturnsAsync((true, "Should not be called"));

            var strategy = new BatchJobStrategy(processorMock.Object);

            // Act
            await strategy.Execute(job, items);

            // Assert
            job.ProcessedItems.Should().Be(0);
            job.FailedItems.Should().Be(1);
            job.Logs.Should().HaveCount(1);
            job.Logs[0].ItemId.Should().Be(1);
            job.Logs[0].Success.Should().BeFalse();

            processorMock.Verify(p => p.ProcessItemAsync(1), Times.Once);
            processorMock.Verify(p => p.ProcessItemAsync(It.IsIn(2, 3)), Times.Never);
        }

        /// <summary>
        /// Creates a new job instance with default values for testing.
        /// </summary>
        private static Job CreateJob() => new()
        {
            Id = Guid.NewGuid(),
            Logs = new List<JobItemLog>(),
            ProcessedItems = 0,
            FailedItems = 0
        };
    }
}