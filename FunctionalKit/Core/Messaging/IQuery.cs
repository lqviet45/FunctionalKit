namespace FunctionalKit.Core.Messaging;

/// <summary>
/// Marker interface for queries that return a result
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IQuery<out TResponse>
{
}