namespace Enterprise.Shared.Common.Interfaces;

/// <summary>
/// Unit of Work pattern interface for managing transactions and repositories
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets repository for the specified entity type
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    /// <returns>Repository instance</returns>
    IRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : class;
    
    /// <summary>
    /// Gets repository for entities with integer primary key
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <returns>Repository instance</returns>
    IRepository<TEntity> GetRepository<TEntity>() where TEntity : BaseEntity;
    
    /// <summary>
    /// Saves all changes within the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected records</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Begins a new transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transaction instance</returns>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes multiple operations within a transaction
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="operation">Operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes multiple operations within a transaction
    /// </summary>
    /// <param name="operation">Operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}

/// <summary>
/// Transaction interface for managing database transactions
/// </summary>
public interface ITransaction : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the transaction identifier
    /// </summary>
    Guid TransactionId { get; }
    
    /// <summary>
    /// Commits the transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CommitAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rolls back the transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a savepoint within the transaction
    /// </summary>
    /// <param name="name">Savepoint name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rolls back to a savepoint within the transaction
    /// </summary>
    /// <param name="name">Savepoint name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);
}