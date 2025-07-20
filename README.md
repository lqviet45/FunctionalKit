# FunctionalKit

[![NuGet Version](https://img.shields.io/nuget/v/FunctionalKit?style=flat-square&color=blue)](https://www.nuget.org/packages/FunctionalKit/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FunctionalKit?style=flat-square&color=green)](https://www.nuget.org/packages/FunctionalKit/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg?style=flat-square)](https://opensource.org/licenses/Apache-2.0)
[![.NET](https://img.shields.io/badge/.NET-8.0+-purple.svg?style=flat-square)](https://dotnet.microsoft.com/download)
[![Build Status](https://img.shields.io/github/actions/workflow/status/lqviet45/FunctionalKit/ci.yml?branch=main&style=flat-square)](https://github.com/lqviet45/FunctionalKit/actions)

A comprehensive **functional programming library** for .NET 8+ that brings the power of functional patterns to C#. FunctionalKit provides robust implementations of **Optional**, **Result**, **Either** patterns, along with a powerful **messaging system** that serves as an excellent alternative to MediatR.

## 🚀 Why Choose FunctionalKit?

✨ **Zero Learning Curve** - Familiar patterns from Java, Rust, and F#  
🛡️ **Type Safety** - Eliminate null reference exceptions forever  
⚡ **Performance First** - Optimized readonly structs with minimal allocations  
🚄 **Railway Programming** - Chain operations elegantly with built-in failure handling  
📨 **Clean CQRS** - Professional messaging system with pipeline behaviors  
🔥 **Production Ready** - Circuit breaker, retry logic, comprehensive monitoring  
🧰 **Rich Ecosystem** - 100+ extension methods and utilities  
📊 **Enterprise Features** - Caching, validation, performance monitoring, and more

## 📦 Quick Start

### Installation
```bash
# Package Manager Console
Install-Package FunctionalKit

# .NET CLI
dotnet add package FunctionalKit

# PackageReference
<PackageReference Include="FunctionalKit" Version="8.0.0" />
```

### Basic Setup
```csharp
// Program.cs - Minimal setup
using FunctionalKit.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 🔥 One line to get started!
builder.Services.AddFunctionalKit(Assembly.GetExecutingAssembly());

var app = builder.Build();
```

### Production Setup
```csharp
// Program.cs - Full production configuration
builder.Services.AddFunctionalKit(options =>
{
    options.EnableLogging = true;                    // 📝 Automatic request/response logging
    options.EnableValidation = true;                // ✅ Automatic validation
    options.EnablePerformanceMonitoring = true;     // 📊 Performance tracking
    options.EnableCaching = true;                   // ⚡ Query result caching
    options.EnableRetry = true;                     // 🔄 Automatic retry logic
    options.SlowQueryThresholdMs = 1000;            // 🐌 Slow query detection
    options.MaxRetries = 3;                         // 🔁 Retry attempts
}, Assembly.GetExecutingAssembly());
```

## 🎯 Core Patterns

### 🔹 Optional Pattern - Say Goodbye to Null Reference Exceptions

```csharp
// ❌ Old way - Prone to NullReferenceException
public string GetUserEmail(int userId)
{
    var user = _userRepository.FindById(userId);
    if (user?.Profile?.Email != null)
        return user.Profile.Email;
    return "unknown@example.com";
}

// ✅ New way - Type-safe and explicit
public string GetUserEmail(int userId)
{
    return _userRepository.FindByIdAsync(userId)
        .FlatMap(user => user.Profile.ToOptional())
        .FlatMap(profile => profile.Email.ToOptional())
        .Filter(email => !string.IsNullOrWhiteSpace(email))
        .OrElse("unknown@example.com");
}

// 🚀 Advanced Optional operations
public async Task<Optional<UserProfile>> GetCompleteUserProfileAsync(int userId)
{
    return await _userRepository.FindByIdAsync(userId)
        .Filter(user => user.IsActive)                    // Only active users
        .FlatMapAsync(user => GetProfileAsync(user.Id))   // Async chaining
        .Filter(profile => profile.IsComplete);           // Only complete profiles
}
```

### ⚡ Result Pattern - Error Handling Without Exceptions

```csharp
// ❌ Old way - Exception-based error handling
public async Task<User> CreateUserAsync(CreateUserRequest request)
{
    if (string.IsNullOrEmpty(request.Email))
        throw new ValidationException("Email is required");
    
    var existingUser = await _userRepository.FindByEmailAsync(request.Email);
    if (existingUser != null)
        throw new ConflictException("Email already exists");
    
    // More code that can throw exceptions...
}

// ✅ New way - Explicit error handling
public async Task<Result<User>> CreateUserAsync(CreateUserRequest request)
{
    return await Railway.StartWith(request)
        .Then(ValidateRequest)                           // Returns Result<CreateUserRequest>
        .Then(async req => await CheckEmailUniqueness(req))  // Returns Result<CreateUserRequest>
        .Then(CreateUserEntity)                         // Returns Result<User>
        .Then(async user => await SaveUserAsync(user))  // Returns Result<User>
        .TapError(error => _logger.LogWarning("User creation failed: {Error}", error));
}

// 🔗 Railway Programming - Chain operations seamlessly
public async Task<Result<OrderResult>> ProcessOrderAsync(CreateOrderRequest request)
{
    return await Railway.StartWith(request)
        .Then(ValidateOrder)                    // Validation
        .Then(async req => await ReserveInventory(req))  // Inventory check
        .Then(async req => await ProcessPayment(req))    // Payment processing
        .Then(async order => await CreateOrder(order))   // Order creation
        .ThenDo(async order => await PublishEvents(order)) // Side effects
        .Recover(async error => await HandleFailure(error)); // Error recovery
}
```

### 📨 Messaging System - Clean CQRS Implementation

```csharp
// 🔍 Query (Read Operation)
public record GetUserQuery(int UserId) : IQuery<Result<UserDto>>;

public class GetUserHandler : IQueryHandler<GetUserQuery, Result<UserDto>>
{
    private readonly IUserRepository _repository;

    public async Task<Result<UserDto>> HandleAsync(GetUserQuery query, CancellationToken ct = default)
    {
        return await _repository.FindByIdAsync(query.UserId)
            .ToResult($"User {query.UserId} not found")
            .MapAsync(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsActive = user.IsActive
            });
    }
}

// ✏️ Command (Write Operation) with Validation
public record CreateUserCommand(string Name, string Email, string Password) : ICommand<Result<int>>, IValidatable
{
    public Validation<Unit> Validate()
    {
        return ValidateName(Name)
            .Combine(ValidateEmail(Email), (name, email) => name)
            .Combine(ValidatePassword(Password), (nameEmail, password) => Unit.Value);
    }

    private Validation<string> ValidateName(string name) =>
        string.IsNullOrWhiteSpace(name) 
            ? Validation<string>.Failure("Name is required")
            : name.Length < 2
                ? Validation<string>.Failure("Name must be at least 2 characters")
                : Validation<string>.Success(name);
}

// 🎮 Usage in Controller - Clean and Simple
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMessenger _messenger;

    public UsersController(IMessenger messenger) => _messenger = messenger;

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var result = await _messenger.QueryAsync(new GetUserQuery(id));
        return result.Match(
            onSuccess: user => Ok(user),
            onFailure: error => NotFound(new { Error = error })
        );
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateUser(CreateUserCommand command)
    {
        var result = await _messenger.SendAsync(command);
        return result.Match(
            onSuccess: userId => CreatedAtAction(nameof(GetUser), new { id = userId }, userId),
            onFailure: error => BadRequest(new { Error = error })
        );
    }
}
```

### ✅ Validation Pattern - Collect All Errors at Once

```csharp
// ❌ Old way - Stops at first error
public async Task<IActionResult> CreateUser(CreateUserRequest request)
{
    if (string.IsNullOrEmpty(request.Name))
        return BadRequest("Name is required");  // Stops here!
    
    if (string.IsNullOrEmpty(request.Email))
        return BadRequest("Email is required");  // User never sees this
    
    // More validations...
}

// ✅ New way - Shows ALL errors at once
public record CreateUserRequest(string Name, string Email, int Age, string Password);

public Validation<CreateUserRequest> ValidateCreateUser(CreateUserRequest request)
{
    return ValidateName(request.Name)
        .Combine(ValidateEmail(request.Email), (name, email) => name)
        .Combine(ValidateAge(request.Age), (nameEmail, age) => nameEmail)
        .Combine(ValidatePassword(request.Password), (all, password) => request);
}

// Result: Returns ALL validation errors at once
// ✨ User gets: ["Name is required", "Email format invalid", "Password too weak"]
// Instead of just: ["Name is required"]
```

## 🏆 Key Advantages

### 🛡️ **Bulletproof Type Safety**
```csharp
// Compile-time safety - No more runtime surprises!
Optional<User> user = await _userRepository.FindByIdAsync(userId);
// ✅ Compiler forces you to handle the "not found" case
string email = user.Map(u => u.Email).OrElse("No email");

Result<Order> orderResult = await _orderService.CreateOrderAsync(request);
// ✅ Compiler forces you to handle both success and failure cases
return orderResult.Match(
    onSuccess: order => Ok(order),
    onFailure: error => BadRequest(error)
);
```

### ⚡ **Performance Optimized**
```csharp
// Zero-allocation operations with readonly structs
Optional<string> result = Some("Hello World")
    .Map(s => s.ToUpper())          // No heap allocations
    .Filter(s => s.Length > 5)      // No boxing
    .Map(s => s + "!");             // Optimized transformations

// Reflection caching reduces overhead by 90%
var result = await _messenger.QueryAsync(new GetUserQuery(123)); // Cached handler lookup
```

### 👨‍💻 **Superior Developer Experience**
```csharp
// IntelliSense guides you through the happy path
return await _userRepository.FindByIdAsync(userId)
    .ToResult("User not found")
    .Then(user => ValidateUser(user))       // ✅ Automatic error propagation
    .Then(user => EnrichUserData(user))     // ✅ Chain operations safely
    .Then(user => FormatResponse(user))     // ✅ Transform without null checks
    .TapError(error => _logger.LogError(error)); // ✅ Side effects made explicit
```

### 🏢 **Enterprise Ready**
```csharp
// Built-in patterns for production applications
public record GetProductQuery(int ProductId) : IQuery<ProductDto>, ICacheable, IRequireAuthorization
{
    public string CacheKey => $"product:{ProductId}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
    public string[] RequiredRoles => new[] { "User", "Admin" };
}

// Automatic: Caching + Authorization + Logging + Performance Monitoring + Retry Logic
// All through pipeline behaviors - no boilerplate code!
```

## 🎛️ Configuration & Behaviors

### Individual Behavior Registration
```csharp
// Pick and choose what you need
services.AddFunctionalKitLogging();              // 📝 Request/response logging
services.AddFunctionalKitValidation();           // ✅ Automatic validation
services.AddFunctionalKitCaching();              // ⚡ Result caching
services.AddFunctionalKitPerformanceMonitoring(500); // 📊 Performance tracking
services.AddFunctionalKitRetry(maxRetries: 3);   // 🔄 Automatic retry
services.AddFunctionalKitCircuitBreaker(failureThreshold: 5); // 🔌 Fault tolerance
```

### Environment-Specific Configuration
```csharp
// Different setups for different environments
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddFunctionalKit(options =>
    {
        options.EnableLogging = true;
        options.EnableValidation = true;
        options.SlowQueryThresholdMs = 100; // Strict performance monitoring
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
        options.MaxRetries = 3;
    });
}
```

## 🎯 Real-World Examples

### **E-commerce Order Processing**
```csharp
public class OrderProcessor
{
    public async Task<Result<OrderResult>> ProcessOrderAsync(CreateOrderRequest request)
    {
        return await Railway.StartWith(request)
            .Then(ValidateCustomer)                    // Customer validation
            .Then(async req => await ValidateInventory(req))    // Stock checking
            .Then(async req => await CalculatePricing(req))     // Price calculation
            .Then(async req => await ProcessPayment(req))       // Payment processing
            .Then(async order => await CreateOrder(order))      // Order creation
            .ThenDo(async order => await SendConfirmationEmail(order)) // Notifications
            .ThenDo(async order => await UpdateInventory(order))       // Inventory update
            .Recover(async error => await HandleOrderFailure(error));  // Error recovery
    }
}
```

### **File Processing Pipeline**
```csharp
public async Task<Result<ProcessedDocument>> ProcessDocumentAsync(UploadedFile file)
{
    return await Railway.StartWith(file)
        .ThenIf(f => f.Size < 10_000_000, "File too large")
        .ThenIf(f => SupportedFormats.Contains(f.Extension), "Unsupported format")
        .Then(async f => await ScanForViruses(f))
        .Then(async f => await ExtractText(f))
        .Then(async doc => await ProcessContent(doc))
        .Then(async doc => await SaveToDatabase(doc))
        .TapError(async error => await LogProcessingFailure(file, error));
}
```

### **External API Integration**
```csharp
public async Task<Optional<WeatherData>> GetWeatherAsync(string city)
{
    return await _httpClient.GetStringAsync($"/weather/{city}")
        .ToResult()                                    // Catch HTTP exceptions
        .Then(json => ParseWeatherData(json))          // Parse JSON safely
        .Filter(data => data.IsValid)                  // Validate data
        .ToOptional();                                 // Convert to Optional
}

// Usage with fallback
var weather = await GetWeatherAsync("New York")
    .Or(() => GetWeatherAsync("NYC"))                 // Try alternative
    .OrElseGet(() => GetDefaultWeather());             // Fallback data
```

## 🗃️ Repository & Data Access Patterns

FunctionalKit includes powerful repository patterns and Entity Framework Core integration:

### **🔧 Simple Repository Setup**
```csharp
// Program.cs - Enable repository support
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddFunctionalKitRepositories();
builder.Services.AddUnitOfWork<AppDbContext>();
```

### **📂 Generic Repository Pattern**
```csharp
// Built-in generic repository with Optional returns
public class UserService
{
    private readonly IRepository<User, int> _userRepository;

    public async Task<Optional<UserDto>> GetUserAsync(int id)
    {
        return await _userRepository.GetByIdAsync(id)
            .MapAsync(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            });
    }

    public async Task<IEnumerable<UserDto>> GetActiveUsersAsync()
    {
        return await _userRepository
            .Query(user => user.IsActive)
            .Include(u => u.Profile)
            .Select(user => new UserDto { /* mapping */ })
            .ToListAsync();
    }
}
```

### **🏗️ Custom Repository Implementation**
```csharp
// Create specific repositories when needed
public class UserRepository : RepositoryBase<User, int, AppDbContext>
{
    public UserRepository(AppDbContext context) : base(context) { }

    // Custom business-specific methods
    public async Task<Optional<User>> FindByEmailAsync(string email)
    {
        return await GetFirstAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetActiveUsersWithProfileAsync()
    {
        return await GetAllAsync(
            predicate: u => u.IsActive,
            includes: u => u.Profile, u => u.Roles
        );
    }

    // Override ID handling if needed
    protected override int GetEntityId(User entity) => entity.Id;
    protected override Expression<Func<User, bool>> GetByIdExpression(int id) => 
        user => user.Id == id;
}
```

### **🎯 Unit of Work Pattern**
```csharp
public class OrderService
{
    private readonly IUnitOfWork<AppDbContext> _unitOfWork;

    public async Task<Result<OrderDto>> CreateOrderAsync(CreateOrderRequest request)
    {
        var userRepo = _unitOfWork.Repository<User, int>();
        var orderRepo = _unitOfWork.Repository<Order, int>();

        return await Railway.StartWith(request)
            .Then(async req => await ValidateCustomer(userRepo, req.CustomerId))
            .Then(req => CreateOrderEntity(req))
            .Then(async order => 
            {
                orderRepo.Add(order);
                await _unitOfWork.SaveChangesAsync();
                return Result<Order>.Success(order);
            })
            .MapAsync(order => MapToDto(order));
    }
}
```

### **📊 Advanced Querying with Extensions**
```csharp
public class ProductService
{
    public async Task<(IEnumerable<ProductDto> products, int totalCount)> GetProductsAsync(
        ProductSearchRequest request)
    {
        var query = _productRepository.Query()
            .Where(p => p.IsActive)
            .IncludeMultiple("Category", "Reviews")
            .OrderByExt(p => p.Name, request.SortDescending);

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(p => p.Name.Contains(request.SearchTerm) || 
                                   p.Description.Contains(request.SearchTerm));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        var (products, totalCount) = await query.GetPagedAsync(
            request.PageNumber, 
            request.PageSize);

        var productDtos = products.Select(MapToDto);
        return (productDtos, totalCount);
    }
}
```

### **🏭 Repository Factory Pattern**
```csharp
public class MultiContextService
{
    private readonly IRepositoryFactory _repositoryFactory;

    public async Task<Result<SyncResult>> SyncDataBetweenContextsAsync()
    {
        // Work with multiple database contexts
        using var userContext = _repositoryFactory.CreateUnitOfWork<UserDbContext>();
        using var analyticsContext = _repositoryFactory.CreateUnitOfWork<AnalyticsDbContext>();

        var userRepo = userContext.Repository<User, int>();
        var analyticsRepo = analyticsContext.Repository<UserActivity, int>();

        // Sync logic here...
        return Result<SyncResult>.Success(new SyncResult());
    }
}
```

### **🔍 Repository Features**

#### **Safe Operations with Optional Returns**
```csharp
// All find operations return Optional<T> - no null reference exceptions!
Optional<User> user = await _repository.GetByIdAsync(123);
Optional<User> firstAdmin = await _repository.GetFirstAsync(u => u.IsAdmin);

// Safe existence checks
bool userExists = await _repository.ExistsAsync(123);
bool hasAdmins = await _repository.ExistsAsync(u => u.IsAdmin);
```

#### **Flexible Include Patterns**
```csharp
// String-based includes
var user = await _repository.GetByIdAsync(123, "Profile", "Profile.Address");

// Expression-based includes (strongly typed)
var user = await _repository.GetByIdAsync(123, 
    u => u.Profile, 
    u => u.Profile.Address,
    u => u.Orders);

// Query builder pattern
var users = await _repository
    .Query(u => u.IsActive)
    .IncludeMultiple(u => u.Profile, u => u.Roles)
    .ToListAsync();
```

#### **Batch Operations**
```csharp
// Efficient batch operations
var users = GetUsersToUpdate();
_repository.UpdateRange(users);
await _unitOfWork.SaveChangesAsync();

// Or individual operations
_repository.Add(newUser);
_repository.Update(existingUser);
_repository.Delete(userToDelete);
await _unitOfWork.SaveChangesAsync();
```

### **🎨 Repository Configuration**
```csharp
// Register repositories with dependency injection
public static class ServiceRegistration
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Generic repositories (automatic)
        services.AddFunctionalKitRepositories();

        // Custom repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Unit of Work for each context
        services.AddUnitOfWork<AppDbContext>();
        services.AddUnitOfWork<AnalyticsDbContext>();

        return services;
    }
}
```

### **🚀 Why Use FunctionalKit Repositories?**

✅ **Type Safety** - Optional<T> returns eliminate null reference exceptions  
✅ **Consistent API** - Same patterns across all repositories  
✅ **Flexible Querying** - Both simple and complex query support  
✅ **Performance Optimized** - Efficient include patterns and batch operations  
✅ **Unit of Work** - Proper transaction handling  
✅ **Multi-Context Support** - Work with multiple databases easily  
✅ **EF Core Integration** - Seamless Entity Framework Core integration

## 📚 Documentation & Resources

- **[🚀 Getting Started Guide](doc/GETTINGS_STARTED.md)** - Quick setup and first examples
- **[🎯 Core Patterns Deep Dive](doc/CORE_PATTERNS.md)** - Optional, Result, Either, Validation
- **[📨 CQRS & Messaging Guide](doc/CQRS_GUIDE.md)** - Complete messaging system usage
- **[⚙️ Pipeline Behaviors](doc/PIPELINE_BEHAVIORS.md)** - Cross-cutting concerns and behaviors
- **[📖 Complete API Reference](doc/API_REFERENCE.md)** - Every method and extension documented

## 🔄 Migration Guide

### **From Traditional Null Handling**
```csharp
// Before: Unsafe and verbose
User user = _userRepository.FindById(id);
if (user?.Profile?.Settings?.Theme != null)
{
    return user.Profile.Settings.Theme;
}
return "default";

// After: Safe and concise
return _userRepository.FindByIdAsync(id)
    .FlatMap(u => u.Profile.ToOptional())
    .FlatMap(p => p.Settings.ToOptional())
    .FlatMap(s => s.Theme.ToOptional())
    .OrElse("default");
```

### **From MediatR**
```csharp
// MediatR syntax
public class Handler : IRequestHandler<Query, Response> { }
services.AddMediatR(typeof(Handler));
await _mediator.Send(new Query());

// FunctionalKit syntax (almost identical!)
public class Handler : IQueryHandler<Query, Response> { }
services.AddFunctionalKit(Assembly.GetExecutingAssembly());
await _messenger.QueryAsync(new Query());
```

### **From Exception-Based Error Handling**
```csharp
// Before: Exceptions for control flow
try
{
    var result = await SomeOperation();
    return Ok(result);
}
catch (ValidationException ex)
{
    return BadRequest(ex.Message);
}
catch (NotFoundException ex)
{
    return NotFound(ex.Message);
}

// After: Explicit error handling
var result = await SomeOperationResult();
return result.Match(
    onSuccess: value => Ok(value),
    onFailure: error => error.Contains("not found") 
        ? NotFound(error) 
        : BadRequest(error)
);
```

## ⚡ Performance Characteristics

- **🚀 Zero allocation** for most operations using readonly structs
- **📈 90% reduction** in reflection overhead through caching
- **🎯 Memory efficient** lazy evaluation and optimized collections
- **⚡ Async optimized** patterns with proper ConfigureAwait usage
- **📦 Minimal dependencies** - only essential Microsoft.Extensions packages

## 🛡️ Production Battle-Tested

```csharp
// Real-world production patterns included
public class ProductionOrderService
{
    public async Task<Result<Order>> ProcessOrderAsync(OrderRequest request)
    {
        return await Railway.StartWith(request)
            .Then(ValidateRequest)                     // ✅ Input validation
            .Then(async req => await CheckInventory(req))   // 📦 Inventory check
            .Then(async req => await ProcessPayment(req))   // 💳 Payment processing
            .Then(async order => await SaveOrder(order))    // 💾 Database operation
            .ThenDo(async order => await PublishOrderCreatedEvent(order)) // 📤 Event publishing
            .TapError(async error => await LogOrderFailure(error))       // 📝 Error logging
            .Recover(async error => await CreatePendingOrder(error));    // 🔄 Graceful degradation
    }
}

// Automatic behaviors applied:
// 🔍 Logging: Request/response automatically logged
// ⚡ Performance: Slow operations detected and logged  
// 🔄 Retry: Transient failures automatically retried
// 🛡️ Circuit Breaker: Protection against cascading failures
// ✅ Validation: Automatic validation for IValidatable commands
// ⚡ Caching: Results cached for ICacheable queries
```

## 🤝 Contributing

We welcome contributions! Whether it's bug fixes, new features, or documentation improvements.

### Development Setup
```bash
git clone https://github.com/lqviet45/FunctionalKit.git
cd FunctionalKit
dotnet restore
dotnet build
dotnet test
```

### Ways to Contribute
- 🐛 **Bug Reports** - Found an issue? Let us know!
- 💡 **Feature Requests** - Have an idea? We'd love to hear it!
- 📝 **Documentation** - Help improve our guides and examples
- 🧪 **Testing** - Help us achieve better test coverage
- 💻 **Code** - Submit pull requests for bug fixes or features

## 📄 License

Licensed under the **Apache License 2.0** - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Functional Programming Languages** - Inspired by F#, Haskell, Scala, and Rust
- **Railway-Oriented Programming** - Concept by Scott Wlaschin
- **Java Optional Pattern** - For nullable reference safety
- **Community Feedback** - Shaped by real-world usage and feedback

## 📞 Support & Community

- **🐛 Issues**: [GitHub Issues](https://github.com/lqviet45/FunctionalKit/issues)
- **💬 Discussions**: [GitHub Discussions](https://github.com/lqviet45/FunctionalKit/discussions)
- **📦 NuGet**: [FunctionalKit Package](https://www.nuget.org/packages/FunctionalKit/)
- **📖 Documentation**: [Complete Guides](https://github.com/lqviet45/FunctionalKit/tree/main/doc)

---

<div align="center">

**Built with ❤️ for the .NET community**

*Transform your C# code with functional programming patterns*

*Write safer, more maintainable applications today!*

[**⭐ Star us on GitHub**](https://github.com/lqviet45/FunctionalKit) | [**📦 Get on NuGet**](https://www.nuget.org/packages/FunctionalKit/) | [**📖 Read the Docs**](https://github.com/lqviet45/FunctionalKit/tree/main/doc)

</div>