# CQRS Guide with FunctionalKit

Complete guide to implementing CQRS (Command Query Responsibility Segregation) using FunctionalKit's messaging system.

## Table of Contents

- [CQRS Overview](#cqrs-overview)
- [Basic Setup](#basic-setup)
- [Queries](#queries)
- [Commands](#commands)
- [Notifications](#notifications)
- [Pipeline Behaviors](#pipeline-behaviors)
- [Advanced Patterns](#advanced-patterns)
- [Error Handling](#error-handling)
- [Performance Optimization](#performance-optimization)
- [Testing CQRS](#testing-cqrs)

## CQRS Overview

CQRS separates read and write operations into different models:
- **Queries**: Read operations that return data without side effects
- **Commands**: Write operations that modify state
- **Notifications**: Events that notify about state changes

FunctionalKit's messaging system (`IMessenger`) provides a clean implementation of CQRS patterns.

## Basic Setup

### Service Registration

```csharp
// Program.cs
using FunctionalKit.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register FunctionalKit with automatic handler discovery
builder.Services.AddFunctionalKit(Assembly.GetExecutingAssembly());

// Add other services
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();
```

### Controller Setup

```csharp
[ApiController]
[Route("api/[controller]")]
public class BaseController : ControllerBase
{
    protected readonly IMessenger Messenger;

    public BaseController(IMessenger messenger)
    {
        Messenger = messenger;
    }

    protected ActionResult<T> HandleResult<T>(Result<T> result)
    {
        return result.Match(
            onSuccess: value => Ok(value),
            onFailure: error => BadRequest(new { Error = error })
        );
    }

    protected ActionResult HandleResult(Result<Unit> result)
    {
        return result.Match(
            onSuccess: _ => NoContent(),
            onFailure: error => BadRequest(new { Error = error })
        );
    }
}
```

## Queries

Queries are read-only operations that return data without modifying state.

### Basic Query

```csharp
// Query definition
public record GetUserByIdQuery(int UserId) : IQuery<Result<UserDto>>;

// Query handler
public class GetUserByIdHandler : IQueryHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await _userRepository.FindByIdAsync(query.UserId)
            .ToResult($"User with ID {query.UserId} not found")
            .MapAsync(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
    }
}

// Controller usage
[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> GetUser(int id)
{
    var result = await Messenger.QueryAsync(new GetUserByIdQuery(id));
    return HandleResult(result);
}
```

### Complex Query with Filtering

```csharp
public record GetUsersQuery(
    string? NameFilter = null,
    bool? IsActive = null,
    DateTime? CreatedAfter = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedResult<UserDto>>;

public class GetUsersHandler : IQueryHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public async Task<PagedResult<UserDto>> HandleAsync(GetUsersQuery query, CancellationToken cancellationToken = default)
    {
        var specification = new UserSpecification()
            .WithNameFilter(query.NameFilter)
            .WithActiveFilter(query.IsActive)
            .WithCreatedAfterFilter(query.CreatedAfter);

        var users = await _userRepository.GetPagedAsync(specification, query.PageNumber, query.PageSize);
        
        var userDtos = users.Items.Select(user => new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        }).ToList();

        return new PagedResult<UserDto>(userDtos, users.TotalCount, query.PageNumber, query.PageSize);
    }
}
```

### Query with Caching

```csharp
public record GetUserProfileQuery(int UserId) : IQuery<UserProfileDto>, ICacheable
{
    public string CacheKey => $"user-profile:{UserId}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

public class GetUserProfileHandler : IQueryHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IUserRepository _userRepository;

    public async Task<UserProfileDto> HandleAsync(GetUserProfileQuery query, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetUserWithProfileAsync(query.UserId);
        
        if (user == null)
            throw new NotFoundException($"User {query.UserId} not found");

        return new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Bio = user.Profile?.Bio,
            AvatarUrl = user.Profile?.AvatarUrl,
            Preferences = user.Profile?.Preferences ?? new UserPreferences()
        };
    }
}
```

### Aggregation Query

```csharp
public record GetUserStatisticsQuery(DateTime? FromDate = null, DateTime? ToDate = null) : IQuery<UserStatisticsDto>;

public class GetUserStatisticsHandler : IQueryHandler<GetUserStatisticsQuery, UserStatisticsDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IOrderRepository _orderRepository;

    public async Task<UserStatisticsDto> HandleAsync(GetUserStatisticsQuery query, CancellationToken cancellationToken = default)
    {
        var fromDate = query.FromDate ?? DateTime.UtcNow.AddMonths(-12);
        var toDate = query.ToDate ?? DateTime.UtcNow;

        var tasks = new[]
        {
            _userRepository.GetUserCountAsync(fromDate, toDate),
            _userRepository.GetActiveUserCountAsync(fromDate, toDate),
            _userRepository.GetNewUserCountAsync(fromDate, toDate),
            _orderRepository.GetOrderCountByUsersAsync(fromDate, toDate)
        };

        var results = await Task.WhenAll(tasks);

        return new UserStatisticsDto
        {
            TotalUsers = results[0],
            ActiveUsers = results[1],
            NewUsers = results[2],
            UsersWithOrders = results[3],
            Period = new DatePeriod(fromDate, toDate)
        };
    }
}
```

## Commands

Commands are write operations that modify state and may or may not return data.

### Basic Command

```csharp
// Command without return value
public record DeleteUserCommand(int UserId) : ICommand;

public class DeleteUserHandler : ICommandHandler<DeleteUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IMessenger _messenger;

    public async Task HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByIdAsync(command.UserId);
        if (user.IsEmpty)
            throw new NotFoundException($"User {command.UserId} not found");

        await _userRepository.DeleteAsync(command.UserId);
        
        // Publish notification
        await _messenger.PublishAsync(new UserDeletedEvent(command.UserId, user.Value.Email));
    }
}
```

### Command with Return Value

```csharp
public record CreateUserCommand(string Name, string Email, string Password) : ICommand<Result<int>>;

public class CreateUserHandler : ICommandHandler<CreateUserCommand, Result<int>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMessenger _messenger;

    public async Task<Result<int>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        var existingUser = await _userRepository.FindByEmailAsync(command.Email);
        if (existingUser.HasValue)
            return Result<int>.Failure("Email already exists");

        // Create user
        var hashedPassword = _passwordHasher.HashPassword(command.Password);
        var user = new User(command.Name, command.Email, hashedPassword);
        
        await _userRepository.SaveAsync(user);

        // Publish notification
        await _messenger.PublishAsync(new UserCreatedEvent(user.Id, user.Name, user.Email));

        return Result<int>.Success(user.Id);
    }
}
```

### Command with Validation

```csharp
public record UpdateUserCommand(int UserId, string Name, string Email) : ICommand<Result<UserDto>>, IValidatable
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
            : Name.Length > 100
                ? Validation<string>.Failure("Name cannot exceed 100 characters")
                : Validation<string>.Success(Name);
    }

    private Validation<string> ValidateEmail()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return Validation<string>.Failure("Email is required");

        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        return emailRegex.IsMatch(Email)
            ? Validation<string>.Success(Email)
            : Validation<string>.Failure("Invalid email format");
    }
}

public class UpdateUserHandler : ICommandHandler<UpdateUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMessenger _messenger;

    public async Task<Result<UserDto>> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        return await _userRepository.FindByIdAsync(command.UserId)
            .ToResult($"User {command.UserId} not found")
            .Then(async user => await CheckEmailUniqueness(user, command.Email))
            .Then(user => ApplyUpdates(user, command))
            .Then(async user => await SaveAndNotify(user));
    }

    private async Task<Result<User>> CheckEmailUniqueness(User user, string newEmail)
    {
        if (user.Email == newEmail)
            return Result<User>.Success(user);

        var existingUser = await _userRepository.FindByEmailAsync(newEmail);
        return existingUser.HasValue
            ? Result<User>.Failure("Email already exists")
            : Result<User>.Success(user);
    }

    private Result<User> ApplyUpdates(User user, UpdateUserCommand command)
    {
        user.UpdateName(command.Name);
        user.UpdateEmail(command.Email);
        return Result<User>.Success(user);
    }

    private async Task<Result<UserDto>> SaveAndNotify(User user)
    {
        await _userRepository.SaveAsync(user);
        
        await _messenger.PublishAsync(new UserUpdatedEvent(user.Id, user.Name, user.Email));

        return Result<UserDto>.Success(new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }
}
```

### Complex Command with Railway Programming

```csharp
public record ProcessOrderCommand(int CustomerId, List<OrderItemRequest> Items, PaymentInfo Payment) : ICommand<Result<OrderDto>>;

public class ProcessOrderHandler : ICommandHandler<ProcessOrderCommand, Result<OrderDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IPaymentService _paymentService;
    private readonly IOrderRepository _orderRepository;
    private readonly IMessenger _messenger;

    public async Task<Result<OrderDto>> HandleAsync(ProcessOrderCommand command, CancellationToken cancellationToken = default)
    {
        return await Railway.StartWith(command)
            .Then(ValidateCommand)
            .Then(async cmd => await ValidateCustomerAsync(cmd))
            .Then(async cmd => await ReserveInventoryAsync(cmd))
            .Then(async cmd => await ProcessPaymentAsync(cmd))
            .Then(async cmd => await CreateOrderAsync(cmd))
            .ThenDo(async order => await PublishOrderEventsAsync(order))
            .MapAsync(order => MapToOrderDto(order));
    }

    private Result<ProcessOrderCommand> ValidateCommand(ProcessOrderCommand command)
    {
        if (!command.Items.Any())
            return Result<ProcessOrderCommand>.Failure("Order must contain at least one item");

        if (command.Items.Any(i => i.Quantity <= 0))
            return Result<ProcessOrderCommand>.Failure("All items must have positive quantity");

        return Result<ProcessOrderCommand>.Success(command);
    }

    private async Task<Result<ValidatedOrderCommand>> ValidateCustomerAsync(ProcessOrderCommand command)
    {
        var customer = await _customerRepository.FindByIdAsync(command.CustomerId);
        
        return customer
            .ToResult($"Customer {command.CustomerId} not found")
            .ThenIf(c => c.IsActive, "Customer account is inactive")
            .ThenIf(c => c.CanPlaceOrders, "Customer cannot place orders")
            .Map(c => new ValidatedOrderCommand(command, c));
    }

    private async Task<Result<OrderWithInventory>> ReserveInventoryAsync(ValidatedOrderCommand command)
    {
        var reservationTasks = command.Command.Items.Select(async item =>
            await _inventoryService.ReserveAsync(item.ProductId, item.Quantity));

        var results = await Task.WhenAll(reservationTasks);
        var failures = results.Where(r => r.IsFailure).ToList();

        if (failures.Any())
        {
            // Rollback successful reservations
            var successes = results.Where(r => r.IsSuccess);
            await Task.WhenAll(successes.Select(s => _inventoryService.ReleaseAsync(s.Value.Id)));
            
            var errors = string.Join(", ", failures.Select(f => f.Error));
            return Result<OrderWithInventory>.Failure($"Inventory reservation failed: {errors}");
        }

        return Result<OrderWithInventory>.Success(
            new OrderWithInventory(command, results.Select(r => r.Value).ToList()));
    }
}
```

## Notifications

Notifications (events) are published when something significant happens and can have multiple handlers.

### Basic Notification

```csharp
// Event definition
public record UserCreatedEvent(int UserId, string Name, string Email) : INotification;

// Multiple handlers for the same event
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly IEmailService _emailService;

    public async Task HandleAsync(UserCreatedEvent notification, CancellationToken cancellationToken = default)
    {
        await _emailService.SendWelcomeEmailAsync(notification.Email, notification.Name);
    }
}

public class CreateUserProfileHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly IUserProfileService _profileService;

    public async Task HandleAsync(UserCreatedEvent notification, CancellationToken cancellationToken = default)
    {
        await _profileService.CreateDefaultProfileAsync(notification.UserId);
    }
}

public class UpdateAnalyticsHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly IAnalyticsService _analyticsService;

    public async Task HandleAsync(UserCreatedEvent notification, CancellationToken cancellationToken = default)
    {
        await _analyticsService.TrackUserRegistrationAsync(notification.UserId);
    }
}
```

### Complex Event with Error Handling

```csharp
public record OrderProcessedEvent(
    int OrderId, 
    int CustomerId, 
    decimal Total, 
    List<OrderItem> Items,
    DateTime ProcessedAt) : INotification;

public class UpdateInventoryHandler : INotificationHandler<OrderProcessedEvent>
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<UpdateInventoryHandler> _logger;

    public async Task HandleAsync(OrderProcessedEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var item in notification.Items)
            {
                await _inventoryService.UpdateStockAsync(item.ProductId, -item.Quantity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update inventory for order {OrderId}", notification.OrderId);
            // Don't rethrow - other handlers should still run
        }
    }
}

public class SendOrderConfirmationHandler : INotificationHandler<OrderProcessedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ICustomerRepository _customerRepository;

    public async Task HandleAsync(OrderProcessedEvent notification, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.FindByIdAsync(notification.CustomerId);
        if (customer.IsEmpty)
            return;

        await _emailService.SendOrderConfirmationAsync(
            customer.Value.Email,
            notification.OrderId,
            notification.Total,
            notification.Items);
    }
}
```

### Event Sourcing Pattern

```csharp
public abstract record DomainEvent : INotification
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
}

public record UserEmailChangedEvent(int UserId, string OldEmail, string NewEmail) : DomainEvent;
public record UserPasswordChangedEvent(int UserId, DateTime ChangedAt) : DomainEvent;
public record UserDeactivatedEvent(int UserId, string Reason) : DomainEvent;

// Event store handler
public class EventStoreHandler<T> : INotificationHandler<T> where T : DomainEvent
{
    private readonly IEventStore _eventStore;

    public async Task HandleAsync(T notification, CancellationToken cancellationToken = default)
    {
        await _eventStore.SaveEventAsync(new StoredEvent
        {
            Id = notification.Id,
            EventType = notification.EventType,
            Data = JsonSerializer.Serialize(notification),
            OccurredAt = notification.OccurredAt
        });
    }
}
```

## Pipeline Behaviors

Pipeline behaviors provide cross-cutting concerns that run before and after handlers.

### Validation Behavior

```csharp
// Commands and queries can implement IValidatable for automatic validation
public record CreateProductCommand(string Name, decimal Price, int CategoryId) : ICommand<Result<int>>, IValidatable
{
    public Validation<Unit> Validate()
    {
        return ValidateName()
            .Combine(ValidatePrice(), (name, price) => name)
            .Combine(ValidateCategoryId(), (namePrice, categoryId) => Unit.Value);
    }

    private Validation<string> ValidateName()
    {
        return string.IsNullOrWhiteSpace(Name)
            ? Validation<string>.Failure("Product name is required")
            : Name.Length > 200
                ? Validation<string>.Failure("Product name cannot exceed 200 characters")
                : Validation<string>.Success(Name);
    }

    private Validation<decimal> ValidatePrice()
    {
        return Price < 0
            ? Validation<decimal>.Failure("Price cannot be negative")
            : Price > 1000000
                ? Validation<decimal>.Failure("Price cannot exceed 1,000,000")
                : Validation<decimal>.Success(Price);
    }

    private Validation<int> ValidateCategoryId()
    {
        return CategoryId <= 0
            ? Validation<int>.Failure("Category ID must be positive")
            : Validation<int>.Success(CategoryId);
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

public record DeleteUserCommand(int UserId) : ICommand, IRequireAuthorization
{
    public string[] RequiredRoles => new[] { "Admin", "UserManager" };
    public string[] RequiredPermissions => new[] { "Users.Delete" };
}

public class AuthorizationBehavior<TRequest> : ICommandPipelineBehavior<TRequest>
    where TRequest : ICommand, IRequireAuthorization
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public async Task HandleAsync(TRequest request, Func<Task> next, CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        if (currentUser == null)
            throw new UnauthorizedException("User not authenticated");

        var hasRequiredRole = request.RequiredRoles.Any(role => currentUser.IsInRole(role));
        if (!hasRequiredRole)
            throw new ForbiddenException("Insufficient role permissions");

        var hasRequiredPermissions = await _authorizationService.HasPermissionsAsync(
            currentUser.Id, request.RequiredPermissions);
        if (!hasRequiredPermissions)
            throw new ForbiddenException("Insufficient permissions");

        await next();
    }
}
```

### Audit Logging Behavior

```csharp
public class AuditLoggingBehavior<TRequest, TResponse> : ICommandPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var commandType = typeof(TRequest).Name;

        // Log command execution start
        var auditEntry = new AuditEntry
        {
            UserId = currentUser?.Id,
            Action = commandType,
            RequestData = JsonSerializer.Serialize(request),
            Timestamp = DateTime.UtcNow
        };

        await _auditService.LogAsync(auditEntry);

        try
        {
            var response = await next();

            // Log successful completion
            auditEntry.Success = true;
            auditEntry.ResponseData = JsonSerializer.Serialize(response);
            await _auditService.UpdateAsync(auditEntry);

            return response;
        }
        catch (Exception ex)
        {
            // Log failure
            auditEntry.Success = false;
            auditEntry.ErrorMessage = ex.Message;
            await _auditService.UpdateAsync(auditEntry);
            throw;
        }
    }
}
```

## Advanced Patterns

### Saga Pattern with Commands

```csharp
public class OrderProcessingSaga
{
    private readonly IMessenger _messenger;

    public async Task<Result<Unit>> ProcessOrderSagaAsync(ProcessOrderCommand initialCommand)
    {
        var sagaId = Guid.NewGuid();
        
        try
        {
            // Step 1: Reserve inventory
            var inventoryResult = await _messenger.SendAsync(
                new ReserveInventoryCommand(sagaId, initialCommand.Items));
            if (inventoryResult.IsFailure)
                return Result<Unit>.Failure($"Inventory reservation failed: {inventoryResult.Error}");

            // Step 2: Process payment
            var paymentResult = await _messenger.SendAsync(
                new ProcessPaymentCommand(sagaId, initialCommand.Payment));
            if (paymentResult.IsFailure)
            {
                // Compensate: Release inventory
                await _messenger.SendAsync(new ReleaseInventoryCommand(sagaId));
                return Result<Unit>.Failure($"Payment failed: {paymentResult.Error}");
            }

            // Step 3: Create order
            var orderResult = await _messenger.SendAsync(
                new CreateOrderCommand(sagaId, initialCommand));
            if (orderResult.IsFailure)
            {
                // Compensate: Refund payment and release inventory
                await _messenger.SendAsync(new RefundPaymentCommand(sagaId));
                await _messenger.SendAsync(new ReleaseInventoryCommand(sagaId));
                return Result<Unit>.Failure($"Order creation failed: {orderResult.Error}");
            }

            // Success: Publish completion event
            await _messenger.PublishAsync(new OrderSagaCompletedEvent(sagaId, orderResult.Value));
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            // Global compensation
            await CompensateAsync(sagaId);
            return Result<Unit>.Failure($"Saga failed: {ex.Message}");
        }
    }

    private async Task CompensateAsync(Guid sagaId)
    {
        // Run all compensation actions
        var compensationTasks = new[]
        {
            _messenger.SendAsync(new RefundPaymentCommand(sagaId)),
            _messenger.SendAsync(new ReleaseInventoryCommand(sagaId)),
            _messenger.SendAsync(new CancelOrderCommand(sagaId))
        };

        await Task.WhenAll(compensationTasks);
        await _messenger.PublishAsync(new OrderSagaFailedEvent(sagaId));
    }
}
```

### Request/Response Pattern

```csharp
public record RequestDataQuery(string ExternalId) : IQuery<Result<ExternalData>>;

public class RequestDataHandler : IQueryHandler<RequestDataQuery, Result<ExternalData>>
{
    private readonly IExternalApiService _externalApiService;
    private readonly IMemoryCache _cache;

    public async Task<Result<ExternalData>> HandleAsync(RequestDataQuery query, CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cacheKey = $"external-data:{query.ExternalId}";
        if (_cache.TryGetValue(cacheKey, out ExternalData cachedData))
            return Result<ExternalData>.Success(cachedData);

        // Make external API call
        try
        {
            var data = await _externalApiService.GetDataAsync(query.ExternalId, cancellationToken);
            
            // Cache the result
            _cache.Set(cacheKey, data, TimeSpan.FromMinutes(30));
            
            return Result<ExternalData>.Success(data);
        }
        catch (HttpRequestException ex)
        {
            return Result<ExternalData>.Failure($"External API error: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return Result<ExternalData>.Failure("Request timed out");
        }
    }
}
```

### Batch Processing Pattern

```csharp
public record ProcessBatchCommand(List<BatchItem> Items) : ICommand<Result<BatchProcessingResult>>;

public class ProcessBatchHandler : ICommandHandler<ProcessBatchCommand, Result<BatchProcessingResult>>
{
    private readonly IMessenger _messenger;

    public async Task<Result<BatchProcessingResult>> HandleAsync(ProcessBatchCommand command, CancellationToken cancellationToken = default)
    {
        var semaphore = new SemaphoreSlim(10); // Limit concurrency
        var results = new ConcurrentBag<Result<ProcessedItem>>();

        var processingTasks = command.Items.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = await _messenger.SendAsync(new ProcessItemCommand(item.Id, item.Data));
                results.Add(result);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(processingTasks);

        var (successes, failures) = results.Partition();
        
        return Result<BatchProcessingResult>.Success(new BatchProcessingResult
        {
            TotalItems = command.Items.Count,
            SuccessfulItems = successes.Count(),
            FailedItems = failures.Count(),
            Errors = failures.ToList()
        });
    }
}
```

## Error Handling

### Centralized Error Handling

```csharp
public class GlobalExceptionBehavior<TRequest, TResponse> : IQueryPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
{
    private readonly ILogger<GlobalExceptionBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        try
        {
            return await next();
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found for request {RequestType}", typeof(TRequest).Name);
            throw;
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for request {RequestType}", typeof(TRequest).Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in {RequestType}", typeof(TRequest).Name);
            throw new ApplicationException("An unexpected error occurred", ex);
        }
    }
}
```

### Result-Based Error Handling

```csharp
public class SafeCommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<Result<TResponse>>
{
    private readonly IActualHandler<TCommand, TResponse> _innerHandler;
    private readonly ILogger<SafeCommandHandler<TCommand, TResponse>> _logger;

    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _innerHandler.HandleAsync(command, cancellationToken);
            return Result<TResponse>.Success(result);
        }
        catch (ValidationException ex)
        {
            return Result<TResponse>.Failure($"Validation error: {ex.Message}");
        }
        catch (NotFoundException ex)
        {
            return Result<TResponse>.Failure($"Not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling command {CommandType}", typeof(TCommand).Name);
            return Result<TResponse>.Failure("An unexpected error occurred");
        }
    }
}
```

## Performance Optimization

### Caching Strategy

```csharp
public record GetProductCatalogQuery(int CategoryId, int Page = 1, int PageSize = 20) : IQuery<ProductCatalogDto>, ICacheable
{
    public string CacheKey => $"product-catalog:{CategoryId}:{Page}:{PageSize}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(30);
}

// Cache invalidation on updates
public class UpdateProductHandler : ICommandHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMemoryCache _cache;
    private readonly IMessenger _messenger;

    public async Task<Result<ProductDto>> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        var result = await UpdateProductInternalAsync(command);
        
        if (result.IsSuccess)
        {
            // Invalidate related cache entries
            InvalidateProductCaches(command.ProductId, result.Value.CategoryId);
            
            // Publish event for distributed cache invalidation
            await _messenger.PublishAsync(new ProductUpdatedEvent(command.ProductId, result.Value.CategoryId));
        }

        return result;
    }

    private void InvalidateProductCaches(int productId, int categoryId)
    {
        // Remove specific product caches
        _cache.Remove($"product:{productId}");
        
        // Remove category catalog caches (all pages)
        var catalogKeys = GetCatalogCacheKeys(categoryId);
        foreach (var key in catalogKeys)
        {
            _cache.Remove(key);
        }
    }
}
```

### Async Patterns

```csharp
public class OptimizedOrderHandler : ICommandHandler<CreateOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        // Parallel data loading
        var customerTask = _customerRepository.FindByIdAsync(command.CustomerId);
        var productsTask = _productRepository.GetByIdsAsync(command.Items.Select(i => i.ProductId));
        var discountsTask = _discountService.GetApplicableDiscountsAsync(command.CustomerId);

        // Wait for all data
        var customer = await customerTask;
        var products = await productsTask;
        var discounts = await discountsTask;

        // Validate and process
        return await ProcessOrderWithDataAsync(command, customer, products, discounts);
    }
}
```

## Testing CQRS

### Unit Testing Handlers

```csharp
[TestClass]
public class CreateUserHandlerTests
{
    private Mock<IUserRepository> _mockRepository;
    private Mock<IMessenger> _mockMessenger;
    private CreateUserHandler _handler;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockMessenger = new Mock<IMessenger>();
        _handler = new CreateUserHandler(_mockRepository.Object, _mockMessenger.Object);
    }

    [TestMethod]
    public async Task HandleAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new CreateUserCommand("John Doe", "john@example.com", "password123");
        _mockRepository.Setup(r => r.FindByEmailAsync("john@example.com"))
            .ReturnsAsync(Optional<User>.Empty());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Once);
        _mockMessenger.Verify(m => m.PublishAsync(It.IsAny<UserCreatedEvent>(), default), Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var command = new CreateUserCommand("John Doe", "existing@example.com", "password123");
        var existingUser = new User("Existing", "existing@example.com", "hash");
        _mockRepository.Setup(r => r.FindByEmailAsync("existing@example.com"))
            .ReturnsAsync(Optional<User>.Of(existingUser));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.IsTrue(result.Error.Contains("already exists"));
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Never);
    }
}
```

### Integration Testing

```csharp
[TestClass]
public class UserManagementIntegrationTests
{
    private TestServer _server;
    private HttpClient _client;
    private IServiceScope _scope;

    [TestInitialize]
    public async Task Setup()
    {
        var factory = new WebApplicationFactory<Program>();
        _server = factory.Server;
        _client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
        
        await SeedTestDataAsync();
    }

    [TestMethod]
    public async Task CreateUser_ValidData_ReturnsCreated()
    {
        // Arrange
        var command = new CreateUserCommand("Test User", "test@example.com", "password123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var userId = await response.Content.ReadFromJsonAsync<int>();
        Assert.IsTrue(userId > 0);

        // Verify user was created
        var getResponse = await _client.GetAsync($"/api/users/{userId}");
        getResponse.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task GetUser_NonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/users/99999");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

This CQRS guide provides a complete foundation for implementing command-query separation using FunctionalKit's messaging system. The patterns shown here will help you build scalable, maintainable applications with clear separation of concerns between read and write operations.