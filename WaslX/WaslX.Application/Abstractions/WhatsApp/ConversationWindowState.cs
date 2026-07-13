using WaslX.Domain.SharedEnums;

namespace WaslX.Application.Abstractions.WhatsApp;


public sealed record ConversationWindowState
{
    public bool IsOpen { get; init; }

    // ── Send capabilities ─────────────────────────────────────────────────────
    public bool CanSendFreeForm      => IsOpen;
    public bool CanSendText          => IsOpen;
    public bool CanSendMedia         => IsOpen;
    public bool CanSendInteractive   => IsOpen;


    public bool CanSendTemplate      => true;

    // ── Window timing ─────────────────────────────────────────────────────────
    public TimeSpan RemainingTime    { get; init; }
    public DateTime? WindowExpiresAt { get; init; }


    public ConversationWindowType WindowType { get; init; }
}
