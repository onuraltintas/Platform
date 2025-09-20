namespace Enterprise.Shared.Common.Extensions;

/// <summary>
/// Extension methods for collections and enumerables
/// </summary>
public static class CollectionExtensions
{
    #region Null and Empty Checks

    /// <summary>
    /// Checks if collection is null or empty
    /// </summary>
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? collection)
    {
        return collection == null || !collection.Any();
    }

    /// <summary>
    /// Checks if collection has any elements
    /// </summary>
    public static bool HasItems<T>([NotNullWhen(true)] this IEnumerable<T>? collection)
    {
        return collection != null && collection.Any();
    }

    /// <summary>
    /// Returns the collection or an empty enumerable if null
    /// </summary>
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection ?? Enumerable.Empty<T>();
    }

    #endregion

    #region Safe Operations

    /// <summary>
    /// Safe version of First() that returns default if empty
    /// </summary>
    public static T? SafeFirst<T>(this IEnumerable<T>? collection)
    {
        return collection == null ? default : collection.FirstOrDefault();
    }

    /// <summary>
    /// Safe version of First(predicate) that returns default if empty
    /// </summary>
    public static T? SafeFirst<T>(this IEnumerable<T>? collection, Func<T, bool> predicate)
    {
        return collection == null ? default : collection.FirstOrDefault(predicate);
    }

    /// <summary>
    /// Safe version of Last() that returns default if empty
    /// </summary>
    public static T? SafeLast<T>(this IEnumerable<T>? collection)
    {
        return collection == null ? default : collection.LastOrDefault();
    }

    /// <summary>
    /// Safe version of ElementAt() that returns default if index is out of range
    /// </summary>
    public static T? SafeElementAt<T>(this IEnumerable<T>? collection, int index)
    {
        if (collection == null || index < 0) return default;
        
        return collection.Skip(index).FirstOrDefault();
    }

    #endregion

    #region Batch Operations

    /// <summary>
    /// Splits the collection into batches of specified size
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than 0");

        using var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return GetBatch(enumerator, batchSize);
        }
    }

    private static IEnumerable<T> GetBatch<T>(IEnumerator<T> enumerator, int batchSize)
    {
        do
        {
            yield return enumerator.Current;
        } while (--batchSize > 0 && enumerator.MoveNext());
    }

    /// <summary>
    /// Splits the collection into chunks of specified size
    /// </summary>
    public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int chunkSize)
    {
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than 0");

        var list = source.ToList();
        for (var i = 0; i < list.Count; i += chunkSize)
        {
            var chunk = new T[Math.Min(chunkSize, list.Count - i)];
            list.CopyTo(i, chunk, 0, chunk.Length);
            yield return chunk;
        }
    }

    #endregion

    #region Distinct Operations

    /// <summary>
    /// Returns distinct elements based on a key selector
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source)
        {
            if (seen.Add(keySelector(item)))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Returns distinct elements based on a key selector with custom comparer
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, 
        Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
    {
        var seen = new HashSet<TKey>(comparer);
        foreach (var item in source)
        {
            if (seen.Add(keySelector(item)))
            {
                yield return item;
            }
        }
    }

    #endregion

    #region ForEach Operations

    /// <summary>
    /// Performs an action on each element in the collection
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }

    /// <summary>
    /// Performs an action on each element with its index
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        var index = 0;
        foreach (var item in source)
        {
            action(item, index++);
        }
    }

    /// <summary>
    /// Asynchronously performs an action on each element in the collection
    /// </summary>
    public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> asyncAction)
    {
        foreach (var item in source)
        {
            await asyncAction(item);
        }
    }

    /// <summary>
    /// Asynchronously performs an action on each element in parallel
    /// </summary>
    public static async Task ForEachParallelAsync<T>(this IEnumerable<T> source, 
        Func<T, Task> asyncAction, int maxDegreeOfParallelism = 8)
    {
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = source.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                await asyncAction(item);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    #endregion

    #region Pagination

    /// <summary>
    /// Paginates the collection
    /// </summary>
    public static IEnumerable<T> Paginate<T>(this IEnumerable<T> source, int page, int pageSize)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than 0");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0");

        return source.Skip((page - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Creates a paged result from the collection
    /// </summary>
    public static PagedResult<T> ToPagedResult<T>(this IEnumerable<T> source, int page, int pageSize)
    {
        var list = source.ToList();
        var totalCount = list.Count;
        var data = list.Paginate(page, pageSize).ToList();

        return PagedResult<T>.Create(data, totalCount, page, pageSize);
    }

    #endregion

    #region Filtering

    /// <summary>
    /// Filters out null values from the collection
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
    {
        return source.Where(x => x != null)!;
    }

    /// <summary>
    /// Filters out null values from nullable value types
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : struct
    {
        return source.Where(x => x.HasValue).Select(x => x!.Value);
    }

    /// <summary>
    /// Filters collection with multiple predicates (AND logic)
    /// </summary>
    public static IEnumerable<T> WhereAll<T>(this IEnumerable<T> source, params Func<T, bool>[] predicates)
    {
        return source.Where(item => predicates.All(predicate => predicate(item)));
    }

    /// <summary>
    /// Filters collection with multiple predicates (OR logic)
    /// </summary>
    public static IEnumerable<T> WhereAny<T>(this IEnumerable<T> source, params Func<T, bool>[] predicates)
    {
        return source.Where(item => predicates.Any(predicate => predicate(item)));
    }

    #endregion

    #region Conversion

    /// <summary>
    /// Converts collection to dictionary with duplicate key handling
    /// </summary>
    public static Dictionary<TKey, TValue> ToSafeDictionary<T, TKey, TValue>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        Func<T, TValue> valueSelector,
        Func<TValue, TValue, TValue>? duplicateHandler = null) where TKey : notnull
    {
        var dictionary = new Dictionary<TKey, TValue>();
        
        foreach (var item in source)
        {
            var key = keySelector(item);
            var value = valueSelector(item);
            
            if (dictionary.TryGetValue(key, out var existingValue))
            {
                dictionary[key] = duplicateHandler != null ? duplicateHandler(existingValue, value) : value;
            }
            else
            {
                dictionary[key] = value;
            }
        }
        
        return dictionary;
    }

    /// <summary>
    /// Converts collection to HashSet
    /// </summary>
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null)
    {
        return new HashSet<T>(source, comparer);
    }

    #endregion

    #region Statistical Operations

    /// <summary>
    /// Calculates the median value
    /// </summary>
    public static double Median<T>(this IEnumerable<T> source, Func<T, double> selector)
    {
        var values = source.Select(selector).OrderBy(x => x).ToList();
        if (!values.Any()) return 0;
        
        var count = values.Count;
        return count % 2 == 0
            ? (values[count / 2 - 1] + values[count / 2]) / 2.0
            : values[count / 2];
    }

    /// <summary>
    /// Calculates the mode (most frequent value)
    /// </summary>
    public static T? Mode<T>(this IEnumerable<T> source) where T : class
    {
        var group = source
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        
        return group?.Key;
    }

    /// <summary>
    /// Calculates the standard deviation
    /// </summary>
    public static double StandardDeviation<T>(this IEnumerable<T> source, Func<T, double> selector)
    {
        var values = source.Select(selector).ToList();
        if (values.Count <= 1) return 0;

        var average = values.Average();
        var sumOfSquaresOfDifferences = values.Sum(val => (val - average) * (val - average));
        return Math.Sqrt(sumOfSquaresOfDifferences / (values.Count - 1));
    }

    #endregion

    #region Random Operations

    /// <summary>
    /// Shuffles the collection randomly
    /// </summary>
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        var random = new Random();
        return source.OrderBy(_ => random.Next());
    }

    /// <summary>
    /// Shuffles the collection using provided Random instance
    /// </summary>
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random random)
    {
        return source.OrderBy(_ => random.Next());
    }

    /// <summary>
    /// Takes a random sample from the collection
    /// </summary>
    public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> source, int count)
    {
        return source.Shuffle().Take(count);
    }

    /// <summary>
    /// Gets a random element from the collection
    /// </summary>
    public static T? Random<T>(this IEnumerable<T> source)
    {
        var list = source.ToList();
        return list.Count > 0 ? list[new Random().Next(list.Count)] : default;
    }

    #endregion

    #region String Operations

    /// <summary>
    /// Joins string collection with separator
    /// </summary>
    public static string JoinWith(this IEnumerable<string> source, string separator)
    {
        return string.Join(separator, source);
    }

    /// <summary>
    /// Joins collection with separator using ToString()
    /// </summary>
    public static string JoinWith<T>(this IEnumerable<T> source, string separator)
    {
        return string.Join(separator, source);
    }

    /// <summary>
    /// Joins collection with custom formatter
    /// </summary>
    public static string JoinWith<T>(this IEnumerable<T> source, string separator, Func<T, string> formatter)
    {
        return string.Join(separator, source.Select(formatter));
    }

    #endregion
}