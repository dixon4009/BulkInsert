using JobHandling.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace JobHandling.Application.DTOs
{
    /// <summary>
    /// Standardized error response returned by the API for all error scenarios.
    /// Used by the global exception handling middleware and individual controllers.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Unique identifier for tracing the request through the application logs.
        /// Matches HttpContext.TraceIdentifier for correlation with Serilog traces.
        /// </summary>
        public string TraceId { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable error message describing what went wrong.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The exception type name (e.g., "JobNotFoundException", "JobHandlingException").
        /// Helps clients identify the category of error for appropriate handling.
        /// </summary>
        public string ErrorType { get; set; } = string.Empty;

        /// <summary>
        /// HTTP status code associated with the error response.
        /// Complements the response status code for API contract clarity.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Dictionary of field-level validation errors, keyed by field name.
        /// Value is an array of error messages for each field.
        /// Example: { "jobType": ["Invalid job type"], "items": ["Items cannot be empty"] }
        /// </summary>
        public Dictionary<string, string[]> ValidationErrors { get; set; } = new();

        public static ErrorResponse ValidationFailed(string traceId, string[] errorMessages, int statusCode = StatusCodes.Status400BadRequest)
        {
            return new ErrorResponse
            {
                TraceId = traceId,
                Message = "Validation failed",
                ErrorType = nameof(JobHandlingException),
                StatusCode = statusCode,
                ValidationErrors = new() { { "jobType", errorMessages } }
            };
        }
    }
}