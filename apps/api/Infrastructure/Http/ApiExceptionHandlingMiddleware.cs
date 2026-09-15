namespace Documate.Api.Infrastructure.Http;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Prevents unhandled exceptions (EF stacks, etc.) from leaking to API clients.
/// Logs the full exception server-side; returns a stable JSON <c>{ error }</c> body.
/// </summary>
public sealed class ApiExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ApiExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            logger.LogError(ex, "Unhandled exception after response started for {Method} {Path}",
                context.Request.Method, context.Request.Path);
            throw ex;
        }

        logger.LogError(ex, "Unhandled exception for {Method} {Path}",
            context.Request.Method, context.Request.Path);

        var (statusCode, message) = MapException(ex);
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new { error = message });
    }

    private static (int StatusCode, string Message) MapException(Exception ex) => ex switch
    {
        OperationCanceledException => (
            StatusCodes.Status499ClientClosedRequest,
            "The request was cancelled."),
        DbUpdateConcurrencyException => (
            StatusCodes.Status409Conflict,
            "The resource was modified concurrently. Please retry."),
        // Intentional domain messages (queue missing, validation) — keep short text, never stacks.
        InvalidOperationException ioe when IsSafeClientMessage(ioe.Message) => (
            StatusCodes.Status400BadRequest,
            ioe.Message),
        _ => (
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.")
    };

    private static bool IsSafeClientMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Length > 300)
            return false;
        if (message.Contains('\n') || message.Contains('\r'))
            return false;
        if (message.Contains("Exception", StringComparison.OrdinalIgnoreCase))
            return false;
        if (message.Contains(" at ", StringComparison.Ordinal))
            return false;
        if (message.Contains("Microsoft.", StringComparison.Ordinal)
            || message.Contains("System.", StringComparison.Ordinal))
            return false;
        return true;
    }
}
