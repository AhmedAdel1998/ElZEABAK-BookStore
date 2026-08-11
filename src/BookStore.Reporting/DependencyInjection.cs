using Microsoft.Extensions.DependencyInjection;
using BookStore.Application.Features.Reports.Services;
using BookStore.Reporting.Services;

namespace BookStore.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddReporting(this IServiceCollection services)
    {
        services.AddScoped<IReportQueryService, BookStoreReportQueryService>();
        return services;
    }
}
