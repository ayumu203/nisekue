using Microsoft.AspNetCore.Diagnostics;

namespace server.infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, message) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "対象データが見つかりません。"),
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
            logger.LogWarning(exception, "Handled exception: {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new { message }, cancellationToken);
        return true;
    }
}
