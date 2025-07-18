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
- [Configuration](#configuration)

## Core Types

### Optional&lt;T&gt;

Represents an optional value that may or may not be present.

#### Properties
- `bool HasValue` - Gets a value indicating whether the optional contains a value
- `bool IsEmpty` - Gets a value indicating whether the optional is empty
- `T Value` - Gets the value if present, otherwise throws InvalidOperationException

#### Static Methods
```csharp
Optional<T> Of(T value)                    // Creates Optional with value (throws if null)
Optional<T> OfNullable(T? value)          // Creates Optional that may contain null
Optional<T> Empty()                       // Creates empty Optional
```

#### Instance Methods
```csharp
T OrElse(T defaultValue)                                    // Returns value or default
T OrElse(Func<T> supplier)                                 // Returns value or supplier result
T OrElseGet(Func<T> supplier)                              // Returns value or supplier result
T OrElseThrow<TException>(Func<TException> exceptionSupplier) // Returns value or throws

Optional<TResult> Map<TResult>(Func<T, TResult> mapper)     // Transforms value if present
Optional<TResult> FlatMap<TResult>(Func<T, Optional<TResult>> mapper) // Flat maps

Optional<T> Filter(Func<T, bool> predicate)               // Filters value
Optional<T> IfPresent(Action<T> action)                   // Executes action if present
Optional<T> IfPresentOrElse(Action<T> action, Action emptyAction) // Conditional actions
```

#### Operators
```csharp
static implicit operator Optional<T>(T value)             // Implicit conversion from T
static bool operator ==(Optional<T> left, Optional<T> right)
static bool operator !=(Optional<T> left, Optional<T> right)
```

### Result&lt;T&gt;

Represents the result of an operation that can either succeed with a value or fail with an error.

#### Properties
- `bool IsSuccess` - Gets a value indicating whether the result represents a success
- `bool IsFailure` - Gets a value indicating whether the result represents a failure
- `T Value` - Gets the success value (throws if failure)
- `string Error` - Gets the error message (throws if success)

#### Static Methods
```csharp
Result<T> Success(T value)                // Creates successful result
Result<T> Failure(string error)          // Creates failed result
```

#### Instance Methods
```csharp
Result<TResult> Map<TResult>(Func<T, TResult> mapper)     // Maps success value
Result<TResult> FlatMap<TResult>(Func<T, Result<TResult>> mapper) // Flat maps

T OrElse(T defaultValue)                                  // Returns value or default
T OrElse(Func<T> supplier)                               // Returns value or supplier result
T OrElseGet(Func<string, T> supplier)                    // Returns value or supplier with error

Result<T> Match(Action<T> onSuccess, Action<string> onFailure) // Pattern matching
TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)

Optional<T> ToOptional()                                 // Converts to Optional
```

### Result&lt;T, TError&gt;

Generic version of Result with custom error type.

#### Properties
- `bool IsSuccess` - Success indicator
- `bool IsFailure` - Failure indicator
- `T Value` - Success value
- `TError Error` - Error value

#### Static Methods
```csharp
Result<T, TError> Success(T value)        // Creates successful result
Result<T, TError> Failure(TError error)   // Creates failed result
```

#### Instance Methods
```csharp
Result<TResult, TError> Map<TResult>(Func<T, TResult> mapper)
Result<T, TNewError> MapError<TNewError>(Func<TError, TNewError> mapper)
Result<TResult, TError> FlatMap<TResult>(Func<T, Result<TResult, TError>> mapper)

T OrElse(T defaultValue)
T OrElseGet(Func<TError, T> supplier)

Result<T, TError> Match(Action<T> onSuccess, Action<TError> onFailure)
TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TError, TResult> onFailure)

Optional<T> ToOptional()
```

### Either&lt;TLeft, TRight&gt;

Represents a value that can be one of two types: Left or Right.

#### Properties
- `bool IsLeft` - Indicates if contains Left value
- `bool IsRight` - Indicates if contains Right value
- `TLeft Left` - Gets Left value (throws if Right)
- `TRight Right` - Gets Right value (throws if Left)

#### Static Methods
```csharp
Either<TLeft, TRight> FromLeft(TLeft value)     // Creates Left Either
Either<TLeft, TRight> FromRight(TRight value)   // Creates Right Either
```

#### Instance Methods
```csharp
Either<TLeft, TResult> Map<TResult>(Func<TRight, TResult> mapper)
Either<TResult, TRight> MapLeft<TResult>(Func<TLeft, TResult> mapper)

Either<TLeft, TResult> FlatMap<TResult>(Func<TRight, Either<TLeft, TResult>> mapper)
Either<TResult, TRight> FlatMapLeft<TResult>(Func<TLeft, Either<TResult, TRight>> mapper)

TRight OrElse(TRight defaultValue)
TRight OrElseGet(Func<TLeft, TRight> supplier)

Either<TLeft, TRight> Match(Action<TLeft> onLeft, Action<TRight> onRight)
TResult Match<TResult>(Func<TLeft, TResult> onLeft, Func<TRight, TResult> onRight)

Either<TRight, TLeft> Swap()                    // Swaps Left and Right
Result<TRight, TLeft> ToResult()               // Converts to Result
Optional<TRight> ToOptional()                  // Converts to Optional

Either<TLeft, TRight> Filter(Func<TRight, bool> predicate, TLeft leftValue)
```

### Validation&lt;T&gt;

Represents a validation result that can accumulate multiple errors.

#### Properties
- `bool IsValid` - Indicates if validation is successful
- `bool IsInvalid` - Indicates if validation has errors
- `T Value` - Gets success value (throws if invalid)
- `IReadOnlyList<string> Errors` - Gets validation errors

#### Static Methods
```csharp
Validation<T> Success(T value)                           // Creates successful validation
Validation<T> Failure(string error)                     // Creates failed validation
Validation<T> Failure(IEnumerable<string> errors)       // Creates failed validation with multiple errors
Validation<T> Failure(params string[] errors)           // Creates failed validation with error array

Validation<IEnumerable<T>> Combine(IEnumerable<Validation<T>> validations) // Combines validations
```

#### Instance Methods
```csharp
Validation<TResult> Map<TResult>(Func<T, TResult> mapper)
Validation<TResult> FlatMap<TResult>(Func<T, Validation<TResult>> mapper)

Validation<TResult> Combine<TOther, TResult>(Validation<TOther> other, Func<T, TOther, TResult> combiner)

T OrElse(T defaultValue)
T OrElseGet(Func<IReadOnlyList<string>, T> supplier)