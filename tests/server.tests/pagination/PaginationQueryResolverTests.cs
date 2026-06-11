using FluentAssertions;
using server.shared.pagination;
using Xunit;

namespace server.tests;

public class PaginationQueryResolverTests
{
    [Fact]
    public void TryResolve_WhenPageSizeIsMaxValue_Succeeds()
    {
        var result = PaginationQueryResolver.TryResolve(
            page: 1,
            pageSize: 100,
            limit: null,
            out var offset,
            out var effectiveLimit,
            out var errorMessage);

        result.Should().BeTrue();
        offset.Should().Be(0);
        effectiveLimit.Should().Be(100);
        errorMessage.Should().BeNull();
    }

    [Fact]
    public void TryResolve_WhenPageSizeExceedsMaxValue_ReturnsValidationError()
    {
        var result = PaginationQueryResolver.TryResolve(
            page: 1,
            pageSize: 101,
            limit: null,
            out var offset,
            out var effectiveLimit,
            out var errorMessage);

        result.Should().BeFalse();
        offset.Should().BeNull();
        effectiveLimit.Should().BeNull();
        errorMessage.Should().Be("pageSize は 100 以下を指定してください。");
    }

    [Fact]
    public void TryResolve_WhenPageAndPageSizeOverflow_ReturnsValidationError()
    {
        var result = PaginationQueryResolver.TryResolve(
            page: int.MaxValue,
            pageSize: 2,
            limit: null,
            out var offset,
            out var effectiveLimit,
            out var errorMessage);

        result.Should().BeFalse();
        offset.Should().BeNull();
        effectiveLimit.Should().BeNull();
        errorMessage.Should().Be("page と pageSize の組み合わせが大きすぎます。");
    }
}
