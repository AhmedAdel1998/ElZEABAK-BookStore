using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BookStore.Shared.Constants;

namespace BookStore.Infrastructure.Authentication;

/// <summary>
/// Stores remember-me state in an encrypted local file.
/// </summary>
public class RememberMeStore : IRememberMeStore
{
    private readonly string _tokenPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="RememberMeStore"/> class.
    /// </summary>
    public RememberMeStore()
    {
        _tokenPath = Path.Combine(ApplicationPaths.ResolveDataPath(FolderConstants.Temp), "remember-me.dat");
    }

    /// <inheritdoc />
    public async Task SaveAsync(Guid userId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Encrypted remember-me storage requires Windows data protection.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_tokenPath)!);
        var payload = JsonSerializer.Serialize(new RememberMePayload(userId, expiresAt));
        var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(payload), null, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(_tokenPath, protectedBytes, cancellationToken);
        File.SetAttributes(_tokenPath, File.GetAttributes(_tokenPath) | FileAttributes.Hidden);
    }

    /// <inheritdoc />
    public async Task<Guid?> ReadUserIdAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_tokenPath))
        {
            return null;
        }

        try
        {
            if (!OperatingSystem.IsWindows())
            {
                await ClearAsync(cancellationToken);
                return null;
            }

            var protectedBytes = await File.ReadAllBytesAsync(_tokenPath, cancellationToken);
            var payloadBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            var payload = JsonSerializer.Deserialize<RememberMePayload>(Encoding.UTF8.GetString(payloadBytes));

            if (payload is null || payload.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                await ClearAsync(cancellationToken);
                return null;
            }

            return payload.UserId;
        }
        catch (CryptographicException)
        {
            await ClearAsync(cancellationToken);
            return null;
        }
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_tokenPath))
        {
            File.Delete(_tokenPath);
        }

        return Task.CompletedTask;
    }

    private sealed record RememberMePayload(Guid UserId, DateTimeOffset ExpiresAt);
}
