using System.Text.Json;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.WhatsApp.Webhook;

/// <summary>
/// Persists a raw inbound webhook payload as an audit/replay row and returns its id so the
/// caller can enqueue asynchronous processing. Signature validation happens in the controller
/// (it needs the app secret); this command only stores and lightly classifies the payload.
/// </summary>
public record IngestWhatsAppWebhookCommand(string RawPayload) : ICommand<int>;

public class IngestWhatsAppWebhookCommandHandler(IUnitOfWork unitOfWork)
    : ICommandHandler<IngestWhatsAppWebhookCommand, int>
{
    public async Task<Result<int>> Handle(IngestWhatsAppWebhookCommand request, CancellationToken cancellationToken)
    {
        var (eventType, phoneNumberId) = Classify(request.RawPayload);

        var log = new WhatsAppWebhookLog
        {
            RawPayload = request.RawPayload,
            EventType = eventType,
            PhoneNumberId = phoneNumberId,
            ReceivedAt = DateTime.UtcNow,
            Processed = false
        };

        var repository = unitOfWork.GetRepository<WhatsAppWebhookLog, int>();
        await repository.AddAsync(log, cancellationToken);
        await unitOfWork.CompleteAsync();

        return Result.Success(log.Id);
    }

    /// <summary>Best-effort classification; never throws so a malformed payload is still stored.</summary>
    private static (string EventType, string? PhoneNumberId) Classify(string rawPayload)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawPayload);
            var change = doc.RootElement
                .GetProperty("entry")[0]
                .GetProperty("changes")[0];

            var value = change.GetProperty("value");

            string? phoneNumberId = value.TryGetProperty("metadata", out var metadata) &&
                                    metadata.TryGetProperty("phone_number_id", out var pid)
                ? pid.GetString()
                : null;

            // message_template_status_update is a field-based event (no messages/statuses array),
            // so route on the change's field first, then fall back to the value's child arrays.
            var field = change.TryGetProperty("field", out var fieldEl) ? fieldEl.GetString() : null;
            var eventType = field == "message_template_status_update" ? "template_status"
                : value.TryGetProperty("messages", out _) ? "message"
                : value.TryGetProperty("statuses", out _) ? "status"
                : "unknown";

            return (eventType, phoneNumberId);
        }
        catch
        {
            return ("unknown", null);
        }
    }
}
