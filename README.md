# FunctionalKit

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![.NET](https://img.shields.io/badge/.NET-8.0+-purple.svg)](https://dotnet.microsoft.com/download)
[![NuGet](https://img.shields.io/badge/NuGet-Coming%20Soon-orange.svg)](#)

A comprehensive functional programming library for .NET 8+ that brings the power of functional patterns to C#. FunctionalKit provides robust implementations of Optional, Result, Either patterns, along with a powerful messaging system that serves as an excellent alternative to MediatR.

## 🚀 Why FunctionalKit?

- **Type Safety**: Eliminate null reference exceptions with Optional pattern
- **Error Handling**: Robust error handling with Result and Either patterns
- **Railway Programming**: Chain operations elegantly with built-in failure handling
- **Messaging System**: Clean CQRS implementation with pipeline behaviors
- **Performance**: Optimized readonly structs with minimal allocations
- **Async First**: Full async/await support throughout the library
- **Rich Extensions**: Over 100 extension methods for functional programming

## 📦 Installation

```bash
# Package Manager
Install-Package FunctionalKit

# .NET CLI
dotnet add package FunctionalKit

# PackageReference
<PackageReference Include="FunctionalKit" Version="1.0.0" />
```

## ⚡ Quick Start

```csharp
// Add to Program.cs or Startup.cs
services.AddFunctionalKit(Assembly.GetExecutingAssembly());

// Or with behaviors
services.AddFunctionalKit(options =>
{
    options.EnableLogging = true;
    options.EnableValidation = true;
    options.EnablePerformanceMonitoring = true;
}, Assembly.GetExecutingAssembly());
```

## 🏗️ Core Features

### 🎯 **Functional Types**
- **Optional<T>** - Safe null handling like Java's Optional
- **Result<T>** - Railway-oriented programming for error handling  
- **Either<TLeft, TRight>** - Union types for representing alternatives
- **Validation<T>** - Accumulate multiple validation errors

### 📨 **Messaging System**
- **IMessenger** - Central messaging interface (MediatR alternative)
- **Commands & Queries** - CQRS pattern implementation
- **Pipeline Behaviors** - Cross-cutting concerns (logging, validation, caching)
- **Notifications** - Pub/sub pattern support

### 🛠️ **Advanced Features**
- **Railway Programming** - Chainable operations with failure handling
- **Pattern Matching** - Functional pattern matching utilities
- **Circuit Breaker** - Fault tolerance for external services
- **Retry Logic** - Configurable retry mechanisms
- **Performance Monitoring** - Built-in query performance tracking

## 📋 Examples

### Optional Pattern
```csharp
// Safe operations without null checks
var user = users.FirstOrNone(u => u.Id == userId)
    .Map(u => u.ToDto())
    .Filter(dto => dto.IsActive)
    .OrElse(() => GetDefaultUser());
```

### Result Pattern
```csharp
// Railway-oriented programming
var result = await ProcessOrderAsync(request)
    .Then(ValidateInventory)
    .Then(CalculateTotal)
    .Then(ProcessPayment)
    .ThenDo(order => PublishEvent(order));
```

### Messaging System
```csharp
// Clean CQRS implementation
public record GetUserQuery(int Id) : IQuery<User>;

public class GetUserHandler : IQueryHandler<GetUserQuery, User>
{
    public async Task<User> HandleAsync(GetUserQuery query, CancellationToken ct = default)
    {
        return await userRepository.GetByIdAsync(query.Id);
    }
}

// Usage
var user = await messenger.QueryAsync(new GetUserQuery(123));
```

## 🏆 Benefits

### **Type Safety**
- Eliminate `NullReferenceException` with Optional pattern
- Compile-time error handling with Result pattern
- Strong typing throughout the library

### **Performance**
- Readonly structs for minimal allocations
- Reflection caching for messaging system
- Optimized async operations with ConfigureAwait(false)

### **Developer Experience**
- Fluent APIs for readable code
- Comprehensive documentation
- Rich IntelliSense support
- Extensive examples and guides

### **Enterprise Ready**
- Circuit breaker for fault tolerance
- Performance monitoring and logging
- Configurable pipeline behaviors
- Robust error handling

## 📚 Documentation

- **[API Reference](doc/API_REFERENCE.md)** - Complete method documentation
- **[Getting Started](doc/GETTINGS_STARTED.md)** - Quick setup guide
- **[Usage Guide](doc/USAGE_GUIDE.md)** - Practical examples and patterns
- **[Migration Guide](#)** - Coming from other libraries
- **[Best Practices](#)** - Recommended patterns and practices

## 🎯 Use Cases

### **Web APIs**
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<User>> GetUser(int id)
{
    var result = await messenger.QueryAsync(new GetUserQuery(id));
    return result.Match(
        onSuccess: user => Ok(user),
        onFailure: error => NotFound(error)
    );
}
```

### **Data Processing**
```csharp
var results = await inputData
    .Select(ProcessItem)
    .Where(r => r.IsSuccess)
    .Select(r => r.Value)
    .Batch(100)
    .Select(batch => ProcessBatch(batch))
    .CombineResults();
```

### **Validation**
```csharp
public Validation<CreateUserRequest> ValidateRequest(CreateUserRequest request)
{
    return ValidateName(request.Name)
        .Combine(ValidateEmail(request.Email), (n, e) => n)
        .Combine(ValidateAge(request.Age), (ne, a) => request);
}
```

## 🔧 Configuration

### Basic Setup
```csharp
services.AddFunctionalKit(Assembly.GetExecutingAssembly());
```

### Advanced Configuration
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
}, Assembly.GetExecutingAssembly());
```

### Individual Behaviors
```csharp
services.AddFunctionalKitLogging();
services.AddFunctionalKitValidation();
services.AddFunctionalKitCaching();
services.AddFunctionalKitCircuitBreaker(failureThreshold: 5);
```

## 🏃‍♂️ Performance

FunctionalKit is designed for high performance:

- **Zero allocation** for most operations using readonly structs
- **Reflection caching** for messaging system reduces overhead
- **Memory efficient** collection operations
- **Optimized async** operations throughout

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Setup
```bash
git clone https://github.com/your-repo/FunctionalKit.git
cd FunctionalKit
dotnet restore
dotnet build
dotnet test
```

## 📄 License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by functional programming languages like F#, Haskell, and Scala
- Railway-oriented programming concept by Scott Wlaschin
- Optional pattern from Java and other languages
- Community feedback and contributions

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/your-repo/FunctionalKit/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-repo/FunctionalKit/discussions)

---

**Made with ❤️ for the .NET community**
