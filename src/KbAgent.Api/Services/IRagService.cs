using KbAgent.Api.Models;

namespace KbAgent.Api.Services;

public interface IRagService
{
    Task<AskResponse> AskAsync(string question, CancellationToken ct = default);

    Task<IngestResponse> IngestAsync(string source, string text, CancellationToken ct = default);
}
