# Getting Started with FunctionalKit

Quick start guide to begin using FunctionalKit in your .NET applications.

## Installation

### Package Manager Console
```bash
Install-Package FunctionalKit
```

### .NET CLI
```bash
dotnet add package FunctionalKit
```

### PackageReference
```xml
<PackageReference Include="FunctionalKit" Version="8.0.0" />
```

## Basic Setup

### Minimal API Setup (Program.cs)
```csharp
using FunctionalKit.Extensions;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Basic registration - scans for handlers in the current assembly
builder.Services.AddFunctionalKit(Assembly.GetExecutingAssembly());

var app = builder.Build();
```

### Advanced Setup with Behaviors
```csharp
using FunctionalKit.Extensions;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Advanced registration with pipeline behaviors
builder.Services.AddFunctionalKit(options =>
{
    options.EnableLogging = true;
    options.EnableValidation = true;
    options.EnablePerformanceMonitoring = true;
    options.SlowQueryThresholdMs = 1000;
    options.EnableCaching = true;
    options.EnableRetry = true;
    options.MaxRetries = 3;
}, Assembly.GetExecutingAssembly());

var app = builder.Build();
```

### Traditional Web API Setup (Startup.cs)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add your other services
        services.AddControllers();
        
        // Add FunctionalKit
        services.AddFunctionalKit(options =>
        {
            options.EnableLogging = true;
            options.EnableValidation = true;
        }, Assembly.GetExecutingAssembly());
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Your app configuration
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
```

## Required Using Statements

Add these using statements to your files:

```csharp
using FunctionalKit.Core;
using FunctionalKit.Extensions;
using static FunctionalKit.Core.Functional; // For helper methods: Some, None, Ok, Err
```

## Your First Example

Let's create a simple user service using FunctionalKit patterns:

### 1. Define Your Models
```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; }
}
```

### 2. Create a Repository with Optional
```csharp
public interface IUserRepository
{
    Task<Optional<User>> FindByIdAsync(int id);
    Task<Optional<User>> FindByEmailAsync(string email);
    Task SaveAsync(User user);
}

public class UserRepository : IUserRepository
{
    private readonly DbContext _context;

    public UserRepository(DbContext context)
    {
        _context = context;
    }

    public async Task<Optional<User>> FindByIdAsync(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        return Optional<User>.OfNullable(user);
    }

    public async Task<Optional<User>> FindByEmailAsync(string email)
    {
        // Using extension method for safe access
        return await _context.Users.FirstOrNoneAsync(u => u.Email == email);
    }

    public async Task SaveAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
```

### 3. Create Queries and Commands
```csharp
// Query
public record GetUserByIdQuery(int UserId) : IQuery<Result<UserDto>>;

public class GetUserByIdHandler : IQueryHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IUserRepository _repository;

    public GetUserByIdHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<UserDto>> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await _repository.FindByIdAsync(query.UserId)
            .ToResult($"User with ID {query.UserId} not found")
            .MapAsync(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsActive = user.IsActive
            });
    }
}

// Command
public record CreateUserCommand(string Name, string Email) : ICommand<Result<int>>;

public class CreateUserHandler : ICommandHandler<CreateUserCommand, Result<int>>
{
    private readonly IUserRepository _repository;

    public CreateUserHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        var existingUser = await _repository.FindByEmailAsync(command.Email);
        if (existingUser.HasValue)
            return Result<int>.Failure("Email already exists");

        // Create new user
        var user = new User
        {
            Name = command.Name,
            Email = command.Email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(user);
        return Result<int>.Success(user.Id);
    }
}
```

### 4. Create a Controller
```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMessenger _messenger;

    public UsersController(IMessenger messenger)
    {
        _messenger = messenger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var result = await _messenger.QueryAsync(new GetUserByIdQuery(id));
        
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

### 5. Register Your Services
```csharp
// In Program.cs or Startup.cs
builder.Services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>();

// FunctionalKit will automatically discover and register your handlers
builder.Services.AddFunctionalKit(Assembly.GetExecutingAssembly());
```

## Quick Examples

### Working with Optional
```csharp
// Safe null handling
public string GetUserDisplayName(int userId)
{
    return _userRepository.FindById(userId)
        .Map(user => user.Name)
        .Filter(name => !string.IsNullOrWhiteSpace(name))
        .OrElse("Anonymous User");
}

// Safe dictionary access
public Optional<string> GetConfigValue(string key)
{
    return _configDictionary.GetOptional(key);
}

// Safe collection access
public Optional<User> GetFirstActiveUser()
{
    return _users.FirstOrNone(user => user.IsActive);
}
```

### Working with Result
```csharp
// Error handling without exceptions
public async Task<Result<User>> UpdateUserAsync(int id, string newName)
{
    var user = await _repository.FindByIdAsync(id);
    if (user.IsEmpty)
        return Result<User>.Failure($"User {id} not found");

    user.Value.Name = newName;
    await _repository.SaveAsync(user.Value);
    
    return Result<User>.Success(user.Value);
}

// Chaining operations
public Result<ProcessedData> ProcessUserData(UserData input)
{
    return ValidateInput(input)
        .Then(TransformData)
        .Then(EnrichData)
        .Then(FormatOutput);
}
```

### Working with Validation
```csharp
// Accumulating validation errors
public Validation<CreateUserRequest> ValidateCreateUser(CreateUserRequest request)
{
    return ValidateName(request.Name)
        .Combine(ValidateEmail(request.Email), (name, email) => name)
        .Combine(ValidateAge(request.Age), (nameEmail, age) => request);
}

private Validation<string> ValidateName(string name)
{
    return string.IsNullOrWhiteSpace(name)
        ? Validation<string>.Failure("Name is required")
        : Validation<string>.Success(name);
}
```

## Next Steps

Now that you have the basics set up, explore these guides:

1. **[Core Patterns Guide](CORE_PATTERNS.md)** - Deep dive into Optional, Result, Either, and Validation
2. **[CQRS Guide](CQRS_GUIDE.md)** - Complete messaging system usage
3. **[Pipeline Behaviors Guide](PIPELINE_BEHAVIORS.md)** - Cross-cutting concerns
4. **[Real-World Examples](REAL_WORLD_EXAMPLES.md)** - Complete application scenarios
5. **[Testing Guide](TESTING_GUIDE.md)** - Testing functional code
6. **[Migration Guide](MIGRATION_GUIDE.md)** - Migrating existing code

## Common Patterns to Start With

### 1. Replace Null Checks with Optional
```csharp
// Old way
if (user != null && user.Profile != null && !string.IsNullOrEmpty(user.Profile.Avatar))
{
    return user.Profile.Avatar;
}
return "/images/default-avatar.png";

// New way
return user.ToOptional()
    .FlatMap(u => u.Profile.ToOptional())
    .FlatMap(p => p.Avatar.ToOptional())
    .OrElse("/images/default-avatar.png");
```

### 2. Replace Try-Catch with Result
```csharp
// Old way
try
{
    var result = await SomeOperation();
    return Ok(result);
}
catch (Exception ex)
{
    return BadRequest(ex.Message);
}

// New way
var result = await SomeOperationResult();
return result.Match(
    onSuccess: value => Ok(value),
    onFailure: error => BadRequest(error)
);
```

### 3. Replace Multiple Validation Checks
```csharp
// Old way
var errors = new List<string>();
if (string.IsNullOrEmpty(name)) errors.Add("Name required");
if (string.IsNullOrEmpty(email)) errors.Add("Email required");
if (errors.Any()) return BadRequest(errors);

// New way
var validation = ValidateName(name)
    .Combine(ValidateEmail(email), (n, e) => new { n, e });

if (validation.IsInvalid)
    return BadRequest(validation.Errors);
```

You're now ready to start using FunctionalKit! The patterns become more powerful as you combine them together.