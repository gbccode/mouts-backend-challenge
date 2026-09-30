using FluentValidation;
using System.Text.Json;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware
{
    public class ValidationExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ValidationExceptionMiddleware(RequestDelegate next)
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Default 500
            int status = StatusCodes.Status500InternalServerError;
            var payload = new
            {
                type = "ServerError",
                error = "Unexpected error",
                detail = "An unexpected error occurred."
            };

            // FluentValidation errors -> 400 ValidationError
            if (exception is ValidationException vf)
            {
                status = StatusCodes.Status400BadRequest;
                var first = vf.Errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid request.";
                payload = new
                {
                    type = "ValidationError",
                    error = "Invalid sale",
                    detail = first
                };
            }
            // Domain errors -> generally 400; special case: edit of cancelled sale -> 409
            else if (exception is DomainException de)
            {
                var msg = de.Message ?? "Domain error";

                // Specific mapping for "edit cancelled sale" -> 409 Conflict
                if (msg.Contains("update a cancelled", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("update items of a cancelled", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("edit a cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    status = StatusCodes.Status409Conflict;
                    payload = new
                    {
                        type = "Conflict",
                        error = "Conflict",
                        detail = msg
                    };
                }
                else
                {
                    status = StatusCodes.Status400BadRequest;
                    payload = new
                    {
                        type = "ValidationError",
                        error = "Invalid sale",
                        detail = msg
                    };
                }
            }
            // Not found -> 404
            else if (exception is KeyNotFoundException knf)
            {
                status = StatusCodes.Status404NotFound;
                payload = new
                {
                    type = "NotFound",
                    error = "Not found",
                    detail = knf.Message
                };
            }
            // Unauthorized from elsewhere will be handled by auth pipeline (401).
            else
            {
                // Keep generic 500 payload; log is done by host logging
            }

            context.Response.StatusCode = status;
            var json = JsonSerializer.Serialize(payload, jsonOptions);
            return context.Response.WriteAsync(json);
        }
    }
}
