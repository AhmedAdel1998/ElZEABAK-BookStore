using BookStore.Application.Features.Administration;
using BookStore.Application.Features.Authentication.Responses;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Infrastructure.Authentication;
using BookStore.Persistence.Context;
using BookStore.Persistence.Repositories;
using BookStore.Persistence.UnitOfWork;
using BookStore.Shared.Constants;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.Infrastructure.Tests;

public sealed class AdministrationServiceTests
{
    [Fact]
    public async Task CreateUser_HashesPasswordAndRejectsCaseInsensitiveDuplicateUsername()
    {
        await using var fixture = await AdministrationFixture.CreateAsync();
        var role = await fixture.Context.Roles.SingleAsync();

        var created = await fixture.Service.CreateUserAsync(new CreateUserAdministrationRequest("Cashier", "Cashier One", "cashier@example.com", role.Id, "Strong123"));
        var duplicate = await fixture.Service.CreateUserAsync(new CreateUserAdministrationRequest("cashier", "Cashier Two", null, role.Id, "Strong123"));

        Assert.True(created.IsSuccess);
        Assert.False(duplicate.IsSuccess);
        var user = await fixture.Context.Users.SingleAsync(item => item.Username == "Cashier");
        Assert.StartsWith("BS2:$2", user.PasswordHash);
        Assert.True(fixture.PasswordHasher.VerifyPassword("Strong123", user.PasswordHash));
        Assert.Equal("cashier@example.com", user.Email?.Value);
    }

    [Fact]
    public async Task Deactivate_PreventsCurrentUserAndLastActiveManagerLockout()
    {
        await using var fixture = await AdministrationFixture.CreateAsync();

        var result = await fixture.Service.SetUserActiveAsync(fixture.Admin.Id, false);

        Assert.False(result.IsSuccess);
        Assert.Contains("own active session", result.Error);
        Assert.True((await fixture.Context.Users.FindAsync(fixture.Admin.Id))!.IsActive);
    }

    [Fact]
    public async Task SaveRole_PreventsRemovingLastRequiredManagementPermissions()
    {
        await using var fixture = await AdministrationFixture.CreateAsync();
        var role = await fixture.Context.Roles.Include(item => item.Permissions).SingleAsync();

        var result = await fixture.Service.SaveRoleAsync(new SaveRoleAdministrationRequest(role.Id, role.Name, role.Description, []));

        Assert.False(result.IsSuccess);
        Assert.Contains("last active administrator", result.Error);
        Assert.Equal(2, (await fixture.Context.Roles.Include(item => item.Permissions).SingleAsync()).Permissions.Count);
    }

    private sealed class AdministrationFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private AdministrationFixture(SqliteConnection connection, BookStoreDbContext context, User admin, PasswordHasher passwordHasher, AdministrationService service)
        {
            _connection = connection; Context = context; Admin = admin; PasswordHasher = passwordHasher; Service = service;
        }

        public BookStoreDbContext Context { get; }
        public User Admin { get; }
        public PasswordHasher PasswordHasher { get; }
        public AdministrationService Service { get; }

        public static async Task<AdministrationFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new BookStoreDbContext(new DbContextOptionsBuilder<BookStoreDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync();
            var usersPermission = new Permission(PermissionConstants.UsersManage);
            var rolesPermission = new Permission(PermissionConstants.RolesManage);
            var role = new Role("Administrator", "Full system access");
            role.AddPermission(usersPermission);
            role.AddPermission(rolesPermission);
            var passwordHasher = new PasswordHasher();
            var admin = new User("admin", passwordHasher.HashPassword("Admin123"), "Administrator", role.Id);
            context.AddRange(usersPermission, rolesPermission, role, admin);
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
            var access = new AdministrationAccess(admin.Id);
            var service = new AdministrationService(unitOfWork, passwordHasher, access, access, NullLogger<AdministrationService>.Instance);
            return new AdministrationFixture(connection, context, admin, passwordHasher, service);
        }

        public async ValueTask DisposeAsync() { await Context.DisposeAsync(); await _connection.DisposeAsync(); }
    }

    private sealed class AdministrationAccess(Guid userId) : IAuthorizationService, ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
        public string? Username => "admin";
        public string? FullName => "Administrator";
        public string? Role => "Administrator";
        public IReadOnlyCollection<string> Permissions { get; } = [PermissionConstants.UsersManage, PermissionConstants.RolesManage];
        public Guid? SessionId => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public DateTimeOffset? LoginTime => DateTimeOffset.UtcNow;
        public bool HasPermission(string permission) => Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        public bool HasPermissions(params string[] permissions) => permissions.All(HasPermission);
        public bool HasRole(string role) => string.Equals(role, Role, StringComparison.OrdinalIgnoreCase);
        public bool CanAccess(string requiredPermission) => HasPermission(requiredPermission);
        public void SignIn(UserSessionSnapshot session) { }
        public void SignOut() { }
    }
}
