using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using server.application.debug;
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

        if (statusCode >= StatusCodes.Status500InternalServerError && IsDebugOperation(httpContext))
        {
            // デバッグ用の操作は一般ユーザーには見えないので、原因が分かるよう例外の中身を返す
            message = $"{message}\n{DescribeException(exception)}";
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

    /// <summary>
    /// デバッグ用の操作（DebugOperationMetadata が付いたエンドポイント）で起きた例外か。
    /// UseExceptionHandler はハンドラーを呼ぶ前に HttpContext のエンドポイントを消すので、IExceptionHandlerFeature から取る。
    /// </summary>
    private static bool IsDebugOperation(HttpContext httpContext)
    {
        var endpoint = httpContext.Features.Get<IExceptionHandlerFeature>()?.Endpoint ?? httpContext.GetEndpoint();
        return endpoint?.Metadata.GetMetadata<DebugOperationMetadata>() is not null;
    }

    /// <summary>例外の種類とメッセージを InnerException までたどって並べる（スタックトレースは含めない）。</summary>
    private static string DescribeException(Exception exception)
    {
        var builder = new StringBuilder();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (builder.Length > 0)
            {
                builder.Append("\n→ ");
            }

            builder.Append(current.GetType().Name).Append(": ").Append(current.Message);
        }

        return builder.ToString();
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
