using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using UserManagement.API.Exceptions;

namespace UserManagement.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");

                context.Response.ContentType = "application/problem+json";

                var problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "An error occurred while processing your request.",
                    Detail = _env.IsDevelopment() ? ex.Message : "Internal Server Error",
                    Instance = context.Request.Path,
                    Status = (int)HttpStatusCode.InternalServerError
                };

                switch (ex)
                {
                    case NotFoundException notFoundEx:
                        problemDetails.Title = "Resource not found.";
                        problemDetails.Status = (int)HttpStatusCode.NotFound;
                        problemDetails.Detail = notFoundEx.Message;
                        break;

                    case UnauthorizedAccessException:
                        problemDetails.Title = "Unauthorized access.";
                        problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                        break;
                }

                context.Response.StatusCode = problemDetails.Status ?? 500;

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(problemDetails, options);
                await context.Response.WriteAsync(json);
            }
        }
    }
}
