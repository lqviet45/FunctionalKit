using FunctionalKit.Core.Messaging;
using FunctionalKit.Core.Messaging.PipelineBehaviors;

namespace FunctionalKit.Behaviors;

/// <summary>
/// Validation behavior for queries
/// </summary>
/// <typeparam name="TQuery">The query type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class QueryValidationBehavior<TQuery, TResponse> : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>, IValidatable
{
    public async Task<TResponse> HandleAsync(TQuery query, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var validation = query.Validate();
        if (validation.IsInvalid)
        {
            var errors = string.Join("; ", validation.Errors);
            throw new ValidationException(errors);
        }

        return await next();
    }
}

/// <summary>
/// Validation behavior for commands
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
public class CommandValidationBehavior<TCommand> : ICommandPipelineBehavior<TCommand>
    where TCommand : ICommand, IValidatable
{
    public async Task HandleAsync(TCommand command, Func<Task> next, CancellationToken cancellationToken = default)
    {
        var validation = command.Validate();
        if (validation.IsInvalid)
        {
            var errors = string.Join("; ", validation.Errors);
            throw new ValidationException(errors);
        }

        await next();
    }
}

/// <summary>
/// Validation behavior for commands with response
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class CommandValidationBehavior<TCommand, TResponse> : ICommandPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>, IValidatable
{
    public async Task<TResponse> HandleAsync(TCommand command, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var validation = command.Validate();
        if (validation.IsInvalid)
        {
            var errors = string.Join("; ", validation.Errors);
            throw new ValidationException(errors);
        }

        return await next();
    }
}