using JobHandling.Application.DTOs;
using JobHandling.Domain.Exceptions;
using Serilog;

namespace JobHandling.API.Middlewares
{
    /// <summary>
    /// Global exception handling middleware that catches and handles exceptions from the request pipeline.
    /// Uses Serilog for consistent logging across the application.
    /// </summary>
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                TraceId = context.TraceIdentifier,
                ErrorType = exception.GetType().Name
            };

            return exception switch
            {
                JobNotFoundException jobNotFound =>
                    HandleJobNotFoundException(context, jobNotFound, response),

                JobHandlingException jobHandling =>
                    HandleJobHandlingException(context, jobHandling, response),

                _ => HandleGeneralException(context, exception, response)
            };
        }

        private static Task HandleJobNotFoundException(HttpContext context,
                                                       JobNotFoundException ex,
                                                       ErrorResponse response)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            response.StatusCode = StatusCodes.Status404NotFound;
            response.Message = ex.Message;

            return context.Response.WriteAsJsonAsync(response);
        }

        private static Task HandleJobHandlingException(HttpContext context,
                                                       JobHandlingException ex,
                                                       ErrorResponse response)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            response.StatusCode = StatusCodes.Status400BadRequest;
            response.Message = ex.Message;

            return context.Response.WriteAsJsonAsync(response);
        }

        private static Task HandleGeneralException(HttpContext context,
                                                   Exception ex,
                                                   ErrorResponse response)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            response.StatusCode = StatusCodes.Status500InternalServerError;
            response.Message = "An internal error occurred. Please contact support.";

            return context.Response.WriteAsJsonAsync(response);
        }
    }
}