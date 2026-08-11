namespace BookStore.Application.Features.Reports.Responses;

public sealed class ReportResponse<TReport>
{
    public TReport? Report { get; init; }
    public string? Message { get; init; }
}
