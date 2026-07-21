using WaslX.Application.Features.Classification.Models;

namespace WaslX.Application.Abstractions.AI;

public interface IMessageClassifier
{
    Task<MessageClassificationResult> ClassifyAsync(
        MessageClassificationInput input,
        CancellationToken cancellationToken = default);
}
