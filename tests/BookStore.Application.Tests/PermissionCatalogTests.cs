using System.Reflection;
using BookStore.Shared.Constants;
using Xunit;

namespace BookStore.Application.Tests;

/// <summary>
/// Guards the invariant that broke the point of sale: a permission that is declared (and therefore
/// can be enforced by a handler or view model) must also be catalogued, because the seeder creates
/// database rows only for catalogued permissions. An uncatalogued permission can never be granted
/// to any role, so the feature behind it is silently dead for every user including the
/// administrator.
/// </summary>
public class PermissionCatalogTests
{
    private static IReadOnlyList<string> DeclaredPermissions() =>
        typeof(PermissionConstants)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    [Fact]
    public void EveryDeclaredPermissionIsCatalogued()
    {
        var catalogued = PermissionCatalog.All.Select(definition => definition.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = DeclaredPermissions().Where(name => !catalogued.Contains(name)).OrderBy(name => name).ToArray();

        Assert.True(
            missing.Length == 0,
            $"These permissions are declared but not catalogued, so the seeder will never create them and no role can be granted them: {string.Join(", ", missing)}");
    }

    [Fact]
    public void EveryCataloguedPermissionIsDeclared()
    {
        var declared = DeclaredPermissions().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = PermissionCatalog.All
            .Select(definition => definition.Name)
            .Where(name => !declared.Contains(name))
            .OrderBy(name => name)
            .ToArray();

        Assert.True(unknown.Length == 0, $"Catalogued permissions with no matching constant: {string.Join(", ", unknown)}");
    }

    [Fact]
    public void CatalogHasNoDuplicatesAndNoBlankDescriptions()
    {
        var duplicates = PermissionCatalog.All
            .GroupBy(definition => definition.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.Empty(duplicates);
        Assert.All(PermissionCatalog.All, definition => Assert.False(string.IsNullOrWhiteSpace(definition.Description)));
    }

    [Theory]
    [InlineData(PermissionConstants.SalesCreate)]
    [InlineData(PermissionConstants.SalesComplete)]
    [InlineData(PermissionConstants.SalesSuspend)]
    [InlineData(PermissionConstants.ReceiptPrint)]
    [InlineData(PermissionConstants.ProductView)]
    [InlineData(PermissionConstants.CustomerView)]
    public void CashierCanRunTheCounterWorkflow(string permission)
    {
        Assert.Contains(permission, PermissionCatalog.CashierPermissions, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ManagerHasEverythingExceptUserRoleAndRestoreAdministration()
    {
        Assert.DoesNotContain(PermissionConstants.UsersManage, PermissionCatalog.ManagerPermissions);
        Assert.DoesNotContain(PermissionConstants.RolesManage, PermissionCatalog.ManagerPermissions);
        Assert.DoesNotContain(PermissionConstants.BackupRestore, PermissionCatalog.ManagerPermissions);
        Assert.Contains(PermissionConstants.SalesComplete, PermissionCatalog.ManagerPermissions);
        Assert.Equal(PermissionCatalog.All.Count - 3, PermissionCatalog.ManagerPermissions.Count);
    }

    [Fact]
    public void CashierPermissionsAreAllCatalogued()
    {
        var catalogued = PermissionCatalog.All.Select(definition => definition.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.All(PermissionCatalog.CashierPermissions, permission => Assert.Contains(permission, catalogued));
    }
}
