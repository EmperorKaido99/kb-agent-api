namespace KbAgent.Api.Configuration;

public sealed class OcrOptions
{
    public const string SectionName = "Ocr";

    /// <summary>Name/path of the Tesseract CLI executable. Must be installed separately (apt: tesseract-ocr).</summary>
    public string TesseractExecutable { get; set; } = "tesseract";

    public string Language { get; set; } = "eng";

    public int TimeoutSeconds { get; set; } = 60;
}
