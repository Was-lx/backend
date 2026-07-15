namespace WaslX.Application.Features.Tags.Dtos;

/// <summary>A tenant-defined tag that can be applied to conversations.</summary>
public record TagResponse(int Id, string Name, string Color, string? Description);

/// <summary>Create/update payload for a tag.</summary>
public record UpsertTagRequest(string Name, string Color, string? Description);
