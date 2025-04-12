using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Diagnostics;

namespace SmartInventoryManagement.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred during request {RequestPath}", context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.StatusCode = exception switch
            {
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            // If it's an API request (AJAX or similar), return JSON response
            if (IsApiRequest(context))
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    error = GetErrorMessage(exception),
                    message = exception.Message,
                    timestamp = DateTime.UtcNow
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            else
            {
                // For regular requests, redirect to the appropriate error page
                var statusCode = context.Response.StatusCode;
                var path = $"/Error/{statusCode}";
                context.Response.Redirect(path);
            }
        }

        private bool IsApiRequest(HttpContext context)
        {
            return context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                   context.Request.Headers["Accept"].ToString().Contains("application/json") ||
                   context.Request.Path.StartsWithSegments("/api");
        }

        private string GetErrorMessage(Exception exception) => exception switch
        {
            UnauthorizedAccessException => "You are not authorized to access this resource.",
            KeyNotFoundException => "The requested resource was not found.",
            InvalidOperationException => "The request could not be processed due to invalid operation.",
            ArgumentException => "The request contains invalid arguments.",
            _ => "An error occurred while processing your request."
        };
    }
} 