using FunctionalKit.Core.Messaging;
using FunctionalKit.Core.Messaging.PipelineBehaviors;

namespace FunctionalKit.Behaviors;

/// <summary>
/// Async validation behavior for commands
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
public class CommandAsyncValidationBehavior<TCommand> : ICommandPipelineBehavior<TCommand>
    where TCommand : ICommand, IAsyncValidatable
{
    public async Task HandleAsync(TCommand command, Func<Task> next, CancellationToken cancellationToken = default)
    {
        var validation = await command.ValidateAsync(cancellationToken).ConfigureAwait(false);
        if (validation.IsInvalid)
        {
            var errors = string.Join("; ", validation.Errors);
            throw new ValidationException(errors);
        }

        await next().ConfigureAwait(false);
    }
}

/// <summary>
/// Async validation behavior for commands with response
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class CommandAsyncValidationBehavior<TCommand, TResponse> : ICommandPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>, IAsyncValidatable
{
    public async Task<TResponse> HandleAsync(TCommand command, Func<Task<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
        var validation = await command.ValidateAsync(cancellationToken).ConfigureAwait(false);
        if (validation.IsInvalid)
        {
            var errors = string.Join("; ", validation.Errors);
            throw new ValidationException(errors);
        }

        return await next().ConfigureAwait(false);
    }
}