using Enterprise.Shared.Validation.Models;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Shared.Validation.Extensions;

/// <summary>
/// Collection extension methods for validation and utility operations
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Checks if collection is null or empty
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection == null || !collection.Any();
    }

    /// <summary>
    /// Checks if collection has any items
    /// </summary>
    public static bool HasAny<T>(this IEnumerable<T>? collection)
    {
        return collection != null && collection.Any();
    }

    /// <summary>
    /// Converts enumerable to paged result
    /// </summary>
    public static PagedResult<T> ToPagedResult<T>(this IEnumerable<T> source, int page, int pageSize)
    {
        var totalCount = source.Count();
        var data = source.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        
        return PagedResult<T>.Create(data, totalCount, page, pageSize);
    }

    /// <summary>
    /// Converts queryable to paged result asynchronously
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> source, int page, int pageSize)
    {
        var totalCount = await source.CountAsync();
        var data = await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        return PagedResult<T>.Create(data, totalCount, page, pageSize);
    }

    /// <summary>
    /// Executes action for each item in collection
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }

    /// <summary>
    /// Executes async action for each item in collection
    /// </summary>
    public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
    {
        foreach (var item in source)
        {
            await action(item);
        }
    }

    /// <summary>
    /// Returns distinct items by key selector (for .NET versions without DistinctBy)
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seenKeys = new HashSet<TKey>();
        foreach (var element in source)
        {
            if (seenKeys.Add(keySelector(element)))
                yield return element;
        }
    }

    /// <summary>
    /// Safely converts to dictionary without duplicate key exceptions
    /// </summary>
    public static Dictionary<TKey, TValue> ToDictionarySafe<TSource, TKey, TValue>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector) where TKey : notnull
    {
        var dictionary = new Dictionary<TKey, TValue>();
        foreach (var item in source)
        {
            var key = keySelector(item);
            if (!dictionary.ContainsKey(key))
            {
                dictionary[key] = valueSelector(item);
            }
        }
        return dictionary;
    }

    /// <summary>
    /// Chunks collection into smaller collections of specified size
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int size)
    {
        if (size <= 0) throw new ArgumentException("Chunk size must be greater than zero.", nameof(size));

        using var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return GetChunk(enumerator, size);
        }
    }

    private static IEnumerable<T> GetChunk<T>(IEnumerator<T> enumerator, int size)
    {
        do
        {
            yield return enumerator.Current;
        } while (--size > 0 && enumerator.MoveNext());
    }

    /// <summary>
    /// Batches async operations with specified batch size
    /// </summary>
    public static async Task<List<TResult>> BatchAsync<T, TResult>(
        this IEnumerable<T> source,
        Func<T, Task<TResult>> selector,
        int batchSize = 10)
    {
        var results = new List<TResult>();
        var batches = source.Chunk(batchSize);

        foreach (var batch in batches)
        {
            var tasks = batch.Select(selector);
            var batchResults = await Task.WhenAll(tasks);
            results.AddRange(batchResults);
        }

        return results;
    }

    /// <summary>
    /// Finds items that are in first collection but not in second
    /// </summary>
    public static IEnumerable<T> Except<T, TKey>(
        this IEnumerable<T> first,
        IEnumerable<T> second,
        Func<T, TKey> keySelector)
    {
        var secondKeys = new HashSet<TKey>(second.Select(keySelector));
        return first.Where(item => !secondKeys.Contains(keySelector(item)));
    }

    /// <summary>
    /// Finds items that are in both collections
    /// </summary>
    public static IEnumerable<T> Intersect<T, TKey>(
        this IEnumerable<T> first,
        IEnumerable<T> second,
        Func<T, TKey> keySelector)
    {
        var secondKeys = new HashSet<TKey>(second.Select(keySelector));
        return first.Where(item => secondKeys.Contains(keySelector(item)));
    }

    /// <summary>
    /// Groups collection by key and returns first item from each group
    /// </summary>
    public static IEnumerable<T> GroupByFirst<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector)
    {
        return source.GroupBy(keySelector).Select(g => g.First());
    }

    /// <summary>
    /// Filters out null values from collection
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
    {
        return source.Where(x => x != null).Select(x => x!);
    }

    /// <summary>
    /// Filters out null values from nullable value type collection
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : struct
    {
        return source.Where(x => x.HasValue).Select(x => x!.Value);
    }

    /// <summary>
    /// Randomizes collection order
    /// </summary>
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        return source.OrderBy(x => Random.Shared.Next());
    }

    /// <summary>
    /// Takes random items from collection
    /// </summary>
    public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> source, int count)
    {
        return source.Shuffle().Take(count);
    }

    /// <summary>
    /// Checks if all items in collection are unique by key
    /// </summary>
    public static bool AllUnique<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var keys = new HashSet<TKey>();
        return source.All(item => keys.Add(keySelector(item)));
    }

    /// <summary>
    /// Returns the index of the first item matching the predicate
    /// </summary>
    public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var index = 0;
        foreach (var item in source)
        {
            if (predicate(item))
                return index;
            index++;
        }
        return -1;
    }

    /// <summary>
    /// Splits collection into two based on predicate
    /// </summary>
    public static (IEnumerable<T> Matching, IEnumerable<T> NotMatching) Split<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
    {
        var sourceList = source.ToList();
        var matching = sourceList.Where(predicate);
        var notMatching = sourceList.Where(x => !predicate(x));
        return (matching, notMatching);
    }

    /// <summary>
    /// Applies selector only to items matching the predicate
    /// </summary>
    public static IEnumerable<TResult> SelectWhere<T, TResult>(
        this IEnumerable<T> source,
        Func<T, bool> predicate,
        Func<T, TResult> selector)
    {
        return source.Where(predicate).Select(selector);
    }

    /// <summary>
    /// Returns the mode (most frequent value) in the collection
    /// </summary>
    public static T? Mode<T>(this IEnumerable<T> source) where T : class
    {
        return source
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;
    }

    /// <summary>
    /// Returns items that appear more than once
    /// </summary>
    public static IEnumerable<T> Duplicates<T>(this IEnumerable<T> source)
    {
        return source
            .GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
    }

    /// <summary>
    /// Converts collection to delimited string with Turkish formatting
    /// </summary>
    public static string ToDelimitedString<T>(this IEnumerable<T> source, string delimiter = ", ")
    {
        return string.Join(delimiter, source);
    }
}