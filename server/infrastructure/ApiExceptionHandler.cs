using Microsoft.AspNetCore.Diagnostics;
using server.domain;

namespace server.infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, message) = ResolveResponse(exception);

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

    private static (int StatusCode, string Message) ResolveResponse(Exception exception)
    {
        if (FindInnerException<TimeoutException>(exception) is not null)
        {
            return (StatusCodes.Status503ServiceUnavailable, "データベース処理が混み合っています。時間をおいて再度お試しください。");
        }

        return exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "対象データが見つかりません。"),
            DomainException => (StatusCodes.Status400BadRequest, exception.Message),
            ArgumentException or ArgumentOutOfRangeException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "予期しないエラーが発生しました。")
        };
    }

    private static TException? FindInnerException<TException>(Exception exception)
        where TException : Exception
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TException matched)
            {
                return matched;
            }
        }

        return null;
    }
}
