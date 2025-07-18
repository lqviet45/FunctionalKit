namespace FunctionalKit.Core.Messaging;

/// <summary>
/// Marker interface for commands that don't return a value
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Marker interface for commands that return a result
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface ICommand<out TResponse>
{
}