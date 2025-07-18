namespace FunctionalKit.Exceptions;

/// <summary>
/// Exception thrown when no handler is found for a command or query
/// </summary>
public class HandlerNotFoundException : FunctionalKitException
{
    public Type RequestType { get; }
    public Type HandlerType { get; }

    public HandlerNotFoundException(Type requestType, Type handlerType) 
        : base($"No handler of type {handlerType.Name} found for request {requestType.Name}")
    {
        RequestType = requestType;
        HandlerType = handlerType;
    }
}