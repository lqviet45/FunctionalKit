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
    /// Returns the first successful result, or the last failure if all fail.
    /// </summary>
    public static Result<T> FirstSuccess<T>(this IEnumerable<Result<T>> results)
    {
        var resultList = results.ToList();
        
        foreach (var result in resultList)
        {
            if (result.IsSuccess) return result;
        }
        
        // If no success found, return the last failure or a default failure
        return resultList.Any() ? resultList.Last() : Result<T>.Failure("No results provided");
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
    /// Applies a function to three Results if all are successful.
    /// </summary>
    public static Result<TResult> Zip<T1, T2, T3, TResult>(
        this Result<T1> result1,
        Result<T2> result2,
        Result<T3> result3,
        Func<T1, T2, T3, TResult> zipper)
    {
        if (result1.IsFailure) return Result<TResult>.Failure(result1.Error);
        if (result2.IsFailure) return Result<TResult>.Failure(result2.Error);
        if (result3.IsFailure) return Result<TResult>.Failure(result3.Error);

        return Result<TResult>.Success(zipper(result1.Value, result2.Value, result3.Value));
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

    /// <summary>
    /// Filters the success value with a predicate, converting to failure if predicate fails.
    /// </summary>
    public static Result<T> Filter<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
    {
        if (result.IsFailure) return result;
        
        return predicate(result.Value) 
            ? result 
            : Result<T>.Failure(errorMessage);
    }

    /// <summary>
    /// Filters the success value with a predicate, using a function to generate error message.
    /// </summary>
    public static Result<T> Filter<T>(this Result<T> result, Func<T, bool> predicate, Func<T, string> errorMessageFactory)
    {
        if (result.IsFailure) return result;
        
        return predicate(result.Value) 
            ? result 
            : Result<T>.Failure(errorMessageFactory(result.Value));
    }

    /// <summary>
    /// Recovers from failure using a function that takes the error message.
    /// </summary>
    public static Result<T> Recover<T>(this Result<T> result, Func<string, Result<T>> recovery)
    {
        return result.IsSuccess ? result : recovery(result.Error);
    }

    /// <summary>
    /// Recovers from failure with a simple value.
    /// </summary>
    public static Result<T> Recover<T>(this Result<T> result, T recoveryValue)
    {
        return result.IsSuccess ? result : Result<T>.Success(recoveryValue);
    }

    /// <summary>
    /// Converts a Result to Either.
    /// </summary>
    public static Either<string, T> ToEither<T>(this Result<T> result)
    {
        return result.IsSuccess 
            ? Either<string, T>.FromRight(result.Value)
            : Either<string, T>.FromLeft(result.Error);
    }

    /// <summary>
    /// Combines results using a binary operation, accumulating errors.
    /// </summary>
    public static Result<T> Reduce<T>(this IEnumerable<Result<T>> results, Func<T, T, T> combiner)
    {
        var resultList = results.ToList();
        if (!resultList.Any())
            return Result<T>.Failure("No results to reduce");

        var failures = resultList.Where(r => r.IsFailure).ToList();
        if (failures.Any())
        {
            var errors = string.Join("; ", failures.Select(f => f.Error));
            return Result<T>.Failure(errors);
        }

        var values = resultList.Select(r => r.Value);
        var reduced = values.Aggregate(combiner);
        return Result<T>.Success(reduced);
    }

    /// <summary>
    /// Partitions a collection of results into successes and failures.
    /// </summary>
    public static (IEnumerable<T> successes, IEnumerable<string> failures) Partition<T>(this IEnumerable<Result<T>> results)
    {
        var successes = new List<T>();
        var failures = new List<string>();

        foreach (var result in results)
        {
            if (result.IsSuccess)
                successes.Add(result.Value);
            else
                failures.Add(result.Error);
        }

        return (successes, failures);
    }

    /// <summary>
    /// Executes a side effect only if the result is successful, returns the original result.
    /// </summary>
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value);
        return result;
    }

    /// <summary>
    /// Executes a side effect only if the result is failure, returns the original result.
    /// </summary>
    public static Result<T> OnFailure<T>(this Result<T> result, Action<string> action)
    {
        if (result.IsFailure)
            action(result.Error);
        return result;
    }
}