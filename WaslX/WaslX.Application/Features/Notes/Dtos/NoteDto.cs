namespace WaslX.Application.Features.Notes.Dtos;

/// <summary>An internal team note on a conversation — visible to the team, never sent to the customer.</summary>
public record NoteDto(int Id, int ConversationId, string Content, string AuthorName, DateTime CreatedAt);
