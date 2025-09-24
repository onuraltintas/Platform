namespace Identity.Core.Interfaces;

public interface IGatewayCacheInvalidationClient
{
    Task InvalidateUserAsync(string userId, CancellationToken cancellationToken = default);
    Task BulkInvalidateAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
}

