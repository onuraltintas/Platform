using Microsoft.Extensions.Logging;
using Polly;
using System.Data;
using EgitimPlatform.Shared.Resilience.Policies;

namespace EgitimPlatform.Shared.Resilience.Services;

public interface IResilientDatabaseService
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<Task> operation, string operationName, CancellationToken cancellationToken = default);
    Task<T> ExecuteWithTransactionAsync<T>(Func<IDbTransaction, Task<T>> operation, string operationName, CancellationToken cancellationToken = default);
    Task ExecuteWithTransactionAsync(Func<IDbTransaction, Task> operation, string operationName, CancellationToken cancellationToken = default);
}

public class ResilientDatabaseService : IResilientDatabaseService
{
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly ILogger<ResilientDatabaseService> _logger;
    private readonly IDbConnection? _dbConnection;

    public ResilientDatabaseService(
        IResiliencePolicyFactory policyFactory,
        ILogger<ResilientDatabaseService> logger,
        IDbConnection? dbConnection = null,
        string? serviceName = null)
    {
        _resiliencePipeline = policyFactory.CreateDatabasePipeline(serviceName);
        _logger = logger;
        _dbConnection = dbConnection;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Executing resilient database operation: {OperationName}", operationName);

            var result = await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                return await operation();
            }, cancellationToken);

            _logger.LogDebug("Successfully completed database operation: {OperationName}", operationName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute database operation: {OperationName}", operationName);
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, string operationName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Executing resilient database operation: {OperationName}", operationName);

            await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                await operation();
            }, cancellationToken);

            _logger.LogDebug("Successfully completed database operation: {OperationName}", operationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute database operation: {OperationName}", operationName);
            throw;
        }
    }

    public async Task<T> ExecuteWithTransactionAsync<T>(Func<IDbTransaction, Task<T>> operation, string operationName, CancellationToken cancellationToken = default)
    {
        if (_dbConnection == null)
        {
            throw new InvalidOperationException("Database connection is not configured for transaction operations");
        }

        return await ExecuteAsync(async () =>
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                var result = await operation(transaction);
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }, $"{operationName} (with transaction)", cancellationToken);
    }

    public async Task ExecuteWithTransactionAsync(Func<IDbTransaction, Task> operation, string operationName, CancellationToken cancellationToken = default)
    {
        if (_dbConnection == null)
        {
            throw new InvalidOperationException("Database connection is not configured for transaction operations");
        }

        await ExecuteAsync(async () =>
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                await operation(transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }, $"{operationName} (with transaction)", cancellationToken);
    }
}