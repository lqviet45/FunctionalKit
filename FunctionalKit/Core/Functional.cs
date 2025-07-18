namespace FunctionalKit.Core;

/// <summary>
/// Static helper methods for creating Optional and Result instances.
/// </summary>
public static class Functional
{
    /// <summary>
    /// Creates an Optional with the specified value.
    /// </summary>
    public static Optional<T> Some<T>(T value) => Optional<T>.Of(value);

    /// <summary>
    /// Creates an empty Optional.
    /// </summary>
    public static Optional<T> None<T>() => Optional<T>.Empty();

    /// <summary>
    /// Creates an Optional from a nullable value.
    /// </summary>
    public static Optional<T> Maybe<T>(T? value) => Optional<T>.OfNullable(value);

    /// <summary>
    /// Creates a successful Result.
    /// </summary>
    public static Result<T> Ok<T>(T value) => Result<T>.Success(value);

    /// <summary>
    /// Creates a failed Result with string error.
    /// </summary>
    public static Result<T> Err<T>(string error) => Result<T>.Failure(error);

    /// <summary>
    /// Creates a successful Result with custom error type.
    /// </summary>
    public static Result<T, TError> Ok<T, TError>(T value) => Result<T, TError>.Success(value);

    /// <summary>
    /// Creates a failed Result with custom error type.
    /// </summary>
    public static Result<T, TError> Err<T, TError>(TError error) => Result<T, TError>.Failure(error);

    /// <summary>
    /// Safely executes a function and returns a Result.
    /// </summary>
    public static Result<T> Try<T>(Func<T> func)
    {
        try
        {
            return Ok(func());
        }
        catch (Exception ex)
        {
            return Err<T>(ex.Message);
        }
    }

    /// <summary>
    /// Safely executes a function and returns a Result with custom error type.
    /// </summary>
    public static Result<T, Exception> TryWithException<T>(Func<T> func)
    {
        try
        {
            return Ok<T, Exception>(func());
        }
        catch (Exception ex)
        {
            return Err<T, Exception>(ex);
        }
    }

    /// <summary>
    /// Safely executes an async function and returns a Result.
    /// </summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> func)
    {
        try
        {
            var result = await func();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Err<T>(ex.Message);
        }
    }

    /// <summary>
    /// Safely executes an async function and returns a Result with custom error type.
    /// </summary>
    public static async Task<Result<T, Exception>> TryWithExceptionAsync<T>(Func<Task<T>> func)
    {
        try
        {
            var result = await func();
            return Ok<T, Exception>(result);
        }
        catch (Exception ex)
        {
            return Err<T, Exception>(ex);
        }
    }
}