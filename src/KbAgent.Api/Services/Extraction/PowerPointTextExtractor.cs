using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DrawingText = DocumentFormat.OpenXml.Drawing.Text;

namespace KbAgent.Api.Services.Extraction;

/// <summary>Handles .pptx files via the OOXML SDK — text runs from each slide, one slide per block.</summary>
public sealed class PowerPointTextExtractor : IDocumentTextExtractor
{
    public IReadOnlyCollection<string> SupportedExtensions { get; } = [".pptx"];

    public Task<string> ExtractTextAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var document = PresentationDocument.Open(fileStream, isEditable: false);
        var presentationPart = document.PresentationPart;
        if (presentationPart is null)
        {
            return Task.FromResult(string.Empty);
        }

        var sb = new StringBuilder();
        var slideIndex = 0;
        foreach (var slidePart in presentationPart.SlideParts)
        {
            ct.ThrowIfCancellationRequested();
            slideIndex++;

            if (slidePart.Slide is null)
            {
                continue;
            }

            var slideTexts = slidePart.Slide.Descendants<DrawingText>()
                .Select(t => t.Text ?? string.Empty)
                .Where(t => !string.IsNullOrWhiteSpace(t));

            var slideText = string.Join(" ", slideTexts);
            if (!string.IsNullOrWhiteSpace(slideText))
            {
                sb.AppendLine($"[Slide {slideIndex}] {slideText}");
            }
        }

        return Task.FromResult(sb.ToString());
    }
}
