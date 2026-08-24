using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.Application.Tests;

public class ArchitectureTests
{
    [Fact]
    public void ApplicationProject_ExposesDependencyInjectionRegistration()
    {
        Assert.NotNull(typeof(BookStore.Application.DependencyInjection));
    }

    /// <summary>
    /// Every handler must be registered. A handler that exists but is not in the container only fails
    /// when a user opens the screen that needs it, which is far too late to find out.
    /// </summary>
    [Fact]
    public void EveryApplicationHandler_IsRegisteredInTheContainer()
    {
        var services = new ServiceCollection().AddApplication();
        var registered = services.Select(descriptor => descriptor.ServiceType).ToHashSet();

        var missing = typeof(BookStore.Application.DependencyInjection).Assembly
            .GetExportedTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(type => type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Where(type => !registered.Contains(type))
            .Select(type => type.FullName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(missing);
    }

    /// <summary>
    /// Every dependency a handler asks for must come from somewhere: the application container, or an
    /// abstraction that the infrastructure and persistence layers implement.
    /// </summary>
    [Fact]
    public void EveryApplicationHandlerDependency_IsSatisfiable()
    {
        var services = new ServiceCollection().AddApplication();
        var registered = services.Select(descriptor => descriptor.ServiceType).ToHashSet();

        var unsatisfied = new List<string>();
        foreach (var handler in typeof(BookStore.Application.DependencyInjection).Assembly
            .GetExportedTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(type => type.Name.EndsWith("Handler", StringComparison.Ordinal)))
        {
            foreach (var parameter in handler.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(constructor => constructor.GetParameters()))
            {
                if (IsSatisfiable(parameter.ParameterType, registered))
                {
                    continue;
                }

                unsatisfied.Add($"{handler.Name}({parameter.ParameterType.Name} {parameter.Name})");
            }
        }

        Assert.Empty(unsatisfied);
    }

    private static bool IsSatisfiable(Type parameterType, IReadOnlySet<Type> registered)
    {
        if (registered.Contains(parameterType))
        {
            return true;
        }

        // Open generics such as IValidator<T> and ILogger<T> are registered by definition, not by
        // closed type.
        if (parameterType.IsGenericType && registered.Contains(parameterType.GetGenericTypeDefinition()))
        {
            return true;
        }

        if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Microsoft.Extensions.Logging.ILogger<>))
        {
            return true;
        }

        if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(FluentValidation.IValidator<>))
        {
            return true;
        }

        // Contracts the outer layers implement: repositories, unit of work, clocks, printers, stores.
        var declaringAssembly = parameterType.Assembly.GetName().Name;
        return parameterType.IsInterface
            && declaringAssembly is "BookStore.Application" or "BookStore.Domain" or "BookStore.Shared";
    }
}
