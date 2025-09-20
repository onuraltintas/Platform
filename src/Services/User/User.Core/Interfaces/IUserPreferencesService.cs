using Enterprise.Shared.Common.Models;
using User.Core.DTOs;
using User.Core.DTOs.Requests;

namespace User.Core.Interfaces;

/// <summary>
/// Service interface for user preferences operations
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Get user preferences by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User preferences or error</returns>
    Task<Result<UserPreferencesDto>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated preferences or error</returns>
    Task<Result<UserPreferencesDto>> UpdateAsync(string userId, UpdateUserPreferencesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset preferences to default
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Default preferences or error</returns>
    Task<Result<UserPreferencesDto>> ResetToDefaultAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update GDPR consent
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="marketingConsent">Marketing emails consent</param>
    /// <param name="dataProcessingConsent">Data processing consent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error</returns>
    Task<Result<bool>> UpdateConsentAsync(string userId, bool marketingConsent, bool dataProcessingConsent, CancellationToken cancellationToken = default);
}