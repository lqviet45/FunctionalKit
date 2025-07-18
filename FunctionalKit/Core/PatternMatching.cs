namespace FunctionalKit.Core;

/// <summary>
/// Provides pattern matching utilities for functional programming.
/// </summary>
public static class PatternMatching
{
    /// <summary>
    /// Pattern matching with multiple cases.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="value">Value to match against</param>
    /// <param name="cases">Array of (predicate, action) tuples</param>
    /// <returns>Result of the first matching case</returns>
    /// <exception cref="InvalidOperationException">Thrown when no case matches</exception>
    public static TResult Switch<T, TResult>(T value, params (Func<T, bool> predicate, Func<T, TResult> action)[] cases)
    {
        foreach (var (predicate, action) in cases)
        {
            if (predicate(value)) 
                return action(value);
        }
        
        throw new InvalidOperationException($"No matching case found for value: {value}");
    }

    /// <summary>
    /// Pattern matching with multiple cases and a default case.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="value">Value to match against</param>
    /// <param name="defaultCase">Default action if no case matches</param>
    /// <param name="cases">Array of (predicate, action) tuples</param>
    /// <returns>Result of the first matching case or default</returns>
    public static TResult Switch<T, TResult>(T value, Func<T, TResult> defaultCase, params (Func<T, bool> predicate, Func<T, TResult> action)[] cases)
    {
        foreach (var (predicate, action) in cases)
        {
            if (predicate(value)) 
                return action(value);
        }
        
        return defaultCase(value);
    }

    /// <summary>
    /// Creates a case for pattern matching based on value equality.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="matchValue">Value to match</param>
    /// <param name="result">Result if matched</param>
    /// <returns>Case tuple</returns>
    public static (Func<T, bool> predicate, Func<T, TResult> action) Case<T, TResult>(T matchValue, TResult result)
        where T : IEquatable<T>
    {
        return (value => value.Equals(matchValue), _ => result);
    }

    /// <summary>
    /// Creates a case for pattern matching based on value equality with function result.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="matchValue">Value to match</param>
    /// <param name="resultFunc">Function to execute if matched</param>
    /// <returns>Case tuple</returns>
    public static (Func<T, bool> predicate, Func<T, TResult> action) Case<T, TResult>(T matchValue, Func<T, TResult> resultFunc)
        where T : IEquatable<T>
    {
        return (value => value.Equals(matchValue), resultFunc);
    }

    /// <summary>
    /// Creates a case for pattern matching based on type.
    /// </summary>
    /// <typeparam name="T">Base type</typeparam>
    /// <typeparam name="TSpecific">Specific type to match</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="resultFunc">Function to execute if type matches</param>
    /// <returns>Case tuple</returns>
    public static (Func<T, bool> predicate, Func<T, TResult> action) Case<T, TSpecific, TResult>(Func<TSpecific, TResult> resultFunc)
        where TSpecific : class, T
    {
        return (value => value is TSpecific, value => resultFunc((TSpecific)value));
    }

    /// <summary>
    /// Creates a case for pattern matching based on predicate.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="predicate">Predicate to test</param>
    /// <param name="result">Result if predicate matches</param>
    /// <returns>Case tuple</returns>
    public static (Func<T, bool> predicate, Func<T, TResult> action) Case<T, TResult>(Func<T, bool> predicate, TResult result)
    {
        return (predicate, _ => result);
    }

    /// <summary>
    /// Creates a case for pattern matching based on predicate with function result.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="predicate">Predicate to test</param>
    /// <param name="resultFunc">Function to execute if predicate matches</param>
    /// <returns>Case tuple</returns>
    public static (Func<T, bool> predicate, Func<T, TResult> action) Case<T, TResult>(Func<T, bool> predicate, Func<T, TResult> resultFunc)
    {
        return (predicate, resultFunc);
    }

    /// <summary>
    /// Creates a wildcard case that matches anything.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="result">Result for wildcard</param>
    /// <returns>Case tuple</returns>
    public static (Func<T, bool> predicate, Func<T, TResult> action) Wildcard<T, TResult>(TResult result)
    {
        return (_ => true, _ => result);
    }

    /// <summary>
    /// Creates a wildcard case that matches anything with function result.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="resultFunc">Function to execute for wildcard</param>
    /// <returns>Case tuple</returns>
    public static (Func<T, bool> predicate, Func<T, TResult> action) Wildcard<T, TResult>(Func<T, TResult> resultFunc)
    {
        return (_ => true, resultFunc);
    }
}

/// <summary>
/// Extension methods for pattern matching.
/// </summary>
public static class PatternMatchingExtensions
{
    /// <summary>
    /// Pattern matching extension method.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="value">Value to match against</param>
    /// <param name="cases">Array of (predicate, action) tuples</param>
    /// <returns>Result of the first matching case</returns>
    public static TResult Switch<T, TResult>(this T value, params (Func<T, bool> predicate, Func<T, TResult> action)[] cases)
    {
        return PatternMatching.Switch(value, cases);
    }

    /// <summary>
    /// Pattern matching extension method with default case.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="value">Value to match against</param>
    /// <param name="defaultCase">Default action if no case matches</param>
    /// <param name="cases">Array of (predicate, action) tuples</param>
    /// <returns>Result of the first matching case or default</returns>
    public static TResult Switch<T, TResult>(this T value, Func<T, TResult> defaultCase, params (Func<T, bool> predicate, Func<T, TResult> action)[] cases)
    {
        return PatternMatching.Switch(value, defaultCase, cases);
    }
}