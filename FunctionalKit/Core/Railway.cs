namespace FunctionalKit.Core;

/// <summary>
/// Railway-oriented programming utilities for chaining operations that can fail.
/// </summary>
public static class Railway
{
    /// <summary>
    /// Creates a railway function that can be composed with other railway functions.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Output type</typeparam>
    /// <param name="func">Function to wrap</param>
    /// <returns>Railway function</returns>
    public static Func<T, Result<TResult>> Bind<T, TResult>(this Func<T, Result<TResult>> func)
    {
        return input => func(input);
    }

    /// <summary>
    /// Chains a Result with another function, continuing only if successful.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Output type</typeparam>
    /// <param name="result">Input result</param>
    /// <param name="next">Next function in the chain</param>
    /// <returns>Chained result</returns>
    public static Result<TResult> Then<T, TResult>(this Result<T> result, Func<T, Result<TResult>> next)
    {
        return result.FlatMap(next);
    }

    /// <summary>
    /// Chains a Result with another function that doesn't return a Result.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Output type</typeparam>
    /// <param name="result">Input result</param>
    /// <param name="next">Next function in the chain</param>
    /// <returns>Chained result</returns>
    public static Result<TResult> Then<T, TResult>(this Result<T> result, Func<T, TResult> next)
    {
        return result.Map(next);
    }

    /// <summary>
    /// Continues the railway only if the predicate is satisfied.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <param name="result">Input result</param>
    /// <param name="predicate">Predicate to test</param>
    /// <param name="errorMessage">Error message if predicate fails</param>
    /// <returns>Filtered result</returns>
    public static Result<T> ThenIf<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
    {
        return result.FlatMap(value => 
            predicate(value) 
                ? Result<T>.Success(value) 
                : Result<T>.Failure(errorMessage));
    }

    /// <summary>
    /// Continues the railway only if the predicate is satisfied, using error factory.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <param name="result">Input result</param>
    /// <param name="predicate">Predicate to test</param>
    /// <param name="errorFactory">Error factory if predicate fails</param>
    /// <returns>Filtered result</returns>
    public static Result<T> ThenIf<T>(this Result<T> result, Func<T, bool> predicate, Func<T, string> errorFactory)
    {
        return result.FlatMap(value => 
            predicate(value) 
                ? Result<T>.Success(value) 
                : Result<T>.Failure(errorFactory(value)));
    }

    /// <summary>
    /// Executes a side effect on the railway without changing the result.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <param name="result">Input result</param>
    /// <param name="action">Side effect to execute</param>
    /// <returns>Original result</returns>
    public static Result<T> ThenDo<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }
        return result;
    }

    /// <summary>
    /// Switches to an alternative railway if the current one fails.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <param name="result">Input result</param>
    /// <param name="alternative">Alternative function to try</param>
    /// <returns>Result or alternative</returns>
    public static Result<T> OrElse<T>(this Result<T> result, Func<string, Result<T>> alternative)
    {
        return result.IsSuccess ? result : alternative(result.Error);
    }

    /// <summary>
    /// Switches to an alternative railway if the current one fails.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <param name="result">Input result</param>
    /// <param name="alternative">Alternative result</param>
    /// <returns>Result or alternative</returns>
    public static Result<T> OrElse<T>(this Result<T> result, Result<T> alternative)
    {
        return result.IsSuccess ? result : alternative;
    }

    /// <summary>
    /// Combines multiple railway operations in parallel.
    /// </summary>
    /// <typeparam name="T">Type of values</typeparam>
    /// <param name="results">Collection of results</param>
    /// <returns>Combined result</returns>
    public static Result<IEnumerable<T>> All<T>(params Result<T>[] results)
    {
        return All((IEnumerable<Result<T>>)results);
    }

    /// <summary>
    /// Combines multiple railway operations in parallel.
    /// </summary>
    /// <typeparam name="T">Type of values</typeparam>
    /// <param name="results">Collection of results</param>
    /// <returns>Combined result</returns>
    public static Result<IEnumerable<T>> All<T>(IEnumerable<Result<T>> results)
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
    /// Returns the first successful result from a collection.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <param name="results">Collection of results</param>
    /// <returns>First successful result or last failure</returns>
    public static Result<T> FirstSuccess<T>(params Result<T>[] results)
    {
        return FirstSuccess((IEnumerable<Result<T>>)results);
    }

    /// <summary>
    /// Returns the first successful result from a collection.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <param name="results">Collection of results</param>
    /// <returns>First successful result or last failure</returns>
    public static Result<T> FirstSuccess<T>(IEnumerable<Result<T>> results)
    {
        var resultList = results.ToList();
        
        foreach (var result in resultList)
        {
            if (result.IsSuccess) return result;
        }
        
        return resultList.Any() ? resultList.Last() : Result<T>.Failure("No results provided");
    }

    /// <summary>
    /// Executes functions in sequence, stopping at first failure.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <param name="input">Initial input</param>
    /// <param name="functions">Functions to execute in sequence</param>
    /// <returns>Result of the sequence</returns>
    public static Result<T> Sequence<T>(T input, params Func<T, Result<T>>[] functions)
    {
        var result = Result<T>.Success(input);
        
        foreach (var func in functions)
        {
            result = result.Then(func);
            if (result.IsFailure) break;
        }
        
        return result;
    }

    /// <summary>
    /// Creates a railway pipeline builder.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <param name="input">Initial input</param>
    /// <returns>Railway pipeline builder</returns>
    public static RailwayPipelineBuilder<T> StartWith<T>(T input)
    {
        return new RailwayPipelineBuilder<T>(Result<T>.Success(input));
    }

    /// <summary>
    /// Creates a railway pipeline builder from a Result.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <param name="result">Initial result</param>
    /// <returns>Railway pipeline builder</returns>
    public static RailwayPipelineBuilder<T> StartWith<T>(Result<T> result)
    {
        return new RailwayPipelineBuilder<T>(result);
    }
}

/// <summary>
/// Builder for creating railway programming pipelines.
/// </summary>
/// <typeparam name="T">Current type in the pipeline</typeparam>
public readonly struct RailwayPipelineBuilder<T>
{
    private readonly Result<T> _result;

    internal RailwayPipelineBuilder(Result<T> result)
    {
        _result = result;
    }

    /// <summary>
    /// Adds a function to the railway pipeline.
    /// </summary>
    /// <typeparam name="TNext">Next type in the pipeline</typeparam>
    /// <param name="func">Function to add</param>
    /// <returns>New pipeline builder</returns>
    public RailwayPipelineBuilder<TNext> Then<TNext>(Func<T, Result<TNext>> func)
    {
        return new RailwayPipelineBuilder<TNext>(_result.Then(func));
    }

    /// <summary>
    /// Adds a function to the railway pipeline.
    /// </summary>
    /// <typeparam name="TNext">Next type in the pipeline</typeparam>
    /// <param name="func">Function to add</param>
    /// <returns>New pipeline builder</returns>
    public RailwayPipelineBuilder<TNext> Then<TNext>(Func<T, TNext> func)
    {
        return new RailwayPipelineBuilder<TNext>(_result.Then(func));
    }

    /// <summary>
    /// Adds a conditional step to the pipeline.
    /// </summary>
    /// <param name="predicate">Condition to check</param>
    /// <param name="errorMessage">Error if condition fails</param>
    /// <returns>Same pipeline builder</returns>
    public RailwayPipelineBuilder<T> ThenIf(Func<T, bool> predicate, string errorMessage)
    {
        return new RailwayPipelineBuilder<T>(_result.ThenIf(predicate, errorMessage));
    }

    /// <summary>
    /// Adds a side effect to the pipeline.
    /// </summary>
    /// <param name="action">Side effect to execute</param>
    /// <returns>Same pipeline builder</returns>
    public RailwayPipelineBuilder<T> ThenDo(Action<T> action)
    {
        return new RailwayPipelineBuilder<T>(_result.ThenDo(action));
    }

    /// <summary>
    /// Gets the final result of the pipeline.
    /// </summary>
    /// <returns>Pipeline result</returns>
    public Result<T> Build()
    {
        return _result;
    }

    /// <summary>
    /// Implicit conversion to Result.
    /// </summary>
    /// <param name="builder">Pipeline builder</param>
    public static implicit operator Result<T>(RailwayPipelineBuilder<T> builder)
    {
        return builder._result;
    }
}