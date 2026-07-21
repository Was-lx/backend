using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Abstractions.Rag;

namespace WaslX.Api.Controllers;

/// <summary>
/// Development-only smoke test for the RAG stack: proves embeddings (Hugging Face, BGE-M3), chat
/// (Groq) and the Qdrant round-trip all work with the configured model ids.
/// Not mapped outside Development.
/// </summary>
[ApiController]
[Route("api/rag")]
[AllowAnonymous]
public class RagHealthController(
    IEmbeddingProvider embeddings,
    ILLMProvider llm,
    IVectorStore vectors,
    IWebHostEnvironment env) : ControllerBase
{
    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return NotFound();

        var report = new Dictionary<string, object>();

        // 1) Embedding round-trip
        var embed = await embeddings.EmbedBatchAsync(["hello from waslx rag health"], EmbeddingPurpose.Query, cancellationToken);
        report["embedding"] = embed.IsSuccess
            ? new { ok = true, dimensions = embed.Value.Vectors.FirstOrDefault()?.Length ?? 0 }
            : new { ok = false, error = embed.Error.Code };

        // 2) Chat ping
        var chat = await llm.GenerateAsync(
            new LlmRequest("You are a health check. Reply with the single word: OK.",
                [new LlmMessage("user", "ping")], Temperature: 0, MaxTokens: 16),
            cancellationToken);
        report["chat"] = chat.IsSuccess
            ? new { ok = true, sample = chat.Value.Text }
            : new { ok = false, error = chat.Error.Code };

        // 3) Qdrant collection reachable
        var collection = await vectors.EnsureCollectionAsync(cancellationToken);
        report["qdrant"] = collection.IsSuccess
            ? new { ok = true }
            : new { ok = false, error = collection.Error.Code };

        var allOk = embed.IsSuccess && chat.IsSuccess && collection.IsSuccess;
        return allOk ? Ok(report) : StatusCode(503, report);
    }
}
