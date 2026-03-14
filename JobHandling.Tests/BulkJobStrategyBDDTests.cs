using FluentAssertions;
using JobHandling.Application.Strategies;
using JobHandling.Domain.Entities;
using JobHandling.Domain.Interfaces;
using Moq;

namespace JobHandling.Tests
{
    /// <summary>
    /// BDD-style tests for BulkJobStrategy behavior.
    /// Scenario: Processing job items in bulk mode
    /// </summary>
    public class BulkJobStrategyBDDTests
    {
        private Job _job;
        private Mock<IItemProcessingService> _processorMock;
        private BulkJobStrategy _strategy;
        private List<int> _itemsToProcess;

        #region Given (Setup)

        private void GivenABulkJobStrategy()
        {
            _processorMock = new Mock<IItemProcessingService>();
            _strategy = new BulkJobStrategy(_processorMock.Object);
        }

        private void GivenANewJob()
        {
            _job = new Job
            {
                Id = Guid.NewGuid(),
                Logs = new List<JobItemLog>(),
                ProcessedItems = 0,
                FailedItems = 0
            };
        }

        private void GivenItemsToProcess(params int[] items)
        {
            _itemsToProcess = items.ToList();
        }

        private void GivenAllItemsWillProcessSuccessfully()
        {
            _processorMock.Setup(p => p.ProcessItemAsync(It.IsAny<int>()))
                .ReturnsAsync((true, "Item processed successfully"));
        }

        private void GivenAllItemsWillFailProcessing()
        {
            _processorMock.Setup(p => p.ProcessItemAsync(It.IsAny<int>()))
                .ReturnsAsync((false, "Processing failed"));
        }

        private void GivenMixedProcessingResults()
        {
            _processorMock.Setup(p => p.ProcessItemAsync(1))
                .ReturnsAsync((true, "Success"));
            _processorMock.Setup(p => p.ProcessItemAsync(2))
                .ReturnsAsync((false, "Item not found"));
            _processorMock.Setup(p => p.ProcessItemAsync(3))
                .ReturnsAsync((true, "Success"));
            _processorMock.Setup(p => p.ProcessItemAsync(4))
                .ReturnsAsync((false, "Invalid data"));
        }

        private void GivenSpecificItemProcessingResults(Dictionary<int, (bool Success, string Description)> results)
        {
            foreach (var result in results)
            {
                _processorMock.Setup(p => p.ProcessItemAsync(result.Key))
                    .ReturnsAsync(result.Value);
            }
        }

        #endregion

        #region When (Action)

        private async Task WhenExecutingTheStrategy()
        {
            await _strategy.Execute(_job, _itemsToProcess);
        }

        #endregion

        #region Then (Assert)

        private void ThenAllItemsShouldBeProcessed()
        {
            _job.ProcessedItems.Should().Be(_itemsToProcess.Count);
            _job.FailedItems.Should().Be(0);
        }

        private void ThenNoItemsShouldBeSuccessful()
        {
            _job.ProcessedItems.Should().Be(0);
            _job.FailedItems.Should().Be(_itemsToProcess.Count);
        }

        private void ThenTheJobShouldHaveLogsForAllItems()
        {
            _job.Logs.Should().HaveCount(_itemsToProcess.Count);
        }

        private void ThenProcessedItemsCountShouldBe(int expectedCount)
        {
            _job.ProcessedItems.Should().Be(expectedCount);
        }

        private void ThenFailedItemsCountShouldBe(int expectedCount)
        {
            _job.FailedItems.Should().Be(expectedCount);
        }

        private void ThenEachLogShouldContainCorrectDescription()
        {
            foreach (var log in _job.Logs)
            {
                log.Description.Should().NotBeNullOrEmpty();
            }
        }

        private void ThenTheProcessorShouldHaveBeenCalledForEachItem()
        {
            _processorMock.Verify(
                p => p.ProcessItemAsync(It.IsAny<int>()),
                Times.Exactly(_itemsToProcess.Count));
        }

        private void ThenItemWithIdShouldHaveStatus(int itemId, bool expectedSuccess)
        {
            var log = _job.Logs.FirstOrDefault(l => l.ItemId == itemId);
            log.Should().NotBeNull();
            log.Success.Should().Be(expectedSuccess);
        }

        #endregion

        #region BDD Tests

        [Fact]
        public async Task Scenario_BulkProcessingAllSuccessfulItems()
        {
            // Given
            GivenABulkJobStrategy();
            GivenANewJob();
            GivenItemsToProcess(1, 2, 3, 4, 5);
            GivenAllItemsWillProcessSuccessfully();

            // When
            await WhenExecutingTheStrategy();

            // Then
            ThenAllItemsShouldBeProcessed();
            ThenTheJobShouldHaveLogsForAllItems();
            ThenEachLogShouldContainCorrectDescription();
            ThenTheProcessorShouldHaveBeenCalledForEachItem();
        }

        [Fact]
        public async Task Scenario_BulkProcessingAllFailedItems()
        {
            // Given
            GivenABulkJobStrategy();
            GivenANewJob();
            GivenItemsToProcess(1, 2, 3);
            GivenAllItemsWillFailProcessing();

            // When
            await WhenExecutingTheStrategy();

            // Then
            ThenNoItemsShouldBeSuccessful();
            ThenTheJobShouldHaveLogsForAllItems();
            ThenEachLogShouldContainCorrectDescription();
            ThenTheProcessorShouldHaveBeenCalledForEachItem();
        }

        [Fact]
        public async Task Scenario_BulkProcessingWithMixedResults()
        {
            // Given
            GivenABulkJobStrategy();
            GivenANewJob();
            GivenItemsToProcess(1, 2, 3, 4);
            GivenMixedProcessingResults();

            // When
            await WhenExecutingTheStrategy();

            // Then
            ThenProcessedItemsCountShouldBe(2);
            ThenFailedItemsCountShouldBe(2);
            ThenTheJobShouldHaveLogsForAllItems();
            ThenItemWithIdShouldHaveStatus(1, true);
            ThenItemWithIdShouldHaveStatus(2, false);
            ThenItemWithIdShouldHaveStatus(3, true);
            ThenItemWithIdShouldHaveStatus(4, false);
        }

        [Fact]
        public async Task Scenario_ContinuesProcessingEvenAfterFailures()
        {
            // Given
            GivenABulkJobStrategy();
            GivenANewJob();
            GivenItemsToProcess(1, 2, 3, 4, 5);
            var results = new Dictionary<int, (bool Success, string Description)>
            {
                { 1, (false, "Failed") },
                { 2, (true, "Success") },
                { 3, (false, "Failed") },
                { 4, (true, "Success") },
                { 5, (false, "Failed") }
            };
            GivenSpecificItemProcessingResults(results);

            // When
            await WhenExecutingTheStrategy();

            // Then
            ThenProcessedItemsCountShouldBe(2);
            ThenFailedItemsCountShouldBe(3);
            _processorMock.Verify(
                p => p.ProcessItemAsync(It.IsAny<int>()),
                Times.Exactly(5),
                "All items should be processed, including after failures");
        }

        [Fact]
        public async Task Scenario_EmptyItemsListProducesNoLogs()
        {
            // Given
            GivenABulkJobStrategy();
            GivenANewJob();
            GivenItemsToProcess();
            GivenAllItemsWillProcessSuccessfully();

            // When
            await WhenExecutingTheStrategy();

            // Then
            _job.ProcessedItems.Should().Be(0);
            _job.FailedItems.Should().Be(0);
            _job.Logs.Should().BeEmpty();
            _processorMock.Verify(
                p => p.ProcessItemAsync(It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task Scenario_LogsContainCorrectItemIdsAndDescriptions()
        {
            // Given
            GivenABulkJobStrategy();
            GivenANewJob();
            GivenItemsToProcess(10, 20, 30);
            var results = new Dictionary<int, (bool Success, string Description)>
            {
                { 10, (true, "Item 10 processed") },
                { 20, (false, "Item 20 error") },
                { 30, (true, "Item 30 processed") }
            };
            GivenSpecificItemProcessingResults(results);

            // When
            await WhenExecutingTheStrategy();

            // Then
            _job.Logs.Should().Contain(l => l.ItemId == 10 && l.Success && l.Description == "Item 10 processed");
            _job.Logs.Should().Contain(l => l.ItemId == 20 && !l.Success && l.Description == "Item 20 error");
            _job.Logs.Should().Contain(l => l.ItemId == 30 && l.Success && l.Description == "Item 30 processed");
        }

        #endregion
    }
}