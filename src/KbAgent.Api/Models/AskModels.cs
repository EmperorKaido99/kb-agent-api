namespace KbAgent.Api.Models;

public sealed record AskRequest(string Question);

public sealed record SourceSnippet(string Source, string Text, float Score);

public sealed record AskResponse(string Answer, IReadOnlyList<SourceSnippet> Sources);
