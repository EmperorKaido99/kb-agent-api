namespace KbAgent.Api.Services.Extraction;

/// <summary>Handles .txt and .md files — read as-is.</summary>
public sealed class PlainTextExtractor : IDocumentTextExtractor
{
    public IReadOnlyCollection<string> SupportedExtensions { get; } = [".txt", ".md"];

    public async Task<string> ExtractTextAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(fileStream, leaveOpen: true);
        return await reader.ReadToEndAsync(ct);
    }
}
