using System.Net;
using System.Text.Json;
using Auth.Application.Common.Exceptions;

namespace Auth.Api.Middleware;

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var result = string.Empty;

        switch (exception)
        {
            case ValidationException validationException:
                code = HttpStatusCode.BadRequest;
                result = JsonSerializer.Serialize(new
                {
                    errors = validationException.Errors
                });
                break;

            case NotFoundException:
                code = HttpStatusCode.NotFound;
                result = JsonSerializer.Serialize(new
                {
                    error = exception.Message
                });
                break;

            case UnauthorizedException:
                code = HttpStatusCode.Unauthorized;
                result = JsonSerializer.Serialize(new
                {
                    error = exception.Message
                });
                break;

            case ForbiddenException:
                code = HttpStatusCode.Forbidden;
                result = JsonSerializer.Serialize(new
                {
                    error = exception.Message
                });
                break;

            default:
                _logger.LogError(exception, "An unhandled exception occurred.");
                result = JsonSerializer.Serialize(new
                {
                    error = "An error occurred while processing your request."
                });
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        if (!string.IsNullOrEmpty(result))
        {
            await context.Response.WriteAsync(result);
        }
    }
}
