namespace KbAgent.Api.Services.Extraction;

public interface IDocumentTextExtractorFactory
{
    /// <summary>Extensions (lowercase, with dot) that some registered extractor can handle.</summary>
    IReadOnlyCollection<string> SupportedExtensions { get; }

    IDocumentTextExtractor? GetExtractor(string filePath);
}

public sealed class DocumentTextExtractorFactory(IEnumerable<IDocumentTextExtractor> extractors) : IDocumentTextExtractorFactory
{
    private readonly Dictionary<string, IDocumentTextExtractor> _byExtension = extractors
        .SelectMany(extractor => extractor.SupportedExtensions.Select(ext => (ext, extractor)))
        .ToDictionary(x => x.ext, x => x.extractor, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> SupportedExtensions => _byExtension.Keys;

    public IDocumentTextExtractor? GetExtractor(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return !string.IsNullOrEmpty(extension) && _byExtension.TryGetValue(extension, out var extractor)
            ? extractor
            : null;
    }
}
