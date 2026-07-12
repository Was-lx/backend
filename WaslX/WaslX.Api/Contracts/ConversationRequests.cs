namespace WaslX.Api.Contracts;

/// <summary>Body for sending a text reply within a conversation.</summary>
public record SendConversationMessageRequest(string Text);

/// <summary>Body for adding an internal team note to a conversation.</summary>
public record AddNoteRequest(string Content);

/// <summary>Body for a manual conversation lifecycle transition (target status name).</summary>
public record ChangeConversationStatusRequest(string Status);
