namespace FunctionalKit.Core;

/// <summary>
/// Provides pipeline extension methods for fluent functional composition.
/// </summary>
public static class Pipe
{
    /// <summary>
    /// Pipes a value through a function, enabling fluent chaining.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Output type</typeparam>
    /// <param name="input">The input value</param>
    /// <param name="func">The function to apply</param>
    /// <returns>The result of applying the function to the input</returns>
    public static TResult PipeTo<T, TResult>(this T input, Func<T, TResult> func)
    {
        return func(input);
    }

    /// <summary>
    /// Pipes a value through an action, returning the original value for chaining.
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="input">The input value</param>
    /// <param name="action">The action to perform</param>
    /// <returns>The original input value</returns>
    public static T Tap<T>(this T input, Action<T> action)
    {
        action(input);
        return input;
    }

    /// <summary>
    /// Conditionally pipes a value through a function.
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="input">The input value</param>
    /// <param name="condition">The condition to check</param>
    /// <param name="func">The function to apply if condition is true</param>
    /// <returns>The transformed value if condition is true, otherwise the original value</returns>
    public static T PipeToIf<T>(this T input, bool condition, Func<T, T> func)
    {
        return condition ? func(input) : input;
    }

    /// <summary>
    /// Conditionally pipes a value through a function based on a predicate.
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="input">The input value</param>
    /// <param name="predicate">The predicate to evaluate</param>
    /// <param name="func">The function to apply if predicate returns true</param>
    /// <returns>The transformed value if predicate is true, otherwise the original value</returns>
    public static T PipeToIf<T>(this T input, Func<T, bool> predicate, Func<T, T> func)
    {
        return predicate(input) ? func(input) : input;
    }

    /// <summary>
    /// Pipes a value through one of two functions based on a condition.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Output type</typeparam>
    /// <param name="input">The input value</param>
    /// <param name="condition">The condition to check</param>
    /// <param name="trueFunc">Function to apply if condition is true</param>
    /// <param name="falseFunc">Function to apply if condition is false</param>
    /// <returns>The result of the appropriate function</returns>
    public static TResult PipeToEither<T, TResult>(this T input, bool condition,
        Func<T, TResult> trueFunc, Func<T, TResult> falseFunc)
    {
        return condition ? trueFunc(input) : falseFunc(input);
    }

    /// <summary>
    /// Pipes a value through one of two functions based on a predicate.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Output type</typeparam>
    /// <param name="input">The input value</param>
    /// <param name="predicate">The predicate to evaluate</param>
    /// <param name="trueFunc">Function to apply if predicate returns true</param>
    /// <param name="falseFunc">Function to apply if predicate returns false</param>
    /// <returns>The result of the appropriate function</returns>
    public static TResult PipeToEither<T, TResult>(this T input, Func<T, bool> predicate,
        Func<T, TResult> trueFunc, Func<T, TResult> falseFunc)
    {
        return predicate(input) ? trueFunc(input) : falseFunc(input);
    }

    /// <summary>
    /// Applies multiple functions in sequence to a value.
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="input">The input value</param>
    /// <param name="functions">The functions to apply in sequence</param>
    /// <returns>The result after applying all functions</returns>
    public static T PipeToMany<T>(this T input, params Func<T, T>[] functions)
    {
        var result = input;
        foreach (var func in functions)
        {
            result = func(result);
        }

        return result;
    }

    /// <summary>
    /// Asynchronously pipes a value through a function.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Output type</typeparam>
    /// <param name="input">The input value</param>
    /// <param name="func">The async function to apply</param>
    /// <returns>A task representing the result</returns>
    public static async Task<TResult> PipeToAsync<T, TResult>(this T input, Func<T, Task<TResult>> func)
    {
        return await func(input);
    }

    /// <summary>
    /// Asynchronously pipes a Task value through a function.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Output type</typeparam>
    /// <param name="input">The input task</param>
    /// <param name="func">The function to apply to the task result</param>
    /// <returns>A task representing the result</returns>
    public static async Task<TResult> PipeToAsync<T, TResult>(this Task<T> input, Func<T, TResult> func)
    {
        var value = await input;
        return func(value);
    }

    /// <summary>
    /// Asynchronously pipes a Task value through an async function.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Output type</typeparam>
    /// <param name="input">The input task</param>
    /// <param name="func">The async function to apply to the task result</param>
    /// <returns>A task representing the result</returns>
    public static async Task<TResult> PipeToAsync<T, TResult>(this Task<T> input, Func<T, Task<TResult>> func)
    {
        var value = await input;
        return await func(value);
    }

    /// <summary>
    /// Asynchronously taps a Task value through an action.
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="input">The input task</param>
    /// <param name="action">The action to perform</param>
    /// <returns>The original task value</returns>
    public static async Task<T> TapAsync<T>(this Task<T> input, Action<T> action)
    {
        var value = await input;
        action(value);
        return value;
    }

    /// <summary>
    /// Asynchronously taps a Task value through an async action.
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="input">The input task</param>
    /// <param name="action">The async action to perform</param>
    /// <returns>The original task value</returns>
    public static async Task<T> TapAsync<T>(this Task<T> input, Func<T, Task> action)
    {
        var value = await input;
        await action(value);
        return value;
    }

    /// <summary>
    /// Creates a function composition pipeline.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Output type</typeparam>
    /// <param name="func">The first function in the pipeline</param>
    /// <returns>A pipeline builder for composing functions</returns>
    public static PipelineBuilder<T, TResult> CreatePipeline<T, TResult>(Func<T, TResult> func)
    {
        return new PipelineBuilder<T, TResult>(func);
    }
}

/// <summary>
/// Builder for creating function composition pipelines.
/// </summary>
/// <typeparam name="TInput">The input type of the pipeline</typeparam>
/// <typeparam name="TOutput">The current output type of the pipeline</typeparam>
public readonly struct PipelineBuilder<TInput, TOutput>
{
    private readonly Func<TInput, TOutput> _pipeline;

    internal PipelineBuilder(Func<TInput, TOutput> pipeline)
    {
        _pipeline = pipeline;
    }

    /// <summary>
    /// Adds another function to the pipeline.
    /// </summary>
    /// <typeparam name="TNext">The output type of the next function</typeparam>
    /// <param name="func">The function to add to the pipeline</param>
    /// <returns>A new pipeline builder with the added function</returns>
    public PipelineBuilder<TInput, TNext> Then<TNext>(Func<TOutput, TNext> func)
    {
        var currentPipeline = _pipeline;
        return new PipelineBuilder<TInput, TNext>(input => func(currentPipeline(input)));
    }

    /// <summary>
    /// Adds a conditional function to the pipeline.
    /// </summary>
    /// <param name="predicate">The condition to check</param>
    /// <param name="func">The function to apply if condition is true</param>
    /// <returns>A new pipeline builder with the conditional function</returns>
    public PipelineBuilder<TInput, TOutput> ThenIf(Func<TOutput, bool> predicate, Func<TOutput, TOutput> func)
    {
        var currentPipeline = _pipeline;
        return new PipelineBuilder<TInput, TOutput>(input =>
        {
            var result = currentPipeline(input);
            return predicate(result) ? func(result) : result;
        });
    }

    /// <summary>
    /// Adds a tap operation to the pipeline (side effect without changing the value).
    /// </summary>
    /// <param name="action">The action to perform</param>
    /// <returns>A new pipeline builder with the tap operation</returns>
    public PipelineBuilder<TInput, TOutput> Tap(Action<TOutput> action)
    {
        var currentPipeline = _pipeline;
        return new PipelineBuilder<TInput, TOutput>(input =>
        {
            var result = currentPipeline(input);
            action(result);
            return result;
        });
    }

    /// <summary>
    /// Builds and returns the final pipeline function.
    /// </summary>
    /// <returns>The composed function pipeline</returns>
    public Func<TInput, TOutput> Build()
    {
        return _pipeline;
    }

    /// <summary>
    /// Executes the pipeline with the given input.
    /// </summary>
    /// <param name="input">The input value</param>
    /// <returns>The result of the pipeline execution</returns>
    public TOutput Execute(TInput input)
    {
        return _pipeline(input);
    }

    /// <summary>
    /// Implicit conversion to the underlying function.
    /// </summary>
    /// <param name="builder">The pipeline builder</param>
    public static implicit operator Func<TInput, TOutput>(PipelineBuilder<TInput, TOutput> builder)
    {
        return builder._pipeline;
    }
}