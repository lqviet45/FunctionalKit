namespace FunctionalKit.Exceptions;

/// <summary>
/// Exception thrown during pipeline behavior execution
/// </summary>
public class PipelineBehaviorException : FunctionalKitException
{
    public Type BehaviorType { get; }
    public Type RequestType { get; }

    public PipelineBehaviorException(Type behaviorType, Type requestType, string message, Exception innerException)
        : base($"Pipeline behavior {behaviorType.Name} failed for request {requestType.Name}: {message}", innerException)
    {
        BehaviorType = behaviorType;
        RequestType = requestType;
    }
}