using MockHealthSystem.Api.Services;

namespace MockHealthSystem.Api.Soap;

public sealed class ReportSoapService : IReportSoapService
{
    private readonly IReportExecutionService _reportExecutionService;

    public ReportSoapService(IReportExecutionService reportExecutionService)
    {
        _reportExecutionService = reportExecutionService;
    }

    public async Task<RunReportSoapResponse> RunReportAsync(
        RunReportSoapRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportExecutionService.ExecuteAsync(request.Password, request.PKey, cancellationToken);
        var response = new RunReportSoapResponse
        {
            Columns = result.Columns.ToList()
        };

        foreach (var row in result.Rows)
        {
            response.Rows.Add(new ReportSoapRow
            {
                Values = row.ToList()
            });
        }

        return response;
    }
}
