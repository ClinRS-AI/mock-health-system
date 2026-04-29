namespace MockHealthSystem.Api.Soap;

public interface IReportSoapService
{
    Task<RunReportSoapResponse> RunReportAsync(RunReportSoapRequest request, CancellationToken cancellationToken = default);
}
