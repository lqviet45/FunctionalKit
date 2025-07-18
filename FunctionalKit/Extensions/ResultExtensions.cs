namespace FunctionalKit.Extensions;

using Core;

public static class ResultExtensions
{
    /// <summary>
    /// Combines multiple Results into a single Result containing a list.
    /// </summary>
    public static Result<IEnumerable<T>> Combine<T>(this IEnumerable<Result<T>> results)
    {
        var resultList = results.ToList();
        var failures = resultList.Where(r => r.IsFailure).ToList();

        if (failures.Any())
        {
            var errors = string.Join("; ", failures.Select(f => f.Error));
            return Result<IEnumerable<T>>.Failure(errors);
        }

        var values = resultList.Select(r => r.Value);
        return Result<IEnumerable<T>>.Success(values);
    }

    /// <summary>
    /// Converts Result to Optional, discarding error information.
    /// </summary>
    public static Optional<T> ToOptional<T>(this Result<T> result)
    {
        return result.IsSuccess ? Optional<T>.Of(result.Value) : Optional<T>.Empty();
    }

    /// <summary>
    /// Applies a function only if all Results are successful.
    /// </summary>
    public static Result<TResult> Zip<T1, T2, TResult>(
        this Result<T1> result1,
        Result<T2> result2,
        Func<T1, T2, TResult> zipper)
    {
        if (result1.IsFailure) return Result<TResult>.Failure(result1.Error);
        if (result2.IsFailure) return Result<TResult>.Failure(result2.Error);

        return Result<TResult>.Success(zipper(result1.Value, result2.Value));
    }

    /// <summary>
    /// Taps into the Result for side effects without changing the Result.
    /// </summary>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value);
        return result;
    }

    /// <summary>
    /// Taps into the error for side effects without changing the Result.
    /// </summary>
    public static Result<T> TapError<T>(this Result<T> result, Action<string> action)
    {
        if (result.IsFailure)
            action(result.Error);
        return result;
    }
}