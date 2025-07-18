using System.Collections.Concurrent;
using FunctionalKit.Configuration;
using Microsoft.Extensions.Logging;
using FunctionalKit.Core.Messaging;
using FunctionalKit.Core.Messaging.PipelineBehaviors;

namespace FunctionalKit.Behaviors;

/// <summary>
/// Circuit breaker state management
/// </summary>
public class CircuitBreakerState
{
    private readonly ConcurrentDictionary<string, CircuitState> _circuits = new();

    public CircuitState GetOrCreateCircuit(string key, int failureThreshold, TimeSpan openDuration)
    {
        return _circuits.GetOrAdd(key, _ => new CircuitState(failureThreshold, openDuration));
    }
}

/// <summary>
/// Individual circuit state
/// </summary>
public class CircuitState
{
    private readonly object _lock = new();
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private CircuitStatus _status = CircuitStatus.Closed;

    public CircuitState(int failureThreshold, TimeSpan openDuration)
    {
        _failureThreshold = failureThreshold;
        _openDuration = openDuration;
    }

    public CircuitStatus Status => _status;
    public int FailureCount => _failureCount;

    public bool CanExecute()
    {
        lock (_lock)
        {
            if (_status == CircuitStatus.Closed)
                return true;

            if (_status == CircuitStatus.Open && DateTime.UtcNow - _lastFailureTime > _openDuration)
            {
                _status = CircuitStatus.HalfOpen;
                return true;
            }

            if (_status == CircuitStatus.HalfOpen)
                return true;

            return false;
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _status = CircuitStatus.Closed;
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold)
            {
                _status = CircuitStatus.Open;
            }
        }
    }
}

/// <summary>
/// Circuit breaker status
/// </summary>
public enum CircuitStatus
{
    Closed,   // Normal operation
    Open,     // Circuit is open, requests are blocked
    HalfOpen  // Testing if the circuit can be closed
}

/// <summary>
/// Circuit breaker exception
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string circuitName) 
        : base($"Circuit breaker '{circuitName}' is open")
    {
    }
}

/// <summary>
/// Circuit breaker behavior for queries
/// </summary>
public class QueryCircuitBreakerBehavior<TQuery, TResponse> : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private readonly CircuitBreakerState _circuitBreakerState;
    private readonly ILogger<QueryCircuitBreakerBehavior<TQuery, TResponse>> _logger;
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;

    public QueryCircuitBreakerBehavior(
        CircuitBreakerState circuitBreakerState,
        ILogger<QueryCircuitBreakerBehavior<TQuery, TResponse>> logger,
        CircuitBreakerOptions? options = null)
    {
        _circuitBreakerState = circuitBreakerState;
        _logger = logger;
        _failureThreshold = options?.FailureThreshold ?? 5;
        _openDuration = options?.CircuitOpenDuration ?? TimeSpan.FromMinutes(1);
    }

    public async Task<TResponse> HandleAsync(TQuery query, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var circuitKey = typeof(TQuery).FullName!;
        var circuit = _circuitBreakerState.GetOrCreateCircuit(circuitKey, _failureThreshold, _openDuration);

        if (!circuit.CanExecute())
        {
            _logger.LogWarning("Circuit breaker is open for query {QueryType}. Failure count: {FailureCount}", 
                typeof(TQuery).Name, circuit.FailureCount);
            throw new CircuitBreakerOpenException(circuitKey);
        }

        try
        {
            var result = await next().ConfigureAwait(false);
            circuit.RecordSuccess();
            
            if (circuit.Status == CircuitStatus.HalfOpen)
            {
                _logger.LogInformation("Circuit breaker closed for query {QueryType}", typeof(TQuery).Name);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            circuit.RecordFailure();
            
            if (circuit.Status == CircuitStatus.Open)
            {
                _logger.LogError(ex, "Circuit breaker opened for query {QueryType} after {FailureCount} failures", 
                    typeof(TQuery).Name, circuit.FailureCount);
            }
            
            throw;
        }
    }
}