namespace KbAgent.Api.Configuration;

public sealed class ApiUsersOptions
{
    public const string SectionName = "ApiUsers";

    /// <summary>Where username → token-hash pairs are persisted. Empty/missing file = no users = auth disabled.</summary>
    public string FilePath { get; set; } = "api-users.json";
}
