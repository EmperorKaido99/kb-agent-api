using KbAgent.Api.Services;

namespace KbAgent.Api.Tests.Services;

public class KnowledgeFolderChangeDetectorTests
{
    [Fact]
    public void GetChangedOrNewFiles_NewFileNotInPrevious_IsReturned()
    {
        var current = new Dictionary<string, string> { ["a.docx"] = "100:1" };
        var previous = new Dictionary<string, string>();

        var result = KnowledgeFolderChangeDetector.GetChangedOrNewFiles(current, previous);

        Assert.Equal(["a.docx"], result);
    }

    [Fact]
    public void GetChangedOrNewFiles_UnchangedFingerprint_IsNotReturned()
    {
        var current = new Dictionary<string, string> { ["a.docx"] = "100:1" };
        var previous = new Dictionary<string, string> { ["a.docx"] = "100:1" };

        var result = KnowledgeFolderChangeDetector.GetChangedOrNewFiles(current, previous);

        Assert.Empty(result);
    }

    [Fact]
    public void GetChangedOrNewFiles_DifferentFingerprint_IsReturned()
    {
        var current = new Dictionary<string, string> { ["a.docx"] = "200:2" };
        var previous = new Dictionary<string, string> { ["a.docx"] = "100:1" };

        var result = KnowledgeFolderChangeDetector.GetChangedOrNewFiles(current, previous);

        Assert.Equal(["a.docx"], result);
    }

    [Fact]
    public void GetChangedOrNewFiles_FileRemovedFromCurrent_IsNotReturnedAsChanged()
    {
        // Deletions aren't "changed files to ingest" — that's a separate concern this detector doesn't handle.
        var current = new Dictionary<string, string>();
        var previous = new Dictionary<string, string> { ["deleted.docx"] = "100:1" };

        var result = KnowledgeFolderChangeDetector.GetChangedOrNewFiles(current, previous);

        Assert.Empty(result);
    }

    [Fact]
    public void ComputeFingerprint_SameInputs_AreEqual()
    {
        var time = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var a = KnowledgeFolderChangeDetector.ComputeFingerprint(1234, time);
        var b = KnowledgeFolderChangeDetector.ComputeFingerprint(1234, time);

        Assert.Equal(a, b);
    }

    [Fact]
    public void ComputeFingerprint_DifferentSize_AreDifferent()
    {
        var time = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var a = KnowledgeFolderChangeDetector.ComputeFingerprint(1234, time);
        var b = KnowledgeFolderChangeDetector.ComputeFingerprint(5678, time);

        Assert.NotEqual(a, b);
    }
}
