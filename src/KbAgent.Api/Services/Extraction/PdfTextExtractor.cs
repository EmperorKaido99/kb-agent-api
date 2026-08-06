using System.Text;
using UglyToad.PdfPig;

namespace KbAgent.Api.Services.Extraction;

/// <summary>
/// Handles .pdf files via PdfPig — extracts each page's text layer. Scanned/image-only pages with no text layer
/// are skipped (rendering + OCR of PDF pages is out of scope; see memorybank/components/kb-ingest.md).
/// </summary>
public sealed class PdfTextExtractor(ILogger<PdfTextExtractor> logger) : IDocumentTextExtractor
{
    public IReadOnlyCollection<string> SupportedExtensions { get; } = [".pdf"];

    public Task<string> ExtractTextAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var document = PdfDocument.Open(fileStream);
        var sb = new StringBuilder();
        var pagesWithNoText = 0;

        foreach (var page in document.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            var text = page.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                pagesWithNoText++;
                continue;
            }

            sb.AppendLine($"[Page {page.Number}] {text}");
        }

        if (pagesWithNoText > 0)
        {
            logger.LogInformation(
                "{PageCount} page(s) had no extractable text layer (likely scanned images) and were skipped.",
                pagesWithNoText);
        }

        return Task.FromResult(sb.ToString());
    }
}
