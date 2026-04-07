using JobHandling.Application;
using JobHandling.Application.DTOs;
using JobHandling.Application.Services;
using JobHandling.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobHandling.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/jobs")]
    public class JobsController : ControllerBase
    {
        private readonly IJobService _service;
        private readonly ILogger<JobsController> _logger;

        public JobsController(IJobService service, ILogger<JobsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Starts a new job for processing.
        /// </summary>
        /// <param name="request">The job request containing type and items to process</param>
        /// <returns>The created job ID</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartJob([FromBody] StartJobRequest request)
        {
            _logger.LogInformation(
                "StartJob endpoint called with JobType: {JobType}, ItemCount: {ItemCount}",
                request?.JobType,
                request?.Items?.Count ?? 0);

            var jobId = await _service.StartJob(request);
            return Ok(jobId);
        }

        /// <summary>
        /// Gets the status of a job.
        /// </summary>
        /// <param name="id">The job ID</param>
        /// <returns>The job status</returns>
        [HttpGet("{id}/status")]
        [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStatus(Guid id)
        {
            _logger.LogInformation("GetStatus endpoint called for job {JobId}", id);

            var status = await _service.GetStatus(id);
            return Ok(status);
        }

        /// <summary>
        /// Gets the logs for a job.
        /// </summary>
        /// <param name="id">The job ID</param>
        /// <returns>The job logs</returns>
        [HttpGet("{id}/logs")]
        [ProducesResponseType(typeof(List<JobItemLog>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetLogs(Guid id)
        {
            _logger.LogInformation("GetLogs endpoint called for job {JobId}", id);

            var logs = await _service.GetLogs(id);
            return Ok(logs);
        }
    }
}