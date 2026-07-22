namespace WaslX.Api.Contracts;

public sealed record UpdateEscalationModeRequest(string Mode);

public sealed record RejectEscalationRequest(string? Reason);
