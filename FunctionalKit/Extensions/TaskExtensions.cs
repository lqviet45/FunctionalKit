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
        var optional = await task;
        return optional.Map(mapper);
    }

    /// <summary>
    /// FlatMaps over a Task&lt;Optional&lt;T&gt;&gt;.
    /// </summary>
    public static async Task<Optional<TResult>> FlatMapAsync<T, TResult>(
        this Task<Optional<T>> task,
        Func<T, Task<Optional<TResult>>> mapper)
    {
        var optional = await task;
        if (optional.IsEmpty)
            return Optional<TResult>.Empty();
        return await mapper(optional.Value);
    }

    /// <summary>
    /// Maps over a Task&lt;Result&lt;T&gt;&gt;.
    /// </summary>
    public static async Task<Result<TResult>> MapAsync<T, TResult>(
        this Task<Result<T>> task,
        Func<T, TResult> mapper)
    {
        var result = await task;
        return result.Map(mapper);
    }

    /// <summary>
    /// FlatMaps over a Task&lt;Result&lt;T&gt;&gt;.
    /// </summary>
    public static async Task<Result<TResult>> FlatMapAsync<T, TResult>(
        this Task<Result<T>> task,
        Func<T, Task<Result<TResult>>> mapper)
    {
        var result = await task;
        if (result.IsFailure)
            return Result<TResult>.Failure(result.Error);
        return await mapper(result.Value);
    }

    /// <summary>
    /// Converts a Task to a Result, catching exceptions.
    /// </summary>
    public static async Task<Result<T>> ToResult<T>(this Task<T> task)
    {
        try
        {
            var result = await task;
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex.Message);
        }
    }
}