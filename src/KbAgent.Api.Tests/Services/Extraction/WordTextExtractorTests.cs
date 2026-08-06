using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using KbAgent.Api.Services.Extraction;

namespace KbAgent.Api.Tests.Services.Extraction;

public class WordTextExtractorTests
{
    [Fact]
    public async Task ExtractTextAsync_DocxWithParagraphs_ReturnsParagraphText()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(
                new Body(
                    new Paragraph(new Run(new Text("Refunds are available within 30 days of purchase."))),
                    new Paragraph(new Run(new Text("Contact support@example.com for exceptions.")))));
            mainPart.Document.Save();
        }

        stream.Position = 0;
        var extractor = new WordTextExtractor();

        var result = await extractor.ExtractTextAsync(stream);

        Assert.Contains("Refunds are available within 30 days of purchase.", result);
        Assert.Contains("Contact support@example.com for exceptions.", result);
    }

    [Fact]
    public async Task ExtractTextAsync_EmptyDocument_ReturnsEmptyString()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            mainPart.Document.Save();
        }

        stream.Position = 0;
        var extractor = new WordTextExtractor();

        var result = await extractor.ExtractTextAsync(stream);

        Assert.Equal(string.Empty, result);
    }
}
