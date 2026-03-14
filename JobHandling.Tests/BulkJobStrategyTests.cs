using FluentAssertions;
using JobHandling.Application.Strategies;
using JobHandling.Domain.Entities;
using JobHandling.Domain.Interfaces;
using Moq;

namespace JobHandling.Tests
{
    /// <summary>
    /// Unit tests for BulkJobStrategy which processes all items regardless of failures.
    /// </summary>
    public class BulkJobStrategyTests
    {
        [Fact]
        public async Task Execute_ProcessesAllItemsAndRecordsResults()
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

            var strategy = new BulkJobStrategy(processorMock.Object);

            // Act
            await strategy.Execute(job, items);

            // Assert
            job.ProcessedItems.Should().Be(2);
            job.FailedItems.Should().Be(1);
            job.Logs.Should().HaveCount(3);
            job.Logs.Should().Contain(l => l.ItemId == 1 && l.Success);
            job.Logs.Should().Contain(l => l.ItemId == 5 && !l.Success && l.Description == "Failed");
            job.Logs.Should().Contain(l => l.ItemId == 3 && l.Success);
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