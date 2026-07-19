namespace WaslX.Api.Contracts;

public record AddWebsiteRequest(string Url, string? Title, string? Language);

public record SearchKnowledgeRequest(string Query, int? TopK, string? SourceType);

public record AskKnowledgeRequest(string Question);
