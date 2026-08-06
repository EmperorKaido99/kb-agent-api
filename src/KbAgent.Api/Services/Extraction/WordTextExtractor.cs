using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace KbAgent.Api.Services.Extraction;

/// <summary>Handles .docx files via the OOXML SDK — one paragraph of extracted text per line.</summary>
public sealed class WordTextExtractor : IDocumentTextExtractor
{
    public IReadOnlyCollection<string> SupportedExtensions { get; } = [".docx"];

    public Task<string> ExtractTextAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var document = WordprocessingDocument.Open(fileStream, isEditable: false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return Task.FromResult(string.Empty);
        }

        var sb = new StringBuilder();
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            ct.ThrowIfCancellationRequested();
            var text = paragraph.InnerText;
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
            }
        }

        return Task.FromResult(sb.ToString());
    }
}
