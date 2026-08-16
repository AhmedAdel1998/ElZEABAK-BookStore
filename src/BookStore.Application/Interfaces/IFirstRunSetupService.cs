using BookStore.Shared.Results;

namespace BookStore.Application.Interfaces;

/// <summary>
/// Creates the initial administrator when a fresh database has no users.
/// </summary>
public interface IFirstRunSetupService
{
    /// <summary>Returns true when the database has no users.</summary>
    Task<bool> IsSetupRequiredAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates the first administrator account.</summary>
    Task<OperationResult> CreateAdministratorAsync(string username, string fullName, string email, string password, string confirmPassword, CancellationToken cancellationToken = default);
}
