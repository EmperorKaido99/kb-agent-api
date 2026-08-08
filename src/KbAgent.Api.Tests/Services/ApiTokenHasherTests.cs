using KbAgent.Api.Services;

namespace KbAgent.Api.Tests.Services;

public class ApiTokenHasherTests
{
    [Fact]
    public void GenerateToken_ProducesA64CharacterHexToken()
    {
        var (rawToken, _) = ApiTokenHasher.GenerateToken();

        Assert.Equal(64, rawToken.Length);
        Assert.Matches("^[0-9a-f]{64}$", rawToken);
    }

    [Fact]
    public void GenerateToken_TokenHashMatchesHashOfRawToken()
    {
        var (rawToken, tokenHash) = ApiTokenHasher.GenerateToken();

        Assert.Equal(ApiTokenHasher.Hash(rawToken), tokenHash);
    }

    [Fact]
    public void GenerateToken_TwoCallsProduceDifferentTokens()
    {
        var (tokenA, _) = ApiTokenHasher.GenerateToken();
        var (tokenB, _) = ApiTokenHasher.GenerateToken();

        Assert.NotEqual(tokenA, tokenB);
    }

    [Fact]
    public void Hash_IsDeterministic()
    {
        Assert.Equal(ApiTokenHasher.Hash("my-token"), ApiTokenHasher.Hash("my-token"));
    }

    [Fact]
    public void Hash_DifferentInputsProduceDifferentHashes()
    {
        Assert.NotEqual(ApiTokenHasher.Hash("token-a"), ApiTokenHasher.Hash("token-b"));
    }

    [Fact]
    public void FixedTimeEquals_EqualStrings_ReturnsTrue()
    {
        Assert.True(ApiTokenHasher.FixedTimeEquals("abc123", "abc123"));
    }

    [Fact]
    public void FixedTimeEquals_DifferentStrings_ReturnsFalse()
    {
        Assert.False(ApiTokenHasher.FixedTimeEquals("abc123", "xyz789"));
    }
}
