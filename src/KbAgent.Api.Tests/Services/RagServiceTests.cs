using KbAgent.Api.Configuration;
using KbAgent.Api.Models;
using KbAgent.Api.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace KbAgent.Api.Tests.Services;

public class RagServiceTests
{
    private const string Backend = "http://laptop-a:11434";

    [Fact]
    public async Task AskAsync_RetrievesContextAndReturnsGeneratedAnswer()
    {
        var ollamaClient = new Mock<IOllamaClient>();
        var loadBalancer = new Mock<IOllamaLoadBalancer>();
        var vectorStore = new Mock<IVectorStore>();
        var chunkingService = new Mock<IChunkingService>();

        loadBalancer.Setup(b => b.GetAvailableBackendAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Backend);
        ollamaClient
            .Setup(c => c.EmbedAsync(Backend, "embed-model", "What is the refund policy?", It.IsAny<CancellationToken>()))
            .ReturnsAsync([0.1f, 0.2f]);

        var sources = new List<SourceSnippet> { new("policy.md", "Refunds within 30 days.", 0.9f) };
        vectorStore
            .Setup(v => v.SearchAsync(new[] { 0.1f, 0.2f }, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);

        ollamaClient
            .Setup(c => c.GenerateAsync(Backend, "gen-model", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("You can get a refund within 30 days. [1]");

        var service = CreateService(ollamaClient, loadBalancer, vectorStore, chunkingService);

        var result = await service.AskAsync("What is the refund policy?");

        Assert.Equal("You can get a refund within 30 days. [1]", result.Answer);
        Assert.Same(sources, result.Sources);
        ollamaClient.Verify(c => c.GenerateAsync(Backend, "gen-model", It.Is<string>(p => p.Contains("Refunds within 30 days.")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_ChunksEmbedsAndUpsertsEachChunk()
    {
        var ollamaClient = new Mock<IOllamaClient>();
        var loadBalancer = new Mock<IOllamaLoadBalancer>();
        var vectorStore = new Mock<IVectorStore>();
        var chunkingService = new Mock<IChunkingService>();

        loadBalancer.Setup(b => b.GetAvailableBackendAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Backend);
        chunkingService.Setup(c => c.Chunk("full document text")).Returns(["chunk one", "chunk two"]);
        ollamaClient
            .Setup(c => c.EmbedAsync(Backend, "embed-model", "chunk one", It.IsAny<CancellationToken>()))
            .ReturnsAsync([1f]);
        ollamaClient
            .Setup(c => c.EmbedAsync(Backend, "embed-model", "chunk two", It.IsAny<CancellationToken>()))
            .ReturnsAsync([2f]);

        var service = CreateService(ollamaClient, loadBalancer, vectorStore, chunkingService);

        var result = await service.IngestAsync("policy.md", "full document text");

        Assert.Equal("policy.md", result.Source);
        Assert.Equal(2, result.ChunkCount);
        vectorStore.Verify(v => v.EnsureCollectionAsync(It.IsAny<CancellationToken>()), Times.Once);
        vectorStore.Verify(
            v => v.UpsertChunksAsync(
                It.Is<IReadOnlyList<DocumentChunk>>(chunks =>
                    chunks.Count == 2 &&
                    chunks[0].Text == "chunk one" &&
                    chunks[1].Text == "chunk two"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IngestAsync_NoChunksProduced_DoesNotCallUpsert()
    {
        var ollamaClient = new Mock<IOllamaClient>();
        var loadBalancer = new Mock<IOllamaLoadBalancer>();
        var vectorStore = new Mock<IVectorStore>();
        var chunkingService = new Mock<IChunkingService>();

        loadBalancer.Setup(b => b.GetAvailableBackendAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Backend);
        chunkingService.Setup(c => c.Chunk(It.IsAny<string>())).Returns([]);

        var service = CreateService(ollamaClient, loadBalancer, vectorStore, chunkingService);

        var result = await service.IngestAsync("empty.md", "   ");

        Assert.Equal(0, result.ChunkCount);
        vectorStore.Verify(v => v.UpsertChunksAsync(It.IsAny<IReadOnlyList<DocumentChunk>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static RagService CreateService(
        Mock<IOllamaClient> ollamaClient,
        Mock<IOllamaLoadBalancer> loadBalancer,
        Mock<IVectorStore> vectorStore,
        Mock<IChunkingService> chunkingService)
    {
        var ollamaOptions = Options.Create(new OllamaOptions
        {
            GenerationModel = "gen-model",
            EmbeddingModel = "embed-model",
        });
        var ragOptions = Options.Create(new RagOptions { TopK = 4 });

        return new RagService(
            ollamaClient.Object,
            loadBalancer.Object,
            vectorStore.Object,
            chunkingService.Object,
            ollamaOptions,
            ragOptions);
    }
}
