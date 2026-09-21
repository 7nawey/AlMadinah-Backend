using System.Net;
using Microsoft.AspNetCore.Mvc;
using AlMadina.Application.Common;
using AlMadina.Application.Exceptions;

namespace AlMadina.API.Middleware
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
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found: {Path}", context.Request.Path);
                await WriteProblemDetailsAsync(context, HttpStatusCode.NotFound, ex.Message);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed: {Path}", context.Request.Path);
                await WriteProblemDetailsAsync(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access: {Path}", context.Request.Path);
                await WriteProblemDetailsAsync(context, HttpStatusCode.Unauthorized, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception at {Path}: {Message}", context.Request.Path, ex.Message);
                await WriteProblemDetailsAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        private static async Task WriteProblemDetailsAsync(HttpContext context, HttpStatusCode statusCode, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = ApiResponse<object>.Fail(message);
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
