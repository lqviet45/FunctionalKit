namespace FunctionalKit.Exceptions;

/// <summary>
/// Exception thrown when multiple handlers are found but only one was expected
/// </summary>
public class MultipleHandlersFoundException : FunctionalKitException
{
    public Type RequestType { get; }
    public int HandlerCount { get; }

    public MultipleHandlersFoundException(Type requestType, int handlerCount)
        : base($"Multiple handlers ({handlerCount}) found for request {requestType.Name}, but only one was expected")
    {
        RequestType = requestType;
        HandlerCount = handlerCount;
    }
}