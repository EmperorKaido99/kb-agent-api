using ClosedXML.Excel;
using KbAgent.Api.Services.Extraction;

namespace KbAgent.Api.Tests.Services.Extraction;

public class ExcelTextExtractorTests
{
    [Fact]
    public async Task ExtractTextAsync_WorksheetWithCells_ReturnsCellValues()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Policies");
            sheet.Cell("A1").Value = "Policy";
            sheet.Cell("B1").Value = "Refund window";
            sheet.Cell("A2").Value = "Standard";
            sheet.Cell("B2").Value = "30 days";
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        var extractor = new ExcelTextExtractor();

        var result = await extractor.ExtractTextAsync(stream);

        Assert.Contains("[Sheet: Policies]", result);
        Assert.Contains("Refund window", result);
        Assert.Contains("30 days", result);
    }

    [Fact]
    public async Task ExtractTextAsync_EmptyWorksheet_ReturnsEmptyString()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            workbook.Worksheets.Add("Empty");
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        var extractor = new ExcelTextExtractor();

        var result = await extractor.ExtractTextAsync(stream);

        Assert.Equal(string.Empty, result);
    }
}
