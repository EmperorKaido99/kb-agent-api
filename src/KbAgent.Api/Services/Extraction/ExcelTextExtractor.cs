using System.Text;
using ClosedXML.Excel;

namespace KbAgent.Api.Services.Extraction;

/// <summary>Handles .xlsx files via ClosedXML — one sheet per block, rows as tab-separated cell values.</summary>
public sealed class ExcelTextExtractor : IDocumentTextExtractor
{
    public IReadOnlyCollection<string> SupportedExtensions { get; } = [".xlsx"];

    public Task<string> ExtractTextAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook(fileStream);
        var sb = new StringBuilder();

        foreach (var worksheet in workbook.Worksheets)
        {
            ct.ThrowIfCancellationRequested();
            var usedRange = worksheet.RangeUsed();
            if (usedRange is null)
            {
                continue;
            }

            sb.AppendLine($"[Sheet: {worksheet.Name}]");
            foreach (var row in usedRange.RowsUsed())
            {
                var cells = row.Cells().Select(c => c.GetFormattedString().Trim());
                var line = string.Join("\t", cells).Trim();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    sb.AppendLine(line);
                }
            }
        }

        return Task.FromResult(sb.ToString());
    }
}
