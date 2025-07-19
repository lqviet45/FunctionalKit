# Pipeline Behaviors Guide

Complete guide to using and creating pipeline behaviors in FunctionalKit for cross-cutting concerns.

## Table of Contents

- [Overview](#overview)
- [Built-in Behaviors](#built-in-behaviors)
- [Configuration](#configuration)
- [Custom Behaviors](#custom-behaviors)
- [Behavior Ordering](#behavior-ordering)
- [Advanced Patterns](#advanced-patterns)
- [Performance Considerations](#performance-considerations)
- [Testing Behaviors](#testing-behaviors)

## Overview

Pipeline behaviors in FunctionalKit provide a way to add cross-cutting concerns to your commands and queries without modifying the handlers themselves. They wrap around handlers and can execute code before and after the handler runs.

### Benefits of Pipeline Behaviors

- **Separation of Concerns**: Keep business logic separate from infrastructure concerns
- **Reusability**: Apply the same behavior to multiple handlers
- **Composability**: Chain multiple behaviors together
- **Testability**: Test behaviors in isolation
- **Maintainability**: Modify cross-cutting concerns in one place

### How Behaviors Work

```csharp
// Behavior execution order (outside to inside)
[Behavior1] -> [Behavior2] -> [Behavior3] -> [Handler] -> [Behavior3] -> [Behavior2] -> [Behavior1]
```

## Built-in Behaviors

FunctionalKit provides several built-in behaviors for common scenarios.

### Validation Behavior

Automatically validates commands and queries that implement `IValidatable` or `IAsyncValidatable`.

```csharp
// Enable validation behavior
services.AddFunctionalKitValidation();

// Command with validation
public record CreateUserCommand(string Name, string Email) : ICommand<Result<int>>, IValidatable
{
    public Validation<Unit> Validate()
    {
        return ValidateName()
            .Combine(ValidateEmail(), (name, email) => Unit.Value);
    }

    private Validation<string> ValidateName()
    {
        return string.IsNullOrWhiteSpace(Name)
            ? Validation<string>.Failure("Name is required")
            : Validation<string>.Success(Name);
    }

    private Validation<string> ValidateEmail()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return Validation<string>.Failure("Email is required");

        var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        return regex.IsMatch(Email)
            ? Validation<string>.Success(Email)
            : Validation<string>.Failure("Invalid email format");
    }
}

// Async validation
public class AsyncCreateUserValidator : IAsyncValidatable
{
    private readonly CreateUserCommand _command;
    private readonly IUserRepository _userRepository;

    public async Task<Validation<Unit>> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var syncValidation = _command.Validate();
        if (syncValidation.IsInvalid)
            return syncValidation;

        var emailUnique = await ValidateEmailUniqueAsync(cancellationToken);
        
        return emailUnique.Map(_ => Unit.Value);
    }

    private async Task<Validation<string>> ValidateEmailUniqueAsync(CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.FindByEmailAsync(_command.Email);
        return existingUser.HasValue
            ? Validation<string>.Failure("Email already exists")
            : Validation<string>.Success(_command.Email);
    }
}
```

### Logging Behavior

Automatically logs the execution of commands and queries.

```csharp
// Enable logging behavior
services.AddFunctionalKitLogging();

// Logs will include:
// - Request type and parameters
// - Execution time
// - Success/failure status
// - Exceptions

// Example log output:
// [INFO] Executing query GetUserByIdQuery with parameters: { UserId: 123 }
// [INFO] Query GetUserByIdQuery executed successfully in 45ms
// [ERROR] Query GetUserByIdQuery failed in 120ms: User not found
```

### Performance Monitoring Behavior

Monitors query execution time and logs slow queries.

```csharp
// Enable performance monitoring
services.AddFunctionalKitPerformanceMonitoring(slowQueryThresholdMs: 1000);

// Configuration options
services.AddFunctionalKit(options =>
{
    options.EnablePerformanceMonitoring = true;
    options.SlowQueryThresholdMs = 500; // Log queries slower than 500ms
});
```

### Caching Behavior

Automatically caches query results for queries that implement `ICacheable`.

```csharp
// Enable caching behavior
services.AddFunctionalKitCaching();

// Cacheable query
public record GetUserProfileQuery(int UserId) : IQuery<UserProfileDto>, ICacheable
{
    public string CacheKey => $"user-profile:{UserId}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

// The caching behavior will:
// 1. Check cache first
// 2. Execute handler if not cached
// 3. Store result in cache
// 4. Return cached result on subsequent calls
```

### Retry Behavior

Automatically retries failed operations with configurable backoff.

```csharp
// Enable retry behavior
services.AddFunctionalKitRetry(maxRetries: 3, delay: TimeSpan.FromSeconds(1));

// Configuration
services.AddFunctionalKit(options =>
{
    options.EnableRetry = true;
    options.MaxRetries = 3;
    options.RetryDelay = TimeSpan.FromSeconds(2);
});

// Retry behavior will:
// - Retry on exceptions (not on Result failures)
// - Use exponential backoff
// - Log retry attempts
```

### Circuit Breaker Behavior

Implements circuit breaker pattern to handle cascading failures.

```csharp
// Enable circuit breaker
services.AddFunctionalKitCircuitBreaker(
    failureThreshold: 5, 
    circuitOpenDuration: TimeSpan.FromMinutes(1));

// Circuit breaker states:
// - Closed: Normal operation
// - Open: Requests fail fast
// - Half-Open: Testing if service recovered
```

## Configuration

### Basic Configuration

```csharp
// Program.cs - Enable specific behaviors
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFunctionalKit(Assembly.GetExecutingAssembly());

// Add individual behaviors
builder.Services.AddFunctionalKitLogging();
builder.Services.AddFunctionalKitValidation();
builder.Services.AddFunctionalKitCaching();
builder.Services.AddFunctionalKitPerformanceMonitoring(1000);
builder.Services.AddFunctionalKitRetry(3, TimeSpan.FromSeconds(1));
```

### Advanced Configuration

```csharp
// Comprehensive configuration
builder.Services.AddFunctionalKit(options =>
{
    // Behavior toggles
    options.EnableLogging = true;
    options.EnableValidation = true;
    options.EnableCaching = true;
    options.EnablePerformanceMonitoring = true;
    options.EnableRetry = false;

    // Performance settings
    options.SlowQueryThresholdMs = 1000;

    // Retry settings
    options.MaxRetries = 3;
    options.RetryDelay = TimeSpan.FromSeconds(1);

    // Behavior execution order
    options.BehaviorOrder.Logging = 1;
    options.BehaviorOrder.PerformanceMonitoring = 2;
    options.BehaviorOrder.Validation = 3;
    options.BehaviorOrder.Caching = 4;
    options.BehaviorOrder.Retry = 5;
}, Assembly.GetExecutingAssembly());
```

### Environment-Specific Configuration

```csharp
// Different configurations per environment
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddFunctionalKit(options =>
    {
        options.EnableLogging = true;
        options.EnableValidation = true;
        options.SlowQueryThresholdMs = 100; // Stricter in development
    });
}
else if (builder.Environment.IsProduction())
{
    builder.Services.AddFunctionalKit(options =>
    {
        options.EnableLogging = true;
        options.EnableValidation = true;
        options.EnableCaching = true;
        options.EnablePerformanceMonitoring = true;
        options.EnableRetry = true;
        options.SlowQueryThresholdMs = 1000;
    });
}
```

## Custom Behaviors

Create custom behaviors to implement your own cross-cutting concerns.

### Simple Timing Behavior

```csharp
public class TimingBehavior<TRequest, TResponse> : IQueryPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
{
    private readonly ILogger<TimingBehavior<TRequest, TResponse>> _logger;

    public TimingBehavior(ILogger<TimingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await next();
            stopwatch.Stop();
            
            _logger.LogInformation("Query {QueryType} completed in {ElapsedMs}ms", 
                typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Query {QueryType} failed after {ElapsedMs}ms", 
                typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### Authorization Behavior

```csharp
public interface IRequireAuthorization
{
    string[] RequiredRoles { get; }
    string[] RequiredPermissions { get; }
}

public class AuthorizationBehavior<TRequest, TResponse> : IQueryPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>, IRequireAuthorization
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        if (currentUser == null)
            throw new UnauthorizedException("User not authenticated");

        // Check roles
        if (request.RequiredRoles.Any())
        {
            var hasRequiredRole = request.RequiredRoles.Any(role => currentUser.IsInRole(role));
            if (!hasRequiredRole)
                throw new ForbiddenException($"User requires one of these roles: {string.Join(", ", request.RequiredRoles)}");
        }

        // Check permissions
        if (request.RequiredPermissions.Any())
        {
            var hasPermissions = await _authorizationService.HasPermissionsAsync(
                currentUser.Id, request.RequiredPermissions, cancellationToken);
            if (!hasPermissions)
                throw new ForbiddenException($"User lacks required permissions: {string.Join(", ", request.RequiredPermissions)}");
        }

        return await next();
    }
}

// Usage
public record GetSensitiveDataQuery(int DataId) : IQuery<SensitiveData>, IRequireAuthorization
{
    public string[] RequiredRoles => new[] { "Admin", "DataAnalyst" };
    public string[] RequiredPermissions => new[] { "SensitiveData.Read" };
}
```

### Transaction Behavior

```csharp
public class TransactionBehavior<TRequest, TResponse> : ICommandPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        // Skip if already in transaction
        if (_dbContext.Database.CurrentTransaction != null)
            return await next();

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            _logger.LogDebug("Starting transaction for {CommandType}", typeof(TRequest).Name);
            
            var response = await next();
            
            await transaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Transaction committed for {CommandType}", typeof(TRequest).Name);
            
            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex, "Transaction rolled back for {CommandType}", typeof(TRequest).Name);
            throw;
        }
    }
}
```

### Request Correlation Behavior

```csharp
public class CorrelationBehavior<TRequest, TResponse> : IQueryPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
{
    private readonly ILogger<CorrelationBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestType"] = typeof(TRequest).Name
        }))
        {
            _logger.LogDebug("Processing request {RequestType} with correlation ID {CorrelationId}", 
                typeof(TRequest).Name, correlationId);
            
            var response = await next();
            
            _logger.LogDebug("Completed request {RequestType} with correlation ID {CorrelationId}", 
                typeof(TRequest).Name, correlationId);
            
            return response;
        }
    }
}
```

### Rate Limiting Behavior

```csharp
public interface IRateLimited
{
    string RateLimitKey { get; }
    int MaxRequests { get; }
    TimeSpan TimeWindow { get; }
}

public class RateLimitingBehavior<TRequest, TResponse> : IQueryPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>, IRateLimited
{
    private readonly IRateLimitService _rateLimitService;
    private readonly ICurrentUserService _currentUserService;

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var rateLimitKey = $"{request.RateLimitKey}:{currentUser?.Id ?? "anonymous"}";

        var isAllowed = await _rateLimitService.IsAllowedAsync(
            rateLimitKey, 
            request.MaxRequests, 
            request.TimeWindow);

        if (!isAllowed)
        {
            throw new RateLimitExceededException(
                $"Rate limit exceeded for {request.RateLimitKey}. Max {request.MaxRequests} requests per {request.TimeWindow}");
        }

        return await next();
    }
}

// Usage
public record SearchProductsQuery(string SearchTerm) : IQuery<List<ProductDto>>, IRateLimited
{
    public string RateLimitKey => "product-search";
    public int MaxRequests => 100;
    public TimeSpan TimeWindow => TimeSpan.FromMinutes(1);
}
```

### Data Encryption Behavior

```csharp
public interface IEncryptedResponse
{
    bool ShouldEncrypt { get; }
}

public class EncryptionBehavior<TRequest, TResponse> : IQueryPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
    where TResponse : IEncryptedResponse
{
    private readonly IEncryptionService _encryptionService;

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var response = await next();

        if (response.ShouldEncrypt)
        {
            // Encrypt sensitive data in response
            return _encryptionService.EncryptResponse(response);
        }

        return response;
    }
}
```

## Behavior Ordering

Control the order in which behaviors execute around your handlers.

### Default Ordering

```csharp
// Default behavior order (lower numbers execute first/outer)
public class BehaviorOrder
{
    public int Logging { get; set; } = 1;                    // Outermost
    public int PerformanceMonitoring { get; set; } = 2;
    public int Validation { get; set; } = 3;
    public int Caching { get; set; } = 4;
    public int Retry { get; set; } = 5;                      // Innermost
}

// Execution flow:
// Logging -> Performance -> Validation -> Caching -> Retry -> Handler -> Retry -> Caching -> Validation -> Performance -> Logging
```

### Custom Ordering

```csharp
services.AddFunctionalKit(options =>
{
    // Custom behavior order
    options.BehaviorOrder.Logging = 1;           // First (outermost)
    options.BehaviorOrder.Validation = 2;        // Validate early
    options.BehaviorOrder.PerformanceMonitoring = 3;
    options.BehaviorOrder.Caching = 4;
    options.BehaviorOrder.Retry = 5;             // Last (innermost)
});
```

### Ordered Behavior Registration

```csharp
// Register behaviors with explicit ordering
public class OrderedBehaviorRegistration
{
    public static void RegisterBehaviors(IServiceCollection services)
    {
        // Register in desired execution order
        services.AddScoped(typeof(IQueryPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IQueryPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddScoped(typeof(IQueryPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IQueryPipelineBehavior<,>), typeof(CachingBehavior<,>));
        services.AddScoped(typeof(IQueryPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
    }
}
```

## Advanced Patterns

### Conditional Behaviors

```csharp
public class ConditionalValidationBehavior<TRequest, TResponse> : ICommandPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IConfiguration _configuration;

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        // Only validate in certain environments
        var shouldValidate = _configuration.GetValue<bool>("EnableStrictValidation");
        
        if (shouldValidate && request is IValidatable validatable)
        {
            var validation = validatable.Validate();
            if (validation.IsInvalid)
                throw new ValidationException(string.Join("; ", validation.Errors));
        }

        return await next();
    }
}
```

### Behavior Composition

```csharp
public class CompositeBehavior<TRequest, TResponse> : IQueryPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
{
    private readonly IEnumerable<IBehaviorComponent<TRequest, TResponse>> _components;

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var pipeline = next;

        // Compose behaviors in reverse order
        foreach (var component in _components.Reverse())
        {
            var currentPipeline = pipeline;
            pipeline = () => component.ExecuteAsync(request, currentPipeline, cancellationToken);
        }

        return await pipeline();
    }
}

public interface IBehaviorComponent<TRequest, TResponse>
{
    Task<TResponse> ExecuteAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken);
}
```

### Feature Flag Behavior

```csharp
public interface IFeatureGated
{
    string FeatureName { get; }
}

public class FeatureFlagBehavior<TRequest, TResponse> : IQueryPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>, IFeatureGated
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly ICurrentUserService _currentUserService;

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var isFeatureEnabled = await _featureFlagService.IsEnabledAsync(
            request.FeatureName, 
            currentUser?.Id);

        if (!isFeatureEnabled)
        {
            throw new FeatureNotAvailableException($"Feature '{request.FeatureName}' is not available");
        }

        return await next();
    }
}
```

### Metrics Collection Behavior

```csharp
public class MetricsBehavior<TRequest, TResponse> : IQueryPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
{
    private readonly IMetricsCollector _metricsCollector;

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var requestType = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            
            stopwatch.Stop();
            _metricsCollector.RecordQuerySuccess(requestType, stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metricsCollector.RecordQueryFailure(requestType, ex.GetType().Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

## Performance Considerations

### Behavior Overhead

```csharp
// Minimize allocations in behaviors
public class OptimizedBehavior<TRequest, TResponse> : IQueryPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
{
    private static readonly ActivitySource ActivitySource = new("MyApp.Behaviors");

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        // Use static activity source to avoid allocations
        using var activity = ActivitySource.StartActivity(typeof(TRequest).Name);
        
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

### Conditional Execution

```csharp
public class SmartCachingBehavior<TRequest, TResponse> : IQueryPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        // Only apply caching to cacheable requests
        if (request is not ICacheable cacheable)
            return await next();

        // Check cache size limits
        if (_cache.Count > MAX_CACHE_SIZE)
            return await next();

        // Apply caching logic only when beneficial
        return await ApplyCachingAsync(cacheable, next, cancellationToken);
    }
}
```

### Async Optimization

```csharp
public class AsyncOptimizedBehavior<TRequest, TResponse> : IQueryPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        // Use ValueTask for potential synchronous completion
        var preProcessingTask = PreProcessAsync(request);
        
        // Don't await immediately if not needed
        var response = await next();
        
        // Await preprocessing if needed
        if (!preProcessingTask.IsCompletedSuccessfully)
            await preProcessingTask;

        return response;
    }

    private ValueTask PreProcessAsync(TRequest request)
    {
        // Return completed task if no async work needed
        if (!NeedsPreProcessing(request))
            return ValueTask.CompletedTask;

        return DoPreProcessingAsync(request);
    }
}
```

## Testing Behaviors

### Unit Testing Behaviors

```csharp
[TestClass]
public class ValidationBehaviorTests
{
    [TestMethod]
    public async Task HandleAsync_ValidRequest_CallsNext()
    {
        // Arrange
        var validCommand = new ValidCreateUserCommand("John", "john@example.com");
        var behavior = new ValidationBehavior<ValidCreateUserCommand, Result<int>>();
        var nextCalled = false;
        
        Task<Result<int>> Next()
        {
            nextCalled = true;
            return Task.FromResult(Result<int>.Success(123));
        }

        // Act
        var result = await behavior.HandleAsync(validCommand, Next);

        // Assert
        Assert.IsTrue(nextCalled);
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task HandleAsync_InvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var invalidCommand = new InvalidCreateUserCommand("", ""); // Invalid data
        var behavior = new ValidationBehavior<InvalidCreateUserCommand, Result<int>>();
        
        Task<Result<int>> Next() => Task.FromResult(Result<int>.Success(123));

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ValidationException>(
            () => behavior.HandleAsync(invalidCommand, Next));
    }
}
```

### Integration Testing with Behaviors

```csharp
[TestClass]
public class BehaviorIntegrationTests
{
    private TestServer _server;
    private HttpClient _client;

    [TestInitialize]
    public void Setup()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Enable specific behaviors for testing
                    services.AddFunctionalKit(options =>
                    {
                        options.EnableValidation = true;
                        options.EnableLogging = true;
                    });
                });
            });

        _server = factory.Server;
        _client = factory.CreateClient();
    }

    [TestMethod]
    public async Task CreateUser_InvalidData_ReturnsBadRequestDueToValidation()
    {
        // Arrange
        var invalidCommand = new { Name = "", Email = "invalid" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", invalidCommand);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.IsTrue(error.Message.Contains("validation"));
    }
}
```

### Behavior Mock Testing

```csharp
[TestClass]
public class BehaviorMockTests
{
    [TestMethod]
    public async Task AuthorizationBehavior_UnauthorizedUser_ThrowsException()
    {
        // Arrange
        var mockUserService = new Mock<ICurrentUserService>();
        var mockAuthService = new Mock<IAuthorizationService>();
        
        mockUserService.Setup(s => s.GetCurrentUser()).Returns((User)null);
        
        var behavior = new AuthorizationBehavior<SecureQuery, SecureData>(
            mockUserService.Object, 
            mockAuthService.Object);
        
        var secureQuery = new SecureQuery();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<UnauthorizedException>(
            () => behavior.HandleAsync(secureQuery, () => Task.FromResult(new SecureData())));
    }
}
```

Pipeline behaviors are a powerful way to implement cross-cutting concerns in a clean, testable, and maintainable way. They help keep your handlers focused on business logic while ensuring consistent application of infrastructure concerns across your entire application.