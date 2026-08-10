using BookStore.Application.Features.Authentication.Responses;
using BookStore.Application.Interfaces;

namespace BookStore.Infrastructure.Authentication;

/// <summary>
/// Stores the authenticated user session for the running application.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly object _syncRoot = new();
    private UserSessionSnapshot? _session;

    /// <inheritdoc />
    public bool IsAuthenticated => _session is not null;

    /// <inheritdoc />
    public Guid? UserId => _session?.UserId;

    /// <inheritdoc />
    public string? Username => _session?.Username;

    /// <inheritdoc />
    public string? FullName => _session?.FullName;

    /// <inheritdoc />
    public string? Role => _session?.Role;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Permissions => _session?.Permissions ?? [];

    /// <inheritdoc />
    public Guid? SessionId => _session?.SessionId;

    /// <inheritdoc />
    public DateTimeOffset? LoginTime => _session?.LoginTime;

    /// <inheritdoc />
    public void SignIn(UserSessionSnapshot session)
    {
        lock (_syncRoot)
        {
            _session = session;
        }
    }

    /// <inheritdoc />
    public void SignOut()
    {
        lock (_syncRoot)
        {
            _session = null;
        }
    }
}
