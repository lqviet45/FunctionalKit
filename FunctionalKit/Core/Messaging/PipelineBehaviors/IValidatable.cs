namespace FunctionalKit.Core.Messaging.PipelineBehaviors;

/// <summary>
/// Interface for objects that can be validated
/// </summary>
public interface IValidatable
{
    Validation<Unit> Validate();
}