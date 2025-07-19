# FunctionalKit

[![NuGet Version](https://img.shields.io/nuget/v/FunctionalKit?style=flat-square&color=blue)](https://www.nuget.org/packages/FunctionalKit/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FunctionalKit?style=flat-square&color=green)](https://www.nuget.org/packages/FunctionalKit/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg?style=flat-square)](https://opensource.org/licenses/Apache-2.0)
[![.NET](https://img.shields.io/badge/.NET-8.0+-purple.svg?style=flat-square)](https://dotnet.microsoft.com/download)

A comprehensive functional programming library for .NET 8+ that brings the power of functional patterns to C#. FunctionalKit provides robust implementations of Optional, Result, Either patterns, along with a powerful messaging system that serves as an excellent alternative to MediatR.

## 🚀 Why Choose FunctionalKit?

- **🛡️ Type Safety**: Eliminate null reference exceptions with Optional pattern
- **⚡ Error Handling**: Robust error handling with Result and Either patterns
- **🚄 Railway Programming**: Chain operations elegantly with built-in failure handling
- **📨 Messaging System**: Clean CQRS implementation with pipeline behaviors (MediatR alternative)
- **🔥 Performance**: Optimized readonly structs with minimal allocations
- **🌐 Async First**: Full async/await support throughout the library
- **🧰 Rich Extensions**: 100+ extension methods for functional programming
- **📊 Production Ready**: Circuit breaker, retry logic, performance monitoring

## 📦 Installation

```bash
# Package Manager
Install-Package FunctionalKit

# .NET CLI
dotnet add package FunctionalKit

# PackageReference
<PackageReference Include="FunctionalKit" Version="8.0.0" />
```

## ⚡ Quick Start

### Basic Setup
```csharp
// Program.cs
using FunctionalKit.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register FunctionalKit with automatic handler discovery
builder.Services.AddFunctionalKit(Assembly.GetExecutingAssembly());

var app = builder.Build();
```

### Advanced Setup with Behaviors
```csharp
// Enable comprehensive pipeline behaviors
builder.Services.AddFunctionalKit(options =>
{
    options.EnableLogging = true;
    options.EnableValidation = true;
    options.EnablePerformanceMonitoring = true;
    options.EnableCaching = true;
    options.EnableRetry = true;
    options.SlowQueryThresholdMs = 1000;
    options.MaxRetries = 3;
}, Assembly.GetExecutingAssembly());
```

## 🎯 Core Features

### 🔹 **Functional Types**
- **Optional&lt;T&gt;** - Java-style Optional for safe null handling
- **Result&lt;T&gt;** - Railway-oriented programming for error handling
- **Either&lt;TLeft, TRight&gt;** - Union types for representing alternatives
- **Validation&lt;T&gt;** - Accumulate multiple validation errors

### 📨 **Messaging System (MediatR Alternative)**
- **IMessenger** - Central messaging interface
- **Commands & Queries** - CQRS pattern implementation
- **Pipeline Behaviors** - Cross-cutting concerns (logging, validation, caching)
- **Notifications** - Pub/sub pattern support

### 🛠️ **Enterprise Features**
- **Railway Programming** - Chainable operations with failure handling
- **Pattern Matching** - Functional pattern matching utilities
- **Circuit Breaker** - Fault tolerance for external services
- **Retry Logic** - Configurable retry mechanisms
- **Performance Monitoring** - Built-in query performance tracking
- **Async Validation** - Full async validation pipeline support

## 📋 Examples

### 🎯 Optional Pattern - Eliminate Null Reference Exceptions
```csharp
// Safe operations without null checks
public async Task<string> GetUserDisplayNameAsync(int userId)
{
    return await _userRepository.FindByIdAsync(userId)
        .Map(user => user.Profile)
        .FlatMap(profile => profile.DisplayName.ToOptional())
        .Filter(name => !string.IsNullOrWhiteSpace(name))
        .OrElse("Anonymous User");
}

// Safe collection operations
public Optional<User> FindFirstActiveAdmin()
{
    return _users
        .Where(u => u.IsActive)
        .FirstOrNone(u => u.Roles.Contains("Admin"));
}
```

### ⚡ Result Pattern - Railway-Oriented Programming
```csharp
// Chain operations with automatic error handling
public async Task<Result<OrderResult>> ProcessOrderAsync(CreateOrderRequest request)
{
    return await Railway.StartWith(request)
        .Then(ValidateOrderRequest)
        .Then(async req => await ReserveInventoryAsync(req))
        .Then(async req => await ProcessPaymentAsync(req))
        .Then(async order => await CreateOrderAsync(order))
        .ThenDo(async order => await PublishOrderEventsAsync(order))
        .MapAsync(order => new OrderResult(order.Id, order.Total));
}

// Error recovery patterns
public async Task<Result<PaymentResult>> ProcessPaymentAsync(PaymentRequest request)
{
    return await TryPrimaryProcessor(request)
        .Recover(async error => await TrySecondaryProcessor(request))
        .TapError(error => _logger.LogWarning("Payment failed: {Error}", error));
}
```

### 📨 Messaging System - Clean CQRS Implementation
```csharp
// Query Definition
public record GetUserQuery(int UserId) : IQuery<Result<UserDto>>;

// Query Handler
public class GetUserHandler : IQueryHandler<GetUserQuery, Result<UserDto>>
{
    private readonly IUserRepository _repository;

    public async Task<Result<UserDto>> HandleAsync(GetUserQuery query, CancellationToken ct = default)
    {
        return await _repository.FindByIdAsync(query.UserId)
            .ToResult($"User {query.UserId} not found")
            .MapAsync(user => new UserDto(user.Id, user.Name, user.Email));
    }
}

// Command with Validation
public record CreateUserCommand(string Name, string Email) : ICommand<Result<int>>, IValidatable
{
    public Validation<Unit> Validate()
    {
        return ValidateName(Name)
            .Combine(ValidateEmail(Email), (name, email) => Unit.Value);
    }
}

// Usage in Controller
[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> GetUser(int id)
{
    var result = await _messenger.QueryAsync(new GetUserQuery(id));
    return result.Match(
        onSuccess: user => Ok(user),
        onFailure: error => NotFound(new { Error = error })
    );
}
```

### ✅ Validation Pattern - Accumulate All Errors
```csharp
public record CreateUserRequest(string Name, string Email, int Age);

public Validation<CreateUserRequest> ValidateCreateUser(CreateUserRequest request)
{
    return ValidateName(request.Name)
        .Combine(ValidateEmail(request.Email), (name, email) => name)
        .Combine(ValidateAge(request.Age), (nameEmail, age) => request);
}

private Validation<string> ValidateName(string name)
{
    if (string.IsNullOrWhiteSpace(name))
        return Validation<string>.Failure("Name is required");
    
    if (name.Length < 2)
        return Validation<string>.Failure("Name must be at least 2 characters");
        
    return Validation<string>.Success(name);
}

// Result: Collects ALL validation errors at once
// Instead of: ["Name is required"] 
// You get: ["Name is required", "Email format invalid", "Age must be positive"]
```

## 🏆 Key Benefits

### **🛡️ Type Safety**
- Compile-time null safety with Optional pattern
- Explicit error handling with Result pattern
- No more `NullReferenceException` or unhandled exceptions

### **⚡ Performance Optimized**
- Readonly structs for minimal allocations
- Reflection caching in messaging system
- Optimized async operations with ConfigureAwait(false)
- Memory-efficient collection operations

### **👨‍💻 Developer Experience**
- Fluent APIs for readable, chainable code
- IntelliSense support with comprehensive XML documentation
- Extensive examples and real-world usage patterns
- Easy migration from existing codebases

### **🏢 Enterprise Ready**
- Circuit breaker pattern for fault tolerance
- Comprehensive logging and performance monitoring
- Configurable pipeline behaviors
- Built-in retry mechanisms with exponential backoff

## 🎛️ Configuration Options

### Individual Behaviors
```csharp
services.AddFunctionalKitLogging();           // Request/response logging
services.AddFunctionalKitValidation();        // Automatic validation
services.AddFunctionalKitCaching();           // Query result caching
services.AddFunctionalKitPerformanceMonitoring(500); // Slow query detection
services.AddFunctionalKitRetry(maxRetries: 3); // Automatic retry logic
services.AddFunctionalKitCircuitBreaker(failureThreshold: 5); // Fault tolerance
```

### Complete Configuration
```csharp
services.AddFunctionalKit(options =>
{
    options.EnableLogging = true;
    options.EnableValidation = true;
    options.EnableCaching = true;
    options.EnablePerformanceMonitoring = true;
    options.SlowQueryThresholdMs = 1000;
    options.EnableRetry = true;
    options.MaxRetries = 3;
    options.RetryDelay = TimeSpan.FromSeconds(1);
}, Assembly.GetExecutingAssembly());
```

## 🎯 Real-World Use Cases

### **Web APIs**
```csharp
[HttpPost]
public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderCommand command)
{
    var result = await _messenger.SendAsync(command);
    return result.Match(
        onSuccess: order => CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order),
        onFailure: error => BadRequest(new { Error = error })
    );
}
```

### **Data Processing Pipelines**
```csharp
public async Task<Result<ProcessedData>> ProcessDataAsync(RawData input)
{
    return await Railway.StartWith(input)
        .Then(ValidateInput)
        .Then(async data => await TransformAsync(data))
        .Then(async data => await EnrichAsync(data))
        .ThenIf(data => data.QualityScore > 0.8m, "Data quality insufficient")
        .ThenDo(async data => await AuditAsync(data));
}
```

### **External Service Integration**
```csharp
public async Task<Optional<WeatherData>> GetWeatherAsync(string city)
{
    return await _httpClient.GetStringAsync($"/weather/{city}")
        .ToResult()
        .Then(json => ParseWeatherData(json))
        .ToOptional(); // Convert failures to empty Optional
}
```

## 📚 Documentation

- **[🚀 Getting Started](doc/GETTINGS_STARTED.md)** - Quick setup guide
- **[🎯 Core Patterns](doc/CORE_PATTERNS.md)** - Optional, Result, Either, Validation deep dive
- **[📨 CQRS Guide](doc/CQRS_GUIDE.md)** - Complete messaging system usage
- **[⚙️ Pipeline Behaviors](doc/PIPELINE_BEHAVIORS.md)** - Cross-cutting concerns
- **[📖 API Reference](doc/API_REFERENCE.md)** - Complete method documentation

## ⚡ Performance Characteristics

FunctionalKit is designed for high-performance applications:

- **Zero allocation** for most operations using readonly structs
- **Reflection caching** reduces messaging overhead by 90%
- **Memory efficient** collection operations with lazy evaluation
- **Optimized async** patterns throughout the library
- **Minimal dependencies** - only Microsoft.Extensions packages

## 🔄 Migration from Existing Code

### From Traditional Null Handling
```csharp
// Before
if (user?.Profile?.Avatar != null)
    return user.Profile.Avatar;
return "/default-avatar.png";

// After  
return user.ToOptional()
    .FlatMap(u => u.Profile.ToOptional())
    .FlatMap(p => p.Avatar.ToOptional())
    .OrElse("/default-avatar.png");
```

### From MediatR
```csharp
// MediatR
public class Handler : IRequestHandler<Query, Response> { }
services.AddMediatR(typeof(Handler));
await _mediator.Send(new Query());

// FunctionalKit
public class Handler : IQueryHandler<Query, Response> { }
services.AddFunctionalKit(Assembly.GetExecutingAssembly());
await _messenger.QueryAsync(new Query());
```

## 🤝 Contributing

We welcome contributions!

### Development Setup
```bash
git clone https://github.com/lqviet45/FunctionalKit.git
cd FunctionalKit
dotnet restore
dotnet build
dotnet test
```

## 📄 License

Licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by functional programming languages (F#, Haskell, Scala)
- Railway-oriented programming concept by Scott Wlaschin
- Java Optional pattern and Rust Result type
- Community feedback and real-world usage patterns

## 📞 Support & Community

- **🐛 Issues**: [GitHub Issues](https://github.com/lqviet45/FunctionalKit/issues)
- **💬 Discussions**: [GitHub Discussions](https://github.com/lqviet45/FunctionalKit/discussions)
- **📦 NuGet**: [FunctionalKit Package](https://www.nuget.org/packages/FunctionalKit/)

---

**Built with ❤️ for the .NET community**

*Transform your C# code with functional programming patterns - write safer, more maintainable applications today!*