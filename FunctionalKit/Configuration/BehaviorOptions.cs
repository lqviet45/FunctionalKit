namespace FunctionalKit.Configuration;

/// <summary>
/// Configuration options for pipeline behaviors
/// </summary>
public class BehaviorOptions
{
    /// <summary>
    /// Enable logging behaviors
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Enable validation behaviors
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// Enable caching behaviors
    /// </summary>
    public bool EnableCaching { get; set; } = false;

    /// <summary>
    /// Enable performance monitoring behaviors
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = false;

    /// <summary>
    /// Enable retry behaviors
    /// </summary>
    public bool EnableRetry { get; set; } = false;

    /// <summary>
    /// Slow query threshold in milliseconds for performance monitoring
    /// </summary>
    public long SlowQueryThresholdMs { get; set; } = 500;

    /// <summary>
    /// Maximum number of retries for retry behavior
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retries
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Order of behaviors (lower numbers execute first)
    /// </summary>
    public BehaviorOrder BehaviorOrder { get; set; } = new();
}

/// <summary>
/// Defines the execution order of behaviors
/// </summary>
public class BehaviorOrder
{
    public int Logging { get; set; } = 1;
    public int PerformanceMonitoring { get; set; } = 2;
    public int Validation { get; set; } = 3;
    public int Caching { get; set; } = 4;
    public int Retry { get; set; } = 5;
}

/// <summary>
/// Configuration options for circuit breaker behavior
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Number of failures before opening the circuit
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Duration to keep the circuit open
    /// </summary>
    public TimeSpan CircuitOpenDuration { get; set; } = TimeSpan.FromMinutes(1);
}