namespace BookStore.Application.Features.Reports.Services;

public enum ReportExportFormat
{
    Csv,
    Excel
}

public sealed record ReportExportRequest<TReport>(string ReportName, IReadOnlyCollection<TReport> Rows, ReportExportFormat Format);

public sealed record ReportExportResult(string FileName, string ContentType, byte[] Content);

public interface IReportExporter
{
    Task<ReportExportResult> ExportAsync<TReport>(ReportExportRequest<TReport> request, CancellationToken cancellationToken = default);
}

public interface IReportChartDataService
{
    Task<IReadOnlyCollection<TPoint>> BuildSeriesAsync<TPoint>(IReadOnlyCollection<TPoint> points, CancellationToken cancellationToken = default);
}
