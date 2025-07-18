namespace FunctionalKit.Core.Messaging.PipelineBehaviors;

/// <summary>
/// Pipeline behavior for commands
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
public interface ICommandPipelineBehavior<in TCommand>
    where TCommand : ICommand
{
    Task HandleAsync(TCommand command, Func<Task> next, CancellationToken cancellationToken = default);
}

/// <summary>
/// Pipeline behavior for commands with response
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public interface ICommandPipelineBehavior<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, Func<Task<TResponse>> next, CancellationToken cancellationToken = default);
}