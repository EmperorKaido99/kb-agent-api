using KbAgent.Api.Services.Extraction;

namespace KbAgent.Api.Tests.Services.Extraction;

public class DocumentTextExtractorFactoryTests
{
    [Theory]
    [InlineData("report.docx", typeof(WordTextExtractor))]
    [InlineData("slides.pptx", typeof(PowerPointTextExtractor))]
    [InlineData("data.xlsx", typeof(ExcelTextExtractor))]
    [InlineData("notes.txt", typeof(PlainTextExtractor))]
    [InlineData("README.md", typeof(PlainTextExtractor))]
    [InlineData("scan.png", typeof(ImageOcrTextExtractor))]
    public void GetExtractor_KnownExtension_ReturnsMatchingExtractor(string fileName, Type expectedExtractorType)
    {
        var factory = CreateFactory();

        var extractor = factory.GetExtractor(fileName);

        Assert.NotNull(extractor);
        Assert.IsType(expectedExtractorType, extractor);
    }

    [Theory]
    [InlineData("archive.zip")]
    [InlineData("no-extension")]
    [InlineData("audio.mp3")]
    public void GetExtractor_UnknownExtension_ReturnsNull(string fileName)
    {
        var factory = CreateFactory();

        var extractor = factory.GetExtractor(fileName);

        Assert.Null(extractor);
    }

    [Fact]
    public void GetExtractor_IsCaseInsensitive()
    {
        var factory = CreateFactory();

        Assert.NotNull(factory.GetExtractor("Report.DOCX"));
    }

    private static DocumentTextExtractorFactory CreateFactory()
    {
        var pdfExtractor = new PdfTextExtractor(NullLoggerFactory.CreateLogger<PdfTextExtractor>());
        var ocrExtractor = new ImageOcrTextExtractor(
            Microsoft.Extensions.Options.Options.Create(new KbAgent.Api.Configuration.OcrOptions()),
            NullLoggerFactory.CreateLogger<ImageOcrTextExtractor>());

        IEnumerable<IDocumentTextExtractor> extractors =
        [
            new PlainTextExtractor(),
            new WordTextExtractor(),
            new PowerPointTextExtractor(),
            new ExcelTextExtractor(),
            pdfExtractor,
            ocrExtractor,
        ];

        return new DocumentTextExtractorFactory(extractors);
    }
}

file static class NullLoggerFactory
{
    public static Microsoft.Extensions.Logging.ILogger<T> CreateLogger<T>() =>
        Microsoft.Extensions.Logging.Abstractions.NullLogger<T>.Instance;
}
