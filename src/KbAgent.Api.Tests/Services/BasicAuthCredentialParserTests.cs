using System.Text;
using KbAgent.Api.Services;

namespace KbAgent.Api.Tests.Services;

public class BasicAuthCredentialParserTests
{
    [Fact]
    public void TryParse_ValidHeader_ExtractsUsernameAndToken()
    {
        var header = BuildHeader("alice", "secret-token");

        var result = BasicAuthCredentialParser.TryParse(header, out var username, out var token);

        Assert.True(result);
        Assert.Equal("alice", username);
        Assert.Equal("secret-token", token);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bearer sometoken")]
    public void TryParse_MissingOrWrongScheme_ReturnsFalse(string? header)
    {
        var result = BasicAuthCredentialParser.TryParse(header, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_NotValidBase64_ReturnsFalse()
    {
        var result = BasicAuthCredentialParser.TryParse("Basic not-valid-base64!!!", out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_DecodedValueMissingColon_ReturnsFalse()
    {
        var header = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("no-colon-here"));

        var result = BasicAuthCredentialParser.TryParse(header, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_TokenContainingColon_KeepsFullRemainderAsToken()
    {
        var header = BuildHeader("alice", "part1:part2");

        var result = BasicAuthCredentialParser.TryParse(header, out var username, out var token);

        Assert.True(result);
        Assert.Equal("alice", username);
        Assert.Equal("part1:part2", token);
    }

    [Fact]
    public void TryParse_IsCaseInsensitiveForScheme()
    {
        var header = "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("alice:secret"));

        var result = BasicAuthCredentialParser.TryParse(header, out var username, out var token);

        Assert.True(result);
        Assert.Equal("alice", username);
        Assert.Equal("secret", token);
    }

    private static string BuildHeader(string username, string token) =>
        "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{token}"));
}
