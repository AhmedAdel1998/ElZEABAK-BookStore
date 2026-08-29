using BookStore.Application.Features.Authentication.DTOs;
using BookStore.Application.Features.Authentication.Responses;
using BookStore.Application.Features.Authentication.Validators;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Infrastructure.Authentication;
using BookStore.Persistence.Context;
using BookStore.Persistence.Repositories;
using BookStore.Persistence.UnitOfWork;
using BookStore.Shared.Models;
using BookStore.Shared.Results;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BookStore.Infrastructure.Tests;

public sealed class AuthenticationServiceTests
{
    private const string ValidPassword = "Admin123!";

    [Fact]
    public async Task LoginAsync_WithValidCredentials_CreatesSessionAndRememberedLogin()
    {
        await using var fixture = await AuthenticationFixture.CreateAsync();

        var result = await fixture.Service.LoginAsync(new LoginRequest
        {
            Username = "  admin  ",
            Password = ValidPassword,
            RememberMe = true
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Session);
        Assert.True(fixture.CurrentUser.IsAuthenticated);
        Assert.Equal(fixture.User.Id, fixture.CurrentUser.UserId);
        Assert.Equal("Administrator", fixture.CurrentUser.Role);
        Assert.Equal(fixture.User.Id, fixture.RememberMe.UserId);
        Assert.True(fixture.RememberMe.ExpiresAt > DateTimeOffset.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task LoginAsync_WithLegacyBcryptHash_UpgradesPersistedHash()
    {
        await using var fixture = await AuthenticationFixture.CreateAsync(useLegacyHash: true);
        Assert.StartsWith("$2", fixture.User.PasswordHash, StringComparison.Ordinal);

        var result = await fixture.Service.LoginAsync(new LoginRequest
        {
            Username = "admin",
            Password = ValidPassword
        });

        Assert.True(result.Succeeded);
        Assert.StartsWith("BS2:$2", fixture.User.PasswordHash, StringComparison.Ordinal);
        Assert.True(fixture.PasswordHasher.VerifyPassword(ValidPassword, fixture.User.PasswordHash));
    }

    [Fact]
    public async Task LoginAsync_RepeatedInvalidPasswords_LocksAccountAndRejectsCorrectPassword()
    {
        await using var fixture = await AuthenticationFixture.CreateAsync(maxAttempts: 3);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var failed = await fixture.Service.LoginAsync(new LoginRequest
            {
                Username = "admin",
                Password = "Wrong123!"
            });

            Assert.False(failed.Succeeded);
        }

        Assert.True(fixture.User.IsLockedOut);
        Assert.Equal(3, fixture.User.FailedLoginCount);

        var locked = await fixture.Service.LoginAsync(new LoginRequest
        {
            Username = "admin",
            Password = ValidPassword
        });

        Assert.False(locked.Succeeded);
        Assert.Contains("locked", locked.Errors.Single().Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(fixture.CurrentUser.IsAuthenticated);
    }

    [Fact]
    public async Task TryRestoreRememberedSession_ForInactiveUser_ClearsTokenAndRejectsSession()
    {
        await using var fixture = await AuthenticationFixture.CreateAsync();
        fixture.User.Deactivate();
        await fixture.Context.SaveChangesAsync();
        fixture.RememberMe.UserId = fixture.User.Id;

        var result = await fixture.Service.TryRestoreRememberedSessionAsync();

        Assert.False(result.Succeeded);
        Assert.False(fixture.CurrentUser.IsAuthenticated);
        Assert.Equal(1, fixture.RememberMe.ClearCount);
        Assert.Null(fixture.RememberMe.UserId);
    }

    [Fact]
    public async Task LogoutAsync_ClearsCurrentAndRememberedSessions()
    {
        await using var fixture = await AuthenticationFixture.CreateAsync();
        var login = await fixture.Service.LoginAsync(new LoginRequest
        {
            Username = "admin",
            Password = ValidPassword,
            RememberMe = true
        });
        Assert.True(login.Succeeded);

        await fixture.Service.LogoutAsync();

        Assert.False(fixture.CurrentUser.IsAuthenticated);
        Assert.Null(fixture.RememberMe.UserId);
        Assert.Equal(1, fixture.RememberMe.ClearCount);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithCurrentPassword_ReplacesPersistedHash()
    {
        await using var fixture = await AuthenticationFixture.CreateAsync();
        var login = await fixture.Service.LoginAsync(new LoginRequest
        {
            Username = "admin",
            Password = ValidPassword
        });
        Assert.True(login.Succeeded);

        var result = await fixture.Service.ChangePasswordAsync(new ChangePasswordRequest
        {
            CurrentPassword = ValidPassword,
            NewPassword = "Changed123!",
            ConfirmPassword = "Changed123!"
        });

        Assert.True(result.Succeeded);
        Assert.False(fixture.PasswordHasher.VerifyPassword(ValidPassword, fixture.User.PasswordHash));
        Assert.True(fixture.PasswordHasher.VerifyPassword("Changed123!", fixture.User.PasswordHash));
    }

    private sealed class AuthenticationFixture : IAsyncDisposable
    {
        private AuthenticationFixture(
            SqliteConnection connection,
            BookStoreDbContext context,
            User user,
            PasswordHasher passwordHasher,
            CurrentUserService currentUser,
            FakeRememberMeStore rememberMe,
            AuthenticationService service)
        {
            Connection = connection;
            Context = context;
            User = user;
            PasswordHasher = passwordHasher;
            CurrentUser = currentUser;
            RememberMe = rememberMe;
            Service = service;
        }

        public SqliteConnection Connection { get; }
        public BookStoreDbContext Context { get; }
        public User User { get; }
        public PasswordHasher PasswordHasher { get; }
        public CurrentUserService CurrentUser { get; }
        public FakeRememberMeStore RememberMe { get; }
        public AuthenticationService Service { get; }

        public static async Task<AuthenticationFixture> CreateAsync(int maxAttempts = 5, bool useLegacyHash = false)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<BookStoreDbContext>().UseSqlite(connection).Options;
            var context = new BookStoreDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var passwordHasher = new PasswordHasher();
            var role = new Role("Administrator", "Full system access");
            var passwordHash = useLegacyHash
                ? BCrypt.Net.BCrypt.HashPassword(ValidPassword, 4)
                : passwordHasher.HashPassword(ValidPassword);
            var user = new User("admin", passwordHash, "Administrator", role.Id);
            context.Roles.Add(role);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var unitOfWork = new UnitOfWork(
                context,
                new ProductRepository(context),
                new CategoryRepository(context),
                new CustomerRepository(context),
                new SupplierRepository(context),
                new UserRepository(context),
                new RoleRepository(context),
                new SaleRepository(context),
                new InventoryRepository(context),
                NullLogger<UnitOfWork>.Instance);
            var currentUser = new CurrentUserService();
            var rememberMe = new FakeRememberMeStore();
            var settings = new StubSettingsService(new SecuritySettingsDto
            {
                MaxLoginAttempts = maxAttempts,
                LockoutDuration = 15,
                SessionTimeout = 30
            });
            var applicationSettings = Options.Create(new ApplicationSettings
            {
                Authentication = new AuthenticationSettings
                {
                    MaxFailedLoginAttempts = maxAttempts,
                    LockoutMinutes = 15,
                    RememberMeDays = 7
                }
            });
            var service = new AuthenticationService(
                unitOfWork,
                passwordHasher,
                currentUser,
                rememberMe,
                new LoginRequestValidator(),
                new ChangePasswordRequestValidator(),
                applicationSettings,
                settings,
                NullLogger<AuthenticationService>.Instance);

            return new AuthenticationFixture(connection, context, user, passwordHasher, currentUser, rememberMe, service);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class FakeRememberMeStore : IRememberMeStore
    {
        public Guid? UserId { get; set; }
        public DateTimeOffset ExpiresAt { get; private set; }
        public int ClearCount { get; private set; }

        public Task SaveAsync(Guid userId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
        {
            UserId = userId;
            ExpiresAt = expiresAt;
            return Task.CompletedTask;
        }

        public Task<Guid?> ReadUserIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(UserId);

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            UserId = null;
            ClearCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubSettingsService(SecuritySettingsDto security) : ISettingsService
    {
        public Task<T> GetAsync<T>(CancellationToken cancellationToken = default)
            where T : class, new()
        {
            object value = typeof(T) == typeof(SecuritySettingsDto) ? security : new T();
            return Task.FromResult((T)value);
        }

        public Task<Result> SetAsync<T>(T settings, CancellationToken cancellationToken = default)
            where T : class, new() => Task.FromResult(Result.Success());

        public Task<IReadOnlyList<SettingEntryDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SettingEntryDto>>([]);

        public Task<Result> ResetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
        public Task<Result> ResetCategoryAsync(SettingsCategory category, CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
