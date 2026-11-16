using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using server.application.debug;
using server.domain;
using server.infrastructure;
using Xunit;

namespace server.tests;

public class ApiExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WhenTimeoutIsNested_ReturnsServiceUnavailable()
    {
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new InvalidOperationException("outer", new Exception("middle", new TimeoutException("db timeout"))),
            CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain("データベース処理が混み合っています");
    }

    [Fact]
    public async Task TryHandleAsync_WhenDebugOperationFails_ReturnsExceptionDetails()
    {
        var httpContext = CreateHttpContext([DebugOperationMetadata.Instance]);

        await HandleAsync(httpContext, new InvalidOperationException("outer", new ArgumentNullException("value", "inner")));

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var body = await ReadBodyAsync(httpContext);
        body.Should().Contain("予期しないエラーが発生しました。");
        body.Should().Contain("InvalidOperationException: outer");
        body.Should().Contain("ArgumentNullException: inner");
    }

    [Fact]
    public async Task TryHandleAsync_WhenNormalEndpointFails_DoesNotReturnExceptionDetails()
    {
        var httpContext = CreateHttpContext([]);

        await HandleAsync(httpContext, new InvalidOperationException("secret detail"));

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var body = await ReadBodyAsync(httpContext);
        body.Should().Contain("予期しないエラーが発生しました。");
        body.Should().NotContain("secret detail");
        body.Should().NotContain("InvalidOperationException");
    }

    [Fact]
    public async Task TryHandleAsync_WhenDebugOperationFailsWithDomainException_ReturnsOnlyDomainMessage()
    {
        var httpContext = CreateHttpContext([DebugOperationMetadata.Instance]);

        await HandleAsync(httpContext, new DomainException("条件を満たしていません。"));

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var body = await ReadBodyAsync(httpContext);
        body.Should().Contain("条件を満たしていません。");
        body.Should().NotContain("DomainException");
    }

    private static DefaultHttpContext CreateHttpContext(object[] endpointMetadata)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature
        {
            Error = new Exception(),
            Path = "/test",
            Endpoint = new Endpoint(null, new EndpointMetadataCollection(endpointMetadata), "test"),
        });
        return httpContext;
    }

    private static async Task HandleAsync(HttpContext httpContext, Exception exception)
    {
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);
        handled.Should().BeTrue();
    }

    private static async Task<string> ReadBodyAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
