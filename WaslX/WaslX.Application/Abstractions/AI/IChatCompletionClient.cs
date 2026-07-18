using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.AI;

/// <summary>
/// Minimal chat-completion abstraction over an LLM provider (OpenAI). Implementations use a typed
/// <see cref="System.Net.Http.HttpClient"/> and never throw to callers — transport / provider / config
/// failures are surfaced as failed <see cref="Result{T}"/>s.
/// </summary>
public interface IChatCompletionClient
{
    /// <summary>
    /// Runs a single-turn completion and returns the assistant's text.
    /// </summary>
    /// <param name="systemPrompt">Instruction/persona for the model.</param>
    /// <param name="userPrompt">The user content (e.g. the conversation transcript).</param>
    Task<Result<string>> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}
