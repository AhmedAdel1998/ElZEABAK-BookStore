using BookStore.Application.Features.Authentication.Responses;
using BookStore.Application.Features.Settings.Commands;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Settings.Handlers;
using BookStore.Application.Features.Settings.Queries;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Features.Settings.Validators;
using BookStore.Application.Interfaces;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Constants;
using BookStore.Shared.Models;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BookStore.Application.Tests;

public class SettingsModuleTests
{
    [Fact]
    public async Task GetSettings_UsesConfiguredDefaults_WhenDatabaseOverrideDoesNotExist()
    {
        var fixture = new Fixture();
        fixture.Defaults.Store.Name = "Configured Store";

        var settings = await fixture.Service.GetAsync<StoreSettingsDto>();

        Assert.Equal("Configured Store", settings.StoreName);
    }

    [Fact]
    public async Task UpdateSettings_PersistsAndInvalidatesCache()
    {
        var fixture = new Fixture();
        var changedEvents = 0;
        fixture.Notifier.SettingsChanged += (_, _) => changedEvents++;

        var first = await fixture.Service.GetAsync<TaxSettingsDto>();
        var result = await fixture.Service.SetAsync(new TaxSettingsDto { Enabled = true, DefaultRate = 0.2m });
        var second = await fixture.Service.GetAsync<TaxSettingsDto>();

        Assert.True(result.IsSuccess);
        Assert.NotSame(first, second);
        Assert.Equal(0.2m, second.DefaultRate);
        Assert.Equal(1, changedEvents);
        Assert.Single(fixture.Store.Records);
    }

    [Fact]
    public async Task Validation_PreventsInvalidSettingsPersistence()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.SetAsync(new CurrencySettingsDto { CurrencyCode = "BADCODE", CurrencySymbol = string.Empty });

        Assert.False(result.IsSuccess);
        Assert.Empty(fixture.Store.Records);
    }

    [Fact]
    public async Task ResetCategory_RemovesPersistentOverride()
    {
        var fixture = new Fixture();
        await fixture.Service.SetAsync(new BarcodeSettingsDto { Prefix = "ALT", DefaultFormat = "Code128", StartingNumber = 10, ScanTimeout = 100, AutoGenerate = true });

        var result = await fixture.Service.ResetCategoryAsync(SettingsCategory.Barcode);
        var settings = await fixture.Service.GetAsync<BarcodeSettingsDto>();

        Assert.True(result.IsSuccess);
        Assert.Equal("BK", settings.Prefix);
        Assert.Empty(fixture.Store.Records);
    }

    [Fact]
    public async Task PermissionChecks_BlockUnauthorizedCategoryUpdate()
    {
        var fixture = new Fixture(permissions: [PermissionConstants.SettingsView]);
        var handler = new SettingsCommandHandler(fixture.Service, fixture.Authorization);

        var result = await handler.Handle(new UpdateSecuritySettingsCommand(new SecuritySettingsDto()));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task PermissionChecks_AllowAuthorizedCategoryQuery()
    {
        var fixture = new Fixture(permissions: [PermissionConstants.SettingsView, PermissionConstants.SettingsTax]);
        var handler = new SettingsQueryHandler(fixture.Service, fixture.Authorization);

        var result = await handler.Handle(new GetTaxSettingsQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    private sealed class Fixture
    {
        public Fixture(IReadOnlyCollection<string>? permissions = null)
        {
            Services = new FakeServiceProvider(
                new StoreSettingsValidator(),
                new CurrencySettingsValidator(),
                new TaxSettingsValidator(),
                new BarcodeSettingsValidator());
            Store = new FakeSettingsStore();
            Defaults = new ApplicationSettings();
            Notifier = new SettingsChangedNotifier();
            Authorization = new FakeAuthorizationService(permissions ?? [PermissionConstants.SettingsView, PermissionConstants.SettingsTax, PermissionConstants.SettingsSecurity]);
            Service = new SettingsService(Store, new SettingsCache(), Notifier, Services, new FakeCurrentUserService(), Options.Create(Defaults), NullLogger<SettingsService>.Instance);
        }

        public IServiceProvider Services { get; }
        public FakeSettingsStore Store { get; }
        public ApplicationSettings Defaults { get; }
        public SettingsChangedNotifier Notifier { get; }
        public FakeAuthorizationService Authorization { get; }
        public ISettingsService Service { get; }
    }

    private sealed class FakeSettingsStore : ISettingsStore
    {
        public Dictionary<string, PersistedSettingRecord> Records { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Task<IReadOnlyList<PersistedSettingRecord>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PersistedSettingRecord>>(Records.Values.ToArray());
        public Task<PersistedSettingRecord?> GetByKeyAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(Records.GetValueOrDefault(key));
        public Task UpsertAsync(PersistedSettingRecord setting, CancellationToken cancellationToken = default) { Records[setting.Key] = setting; return Task.CompletedTask; }
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) { Records.Remove(key); return Task.CompletedTask; }
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = Guid.NewGuid();
        public string? Username => "admin";
        public string? FullName => "Admin";
        public string? Role => "Administrator";
        public IReadOnlyCollection<string> Permissions => [];
        public Guid? SessionId => Guid.NewGuid();
        public DateTimeOffset? LoginTime => DateTimeOffset.UtcNow;
        public void SignIn(UserSessionSnapshot session) { }
        public void SignOut() { }
    }

    private sealed class FakeAuthorizationService(IReadOnlyCollection<string> permissions) : IAuthorizationService
    {
        public bool HasPermission(string permission) => permissions.Contains(permission);
        public bool HasPermissions(params string[] permissionsToCheck) => permissionsToCheck.All(HasPermission);
        public bool HasRole(string role) => false;
        public bool CanAccess(string requiredPermission) => HasPermission(requiredPermission);
    }

    private sealed class FakeServiceProvider(params object[] services) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return services.FirstOrDefault(service => serviceType.IsInstanceOfType(service));
        }
    }
}
