using Microsoft.EntityFrameworkCore;

namespace SkyLearnApi.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            IWebHostEnvironment env)
        {
            _next = next;
            _env = env;
        }

        public async Task Invoke(HttpContext context)
        {
            // Generate a correlation ID for request tracing
            var correlationId = context.TraceIdentifier;
            context.Response.Headers["X-Correlation-Id"] = correlationId;

            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // Client disconnected — not a server error
                Log.Warning("Request cancelled by client. Path: {Path}, Method: {Method}, CorrelationId: {CorrelationId}",
                    context.Request.Path, context.Request.Method, correlationId);
                context.Response.StatusCode = 499; // Client Closed Request
            }
            catch (Exception ex)
            {
                var userId = context.User?.FindFirst("UserId")?.Value ?? "anonymous";
                var innerMsg = GetFullExceptionMessage(ex);

                Log.Error(ex,
                    "Unhandled exception. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}, " +
                    "UserId: {UserId}, QueryString: {QueryString}, ExceptionType: {ExceptionType}, " +
                    "InnerExceptionChain: {InnerExceptionChain}",
                    correlationId, context.Request.Path, context.Request.Method,
                    userId, context.Request.QueryString.ToString(),
                    ex.GetType().FullName, innerMsg);

                var (statusCode, title, detail) = ex switch
                {
                    DbUpdateException dbEx when IsForeignKeyViolation(dbEx) => (
                        StatusCodes.Status409Conflict,
                        "Conflict",
                        "Cannot delete this item because it is linked to other data in the system."
                    ),

                    DbUpdateConcurrencyException => (
                        StatusCodes.Status409Conflict,
                        "Concurrency Conflict",
                        "The data was modified by another user. Please refresh and try again."
                    ),

                    DbUpdateException dbEx => (
                        StatusCodes.Status400BadRequest,
                        "Database Error",
                        _env.IsDevelopment()
                            ? $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}"
                            : "A database error occurred. Please check your data and try again."
                    ),

                    InvalidOperationException => (
                        StatusCodes.Status400BadRequest,
                        "Bad Request",
                        ex.Message
                    ),

                    KeyNotFoundException => (
                        StatusCodes.Status404NotFound,
                        "Not Found",
                        ex.Message
                    ),

                    UnauthorizedAccessException => (
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized",
                        ex.Message
                    ),

                    ArgumentException => (
                        StatusCodes.Status400BadRequest,
                        "Bad Request",
                        ex.Message
                    ),

                    TimeoutException => (
                        StatusCodes.Status504GatewayTimeout,
                        "Request Timeout",
                        "The operation timed out. Please try again later."
                    ),

                    TaskCanceledException => (
                        StatusCodes.Status504GatewayTimeout,
                        "Request Timeout",
                        "The operation timed out. Please try again later."
                    ),

                    NotImplementedException => (
                        StatusCodes.Status501NotImplemented,
                        "Not Implemented",
                        "This feature is not yet available."
                    ),

                    _ => (
                        StatusCodes.Status500InternalServerError,
                        "Internal Server Error",
                        (string?)null
                    )
                };

                context.Response.ContentType = "application/problem+json";
                context.Response.StatusCode = statusCode;

                var problemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = detail ?? (_env.IsDevelopment() ? ex.Message : "An unexpected error occurred. Please contact support with the correlation ID."),
                    Instance = context.Request.Path
                };

                problemDetails.Extensions["correlationId"] = correlationId;
                problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("o");

                if (_env.IsDevelopment() && statusCode >= 500)
                {
                    problemDetails.Extensions["exceptionType"] = ex.GetType().FullName;
                    problemDetails.Extensions["stackTrace"] = ex.StackTrace;
                    if (ex.InnerException != null)
                        problemDetails.Extensions["innerException"] = ex.InnerException.Message;
                }

                await context.Response.WriteAsJsonAsync(problemDetails);
            }
        }

        private static bool IsForeignKeyViolation(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? string.Empty;
            return message.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase)
                || message.Contains("FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetFullExceptionMessage(Exception ex)
        {
            var messages = new List<string>();
            var current = ex;
            while (current != null)
            {
                messages.Add($"[{current.GetType().Name}] {current.Message}");
                current = current.InnerException;
            }
            return string.Join(" → ", messages);
        }
    }
}
