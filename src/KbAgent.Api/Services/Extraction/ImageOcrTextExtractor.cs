using System.ComponentModel;
using System.Diagnostics;
using KbAgent.Api.Configuration;
using Microsoft.Extensions.Options;

namespace KbAgent.Api.Services.Extraction;

/// <summary>
/// OCRs standalone image files by shelling out to the Tesseract CLI (apt: tesseract-ocr). Shelling out avoids
/// the native-binding version/platform mismatches of the managed Tesseract NuGet wrapper (Windows-only natives).
/// </summary>
public sealed class ImageOcrTextExtractor(IOptions<OcrOptions> options, ILogger<ImageOcrTextExtractor> logger)
    : IDocumentTextExtractor
{
    public IReadOnlyCollection<string> SupportedExtensions { get; } = [".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff"];

    public async Task<string> ExtractTextAsync(Stream fileStream, CancellationToken ct = default)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"kb-ocr-{Guid.NewGuid():N}");
        try
        {
            await using (var fileOut = File.Create(tempFile))
            {
                await fileStream.CopyToAsync(fileOut, ct);
            }

            return await RunTesseractAsync(tempFile, ct);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private async Task<string> RunTesseractAsync(string imagePath, CancellationToken ct)
    {
        var opts = options.Value;
        var startInfo = new ProcessStartInfo
        {
            FileName = opts.TesseractExecutable,
            ArgumentList = { imagePath, "stdout", "-l", opts.Language },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        Process process;
        try
        {
            process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start the tesseract process.");
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException)
        {
            logger.LogWarning(
                ex,
                "Could not run '{Executable}' — is Tesseract OCR installed? OCR skipped for this file.",
                opts.TesseractExecutable);
            return string.Empty;
        }

        using (process)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(opts.TimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                process.Kill(entireProcessTree: true);
                logger.LogWarning("tesseract timed out after {TimeoutSeconds}s and was killed.", opts.TimeoutSeconds);
                return string.Empty;
            }

            if (process.ExitCode != 0)
            {
                var stderr = await stderrTask;
                logger.LogWarning("tesseract exited with code {ExitCode}: {Error}", process.ExitCode, stderr);
                return string.Empty;
            }

            return await stdoutTask;
        }
    }
}
