using System.Text.Json;
using Khyout.Api.Contracts;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Common;
using Microsoft.Extensions.Hosting;

namespace Khyout.Api.Middleware;

/// <summary>Maps application/domain exceptions to the standard error envelope.</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (RequestValidationException ex)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest, "validation_failed", ex.Message, ex.Errors);
        }
        catch (DomainRuleException ex)
        {
            await WriteAsync(context, StatusCodes.Status409Conflict, ex.Code, ex.Message);
        }
        catch (NotFoundException ex)
        {
            await WriteAsync(context, StatusCodes.Status404NotFound, "not_found", ex.Message);
        }
        catch (ForbiddenException ex)
        {
            await WriteAsync(context, StatusCodes.Status403Forbidden, "forbidden", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            // Surface the exception type/message outside Production to aid development
            // and test diagnostics; production keeps a generic message.
            var inner = ex.InnerException is { } innerException
                ? $" | {innerException.GetType().Name}: {innerException.Message}"
                : string.Empty;
            var message = context.RequestServices.GetService<IHostEnvironment>() is { } environment
                          && !environment.IsProduction()
                ? $"Unhandled {ex.GetType().Name}: {ex.Message}{inner}"
                : "An unexpected error occurred.";

            await WriteAsync(context, StatusCodes.Status500InternalServerError, "server_error", message);
        }
    }

    private static Task WriteAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? details = null)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new ApiResponse<object?>(false, null, new ApiError(code, message, details));
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
