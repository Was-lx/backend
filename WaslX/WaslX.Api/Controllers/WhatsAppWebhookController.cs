using System.Security.Cryptography;
using System.Text;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.WhatsApp.Webhook;
using WaslX.Infrastructure.Settings;

namespace WaslX.Api.Controllers;

/// <summary>
/// Meta WhatsApp webhook endpoints. GET performs the one-time verification handshake;
/// POST validates the payload signature, stores it, and hands processing to Hangfire.
/// </summary>
[ApiController]
[Route("api/whatsapp/webhook")]
[AllowAnonymous]
public class WhatsAppWebhookController(
    ISender sender,
    IBackgroundJobClient backgroundJobs,
    IOptions<WhatsAppOptions> options,
    ILogger<WhatsAppWebhookController> logger) : ControllerBase
{
    private readonly WhatsAppOptions _options = options.Value;

    /// <summary>Webhook verification: echo hub.challenge when the verify token matches.</summary>
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (mode == "subscribe" && verifyToken == _options.WebhookVerifyToken)
            return Content(challenge ?? string.Empty, "text/plain");

        logger.LogWarning("WhatsApp webhook verification failed (mode={Mode})", mode);
        return StatusCode(403);
    }

    /// <summary>Receives webhook events. Returns 200 fast; heavy work runs in the background.</summary>
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
            rawBody = await reader.ReadToEndAsync(cancellationToken);

        if (!IsSignatureValid(rawBody))
        {
            logger.LogWarning("WhatsApp webhook rejected: invalid X-Hub-Signature-256");
            return Unauthorized();
        }

        var ingest = await sender.Send(new IngestWhatsAppWebhookCommand(rawBody), cancellationToken);
        if (ingest.IsFailure)
        {
            // Signal a retryable failure to Meta so the event isn't lost.
            logger.LogError("Failed to persist WhatsApp webhook payload: {Error}", ingest.Error.Description);
            return StatusCode(500);
        }

        backgroundJobs.Enqueue<IWhatsAppWebhookProcessor>(p => p.ProcessAsync(ingest.Value, CancellationToken.None));
        return Ok();
    }

    /// <summary>Validates Meta's HMAC-SHA256 signature of the raw body using the app secret.</summary>
    private bool IsSignatureValid(string rawBody)
    {
        if (!Request.Headers.TryGetValue("X-Hub-Signature-256", out var signatureHeader))
            return false;

        var provided = signatureHeader.ToString();
        const string prefix = "sha256=";
        if (!provided.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var key = Encoding.UTF8.GetBytes(_options.AppSecret);
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(rawBody));
        var expected = Convert.ToHexStringLower(hash);

        var providedHex = provided[prefix.Length..];
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(providedHex));
    }
}
