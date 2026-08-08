namespace KbAgent.Api.Services;

/// <summary>Persists API users as username → token-hash pairs. Never stores raw tokens.</summary>
public interface IApiUserStore
{
    Task<IReadOnlyDictionary<string, string>> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(IReadOnlyDictionary<string, string> usersByTokenHash, CancellationToken ct = default);
}
