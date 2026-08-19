using System.Collections;
using System.Reflection;
using BookStore.Persistence.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Tests;

/// <summary>
/// Executes every awaitable query method on every repository and reporting service against a real
/// SQLite database.
/// </summary>
/// <remarks>
/// EF Core reports an untranslatable query only when that query runs, so neither compilation nor
/// code review can detect one. This regression suite exists because a value-converted property is
/// opaque inside a predicate: <c>Barcode.Value.ToUpper() == code</c> compiled and reviewed cleanly
/// while throwing at runtime, which silently broke POS barcode scanning, product saving, product
/// search, the audit trail, and the data quality page at the same time. Any query shape that cannot
/// reach the database fails here instead of in front of a cashier.
/// </remarks>
public class QueryTranslationSweepTests
{
    /// <summary>
    /// Guards against the reflection discovery silently finding nothing. If a rename or refactor
    /// stops surfacing the query types, the sweep must fail rather than vacuously pass.
    /// </summary>
    private const int MinimumExpectedTypes = 10;

    private const int MinimumExpectedCalls = 60;

    private static readonly HashSet<string> MutatingMethods = new(StringComparer.Ordinal)
    {
        "AddAsync",
        "AddRangeAsync",
        "SaveChangesAsync",
        "BeginTransactionAsync",
        "CommitAsync",
        "RollbackAsync"
    };

    [Fact]
    public async Task EveryRepositoryAndReportingQuery_TranslatesToSql()
    {
        await using var fixture = await SweepFixture.CreateAsync();

        var targets = DiscoverQueryTypes(fixture.Context);
        Assert.True(
            targets.Count >= MinimumExpectedTypes,
            $"Query type discovery found only {targets.Count} type(s); expected at least {MinimumExpectedTypes}. " +
            "The reflection filter is probably stale.");

        var failures = new List<string>();
        var executed = 0;

        foreach (var (name, instance) in targets)
        {
            foreach (var method in QueryMethods(instance))
            {
                // Several predicates are only added when a search term is supplied, so the empty
                // and populated cases are different query shapes and both must be exercised.
                foreach (var searchTerm in SearchTermsFor(method))
                {
                    var outcome = await InvokeAsync(instance, method, searchTerm);
                    if (outcome is null)
                    {
                        continue;
                    }

                    executed++;
                    if (outcome.Length > 0)
                    {
                        failures.Add($"{name}.{method.Name}(search: {searchTerm ?? "none"}): {outcome}");
                    }
                }
            }
        }

        Assert.True(
            executed >= MinimumExpectedCalls,
            $"Only {executed} query call(s) executed; expected at least {MinimumExpectedCalls}.");

        Assert.True(
            failures.Count == 0,
            $"{failures.Count} query/queries cannot be translated to SQL:{Environment.NewLine}" +
            string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// Every repository and reporting service is constructible from the context alone, which makes
    /// that constructor shape a reliable marker for "this type queries the database".
    /// </summary>
    private static List<(string Name, object Instance)> DiscoverQueryTypes(BookStoreDbContext context)
    {
        var assemblies = new[]
        {
            typeof(BookStoreDbContext).Assembly,
            typeof(BookStore.Reporting.Services.AuditTrailService).Assembly
        };

        var targets = new List<(string, object)>();
        foreach (var type in assemblies.SelectMany(assembly => assembly.GetTypes()))
        {
            if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
            {
                continue;
            }

            var constructor = type.GetConstructors().FirstOrDefault(candidate =>
                candidate.GetParameters() is { Length: 1 } parameters
                && parameters[0].ParameterType == typeof(BookStoreDbContext));

            if (constructor is null)
            {
                continue;
            }

            targets.Add((type.Name, constructor.Invoke([context])));
        }

        return [.. targets.OrderBy(target => target.Item1, StringComparer.Ordinal)];
    }

    private static IEnumerable<MethodInfo> QueryMethods(object instance) =>
        instance.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => !method.IsSpecialName
                             && method.DeclaringType != typeof(object)
                             && !method.IsGenericMethodDefinition
                             && typeof(Task).IsAssignableFrom(method.ReturnType)
                             && !MutatingMethods.Contains(method.Name))
            .OrderBy(method => method.Name, StringComparer.Ordinal);

    private static IEnumerable<string?> SearchTermsFor(MethodInfo method)
    {
        yield return null;

        var takesText = method.GetParameters().Any(parameter =>
            parameter.ParameterType == typeof(string) || !parameter.ParameterType.IsValueType);

        if (takesText)
        {
            yield return "abc";
        }
    }

    /// <summary>
    /// Runs one query. Returns null when the call could not be attempted, an empty string when the
    /// query reached the database, and a description when the query could not be translated.
    /// </summary>
    private static async Task<string?> InvokeAsync(object instance, MethodInfo method, string? searchTerm)
    {
        object?[] arguments;
        try
        {
            arguments = [.. method.GetParameters().Select(parameter => BuildArgument(parameter, searchTerm))];
        }
        catch
        {
            // A parameter shape this harness cannot synthesise is not a translation defect.
            return null;
        }

        try
        {
            if (method.Invoke(instance, arguments) is Task task)
            {
                await task;
            }

            return string.Empty;
        }
        catch (Exception exception)
        {
            var root = Unwrap(exception);
            return IsTranslationFailure(root) ? Describe(root) : string.Empty;
        }
    }

    private static bool IsTranslationFailure(Exception exception) =>
        (exception is InvalidOperationException && exception.Message.Contains("could not be translated", StringComparison.Ordinal))
        || (exception is NotSupportedException && exception.Message.Contains("SQLite", StringComparison.Ordinal))
        || exception is SqliteException;

    private static string Describe(Exception exception)
    {
        var relevant = exception.Message.Split("Either rewrite", StringSplitOptions.None)[0];
        var lines = relevant.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim());
        return string.Join(' ', lines);
    }

    private static Exception Unwrap(Exception exception) =>
        exception is TargetInvocationException { InnerException: { } inner }
            ? Unwrap(inner)
            : exception;

    private static object? BuildArgument(ParameterInfo parameter, string? searchTerm)
    {
        var type = parameter.ParameterType;

        if (type == typeof(CancellationToken))
        {
            return CancellationToken.None;
        }

        if (type == typeof(string))
        {
            return BuildStringArgument(parameter, searchTerm);
        }

        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return searchTerm is null ? null : BuildScalar(underlying);
        }

        if (type == typeof(Guid))
        {
            return Guid.NewGuid();
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0);
        }

        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        if (TryBuildCollection(type, out var collection))
        {
            return collection;
        }

        return parameter.HasDefaultValue ? parameter.DefaultValue : BuildQueryObject(type, searchTerm);
    }

    /// <summary>
    /// Identifier-style parameters need a plausible value even on the pass that leaves free-text
    /// search empty, otherwise lookups short-circuit before building a query.
    /// </summary>
    private static string? BuildStringArgument(ParameterInfo parameter, string? searchTerm)
    {
        var name = parameter.Name?.ToLowerInvariant() ?? string.Empty;

        if (name.Contains("barcode", StringComparison.Ordinal))
        {
            return "ABC123";
        }

        if (name.Contains("isbn", StringComparison.Ordinal))
        {
            return "9780000000002";
        }

        if (name.Contains("invoice", StringComparison.Ordinal))
        {
            return "INV-1";
        }

        if (name.Contains("username", StringComparison.Ordinal) || name.Contains("name", StringComparison.Ordinal))
        {
            return "probe";
        }

        if (name.Contains("key", StringComparison.Ordinal) || name.Contains("category", StringComparison.Ordinal))
        {
            return "probe";
        }

        return searchTerm ?? (parameter.HasDefaultValue ? parameter.DefaultValue as string : "abc");
    }

    private static object? BuildScalar(Type type)
    {
        if (type == typeof(Guid))
        {
            return Guid.NewGuid();
        }

        if (type == typeof(DateTimeOffset))
        {
            return DateTimeOffset.UtcNow.AddDays(-7);
        }

        if (type == typeof(DateTime))
        {
            return DateTime.UtcNow.AddDays(-7);
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0);
        }

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static bool TryBuildCollection(Type type, out object? collection)
    {
        collection = null;

        Type? element = null;
        if (type.IsArray)
        {
            element = type.GetElementType();
        }
        else if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
        {
            element = type.GetGenericArguments().FirstOrDefault();
        }

        if (element is null)
        {
            return false;
        }

        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element))!;
        list.Add(element == typeof(Guid) ? Guid.NewGuid() : BuildScalar(element));

        if (type.IsArray)
        {
            var array = Array.CreateInstance(element, list.Count);
            list.CopyTo(array, 0);
            collection = array;
            return true;
        }

        collection = type.IsAssignableFrom(list.GetType()) ? list : null;
        return collection is not null;
    }

    /// <summary>
    /// Builds a filter or query object and populates the members that switch on optional
    /// predicates, so the risky branches are actually compiled into SQL.
    /// </summary>
    private static object? BuildQueryObject(Type type, string? searchTerm)
    {
        object instance;
        var parameterless = type.GetConstructor(Type.EmptyTypes);
        if (parameterless is not null)
        {
            instance = parameterless.Invoke(null);
        }
        else
        {
            var constructor = type.GetConstructors().OrderBy(candidate => candidate.GetParameters().Length).FirstOrDefault();
            if (constructor is null)
            {
                return null;
            }

            var arguments = constructor.GetParameters()
                .Select(parameter => BuildArgument(parameter, searchTerm))
                .ToArray();
            instance = constructor.Invoke(arguments);
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
        {
            TrySetQueryMember(instance, property, searchTerm);
        }

        return instance;
    }

    private static void TrySetQueryMember(object instance, PropertyInfo property, string? searchTerm)
    {
        var name = property.Name.ToLowerInvariant();
        var type = property.PropertyType;

        object? value = null;
        if (type == typeof(string) && NameSuggestsText(name))
        {
            value = searchTerm ?? "abc";
        }
        else if (type == typeof(DateTimeOffset?) || type == typeof(DateTimeOffset))
        {
            value = NameSuggestsRangeStart(name) ? DateTimeOffset.UtcNow.AddDays(-30) : DateTimeOffset.UtcNow;
        }
        else if (type == typeof(bool?) && name.Contains("active", StringComparison.Ordinal))
        {
            value = true;
        }
        else if (type == typeof(Guid?))
        {
            value = Guid.NewGuid();
        }
        else if (type == typeof(int) && (name.Contains("pagesize", StringComparison.Ordinal) || name.Contains("maxissues", StringComparison.Ordinal)))
        {
            value = 25;
        }
        else if (type == typeof(int) && name.Contains("pagenumber", StringComparison.Ordinal))
        {
            value = 1;
        }

        if (value is null)
        {
            return;
        }

        try
        {
            property.SetValue(instance, value);
        }
        catch
        {
            // A property that rejects the synthesised value is not what this sweep verifies.
        }
    }

    private static bool NameSuggestsText(string name) =>
        name.Contains("search", StringComparison.Ordinal)
        || name.Contains("term", StringComparison.Ordinal)
        || name.Contains("text", StringComparison.Ordinal)
        || name.Contains("area", StringComparison.Ordinal);

    private static bool NameSuggestsRangeStart(string name) =>
        name.Contains("from", StringComparison.Ordinal) || name.Contains("start", StringComparison.Ordinal);

    /// <summary>
    /// Builds the schema by applying the migrations rather than from the model, so drift between
    /// the two also fails this test.
    /// </summary>
    private sealed class SweepFixture : IAsyncDisposable
    {
        private SweepFixture(SqliteConnection connection, BookStoreDbContext context)
        {
            Connection = connection;
            Context = context;
        }

        private SqliteConnection Connection { get; }

        public BookStoreDbContext Context { get; }

        public static async Task<SweepFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<BookStoreDbContext>().UseSqlite(connection).Options;
            var context = new BookStoreDbContext(options);
            await context.Database.MigrateAsync();
            return new SweepFixture(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
