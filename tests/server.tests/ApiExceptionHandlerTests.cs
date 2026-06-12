using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
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
}
