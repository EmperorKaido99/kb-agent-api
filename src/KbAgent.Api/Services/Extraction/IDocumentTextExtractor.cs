namespace KbAgent.Api.Services.Extraction;

/// <summary>Extracts plain text from one file format, identified by extension (e.g. ".pdf").</summary>
public interface IDocumentTextExtractor
{
    /// <summary>File extensions this extractor handles, lowercase with leading dot (e.g. [".docx"]).</summary>
    IReadOnlyCollection<string> SupportedExtensions { get; }

    Task<string> ExtractTextAsync(Stream fileStream, CancellationToken ct = default);
}
