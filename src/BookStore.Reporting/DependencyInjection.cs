using Microsoft.Extensions.DependencyInjection;

namespace BookStore.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddReporting(this IServiceCollection services)
    {
        return services;
    }
}
