using System.IO;
using BookStore.Application.Features.Authentication.DTOs;
using BookStore.Application.Features.Authentication.Responses;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Interfaces;
using BookStore.Shared.Results;
using BookStore.UI.Services;
using BookStore.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.UI.Tests;

public sealed class SessionTimeoutServiceTests
{
    [Fact]
    public async Task ExpireSessionAsync_WhenLogoutFails_StillNavigatesToLoginWithoutThrowing()
    {
        var navigation = new FakeApplicationNavigationService();
        var services = new ServiceCollection();
        services.AddScoped<IAuthenticationService, ThrowingLogoutAuthenticationService>();
        await using var provider = services.BuildServiceProvider();
        var service = new SessionTimeoutService(
            navigation,
            provider.GetRequiredService<IServiceScopeFactory>(),
            new SettingsChangedNotifier(),
            NullLogger<SessionTimeoutService>.Instance);

        await service.ExpireSessionAsync();

        Assert.Contains(typeof(LoginViewModel), navigation.Navigations);
    }

    private sealed class ThrowingLogoutAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AuthenticationResult> TryRestoreRememberedSessionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task LogoutAsync(CancellationToken cancellationToken = default) =>
            Task.FromException(new IOException("Remember-me token is locked."));

        public Task<OperationResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
