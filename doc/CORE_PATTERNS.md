# Core Patterns Guide

Deep dive into FunctionalKit's core functional programming patterns: Optional, Result, Either, and Validation.

## Table of Contents

- [Optional Pattern](#optional-pattern)
- [Result Pattern](#result-pattern)
- [Either Pattern](#either-pattern)
- [Validation Pattern](#validation-pattern)
- [Railway Programming](#railway-programming)
- [Pattern Matching](#pattern-matching)
- [Combining Patterns](#combining-patterns)

## Optional Pattern

The Optional pattern eliminates null reference exceptions by making the absence of a value explicit and type-safe.

### When to Use Optional
- Repository methods that might not find an entity
- Configuration values that might not exist
- User profile data that might be incomplete
- Any operation where "not found" is a valid outcome

### Creating Optionals

```csharp
// Creating Optional values
var some = Optional<string>.Of("Hello World");           // Contains a value
var none = Optional<string>.Empty();                     // Empty
var maybe = Optional<string>.OfNullable(nullableString); // Safe from null

// Using helper methods
var some2 = Some("Hello World");
var none2 = None<string>();
var maybe2 = Maybe(nullableString);
```

### Basic Operations

```csharp
public class UserService
{
    public Optional<string> GetUserDisplayName(int userId)
    {
        return _userRepository.FindById(userId)
            .Map(user => user.Name)                          // Transform if present
            .Filter(name => !string.IsNullOrWhiteSpace(name)) // Filter out empty names
            .OrElse("Anonymous");                            // Provide default
    }

    public bool IsUserAdmin(int userId)
    {
        return _userRepository.FindById(userId)
            .Map(user => user.Roles)
            .Filter(roles => roles.Any())
            .Map(roles => roles.Contains("Admin"))
            .OrElse(false);
    }
}
```

### Chaining Operations

```csharp
public class ProfileService
{
    public Optional<string> GetUserAvatarUrl(int userId)
    {
        return _userRepository.FindById(userId)
            .FlatMap(user => user.Profile.ToOptional())     // Chain Optionals
            .FlatMap(profile => profile.AvatarUrl.ToOptional())
            .Filter(url => Uri.IsWellFormedUriString(url, UriKind.Absolute));
    }

    public UserSummary GetUserSummary(int userId)
    {
        var user = _userRepository.FindById(userId);
        
        return new UserSummary
        {
            HasUser = user.HasValue,
            DisplayName = user.Map(u => u.Name).OrElse("Unknown"),
            Email = user.Map(u => u.Email).OrElse(""),
            ProfileComplete = user
                .FlatMap(u => u.Profile.ToOptional())
                .Map(p => p.IsComplete)
                .OrElse(false)
        };
    }
}
```

### Collection Operations

```csharp
public class DataProcessor
{
    public List<ProcessedItem> ProcessItems(List<RawItem> items)
    {
        return items
            .Select(TryProcessItem)           // Convert to Optional<ProcessedItem>
            .CatOptionals()                   // Keep only successful conversions
            .ToList();
    }

    public Optional<User> FindFirstActiveAdmin()
    {
        return _users
            .Where(u => u.IsActive)
            .FirstOrNone(u => u.Roles.Contains("Admin"));
    }

    public Dictionary<string, string> GetValidConfigValues()
    {
        return _rawConfig
            .Select(kvp => new 
            { 
                Key = kvp.Key, 
                Value = kvp.Value.ToOptional().Filter(v => !string.IsNullOrEmpty(v))
            })
            .Where(x => x.Value.HasValue)
            .ToDictionary(x => x.Key, x => x.Value.Value);
    }

    private Optional<ProcessedItem> TryProcessItem(RawItem item)
    {
        try
        {
            var processed = new ProcessedItem(item);
            return processed.IsValid ? Some(processed) : None<ProcessedItem>();
        }
        catch
        {
            return None<ProcessedItem>();
        }
    }
}
```

### Safe Dictionary and Collection Access

```csharp
public class ConfigurationService
{
    private readonly Dictionary<string, string> _settings;
    private readonly List<Feature> _features;

    public Optional<T> GetSetting<T>(string key, Func<string, T> parser)
    {
        return _settings.GetOptional(key)
            .FlatMap(value => TryParse(value, parser));
    }

    public Optional<Feature> GetFeature(string name)
    {
        return _features.Find(f => f.Name == name);
    }

    public Optional<User> GetUserAtPosition(int index)
    {
        return _users.ElementAtOrNone(index);
    }

    private Optional<T> TryParse<T>(string value, Func<string, T> parser)
    {
        try
        {
            return Some(parser(value));
        }
        catch
        {
            return None<T>();
        }
    }
}
```

## Result Pattern

The Result pattern provides explicit error handling without exceptions, making error states part of the type system.

### When to Use Result
- Operations that can fail with meaningful error messages
- External API calls
- File operations
- Database operations
- Business rule validation

### Creating Results

```csharp
// Creating Result values
var success = Result<int>.Success(42);
var failure = Result<int>.Failure("Something went wrong");

// Using helper methods
var success2 = Ok(42);
var failure2 = Err<int>("Error message");

// From exceptions
var result = Try(() => int.Parse("invalid"));  // Returns Result<int>
```

### Basic Operations

```csharp
public class UserService
{
    public async Task<Result<User>> CreateUserAsync(CreateUserRequest request)
    {
        return await ValidateRequest(request)
            .Then(async req => await CheckEmailUniqueness(req.Email))
            .Then(req => CreateUserEntity(req))
            .Then(async user => await SaveUserAsync(user));
    }

    public async Task<Result<User>> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var userResult = await GetUserByIdAsync(id);
        
        return userResult
            .Then(user => ValidateUpdateRequest(user, request))
            .Then(user => ApplyUpdates(user, request))
            .Then(async user => await SaveUserAsync(user));
    }

    private Result<CreateUserRequest> ValidateRequest(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Err<CreateUserRequest>("Name is required");

        if (string.IsNullOrWhiteSpace(request.Email))
            return Err<CreateUserRequest>("Email is required");

        return Ok(request);
    }
}
```

### Error Recovery

```csharp
public class PaymentService
{
    public async Task<Result<PaymentResult>> ProcessPaymentAsync(PaymentRequest request)
    {
        return await TryPrimaryProcessor(request)
            .Recover(async error => await TrySecondaryProcessor(request))
            .Recover(async error => await TryTertiaryProcessor(request))
            .TapError(error => LogPaymentFailure(request, error));
    }

    public async Task<Result<Order>> PlaceOrderAsync(OrderRequest request)
    {
        var result = await ProcessOrderAsync(request);
        
        return result.Match(
            onSuccess: order => Ok(order),
            onFailure: error => error.Contains("inventory") 
                ? CreateBackorderAsync(request)
                : Err<Order>(error)
        );
    }

    private async Task<Result<PaymentResult>> TryPrimaryProcessor(PaymentRequest request)
    {
        try
        {
            var result = await _primaryProcessor.ProcessAsync(request);
            return result.IsSuccessful ? Ok(result) : Err<PaymentResult>("Primary processor failed");
        }
        catch (Exception ex)
        {
            return Err<PaymentResult>($"Primary processor error: {ex.Message}");
        }
    }
}
```

### Combining Multiple Results

```csharp
public class OrderValidator
{
    public async Task<Result<ValidatedOrder>> ValidateOrderAsync(OrderRequest request)
    {
        // Run validations in parallel
        var customerTask = ValidateCustomerAsync(request.CustomerId);
        var inventoryTask = ValidateInventoryAsync(request.Items);
        var pricingTask = ValidatePricingAsync(request.Items);

        var customerResult = await customerTask;
        var inventoryResult = await inventoryTask;  
        var pricingResult = await pricingTask;

        // Combine all results
        return customerResult
            .Zip(inventoryResult, pricingResult,
                (customer, inventory, pricing) => new ValidatedOrder(customer, inventory, pricing));
    }

    public async Task<Result<IEnumerable<User>>> GetMultipleUsersAsync(IEnumerable<int> userIds)
    {
        var tasks = userIds.Select(id => GetUserAsync(id));
        return await tasks.CombineResults();
    }

    public Result<ProcessingReport> ProcessBatch(IEnumerable<Item> items)
    {
        var results = items.Select(ProcessItem);
        var (successes, failures) = results.Partition();

        return Ok(new ProcessingReport
        {
            SuccessCount = successes.Count(),
            FailureCount = failures.Count(),
            Errors = failures.ToList()
        });
    }
}
```

## Either Pattern

Either represents a value that can be one of two types, typically used for computations that can succeed or fail with rich error information.

### When to Use Either
- When you need rich error information (not just strings)
- Representing union types
- Pipeline operations with detailed error context
- When Result<T> isn't expressive enough

### Creating Either Values

```csharp
// Creating Either values
var right = Either<Error, User>.FromRight(user);        // Success case
var left = Either<Error, User>.FromLeft(new Error("Failed")); // Error case

// Implicit conversions
Either<string, int> result1 = 42;              // Becomes Right(42)
Either<string, int> result2 = "Error";         // Becomes Left("Error")
```

### Basic Operations

```csharp
public class DataProcessor
{
    public Either<ProcessingError, ProcessedData> ProcessData(RawData input)
    {
        return ValidateInput(input)
            .FlatMap(TransformData)
            .FlatMap(EnrichData)
            .Map(data => new ProcessedData(data));
    }

    private Either<ProcessingError, RawData> ValidateInput(RawData input)
    {
        if (input == null)
            return Either<ProcessingError, RawData>.FromLeft(
                new ProcessingError("INPUT_NULL", "Input cannot be null"));

        if (string.IsNullOrEmpty(input.Id))
            return Either<ProcessingError, RawData>.FromLeft(
                new ProcessingError("MISSING_ID", "Input ID is required"));

        return Either<ProcessingError, RawData>.FromRight(input);
    }

    private Either<ProcessingError, TransformedData> TransformData(RawData input)
    {
        try
        {
            var transformed = new TransformedData(input);
            return Either<ProcessingError, TransformedData>.FromRight(transformed);
        }
        catch (InvalidDataException ex)
        {
            return Either<ProcessingError, TransformedData>.FromLeft(
                new ProcessingError("TRANSFORM_FAILED", $"Data transformation failed: {ex.Message}"));
        }
    }

    // Usage with pattern matching
    public string HandleProcessingResult(Either<ProcessingError, ProcessedData> result)
    {
        return result.Match(
            onLeft: error => $"Processing failed: {error.Code} - {error.Message}",
            onRight: data => $"Processing successful: {data.Id}"
        );
    }
}

public record ProcessingError(string Code, string Message);
```

### Complex Error Handling

```csharp
public class FileProcessor
{
    public Either<FileError, ProcessedFile> ProcessFile(FileInfo fileInfo)
    {
        return ValidateFile(fileInfo)
            .FlatMap(ReadFileContent)
            .FlatMap(ParseContent)
            .FlatMap(ValidateContent)
            .Map(content => new ProcessedFile(fileInfo.Name, content));
    }

    private Either<FileError, FileInfo> ValidateFile(FileInfo fileInfo)
    {
        if (!fileInfo.Exists)
            return Either<FileError, FileInfo>.FromLeft(
                new FileError(FileErrorType.NotFound, $"File {fileInfo.Name} not found"));

        if (fileInfo.Length == 0)
            return Either<FileError, FileInfo>.FromLeft(
                new FileError(FileErrorType.Empty, $"File {fileInfo.Name} is empty"));

        if (fileInfo.Length > 10 * 1024 * 1024) // 10MB
            return Either<FileError, FileInfo>.FromLeft(
                new FileError(FileErrorType.TooLarge, $"File {fileInfo.Name} exceeds size limit"));

        return Either<FileError, FileInfo>.FromRight(fileInfo);
    }

    private Either<FileError, string> ReadFileContent(FileInfo fileInfo)
    {
        try
        {
            var content = File.ReadAllText(fileInfo.FullName);
            return Either<FileError, string>.FromRight(content);
        }
        catch (UnauthorizedAccessException)
        {
            return Either<FileError, string>.FromLeft(
                new FileError(FileErrorType.AccessDenied, $"Access denied to file {fileInfo.Name}"));
        }
        catch (IOException ex)
        {
            return Either<FileError, string>.FromLeft(
                new FileError(FileErrorType.ReadError, $"IO error reading {fileInfo.Name}: {ex.Message}"));
        }
    }
}

public record FileError(FileErrorType Type, string Message);
public enum FileErrorType { NotFound, Empty, TooLarge, AccessDenied, ReadError, ParseError }
```

## Validation Pattern

The Validation pattern accumulates multiple errors instead of failing on the first error, providing complete feedback to users.

### When to Use Validation
- Form validation
- Business rule validation
- Data import validation
- Any scenario where you want to collect all errors

### Single Field Validation

```csharp
public static class Validators
{
    public static Validation<string> ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Validation<string>.Failure("Email is required");

        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        if (!emailRegex.IsMatch(email))
            return Validation<string>.Failure("Email format is invalid");

        if (email.Length > 100)
            return Validation<string>.Failure("Email cannot exceed 100 characters");

        return Validation<string>.Success(email);
    }

    public static Validation<string> ValidateName(string name)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Name is required");
        else
        {
            if (name.Length < 2)
                errors.Add("Name must be at least 2 characters");

            if (name.Length > 100)
                errors.Add("Name cannot exceed 100 characters");

            if (name.Any(char.IsDigit))
                errors.Add("Name cannot contain numbers");
        }

        return errors.Any()
            ? Validation<string>.Failure(errors)
            : Validation<string>.Success(name);
    }

    public static Validation<int> ValidateAge(int age)
    {
        return age switch
        {
            < 0 => Validation<int>.Failure("Age cannot be negative"),
            > 150 => Validation<int>.Failure("Age must be realistic"),
            < 13 => Validation<int>.Failure("User must be at least 13 years old"),
            _ => Validation<int>.Success(age)
        };
    }
}
```

### Complex Object Validation

```csharp
public record CreateUserRequest(string Name, string Email, int Age, string Password, string ConfirmPassword);

public class CreateUserRequestValidator
{
    public Validation<CreateUserRequest> Validate(CreateUserRequest request)
    {
        return Validators.ValidateName(request.Name)
            .Combine(Validators.ValidateEmail(request.Email), (name, email) => name)
            .Combine(Validators.ValidateAge(request.Age), (nameEmail, age) => nameEmail)
            .Combine(ValidatePassword(request.Password), (nameEmailAge, password) => nameEmailAge)
            .Combine(ValidatePasswordConfirmation(request.Password, request.ConfirmPassword), (all, confirmation) => all)
            .Map(_ => request);
    }

    private Validation<string> ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
            errors.Add("Password is required");
        else
        {
            if (password.Length < 8)
                errors.Add("Password must be at least 8 characters");

            if (!password.Any(char.IsUpper))
                errors.Add("Password must contain at least one uppercase letter");

            if (!password.Any(char.IsLower))
                errors.Add("Password must contain at least one lowercase letter");

            if (!password.Any(char.IsDigit))
                errors.Add("Password must contain at least one digit");

            if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c)))
                errors.Add("Password must contain at least one special character");
        }

        return errors.Any()
            ? Validation<string>.Failure(errors)
            : Validation<string>.Success(password);
    }

    private Validation<string> ValidatePasswordConfirmation(string password, string confirmPassword)
    {
        if (password != confirmPassword)
            return Validation<string>.Failure("Password confirmation does not match");

        return Validation<string>.Success(confirmPassword);
    }
}
```

### Async Validation

```csharp
public class AsyncUserValidator : IAsyncValidatable
{
    private readonly CreateUserRequest _request;
    private readonly IUserRepository _userRepository;
    private readonly IBlacklistService _blacklistService;

    public async Task<Validation<Unit>> ValidateAsync(CancellationToken cancellationToken = default)
    {
        // Run synchronous validations first
        var syncValidation = new CreateUserRequestValidator().Validate(_request);
        if (syncValidation.IsInvalid)
            return Validation<Unit>.Failure(syncValidation.Errors);

        // Run async validations
        var emailUniqueTask = ValidateEmailUniqueAsync(cancellationToken);
        var domainAllowedTask = ValidateEmailDomainAsync(cancellationToken);
        var usernameAvailableTask = ValidateUsernameAvailableAsync(cancellationToken);

        var emailUnique = await emailUniqueTask;
        var domainAllowed = await domainAllowedTask;
        var usernameAvailable = await usernameAvailableTask;

        return emailUnique
            .Combine(domainAllowed, (e1, e2) => e1)
            .Combine(usernameAvailable, (e1e2, e3) => Unit.Value);
    }

    private async Task<Validation<string>> ValidateEmailUniqueAsync(CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.FindByEmailAsync(_request.Email);
        return existingUser.HasValue
            ? Validation<string>.Failure("Email is already registered")
            : Validation<string>.Success(_request.Email);
    }

    private async Task<Validation<string>> ValidateEmailDomainAsync(CancellationToken cancellationToken)
    {
        var domain = _request.Email.Split('@').LastOrDefault();
        if (string.IsNullOrEmpty(domain))
            return Validation<string>.Failure("Invalid email format");

        var isBlacklisted = await _blacklistService.IsDomainBlacklistedAsync(domain, cancellationToken);
        return isBlacklisted
            ? Validation<string>.Failure("Email domain is not allowed")
            : Validation<string>.Success(domain);
    }

    private async Task<Validation<string>> ValidateUsernameAvailableAsync(CancellationToken cancellationToken)
    {
        var username = ExtractUsernameFromEmail(_request.Email);
        var isAvailable = await _userRepository.IsUsernameAvailableAsync(username);
        
        return isAvailable
            ? Validation<string>.Success(username)
            : Validation<string>.Failure("Username is not available");
    }
}
```

### Nested Object Validation

```csharp
public record CreateOrderRequest(CustomerInfo Customer, List<OrderItem> Items, PaymentInfo Payment);
public record CustomerInfo(string Name, string Email, Address BillingAddress);
public record Address(string Street, string City, string State, string ZipCode, string Country);
public record OrderItem(int ProductId, int Quantity, decimal Price);
public record PaymentInfo(string CardNumber, string ExpiryDate, string CVV);

public class CreateOrderRequestValidator
{
    public Validation<CreateOrderRequest> Validate(CreateOrderRequest request)
    {
        return ValidateCustomer(request.Customer)
            .Combine(ValidateItems(request.Items), (customer, items) => customer)
            .Combine(ValidatePayment(request.Payment), (customerItems, payment) => customerItems)
            .Map(_ => request);
    }

    private Validation<CustomerInfo> ValidateCustomer(CustomerInfo customer)
    {
        return Validators.ValidateName(customer.Name)
            .Combine(Validators.ValidateEmail(customer.Email), (name, email) => name)
            .Combine(ValidateAddress(customer.BillingAddress), (nameEmail, address) => customer);
    }

    private Validation<Address> ValidateAddress(Address address)
    {
        return ValidateStreet(address.Street)
            .Combine(ValidateCity(address.City), (street, city) => street)
            .Combine(ValidateState(address.State), (streetCity, state) => streetCity)
            .Combine(ValidateZipCode(address.ZipCode), (streetCityState, zip) => streetCityState)
            .Combine(ValidateCountry(address.Country), (all, country) => address);
    }

    private Validation<List<OrderItem>> ValidateItems(List<OrderItem> items)
    {
        if (items == null || !items.Any())
            return Validation<List<OrderItem>>.Failure("Order must contain at least one item");

        var itemValidations = items.Select((item, index) => ValidateOrderItem(item, index));
        return Validation<OrderItem>.Combine(itemValidations)
            .Map(validItems => validItems.ToList());
    }

    private Validation<OrderItem> ValidateOrderItem(OrderItem item, int index)
    {
        var errors = new List<string>();

        if (item.ProductId <= 0)
            errors.Add($"Item {index + 1}: Product ID must be positive");

        if (item.Quantity <= 0)
            errors.Add($"Item {index + 1}: Quantity must be positive");

        if (item.Price < 0)
            errors.Add($"Item {index + 1}: Price cannot be negative");

        return errors.Any()
            ? Validation<OrderItem>.Failure(errors)
            : Validation<OrderItem>.Success(item);
    }
}
```

## Railway Programming

Railway programming allows you to chain operations that can fail, automatically handling the error flow.

### Basic Railway Operations

```csharp
public class OrderProcessor
{
    public async Task<Result<OrderResult>> ProcessOrderAsync(CreateOrderRequest request)
    {
        return await Railway.StartWith(request)
            .Then(ValidateOrderRequest)
            .Then(async req => await ReserveInventoryAsync(req))
            .Then(async req => await CalculatePricingAsync(req))
            .Then(async order => await ProcessPaymentAsync(order))
            .Then(async order => await CreateOrderAsync(order))
            .ThenDo(async order => await PublishOrderEventsAsync(order))
            .MapAsync(order => new OrderResult(order.Id, order.Total, order.Status));
    }

    private Result<CreateOrderRequest> ValidateOrderRequest(CreateOrderRequest request)
    {
        var validation = new CreateOrderRequestValidator().Validate(request);
        return validation.IsValid
            ? Ok(request)
            : Err<CreateOrderRequest>(string.Join("; ", validation.Errors));
    }

    private async Task<Result<OrderWithInventory>> ReserveInventoryAsync(CreateOrderRequest request)
    {
        var reservationTasks = request.Items.Select(async item =>
            await _inventoryService.ReserveAsync(item.ProductId, item.Quantity));

        var results = await Task.WhenAll(reservationTasks);
        var failures = results.Where(r => r.IsFailure).ToList();

        if (failures.Any())
        {
            // Rollback successful reservations
            var successes = results.Where(r => r.IsSuccess);
            await Task.WhenAll(successes.Select(s => _inventoryService.ReleaseAsync(s.Value.Id)));
            
            return Err<OrderWithInventory>($"Inventory reservation failed: {string.Join(", ", failures.Select(f => f.Error))}");
        }

        return Ok(new OrderWithInventory(request, results.Select(r => r.Value).ToList()));
    }
}
```

### Complex Railway with Recovery

```csharp
public class DocumentProcessor
{
    public async Task<Result<ProcessedDocument>> ProcessDocumentAsync(UploadedDocument document)
    {
        return await Railway.StartWith(document)
            .Then(ValidateDocument)
            .Then(async doc => await ExtractTextAsync(doc))
            .Then(async doc => await ClassifyDocumentAsync(doc))
            .Then(async doc => await ValidateContentAsync(doc))
            .Then(async doc => await EnrichDocumentAsync(doc))
            .OrElse(async error => await TryAlternativeProcessingAsync(document, error))
            .Then(async doc => await StoreDocumentAsync(doc))
            .ThenDo(async doc => await NotifyProcessingCompleteAsync(doc))
            .TapError(async error => await LogProcessingFailureAsync(document, error));
    }

    private async Task<Result<ProcessedDocument>> TryAlternativeProcessingAsync(UploadedDocument document, string error)
    {
        if (error.Contains("classification"))
        {
            // Try manual classification
            return await ProcessWithManualClassificationAsync(document);
        }

        if (error.Contains("text extraction"))
        {
            // Try alternative OCR service
            return await ProcessWithAlternativeOcrAsync(document);
        }

        return Err<ProcessedDocument>(error);
    }
}
```

### Pipeline Builder Pattern

```csharp
public class DataTransformationPipeline
{
    public Result<FinalData> TransformData(RawData input)
    {
        return Railway.StartWith(input)
            .Then(ValidateInput)
            .ThenIf(data => data.IsComplete, "Data is incomplete")
            .Then(NormalizeData)
            .Then(EnrichWithExternalData)
            .ThenIf(data => data.QualityScore > 0.8m, "Data quality too low")
            .Then(ApplyBusinessRules)
            .ThenDo(data => LogSuccessfulTransformation(data))
            .Then(ConvertToFinalFormat)
            .Build();
    }

    // Alternative fluent syntax
    public Result<FinalData> TransformDataFluent(RawData input)
    {
        var pipeline = Pipe.CreatePipeline<RawData, Result<FinalData>>(ValidateInput)
            .Then(result => result.Then(NormalizeData))
            .Then(result => result.ThenIf(data => data.IsComplete, "Data is incomplete"))
            .Then(result => result.Then(EnrichWithExternalData))
            .Build();

        return pipeline(input);
    }
}
```

## Pattern Matching

Pattern matching provides a clean, functional way to handle different cases and types.

### Basic Pattern Matching

```csharp
public class NotificationService
{
    public async Task<string> ProcessNotificationAsync(Notification notification)
    {
        return await notification.Type.Switch(
            PatternMatching.Case(NotificationType.Email, async () => await SendEmailAsync(notification)),
            PatternMatching.Case(NotificationType.SMS, async () => await SendSmsAsync(notification)),
            PatternMatching.Case(NotificationType.Push, async () => await SendPushAsync(notification)),
            PatternMatching.Wildcard<NotificationType, Task<string>>(async () => "Unknown notification type")
        );
    }

    public string GetPriorityLevel(Order order)
    {
        return order.Status.Switch(
            PatternMatching.Case<OrderStatus, string>(
                s => s == OrderStatus.Pending && order.Total > 1000, "High Priority"),
            PatternMatching.Case<OrderStatus, string>(
                s => s == OrderStatus.Pending && order.Total > 500, "Medium Priority"),
            PatternMatching.Case(OrderStatus.Pending, "Normal Priority"),
            PatternMatching.Case(OrderStatus.Cancelled, "No Priority"),
            PatternMatching.Wildcard<OrderStatus, string>("Standard Priority")
        );
    }
}
```

### Type-Based Pattern Matching

```csharp
public class PaymentProcessor
{
    public Result<PaymentResult> ProcessPayment(IPaymentMethod paymentMethod)
    {
        return paymentMethod.Switch(
            PatternMatching.Case<IPaymentMethod, CreditCard, Result<PaymentResult>>(
                card => ProcessCreditCard(card)),
            PatternMatching.Case<IPaymentMethod, BankTransfer, Result<PaymentResult>>(
                transfer => ProcessBankTransfer(transfer)),
            PatternMatching.Case<IPaymentMethod, DigitalWallet, Result<PaymentResult>>(
                wallet => ProcessDigitalWallet(wallet)),
            PatternMatching.Wildcard<IPaymentMethod, Result<PaymentResult>>(
                _ => Err<PaymentResult>("Unsupported payment method"))
        );
    }

    public string FormatPaymentMethod(IPaymentMethod paymentMethod)
    {
        return paymentMethod.Switch(
            PatternMatching.Case<IPaymentMethod, CreditCard, string>(
                card => $"Credit Card ending in {card.LastFourDigits}"),
            PatternMatching.Case<IPaymentMethod, BankTransfer, string>(
                transfer => $"Bank Transfer from {transfer.BankName}"),
            PatternMatching.Case<IPaymentMethod, DigitalWallet, string>(
                wallet => $"Digital Wallet ({wallet.Provider})"),
            PatternMatching.Wildcard<IPaymentMethod, string>(
                _ => "Unknown payment method")
        );
    }
}
```

### Complex Conditional Pattern Matching

```csharp
public class OrderStatusHandler
{
    public string GetStatusMessage(Order order, User currentUser)
    {
        return order.Switch(
            PatternMatching.Case<Order, string>(
                o => o.Status == OrderStatus.Pending && o.CreatedDate.AddHours(24) < DateTime.UtcNow,
                o => $"Order #{o.Id} is overdue - created {o.CreatedDate:yyyy-MM-dd}"),
            
            PatternMatching.Case<Order, string>(
                o => o.Status == OrderStatus.Pending && o.CustomerId == currentUser.Id,
                o => $"Your order #{o.Id} is being processed"),
            
            PatternMatching.Case<Order, string>(
                o => o.Status == OrderStatus.Shipped && o.EstimatedDelivery.HasValue,
                o => $"Order #{o.Id} shipped - estimated delivery: {o.EstimatedDelivery:yyyy-MM-dd}"),
            
            PatternMatching.Case<Order, string>(
                o => o.Status == OrderStatus.Delivered && o.CustomerId == currentUser.Id,
                o => $"Your order #{o.Id} has been delivered"),
            
            PatternMatching.Wildcard<Order, string>(
                o => $"Order #{o.Id} status: {o.Status}")
        );
    }

    public OrderAction GetAvailableAction(Order order, User currentUser)
    {
        return order.Switch(
            PatternMatching.Case<Order, OrderAction>(
                o => o.Status == OrderStatus.Pending && o.CustomerId == currentUser.Id,
                _ => OrderAction.Cancel),
            
            PatternMatching.Case<Order, OrderAction>(
                o => o.Status == OrderStatus.Shipped && o.CustomerId == currentUser.Id,
                _ => OrderAction.Track),
            
            PatternMatching.Case<Order, OrderAction>(
                o => o.Status == OrderStatus.Delivered && o.CustomerId == currentUser.Id && !o.IsReviewed,
                _ => OrderAction.Review),
            
            PatternMatching.Wildcard<Order, OrderAction>(
                _ => OrderAction.View)
        );
    }
}
```

## Combining Patterns

Real-world scenarios often combine multiple patterns for maximum effectiveness.

### Repository with Optional and Result

```csharp
public class UserRepository : IUserRepository
{
    public async Task<Optional<User>> FindByIdAsync(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        return Optional<User>.OfNullable(user);
    }

    public async Task<Result<User>> CreateUserAsync(User user)
    {
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(user);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            return Err<User>("Email already exists");
        }
        catch (Exception ex)
        {
            return Err<User>($"Failed to create user: {ex.Message}");
        }
    }

    public async Task<Result<User>> UpdateUserAsync(User user)
    {
        var existingUser = await FindByIdAsync(user.Id);
        
        return await existingUser
            .ToResult($"User {user.Id} not found")
            .Then(async existing => 
            {
                existing.UpdateFrom(user);
                await _context.SaveChangesAsync();
                return Ok(existing);
            });
    }
}
```

### Service Layer with All Patterns

```csharp
public class UserManagementService
{
    public async Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        // 1. Validation pattern
        var validation = await new AsyncUserValidator(request, _userRepository).ValidateAsync();
        if (validation.IsInvalid)
            return Err<UserDto>(string.Join("; ", validation.Errors));

        // 2. Railway programming with Result pattern
        return await Railway.StartWith(request)
            .Then(req => CreateUserEntity(req))
            .Then(async user => await _userRepository.CreateUserAsync(user))
            .Then(async user => await AssignDefaultRoleAsync(user))
            .Then(async user => await SendWelcomeEmailAsync(user))
            .ThenDo(async user => await PublishUserCreatedEventAsync(user))
            .MapAsync(user => MapToDto(user));
    }

    public async Task<Optional<UserProfileDto>> GetUserProfileAsync(int userId, int requestingUserId)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        
        return user
            .Filter(u => u.IsActive)
            .Filter(u => CanViewProfile(u, requestingUserId))
            .FlatMap(u => u.Profile.ToOptional())
            .Map(profile => new UserProfileDto(profile));
    }

    public async Task<Either<AuthError, AuthenticatedUser>> AuthenticateUserAsync(LoginRequest request)
    {
        return await ValidateLoginRequest(request)
            .FlatMap(async req => await FindUserByEmailAsync(req.Email))
            .FlatMap(user => ValidateUserStatus(user))
            .FlatMap(user => ValidatePassword(user, request.Password))
            .FlatMap(async user => await CreateSessionAsync(user))
            .MapAsync(session => new AuthenticatedUser(session.User, session.Token));
    }

    private bool CanViewProfile(User user, int requestingUserId)
    {
        return user.Privacy.Switch(
            PatternMatching.Case(PrivacyLevel.Public, true),
            PatternMatching.Case(PrivacyLevel.Friends, user.IsFriendsWith(requestingUserId)),
            PatternMatching.Case(PrivacyLevel.Private, user.Id == requestingUserId),
            PatternMatching.Wildcard<PrivacyLevel, bool>(false)
        );
    }
}
```

### Complete CRUD Operations

```csharp
public class ProductService
{
    public async Task<Result<PagedResult<ProductDto>>> GetProductsAsync(ProductSearchRequest request)
    {
        var validation = ValidateSearchRequest(request);
        if (validation.IsInvalid)
            return Err<PagedResult<ProductDto>>(string.Join("; ", validation.Errors));

        var products = await _productRepository.SearchAsync(request);
        var productDtos = products.Select(MapToDto).ToList();
        
        return Ok(new PagedResult<ProductDto>(productDtos, request.PageNumber, request.PageSize));
    }

    public async Task<Optional<ProductDto>> GetProductByIdAsync(int id)
    {
        return await _productRepository.FindByIdAsync(id)
            .Filter(p => p.IsActive)
            .MapAsync(product => MapToDto(product));
    }

    public async Task<Result<ProductDto>> CreateProductAsync(CreateProductRequest request)
    {
        return await Railway.StartWith(request)
            .Then(ValidateCreateRequest)
            .Then(async req => await CheckProductNameUniqueness(req.Name))
            .Then(req => CreateProductEntity(req))
            .Then(async product => await _productRepository.CreateAsync(product))
            .ThenDo(async product => await InvalidateCacheAsync())
            .ThenDo(async product => await PublishProductCreatedEventAsync(product))
            .MapAsync(product => MapToDto(product));
    }

    public async Task<Result<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        var existingProduct = await _productRepository.FindByIdAsync(id);
        
        return await existingProduct
            .ToResult($"Product {id} not found")
            .Then(product => ValidateUpdateRequest(product, request))
            .Then(product => ApplyUpdates(product, request))
            .Then(async product => await _productRepository.UpdateAsync(product))
            .ThenDo(async product => await InvalidateCacheAsync())
            .MapAsync(product => MapToDto(product));
    }

    public async Task<Result<Unit>> DeleteProductAsync(int id)
    {
        var existingProduct = await _productRepository.FindByIdAsync(id);
        
        return await existingProduct
            .ToResult($"Product {id} not found")
            .ThenIf(product => !product.HasActiveOrders, "Cannot delete product with active orders")
            .Then(async product => 
            {
                await _productRepository.DeleteAsync(product.Id);
                return Ok(Unit.Value);
            })
            .ThenDo(async _ => await InvalidateCacheAsync());
    }
}
```

These core patterns form the foundation of functional programming in .NET with FunctionalKit. Master these patterns, and you'll be able to write more robust, maintainable, and testable code. Each pattern serves a specific purpose, but they become truly powerful when combined together in real-world scenarios.