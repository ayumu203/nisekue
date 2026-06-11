using Microsoft.AspNetCore.Diagnostics;
using server.domain;

namespace server.infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, message) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "対象データが見つかりません。"),
            DomainException => (StatusCodes.Status400BadRequest, exception.Message),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "リクエストを処理できませんでした。"),
            ArgumentException or ArgumentOutOfRangeException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "予期しないエラーが発生しました。")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception: {Path}", httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation(
                "Handled exception: {Path} {ExceptionType}: {Message}",
                httpContext.Request.Path,
                exception.GetType().Name,
                exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new { message }, cancellationToken);
        return true;
    }
}
