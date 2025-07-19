# FunctionalKit API Reference

Complete reference documentation for all classes, methods, and extension methods in FunctionalKit.

## Table of Contents

- [Core Types](#core-types)
- [Optional](#optional)
- [Result](#result)
- [Either](#either)
- [Validation](#validation)
- [Railway Programming](#railway-programming)
- [Pattern Matching](#pattern-matching)
- [Messaging System](#messaging-system)
- [Pipeline Behaviors](#pipeline-behaviors)
- [Extension Methods](#extension-methods)
- [Utility Classes](#utility-classes)
- [Configuration](#configuration)

## Core Types

### Optional&lt;T&gt;

Eliminates null reference exceptions by making the absence of a value explicit and type-safe.

#### Properties
```csharp
bool HasValue                    // True if the Optional contains a value
bool IsEmpty                     // True if the Optional is empty (no value)
T Value                          // Gets the value (throws if empty)
```

#### Creating Optionals
```csharp
Optional<T>.Of(T value)                    // Creates Optional with value (throws if null)
Optional<T>.OfNullable(T? value)          // Creates Optional, handles null safely
Optional<T>.Empty()                       // Creates empty Optional
```

#### Getting Values Safely
```csharp
T OrElse(T defaultValue)                                    // Returns value or default if empty
T OrElse(Func<T> supplier)                                 // Returns value or calls supplier if empty
T OrElseGet(Func<T> supplier)                              // Same as above
T OrElseThrow<TException>(Func<TException> exceptionSupplier) // Returns value or throws custom exception
```

#### Transforming Values
```csharp
Optional<TResult> Map<TResult>(Func<T, TResult> mapper)     // Transforms value if present, empty if not
Optional<TResult> FlatMap<TResult>(Func<T, Optional<TResult>> mapper) // Chains Optionals together
Optional<T> Filter(Func<T, bool> predicate)               // Keeps value only if predicate is true
```

#### Side Effects
```csharp
Optional<T> IfPresent(Action<T> action)                   // Executes action if value exists
Optional<T> IfPresentOrElse(Action<T> action, Action emptyAction) // Different actions for present/empty
```

#### Conversions
```csharp
Result<T> ToResult(string errorMessage)                   // Converts to Result (empty becomes failure)
Either<TLeft, T> ToEither<TLeft>(TLeft leftValue)         // Converts to Either (empty becomes left)
T? ToNullable()                                           // Converts to nullable type
```

### Result&lt;T&gt;

Handles errors explicitly without exceptions, making error states part of the type system.

#### Properties
```csharp
bool IsSuccess                   // True if operation succeeded
bool IsFailure                   // True if operation failed
T Value                          // Gets success value (throws if failure)
string Error                     // Gets error message (throws if success)
```

#### Creating Results
```csharp
Result<T>.Success(T value)                // Creates successful result
Result<T>.Failure(string error)          // Creates failed result with error message
```

#### Getting Values Safely
```csharp
T OrElse(T defaultValue)                                  // Returns value or default if failed
T OrElse(Func<T> supplier)                               // Returns value or calls supplier if failed
T OrElseGet(Func<string, T> supplier)                    // Returns value or calls supplier with error message
```

#### Transforming Results
```csharp
Result<TResult> Map<TResult>(Func<T, TResult> mapper)     // Transforms success value, preserves failure
Result<TResult> FlatMap<TResult>(Func<T, Result<TResult>> mapper) // Chains Results together
```

#### Pattern Matching
```csharp
Result<T> Match(Action<T> onSuccess, Action<string> onFailure) // Execute different actions based on result
TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure) // Return different values
```

#### Side Effects
```csharp
Result<T> Tap(Action<T> action)                          // Execute action on success value
Result<T> TapError(Action<string> action)                // Execute action on error
Result<T> OnSuccess(Action<T> action)                    // Execute action if successful
Result<T> OnFailure(Action<string> action)               // Execute action if failed
```

#### Error Recovery
```csharp
Result<T> Recover(Func<string, Result<T>> recovery)      // Try alternative operation on failure
Result<T> Recover(T recoveryValue)                       // Use default value on failure
```

#### Filtering
```csharp
Result<T> Filter(Func<T, bool> predicate, string errorMessage) // Convert to failure if predicate fails
```

### Either&lt;TLeft, TRight&gt;

Represents a value that can be one of two types (union type). By convention: Left = error, Right = success.

#### Properties
```csharp
bool IsLeft                      // True if contains Left value
bool IsRight                     // True if contains Right value
TLeft Left                       // Gets Left value (throws if Right)
TRight Right                     // Gets Right value (throws if Left)
```

#### Creating Either
```csharp
Either<TLeft, TRight>.FromLeft(TLeft value)     // Creates Left Either (typically error)
Either<TLeft, TRight>.FromRight(TRight value)   // Creates Right Either (typically success)
```

#### Transforming Either
```csharp
Either<TLeft, TResult> Map<TResult>(Func<TRight, TResult> mapper) // Transform Right value, preserve Left
Either<TResult, TRight> MapLeft<TResult>(Func<TLeft, TResult> mapper) // Transform Left value, preserve Right
Either<TLeft, TResult> FlatMap<TResult>(Func<TRight, Either<TLeft, TResult>> mapper) // Chain Right operations
```

#### Getting Values
```csharp
TRight OrElse(TRight defaultValue)                        // Returns Right value or default
TRight OrElseGet(Func<TLeft, TRight> supplier)           // Returns Right or calls supplier with Left value
```

#### Pattern Matching
```csharp
Either<TLeft, TRight> Match(Action<TLeft> onLeft, Action<TRight> onRight) // Execute based on Left/Right
TResult Match<TResult>(Func<TLeft, TResult> onLeft, Func<TRight, TResult> onRight) // Return based on Left/Right
```

#### Utilities
```csharp
Either<TRight, TLeft> Swap()                    // Swaps Left and Right sides
Result<TRight, TLeft> ToResult()               // Converts to Result (Left becomes error)
Optional<TRight> ToOptional()                  // Converts to Optional (Left becomes empty)
```

### Validation&lt;T&gt;

Accumulates multiple validation errors instead of stopping at the first one.

#### Properties
```csharp
bool IsValid                     // True if validation passed
bool IsInvalid                   // True if validation failed
T Value                          // Gets validated value (throws if invalid)
IReadOnlyList<string> Errors     // Gets all validation error messages
```

#### Creating Validations
```csharp
Validation<T>.Success(T value)                           // Creates successful validation
Validation<T>.Failure(string error)                     // Creates failed validation with single error
Validation<T>.Failure(IEnumerable<string> errors)       // Creates failed validation with multiple errors
Validation<T>.Failure(params string[] errors)           // Creates failed validation with error array
```

#### Combining Validations
```csharp
Validation<TResult> Combine<TOther, TResult>(Validation<TOther> other, Func<T, TOther, TResult> combiner)
// Combines two validations - if both valid, applies combiner; if either invalid, accumulates all errors

static Validation<IEnumerable<T>> Combine(IEnumerable<Validation<T>> validations)
// Combines multiple validations into one - accumulates all errors if any fail
```

#### Transforming Validations
```csharp
Validation<TResult> Map<TResult>(Func<T, TResult> mapper) // Transform valid value, preserve errors
Validation<TResult> FlatMap<TResult>(Func<T, Validation<TResult>> mapper) // Chain validations
```

#### Getting Values
```csharp
T OrElse(T defaultValue)                                  // Returns value or default if invalid
T OrElseGet(Func<IReadOnlyList<string>, T> supplier)     // Returns value or calls supplier with all errors
```

#### Pattern Matching
```csharp
Validation<T> Match(Action<T> onValid, Action<IReadOnlyList<string>> onInvalid) // Execute based on validity
TResult Match<TResult>(Func<T, TResult> onValid, Func<IReadOnlyList<string>, TResult> onInvalid) // Return based on validity
```

## Railway Programming

Chains operations that can fail, automatically handling the error flow.

### Railway Static Methods

#### Starting a Pipeline
```csharp
static RailwayPipelineBuilder<T> StartWith<T>(T input)      // Start pipeline with value
static RailwayPipelineBuilder<T> StartWith<T>(Result<T> result) // Start pipeline with Result
```

#### Result Extension Methods
```csharp
Result<TResult> Then<TResult>(Func<T, Result<TResult>> next) // Chain operation that can fail
Result<TResult> Then<TResult>(Func<T, TResult> next)        // Chain operation that always succeeds
Result<T> ThenIf(Func<T, bool> predicate, string errorMessage) // Continue only if condition is met
Result<T> ThenDo(Action<T> action)                          // Execute side effect without changing value
Result<T> OrElse(Func<string, Result<T>> alternative)       // Try alternative on failure
```

#### Combining Results
```csharp
static Result<IEnumerable<T>> All<T>(params Result<T>[] results) // All must succeed or returns first error
static Result<T> FirstSuccess<T>(params Result<T>[] results)     // Returns first success or last error
```

### RailwayPipelineBuilder&lt;T&gt;

#### Building Pipeline
```csharp
RailwayPipelineBuilder<TNext> Then<TNext>(Func<T, Result<TNext>> func) // Add operation that can fail
RailwayPipelineBuilder<TNext> Then<TNext>(Func<T, TNext> func)         // Add operation that always succeeds
RailwayPipelineBuilder<T> ThenIf(Func<T, bool> predicate, string errorMessage) // Add conditional check
RailwayPipelineBuilder<T> ThenDo(Action<T> action)                     // Add side effect
Result<T> Build()                                                      // Get final result
```

## Pattern Matching

Functional pattern matching for handling different cases and types.

### PatternMatching Static Methods

#### Switch Operations
```csharp
static TResult Switch<T, TResult>(T value, params (Func<T, bool> predicate, Func<T, TResult> action)[] cases)
// Matches value against cases, returns result of first matching case

static TResult Switch<T, TResult>(T value, Func<T, TResult> defaultCase, params cases[])
// Same as above but with default case if no matches
```

#### Creating Cases
```csharp
static (Func<T, bool>, Func<T, TResult>) Case<T, TResult>(T matchValue, TResult result)
// Creates case that matches specific value and returns result

static (Func<T, bool>, Func<T, TResult>) Case<T, TResult>(Func<T, bool> predicate, TResult result)
// Creates case with custom predicate

static (Func<T, bool>, Func<T, TResult>) Case<T, TSpecific, TResult>(Func<TSpecific, TResult> resultFunc)
// Creates case that matches specific type

static (Func<T, bool>, Func<T, TResult>) Wildcard<T, TResult>(TResult result)
// Creates case that matches anything (use as default)
```

## Messaging System

Clean CQRS implementation with pipeline behaviors (alternative to MediatR).

### Core Interfaces

#### IMessenger
```csharp
Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
// Executes a query and returns response

Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
// Executes a command without return value

Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
// Executes a command and returns response

Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
// Publishes notification to all handlers
```

#### Marker Interfaces
```csharp
IQuery<TResponse>        // Marks queries that return data
ICommand                 // Marks commands that don't return data
ICommand<TResponse>      // Marks commands that return data
INotification           // Marks notifications for pub/sub
```

### Handler Interfaces

#### Query Handler
```csharp
IQueryHandler<TQuery, TResponse>
Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
// Handles queries (read operations)
```

#### Command Handlers
```csharp
ICommandHandler<TCommand>
Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
// Handles commands without return value

ICommandHandler<TCommand, TResponse>
Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
// Handles commands with return value
```

#### Notification Handler
```csharp
INotificationHandler<TNotification>
Task HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
// Handles notifications (can have multiple handlers for same notification)
```

### Pipeline Behaviors

Cross-cutting concerns that wrap around handlers.

#### Behavior Interfaces
```csharp
IQueryPipelineBehavior<TQuery, TResponse>
Task<TResponse> HandleAsync(TQuery query, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
// Wraps query execution with cross-cutting logic

ICommandPipelineBehavior<TCommand>
Task HandleAsync(TCommand command, Func<Task> next, CancellationToken cancellationToken = default)
// Wraps command execution with cross-cutting logic

ICommandPipelineBehavior<TCommand, TResponse>
Task<TResponse> HandleAsync(TCommand command, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
// Wraps command execution (with response) with cross-cutting logic
```

#### Validation Interfaces
```csharp
IValidatable
Validation<Unit> Validate()
// Implement on commands/queries for synchronous validation

IAsyncValidatable
Task<Validation<Unit>> ValidateAsync(CancellationToken cancellationToken = default)
// Implement on commands/queries for asynchronous validation
```

#### Caching Interface
```csharp
ICacheable
string CacheKey { get; }      // Unique key for caching
TimeSpan CacheDuration { get; } // How long to cache
// Implement on queries to enable automatic caching
```

### Built-in Behaviors

#### Validation Behaviors
- **QueryValidationBehavior**: Automatically validates queries implementing IValidatable
- **CommandValidationBehavior**: Automatically validates commands implementing IValidatable
- **QueryAsyncValidationBehavior**: Automatically validates queries implementing IAsyncValidatable
- **CommandAsyncValidationBehavior**: Automatically validates commands implementing IAsyncValidatable

#### Performance Behaviors
- **QueryLoggingBehavior**: Logs query execution (start, success, failure, timing)
- **CommandLoggingBehavior**: Logs command execution (start, success, failure, timing)
- **QueryPerformanceBehavior**: Monitors query performance, logs slow queries
- **QueryCachingBehavior**: Caches query results for queries implementing ICacheable
- **QueryRetryBehavior**: Retries failed queries with exponential backoff
- **QueryCircuitBreakerBehavior**: Implements circuit breaker pattern for fault tolerance

## Extension Methods

### Optional Extensions

#### Collection to Optional
```csharp
Optional<T> FirstOrNone<T>(this IEnumerable<T> source)
// Returns first element as Optional, or empty if collection is empty

Optional<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
// Returns first matching element as Optional, or empty if none match

Optional<T> LastOrNone<T>(this IEnumerable<T> source)
// Returns last element as Optional, or empty if collection is empty

Optional<T> SingleOrNone<T>(this IEnumerable<T> source)
// Returns single element as Optional, or empty if zero or multiple elements

Optional<T> Find<T>(this IEnumerable<T> source, Func<T, bool> predicate)
// Finds element matching predicate, returns as Optional

Optional<T> ElementAtOrNone<T>(this IEnumerable<T> source, int index)
// Gets element at index as Optional, empty if index out of bounds
```

#### Dictionary Extensions
```csharp
Optional<TValue> GetOptional<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
// Safely gets value from dictionary, returns empty if key not found
```

#### Optional Operations
```csharp
Optional<T> Flatten<T>(this Optional<Optional<T>> optional)
// Flattens nested Optional (Optional<Optional<T>> becomes Optional<T>)

IEnumerable<T> CatOptionals<T>(this IEnumerable<Optional<T>> source)
// Filters out empty Optionals, returns only the values

IEnumerable<TResult> MapOptional<T, TResult>(this IEnumerable<T> source, Func<T, Optional<TResult>> mapper)
// Maps collection through Optional-returning function, keeps only successful results
```

#### Optional Conversions
```csharp
T? ToNullable<T>(this Optional<T> optional) where T : class
// Converts Optional to nullable reference type

T? ToNullableValue<T>(this Optional<T> optional) where T : struct  
// Converts Optional to nullable value type

Result<T> ToResult<T>(this Optional<T> optional, string errorMessage)
// Converts Optional to Result (empty becomes failure with message)

Either<TLeft, T> ToEither<TLeft, T>(this Optional<T> optional, TLeft leftValue)
// Converts Optional to Either (empty becomes left value)
```

#### Combining Optionals
```csharp
Optional<TResult> Zip<T1, T2, TResult>(this Optional<T1> optional1, Optional<T2> optional2, Func<T1, T2, TResult> zipper)
// Combines two Optionals with function (both must have values)

Optional<IEnumerable<T>> Sequence<T>(this IEnumerable<Optional<T>> optionals)
// Converts collection of Optionals to Optional of collection (all must have values)

Optional<T> Or<T>(this Optional<T> optional, Optional<T> alternative)
// Returns first Optional if it has value, otherwise returns alternative
```

### Result Extensions

#### Combining Results
```csharp
Result<IEnumerable<T>> Combine<T>(this IEnumerable<Result<T>> results)
// Combines multiple Results - all must succeed or returns accumulated errors

Result<T> FirstSuccess<T>(this IEnumerable<Result<T>> results)
// Returns first successful Result, or last failure if all fail

Result<TResult> Zip<T1, T2, TResult>(this Result<T1> result1, Result<T2> result2, Func<T1, T2, TResult> zipper)
// Combines two Results with function (both must succeed)

Result<T> Reduce<T>(this IEnumerable<Result<T>> results, Func<T, T, T> combiner)
// Reduces collection of Results to single Result using combiner function
```

#### Result Side Effects
```csharp
Result<T> Tap<T>(this Result<T> result, Action<T> action)
// Executes action on success value, returns original result

Result<T> TapError<T>(this Result<T> result, Action<string> action)
// Executes action on error message, returns original result

Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
// Executes action only if result is successful

Result<T> OnFailure<T>(this Result<T> result, Action<string> action)
// Executes action only if result is failure
```

#### Result Filtering and Recovery
```csharp
Result<T> Filter<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
// Converts success to failure if predicate returns false

Result<T> Recover<T>(this Result<T> result, Func<string, Result<T>> recovery)
// Tries recovery function if result is failure

Result<T> Recover<T>(this Result<T> result, T recoveryValue)
// Uses recovery value if result is failure
```

#### Result Conversions
```csharp
Optional<T> ToOptional<T>(this Result<T> result)
// Converts Result to Optional (failure becomes empty)

Either<string, T> ToEither<T>(this Result<T> result)
// Converts Result to Either (failure becomes left)

(IEnumerable<T> successes, IEnumerable<string> failures) Partition<T>(this IEnumerable<Result<T>> results)
// Separates collection of Results into successes and failures
```

### Collection Extensions

#### Safe Collection Operations
```csharp
Optional<T> Head<T>(this IEnumerable<T> source)
// Gets first element safely (returns empty if collection is empty)

IEnumerable<T> Tail<T>(this IEnumerable<T> source)
// Gets all elements except the first

Optional<T> LastOptional<T>(this IEnumerable<T> source)
// Gets last element safely (returns empty if collection is empty)
```

#### Collection Transformations
```csharp
IEnumerable<T> TakeWhileInclusive<T>(this IEnumerable<T> source, Func<T, bool> predicate)
// Takes elements while predicate is true, includes the first element where predicate is false

(IEnumerable<T> matches, IEnumerable<T> nonMatches) Partition<T>(this IEnumerable<T> source, Func<T, bool> predicate)
// Splits collection into two groups based on predicate

IEnumerable<IGrouping<TKey, T>> GroupConsecutive<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
// Groups consecutive elements with the same key

IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source)
// Flattens nested collections into single collection

IEnumerable<TResult> FlatMap<T, TResult>(this IEnumerable<T> source, Func<T, IEnumerable<TResult>> mapper)
// Maps each element to collection and flattens the result
```

#### Functional Operations
```csharp
IEnumerable<TAccumulate> Scan<T, TAccumulate>(this IEnumerable<T> source, TAccumulate seed, Func<TAccumulate, T, TAccumulate> accumulator)
// Like Aggregate but returns all intermediate results

IEnumerable<T> Intersperse<T>(this IEnumerable<T> source, T separator)
// Inserts separator between each element

IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
// Splits collection into batches of specified size

IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
// Executes action for each element, returns original collection

IEnumerable<T> ForEachIndexed<T>(this IEnumerable<T> source, Action<T, int> action)
// Executes action for each element with index, returns original collection
```

#### Collection Validation
```csharp
bool AllDistinct<T>(this IEnumerable<T> source)
// Checks if all elements in collection are unique

bool AllDistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
// Checks if all elements have unique keys

Optional<IEnumerable<T>> NonEmpty<T>(this IEnumerable<T> source)
// Returns collection as Optional if it has elements, empty Optional if empty collection
```

### Task Extensions

#### Optional Task Operations
```csharp
Task<Optional<TResult>> MapAsync<T, TResult>(this Task<Optional<T>> task, Func<T, TResult> mapper)
// Transforms Optional value inside Task

Task<Optional<TResult>> FlatMapAsync<T, TResult>(this Task<Optional<T>> task, Func<T, Task<Optional<TResult>>> mapper)
// Chains Optional operations inside Task
```

#### Result Task Operations
```csharp
Task<Result<TResult>> MapAsync<T, TResult>(this Task<Result<T>> task, Func<T, TResult> mapper)
// Transforms Result value inside Task

Task<Result<TResult>> FlatMapAsync<T, TResult>(this Task<Result<T>> task, Func<T, Task<Result<TResult>>> mapper)
// Chains Result operations inside Task
```

#### Task Conversions
```csharp
Task<Result<T>> ToResult<T>(this Task<T> task)
// Converts Task to Result (exceptions become failures)

Task<Optional<T>> ToOptional<T>(this Task<T> task)
// Converts Task to Optional (exceptions become empty)

Task<Result<T>> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
// Adds timeout to Task, returns Result (timeout becomes failure)
```

#### Task Combinations
```csharp
Task<Result<IEnumerable<T>>> CombineResults<T>(this IEnumerable<Task<Result<T>>> tasks)
// Waits for all tasks and combines their Results

Task<Result<IEnumerable<T>>> SequentialResults<T>(this IEnumerable<Func<Task<Result<T>>>> taskFactories)
// Executes task factories sequentially, stops on first failure
```

### Pipe Extensions

#### Basic Piping
```csharp
TResult PipeTo<T, TResult>(this T input, Func<T, TResult> func)
// Pipes value through function (enables fluent chaining)

T Tap<T>(this T input, Action<T> action)
// Executes action on value and returns original value (side effect)
```

#### Conditional Piping
```csharp
T PipeToIf<T>(this T input, bool condition, Func<T, T> func)
// Applies function only if condition is true

T PipeToIf<T>(this T input, Func<T, bool> predicate, Func<T, T> func)
// Applies function only if predicate returns true

TResult PipeToEither<T, TResult>(this T input, bool condition, Func<T, TResult> trueFunc, Func<T, TResult> falseFunc)
// Applies different functions based on condition
```

#### Sequential Piping
```csharp
T PipeToMany<T>(this T input, params Func<T, T>[] functions)
// Applies multiple functions in sequence
```

#### Async Piping
```csharp
Task<TResult> PipeToAsync<T, TResult>(this T input, Func<T, Task<TResult>> func)
// Pipes value through async function

Task<TResult> PipeToAsync<T, TResult>(this Task<T> input, Func<T, TResult> func)
// Pipes Task result through function

Task<T> TapAsync<T>(this Task<T> input, Action<T> action)
// Executes action on Task result, returns original Task result
```

### Messenger Extensions

#### Safe Query Operations
```csharp
Task<Optional<T>> QueryOptionalAsync<T>(this IMessenger messenger, IQuery<Result<T>> query, CancellationToken cancellationToken = default)
// Executes query that returns Result<T> and converts to Optional<T>

Task<Result<T>> QuerySafeAsync<T>(this IMessenger messenger, IQuery<T> query, CancellationToken cancellationToken = default)
// Executes query and wraps result in Result (catches exceptions)
```

#### Safe Command Operations
```csharp
Task<Result<Unit>> SendSafeAsync(this IMessenger messenger, ICommand command, CancellationToken cancellationToken = default)
// Executes command and wraps in Result (catches exceptions)

Task<Result<T>> SendSafeAsync<T>(this IMessenger messenger, ICommand<T> command, CancellationToken cancellationToken = default)
// Executes command with response and wraps in Result (catches exceptions)
```

### Service Collection Extensions

#### Core Registration
```csharp
IServiceCollection AddFunctionalKit(this IServiceCollection services, params Assembly[] assemblies)
// Registers FunctionalKit with automatic handler discovery in specified assemblies

IServiceCollection AddFunctionalKit(this IServiceCollection services, Action<BehaviorOptions> configure, params Assembly[] assemblies)
// Registers FunctionalKit with behavior configuration
```

#### Individual Behavior Registration
```csharp
IServiceCollection AddFunctionalKitLogging(this IServiceCollection services)
// Adds logging behaviors for all commands and queries

IServiceCollection AddFunctionalKitValidation(this IServiceCollection services)
// Adds validation behaviors for commands/queries implementing IValidatable

IServiceCollection AddFunctionalKitCaching(this IServiceCollection services)
// Adds caching behavior for queries implementing ICacheable

IServiceCollection AddFunctionalKitPerformanceMonitoring(this IServiceCollection services, long slowQueryThresholdMs = 500)
// Adds performance monitoring behavior with configurable slow query threshold

IServiceCollection AddFunctionalKitRetry(this IServiceCollection services, int maxRetries = 3, TimeSpan? delay = null)
// Adds retry behavior with configurable retry count and delay

IServiceCollection AddFunctionalKitCircuitBreaker(this IServiceCollection services, int failureThreshold = 5, TimeSpan circuitOpenDuration = default)
// Adds circuit breaker behavior with configurable failure threshold and open duration
```

#### Convenience Methods
```csharp
IServiceCollection AddFunctionalKitDefaults(this IServiceCollection services, params Assembly[] assemblies)
// Registers FunctionalKit with commonly used behaviors (logging, validation, performance monitoring)
```

## Utility Classes

### Functional Static Class

Helper methods for creating functional types more concisely.

```csharp
// Optional creation
static Optional<T> Some<T>(T value)        // Creates Optional with value
static Optional<T> None<T>()               // Creates empty Optional
static Optional<T> Maybe<T>(T? value)      // Creates Optional from nullable

// Result creation  
static Result<T> Ok<T>(T value)            // Creates successful Result
static Result<T> Err<T>(string error)      // Creates failed Result

// Safe execution
static Result<T> Try<T>(Func<T> func)                               // Executes function safely, catches exceptions
static Task<Result<T>> TryAsync<T>(Func<Task<T>> func)             // Executes async function safely
static Result<T, Exception> TryWithException<T>(Func<T> func)       // Like Try but preserves original exception
static Task<Result<T, Exception>> TryWithExceptionAsync<T>(Func<Task<T>> func) // Async version with exception
```

### Unit Type

Represents void/nothing in functional programming contexts.

```csharp
struct Unit
{
    static Unit Value { get; }           // Singleton instance representing "no value"
    string ToString()                    // Returns "()" to represent empty value
}
```

### Circuit Breaker Components

#### CircuitBreakerState
Manages multiple circuit breakers by key.

```csharp
CircuitState GetOrCreateCircuit(string key, int failureThreshold, TimeSpan openDuration)
// Gets existing circuit or creates new one with specified parameters
```

#### CircuitState
Individual circuit breaker state management.

```csharp
CircuitStatus Status { get; }           // Current state: Closed, Open, or HalfOpen
int FailureCount { get; }               // Number of consecutive failures

bool CanExecute()                       // Returns true if operation should be allowed
void RecordSuccess()                    // Records successful operation, may close circuit
void RecordFailure()                    // Records failed operation, may open circuit
```

#### CircuitStatus Enumeration
```csharp
enum CircuitStatus
{
    Closed,      // Normal operation - requests pass through
    Open,        // Circuit is open - requests fail fast without execution
    HalfOpen     // Testing state - single request allowed to test if service recovered
}
```

## Configuration

### BehaviorOptions

Configuration class for pipeline behaviors.

```csharp
class BehaviorOptions
{
    // Behavior toggles
    bool EnableLogging { get; set; } = true                    // Enable/disable logging behavior
    bool EnableValidation { get; set; } = true                // Enable/disable validation behavior
    bool EnableCaching { get; set; } = false                  // Enable/disable caching behavior
    bool EnablePerformanceMonitoring { get; set; } = false    // Enable/disable performance monitoring
    bool EnableRetry { get; set; } = false                    // Enable/disable retry behavior

    // Performance settings
    long SlowQueryThresholdMs { get; set; } = 500             // Threshold for logging slow queries (milliseconds)

    // Retry settings
    int MaxRetries { get; set; } = 3                          // Maximum number of retry attempts
    TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1) // Base delay between retries

    // Behavior execution order
    BehaviorOrder BehaviorOrder { get; set; } = new()         // Controls order of behavior execution
}
```

### BehaviorOrder

Defines the execution order of behaviors (lower numbers execute first/outermost).

```csharp
class BehaviorOrder
{
    int Logging { get; set; } = 1                    // Logging behavior order (typically first)
    int PerformanceMonitoring { get; set; } = 2      // Performance monitoring order
    int Validation { get; set; } = 3                 // Validation behavior order
    int Caching { get; set; } = 4                    // Caching behavior order
    int Retry { get; set; } = 5                      // Retry behavior order (typically last/innermost)
}
```

### CircuitBreakerOptions

Configuration for circuit breaker behavior.

```csharp
class CircuitBreakerOptions
{
    int FailureThreshold { get; set; } = 5                           // Number of failures before opening circuit
    TimeSpan CircuitOpenDuration { get; set; } = TimeSpan.FromMinutes(1) // How long circuit stays open
}
```

## Exception Types

### FunctionalKitException

Base exception for the library.

```csharp
class FunctionalKitException : Exception
{
    FunctionalKitException(string message)                     // Creates exception with message
    FunctionalKitException(string message, Exception innerException) // Creates exception with inner exception
}
```

### HandlerNotFoundException

Thrown when no handler is found for a command or query.

```csharp
class HandlerNotFoundException : FunctionalKitException
{
    Type RequestType { get; }           // The request type that couldn't find a handler
    Type HandlerType { get; }           // The expected handler interface type

    HandlerNotFoundException(Type requestType, Type handlerType)
    // Creates exception indicating missing handler for specific request type
}
```

### MultipleHandlersFoundException

Thrown when multiple handlers are found but only one was expected.

```csharp
class MultipleHandlersFoundException : FunctionalKitException
{
    Type RequestType { get; }           // The request type with multiple handlers
    int HandlerCount { get; }           // Number of handlers found

    MultipleHandlersFoundException(Type requestType, int handlerCount)
    // Creates exception indicating too many handlers for request type
}
```

### PipelineBehaviorException

Thrown during pipeline behavior execution.

```csharp
class PipelineBehaviorException : FunctionalKitException
{
    Type BehaviorType { get; }          // The behavior that caused the exception
    Type RequestType { get; }           // The request being processed when exception occurred

    PipelineBehaviorException(Type behaviorType, Type requestType, string message, Exception innerException)
    // Creates exception with context about which behavior failed
}
```

### ValidationException

Thrown when validation fails in validation behaviors.

```csharp
class ValidationException : Exception
{
    ValidationException(string message)                     // Creates validation exception with error message
    ValidationException(string message, Exception innerException) // Creates validation exception with inner exception
}
```

### CircuitBreakerOpenException

Thrown when circuit breaker is open and blocks execution.

```csharp
class CircuitBreakerOpenException : Exception
{
    CircuitBreakerOpenException(string circuitName)         // Creates exception indicating which circuit is open
}
```

## Usage Examples

### Basic Optional Usage
```csharp
// Safe null handling
public string GetUserDisplayName(int userId)
{
    return _userRepository.FindById(userId)        // Returns Optional<User>
        .Map(user => user.Name)                    // Transform to Optional<string>
        .Filter(name => !string.IsNullOrWhiteSpace(name)) // Keep only valid names
        .OrElse("Anonymous User");                 // Provide default if empty
}

// Safe collection access
public Optional<User> GetFirstActiveAdmin()
{
    return _users.FirstOrNone(user => user.IsActive && user.IsAdmin);
}

// Safe dictionary access
public Optional<string> GetConfigValue(string key)
{
    return _config.GetOptional(key);
}
```

### Basic Result Usage
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

// Railway programming - chain operations
public Result<ProcessedData> ProcessData(RawData input)
{
    return ValidateInput(input)           // Returns Result<RawData>
        .Then(TransformData)              // Returns Result<TransformedData>
        .Then(EnrichData)                 // Returns Result<EnrichedData>
        .Then(FormatOutput);              // Returns Result<ProcessedData>
}
```

### Basic Validation Usage
```csharp
// Accumulate all validation errors
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

// Result: Gets ALL errors at once instead of stopping at first failure
// ["Name is required", "Email format invalid", "Age must be positive"]
```

### Basic Messaging Usage
```csharp
// Query (read operation)
public record GetUserQuery(int UserId) : IQuery<Result<UserDto>>;

public class GetUserHandler : IQueryHandler<GetUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> HandleAsync(GetUserQuery query, CancellationToken ct = default)
    {
        return await _repository.FindByIdAsync(query.UserId)
            .ToResult($"User {query.UserId} not found")
            .MapAsync(user => new UserDto(user.Id, user.Name, user.Email));
    }
}

// Command (write operation)
public record CreateUserCommand(string Name, string Email) : ICommand<Result<int>>;

public class CreateUserHandler : ICommandHandler<CreateUserCommand, Result<int>>
{
    public async Task<Result<int>> HandleAsync(CreateUserCommand command, CancellationToken ct = default)
    {
        var user = new User(command.Name, command.Email);
        await _repository.SaveAsync(user);
        return Result<int>.Success(user.Id);
    }
}

// Usage in controller
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

### Pattern Matching Usage
```csharp
public string ProcessNotification(NotificationType type)
{
    return type.Switch(
        PatternMatching.Case(NotificationType.Email, "Sending email..."),
        PatternMatching.Case(NotificationType.SMS, "Sending SMS..."),
        PatternMatching.Case(NotificationType.Push, "Sending push notification..."),
        PatternMatching.Wildcard<NotificationType, string>("Unknown notification type")
    );
}

public string GetPriorityLevel(Order order)
{
    return order.Status.Switch(
        PatternMatching.Case<OrderStatus, string>(
            s => s == OrderStatus.Pending && order.Total > 1000, 
            "High Priority"),
        PatternMatching.Case<OrderStatus, string>(
            s => s == OrderStatus.Pending && order.Total > 500, 
            "Medium Priority"),
        PatternMatching.Case(OrderStatus.Pending, "Normal Priority"),
        PatternMatching.Wildcard<OrderStatus, string>("Standard Priority")
    );
}
```

This API reference covers all the major methods and functionality available in FunctionalKit. Each method is designed to work together to create robust, functional programming patterns in .NET applications.