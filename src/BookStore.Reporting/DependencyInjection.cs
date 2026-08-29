using Microsoft.Extensions.DependencyInjection;
using BookStore.Application.Features.Reports.Services;
using BookStore.Application.Features.Audit.Services;
using BookStore.Application.Features.DataQuality.Services;
using BookStore.Reporting.Services;

namespace BookStore.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddReporting(this IServiceCollection services)
    {
        services.AddScoped<IReportQueryService, BookStoreReportQueryService>();
        services.AddScoped<IAuditTrailService, AuditTrailService>();
        services.AddScoped<IDataQualityService, DataQualityService>();
        services.AddScoped<IReportExporter, ReportExporter>();
        return services;
    }
}
