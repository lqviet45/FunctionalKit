using FunctionalKit.Core;

namespace FunctionalKit.Extensions;

public static class TaskExtensions
{
    /// <summary>
    /// Maps over a Task&lt;Optional&lt;T&gt;&gt;.
    /// </summary>
    public static async Task<Optional<TResult>> MapAsync<T, TResult>(
        this Task<Optional<T>> task,
        Func<T, TResult> mapper)
    {
        var optional = await task.ConfigureAwait(false);
        return optional.Map(mapper);
    }

    /// <summary>
    /// FlatMaps over a Task&lt;Optional&lt;T&gt;&gt;.
    /// </summary>
    public static async Task<Optional<TResult>> FlatMapAsync<T, TResult>(
        this Task<Optional<T>> task,
        Func<T, Task<Optional<TResult>>> mapper)
    {
        var optional = await task.ConfigureAwait(false);
        if (optional.IsEmpty)
            return Optional<TResult>.Empty();
        return await mapper(optional.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps over a Task&lt;Result&lt;T&gt;&gt;.
    /// </summary>
    public static async Task<Result<TResult>> MapAsync<T, TResult>(
        this Task<Result<T>> task,
        Func<T, TResult> mapper)
    {
        var result = await task.ConfigureAwait(false);
        return result.Map(mapper);
    }

    /// <summary>
    /// FlatMaps over a Task&lt;Result&lt;T&gt;&gt;.
    /// </summary>
    public static async Task<Result<TResult>> FlatMapAsync<T, TResult>(
        this Task<Result<T>> task,
        Func<T, Task<Result<TResult>>> mapper)
    {
        var result = await task.ConfigureAwait(false);
        if (result.IsFailure)
            return Result<TResult>.Failure(result.Error);
        return await mapper(result.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps over a Task&lt;Validation&lt;T&gt;&gt;.
    /// </summary>
    public static async Task<Validation<TResult>> MapAsync<T, TResult>(
        this Task<Validation<T>> task,
        Func<T, TResult> mapper)
    {
        var validation = await task.ConfigureAwait(false);
        return validation.Map(mapper);
    }

    /// <summary>
    /// FlatMaps over a Task&lt;Validation&lt;T&gt;&gt;.
    /// </summary>
    public static async Task<Validation<TResult>> FlatMapAsync<T, TResult>(
        this Task<Validation<T>> task,
        Func<T, Task<Validation<TResult>>> mapper)
    {
        var validation = await task.ConfigureAwait(false);
        if (validation.IsInvalid)
            return Validation<TResult>.Failure(validation.Errors);
        return await mapper(validation.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts a Task to a Result, catching exceptions.
    /// </summary>
    public static async Task<Result<T>> ToResult<T>(this Task<T> task)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Converts a Task to a Result with custom error type, catching exceptions.
    /// </summary>
    public static async Task<Result<T, Exception>> ToResultWithException<T>(this Task<T> task)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            return Result<T, Exception>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T, Exception>.Failure(ex);
        }
    }

    /// <summary>
    /// Converts a Task to an Optional, returning empty on exception.
    /// </summary>
    public static async Task<Optional<T>> ToOptional<T>(this Task<T> task)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            return Optional<T>.OfNullable(result);
        }
        catch
        {
            return Optional<T>.Empty();
        }
    }

    /// <summary>
    /// Applies a timeout to a task and returns a Result.
    /// </summary>
    public static async Task<Result<T>> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
    {
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            var result = await task.WaitAsync(cts.Token).ConfigureAwait(false);
            return Result<T>.Success(result);
        }
        catch (TimeoutException)
        {
            return Result<T>.Failure($"Operation timed out after {timeout.TotalSeconds} seconds");
        }
        catch (OperationCanceledException)
        {
            return Result<T>.Failure($"Operation timed out after {timeout.TotalSeconds} seconds");
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Combines multiple tasks into a single result, collecting all errors.
    /// </summary>
    public static async Task<Result<IEnumerable<T>>> CombineResults<T>(this IEnumerable<Task<Result<T>>> tasks)
    {
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        var failures = results.Where(r => r.IsFailure).ToList();
        
        if (failures.Any())
        {
            var errors = string.Join("; ", failures.Select(f => f.Error));
            return Result<IEnumerable<T>>.Failure(errors);
        }

        var values = results.Select(r => r.Value);
        return Result<IEnumerable<T>>.Success(values);
    }

    /// <summary>
    /// Executes tasks sequentially and stops on first failure.
    /// </summary>
    public static async Task<Result<IEnumerable<T>>> SequentialResults<T>(this IEnumerable<Func<Task<Result<T>>>> taskFactories)
    {
        var results = new List<T>();
        
        foreach (var taskFactory in taskFactories)
        {
            var result = await taskFactory().ConfigureAwait(false);
            if (result.IsFailure)
                return Result<IEnumerable<T>>.Failure(result.Error);
            
            results.Add(result.Value);
        }

        return Result<IEnumerable<T>>.Success(results);
    }
}