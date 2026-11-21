using FluentAssertions;
using server.infrastructure.chat.images;
using Xunit;

namespace server.tests;

public class S3RequestSignerTests
{
    // AWS のドキュメントにある Signature Version 4 の例（GET Object）
    [Fact]
    public void CreateAuthorization_WithAwsDocumentExample_ReturnsExpectedSignature()
    {
        var headers = new Dictionary<string, string>
        {
            ["Host"] = "examplebucket.s3.amazonaws.com",
            ["Range"] = "bytes=0-9",
            ["x-amz-content-sha256"] = S3RequestSigner.EmptyPayloadHash,
            ["x-amz-date"] = "20130524T000000Z"
        };

        var authorization = S3RequestSigner.CreateAuthorization(
            "GET",
            "/test.txt",
            headers,
            S3RequestSigner.EmptyPayloadHash,
            "AKIAIOSFODNN7EXAMPLE",
            "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
            "us-east-1",
            "s3",
            new DateTimeOffset(2013, 5, 24, 0, 0, 0, TimeSpan.Zero));

        authorization.Should().Be(
            "AWS4-HMAC-SHA256 Credential=AKIAIOSFODNN7EXAMPLE/20130524/us-east-1/s3/aws4_request, "
            + "SignedHeaders=host;range;x-amz-content-sha256;x-amz-date, "
            + "Signature=f0e8bdb87c964420e857bd35b5d6ed310bd44f0170aba48dd91039c6036bdb41");
    }

    [Fact]
    public void HashHex_WithEmptyPayload_ReturnsEmptyPayloadHash()
    {
        S3RequestSigner.HashHex([]).Should().Be(S3RequestSigner.EmptyPayloadHash);
    }

    [Fact]
    public void FormatAmzDate_ReturnsBasicIso8601InUtc()
    {
        var now = new DateTimeOffset(2026, 11, 21, 9, 30, 15, TimeSpan.FromHours(9));

        S3RequestSigner.FormatAmzDate(now).Should().Be("20261121T003015Z");
    }

    [Fact]
    public void EncodePath_KeepsSlashesAndEncodesOtherReservedCharacters()
    {
        S3RequestSigner.EncodePath("bucket/threads/a b+c.png").Should().Be("bucket/threads/a%20b%2Bc.png");
    }
}
