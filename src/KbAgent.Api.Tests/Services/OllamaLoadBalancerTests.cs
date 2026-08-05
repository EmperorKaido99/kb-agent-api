using KbAgent.Api.Configuration;
using KbAgent.Api.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace KbAgent.Api.Tests.Services;

public class OllamaLoadBalancerTests
{
    [Fact]
    public async Task GetAvailableBackendAsync_NoBackendsConfigured_Throws()
    {
        var client = new Mock<IOllamaClient>();
        var balancer = CreateBalancer(client, []);

        await Assert.ThrowsAsync<InvalidOperationException>(() => balancer.GetAvailableBackendAsync());
    }

    [Fact]
    public async Task GetAvailableBackendAsync_AllBackendsHealthy_RoundRobins()
    {
        var client = new Mock<IOllamaClient>();
        client.Setup(c => c.IsHealthyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var backends = new List<string> { "http://laptop-a:11434", "http://laptop-b:11434" };
        var balancer = CreateBalancer(client, backends);

        var first = await balancer.GetAvailableBackendAsync();
        var second = await balancer.GetAvailableBackendAsync();
        var third = await balancer.GetAvailableBackendAsync();

        Assert.Equal(backends[0], first);
        Assert.Equal(backends[1], second);
        Assert.Equal(backends[0], third);
    }

    [Fact]
    public async Task GetAvailableBackendAsync_OneBackendUnhealthy_SkipsIt()
    {
        var client = new Mock<IOllamaClient>();
        client.Setup(c => c.IsHealthyAsync("http://down:11434", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        client.Setup(c => c.IsHealthyAsync("http://up:11434", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var balancer = CreateBalancer(client, ["http://down:11434", "http://up:11434"]);

        var result = await balancer.GetAvailableBackendAsync();

        Assert.Equal("http://up:11434", result);
    }

    [Fact]
    public async Task GetAvailableBackendAsync_NoHealthyBackends_Throws()
    {
        var client = new Mock<IOllamaClient>();
        client.Setup(c => c.IsHealthyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var balancer = CreateBalancer(client, ["http://a:11434", "http://b:11434"]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => balancer.GetAvailableBackendAsync());
    }

    private static OllamaLoadBalancer CreateBalancer(Mock<IOllamaClient> client, List<string> backends)
    {
        var options = Options.Create(new OllamaOptions { BackendBaseUrls = backends });
        return new OllamaLoadBalancer(client.Object, options);
    }
}
