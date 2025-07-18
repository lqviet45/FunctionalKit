using FunctionalKit.Core;

namespace FunctionalKit.Extensions;

/// <summary>
/// Functional programming extensions for collections
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Safely gets the head (first element) of a collection
    /// </summary>
    public static Optional<T> Head<T>(this IEnumerable<T> source)
    {
        return source.FirstOrNone();
    }

    /// <summary>
    /// Gets the tail (all elements except the first) of a collection
    /// </summary>
    public static IEnumerable<T> Tail<T>(this IEnumerable<T> source)
    {
        return source.Skip(1);
    }

    /// <summary>
    /// Safely gets the last element of a collection
    /// </summary>
    public static Optional<T> LastOptional<T>(this IEnumerable<T> source)
    {
        return source.LastOrNone();
    }

    /// <summary>
    /// Takes elements while the predicate is true
    /// </summary>
    public static IEnumerable<T> TakeWhileInclusive<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (var item in source)
        {
            yield return item;
            if (!predicate(item))
                break;
        }
    }

    /// <summary>
    /// Partitions a collection into two based on a predicate
    /// </summary>
    public static (IEnumerable<T> matches, IEnumerable<T> nonMatches) Partition<T>(
        this IEnumerable<T> source, 
        Func<T, bool> predicate)
    {
        var matches = new List<T>();
        var nonMatches = new List<T>();

        foreach (var item in source)
        {
            if (predicate(item))
                matches.Add(item);
            else
                nonMatches.Add(item);
        }

        return (matches, nonMatches);
    }

    /// <summary>
    /// Groups consecutive elements that satisfy the same condition
    /// </summary>
    public static IEnumerable<IGrouping<TKey, T>> GroupConsecutive<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector)
    {
        var comparer = EqualityComparer<TKey>.Default;
        using var enumerator = source.GetEnumerator();
        
        if (!enumerator.MoveNext())
            yield break;

        var currentKey = keySelector(enumerator.Current);
        var currentGroup = new List<T> { enumerator.Current };

        while (enumerator.MoveNext())
        {
            var newKey = keySelector(enumerator.Current);
            
            if (comparer.Equals(currentKey, newKey))
            {
                currentGroup.Add(enumerator.Current);
            }
            else
            {
                yield return new ConsecutiveGrouping<TKey, T>(currentKey, currentGroup);
                currentKey = newKey;
                currentGroup = new List<T> { enumerator.Current };
            }
        }

        if (currentGroup.Any())
            yield return new ConsecutiveGrouping<TKey, T>(currentKey, currentGroup);
    }

    /// <summary>
    /// Flattens a collection of collections
    /// </summary>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source)
    {
        return source.SelectMany(x => x);
    }

    /// <summary>
    /// Applies a function to each element and flattens the results
    /// </summary>
    public static IEnumerable<TResult> FlatMap<T, TResult>(
        this IEnumerable<T> source,
        Func<T, IEnumerable<TResult>> mapper)
    {
        return source.SelectMany(mapper);
    }

    /// <summary>
    /// Scans (like Aggregate but returns intermediate results)
    /// </summary>
    public static IEnumerable<TAccumulate> Scan<T, TAccumulate>(
        this IEnumerable<T> source,
        TAccumulate seed,
        Func<TAccumulate, T, TAccumulate> accumulator)
    {
        var current = seed;
        yield return current;

        foreach (var item in source)
        {
            current = accumulator(current, item);
            yield return current;
        }
    }

    /// <summary>
    /// Intersperses a value between elements
    /// </summary>
    public static IEnumerable<T> Intersperse<T>(this IEnumerable<T> source, T separator)
    {
        using var enumerator = source.GetEnumerator();
        
        if (!enumerator.MoveNext())
            yield break;

        yield return enumerator.Current;

        while (enumerator.MoveNext())
        {
            yield return separator;
            yield return enumerator.Current;
        }
    }

    /// <summary>
    /// Chunks the collection into batches of specified size
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be positive", nameof(batchSize));

        var batch = new List<T>(batchSize);
        
        foreach (var item in source)
        {
            batch.Add(item);
            
            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }

        if (batch.Any())
            yield return batch;
    }

    /// <summary>
    /// Performs side effects for each element
    /// </summary>
    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
            yield return item;
        }
    }

    /// <summary>
    /// Performs side effects for each element with index
    /// </summary>
    public static IEnumerable<T> ForEachIndexed<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        var index = 0;
        foreach (var item in source)
        {
            action(item, index++);
            yield return item;
        }
    }

    /// <summary>
    /// Checks if all elements are distinct
    /// </summary>
    public static bool AllDistinct<T>(this IEnumerable<T> source)
    {
        var seen = new HashSet<T>();
        return source.All(seen.Add);
    }

    /// <summary>
    /// Checks if all elements are distinct by a key selector
    /// </summary>
    public static bool AllDistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        return source.All(item => seen.Add(keySelector(item)));
    }

    /// <summary>
    /// Returns None if the collection is empty, otherwise Some with the collection
    /// </summary>
    public static Optional<IEnumerable<T>> NonEmpty<T>(this IEnumerable<T> source)
    {
        var materialized = source.ToList();
        return materialized.Any() ? Optional<IEnumerable<T>>.Of(materialized) : Optional<IEnumerable<T>>.Empty();
    }

    /// <summary>
    /// Converts a collection of Results to a Result of collection
    /// </summary>
    public static Result<IEnumerable<T>> Sequence<T>(this IEnumerable<Result<T>> source)
    {
        return source.Combine();
    }

    /// <summary>
    /// Converts a collection of Optionals to an Optional of collection (all must have values)
    /// </summary>
    public static Optional<IEnumerable<T>> SequenceOptional<T>(this IEnumerable<Optional<T>> source)
    {
        return source.Sequence();
    }
}

/// <summary>
/// Helper class for consecutive grouping
/// </summary>
internal class ConsecutiveGrouping<TKey, TElement> : IGrouping<TKey, TElement>
{
    private readonly IEnumerable<TElement> _elements;

    public ConsecutiveGrouping(TKey key, IEnumerable<TElement> elements)
    {
        Key = key;
        _elements = elements;
    }

    public TKey Key { get; }
    
    public IEnumerator<TElement> GetEnumerator() => _elements.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}