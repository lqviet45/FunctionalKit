namespace FunctionalKit.Exceptions;

/// <summary>
/// Base exception for FunctionalKit library
/// </summary>
public class FunctionalKitException : Exception
{
    public FunctionalKitException(string message) : base(message) { }
    public FunctionalKitException(string message, Exception innerException) : base(message, innerException) { }
}