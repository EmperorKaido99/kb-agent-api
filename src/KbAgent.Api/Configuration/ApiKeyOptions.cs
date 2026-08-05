namespace KbAgent.Api.Configuration;

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKey";

    /// <summary>Empty/unset disables API key auth (e.g. local dev).</summary>
    public string Key { get; set; } = "";
}
