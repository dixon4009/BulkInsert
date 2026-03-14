using Serilog;
using Serilog.Context;
using System.Diagnostics;

namespace JobHandling.API.Middlewares
{
    /// <summary>
    /// Middleware for logging HTTP requests and responses.
    /// Captures request/response details, performance metrics, and enriches logs with trace context.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var traceId = context.TraceIdentifier;

            using (LogContext.PushProperty("TraceId", traceId))
            using (LogContext.PushProperty("RequestMethod", context.Request.Method))
            using (LogContext.PushProperty("RequestPath", context.Request.Path))
            {
                try
                {
                    Log.Information(
                        "HTTP {RequestMethod} {RequestPath} started",
                        context.Request.Method,
                        context.Request.Path);

                    // Capture original response body stream
                    var originalBodyStream = context.Response.Body;

                    using (var responseBody = new MemoryStream())
                    {
                        context.Response.Body = responseBody;

                        // Call next middleware
                        await _next(context);

                        stopwatch.Stop();

                        // Log response details
                        Log.Information(
                            "HTTP {RequestMethod} {RequestPath} completed with status {StatusCode} in {ElapsedMilliseconds}ms",
                            context.Request.Method,
                            context.Request.Path,
                            context.Response.StatusCode,
                            stopwatch.ElapsedMilliseconds);

                        // Copy response body back to original stream
                        responseBody.Seek(0, SeekOrigin.Begin);
                        await responseBody.CopyToAsync(originalBodyStream);
                        context.Response.Body = originalBodyStream;
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    
                    Log.Error(
                        ex,
                        "HTTP {RequestMethod} {RequestPath} failed after {ElapsedMilliseconds}ms",
                        context.Request.Method,
                        context.Request.Path,
                        stopwatch.ElapsedMilliseconds);

                    throw;
                }
            }
        }
    }
}