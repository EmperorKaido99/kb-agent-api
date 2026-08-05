namespace KbAgent.Api.Services;

public interface IChunkingService
{
    IReadOnlyList<string> Chunk(string text);
}
