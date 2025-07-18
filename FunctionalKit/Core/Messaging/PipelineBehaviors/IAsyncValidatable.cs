namespace FunctionalKit.Core.Messaging.PipelineBehaviors;

/// <summary>
/// Interface for objects that can be validated asynchronously
/// </summary>
public interface IAsyncValidatable
{
    Task<Validation<Unit>> ValidateAsync(CancellationToken cancellationToken = default);
}