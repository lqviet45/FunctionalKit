using Microsoft.Extensions.Logging;
using FunctionalKit.Core.Messaging;
using FunctionalKit.Core.Messaging.PipelineBehaviors;

namespace FunctionalKit.Behaviors;

/// <summary>
/// Logging behavior for queries
/// </summary>
public class QueryLoggingBehavior<TQuery, TResponse> : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private readonly ILogger<QueryLoggingBehavior<TQuery, TResponse>> _logger;

    public QueryLoggingBehavior(ILogger<QueryLoggingBehavior<TQuery, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(TQuery query, Func<Task<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
        var queryName = typeof(TQuery).Name;

        _logger.LogInformation("Executing query {QueryName}", queryName);

        try
        {
            var result = await next();
            _logger.LogInformation("Query {QueryName} executed successfully", queryName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query {QueryName} failed", queryName);
            throw;
        }
    }
}

/// <summary>
/// Logging behavior for commands
/// </summary>
public class CommandLoggingBehavior<TCommand> : ICommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly ILogger<CommandLoggingBehavior<TCommand>> _logger;

    public CommandLoggingBehavior(ILogger<CommandLoggingBehavior<TCommand>> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(TCommand command, Func<Task> next, CancellationToken cancellationToken = default)
    {
        var commandName = typeof(TCommand).Name;

        _logger.LogInformation("Executing command {CommandName}", commandName);

        try
        {
            await next();
            _logger.LogInformation("Command {CommandName} executed successfully", commandName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command {CommandName} failed", commandName);
            throw;
        }
    }
}