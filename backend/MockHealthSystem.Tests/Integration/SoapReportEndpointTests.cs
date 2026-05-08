using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

[Collection("EnvironmentMutating")]
public sealed class SoapReportEndpointTests : IClassFixture<MockHealthSystemWebApplicationFactory>
{
    private readonly MockHealthSystemWebApplicationFactory _factory;

    public SoapReportEndpointTests(MockHealthSystemWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Wsdl_IsReachable_AndContains_RunReportContract()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/soap/report?wsdl");

        response.EnsureSuccessStatusCode();
        var wsdl = await response.Content.ReadAsStringAsync();

        Assert.Contains("ReportSoapService", wsdl, StringComparison.Ordinal);
        Assert.Contains("RunReport", wsdl, StringComparison.Ordinal);
        Assert.Contains("soap:address", wsdl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunReport_Succeeds_WithValidPasswordAndPkey()
    {
        using var _ = new EnvironmentVariableScope("SOAP_REPORT_PASSWORD", "soap-secret");
        await SeedReportDefinitionAsync("PATIENTS_ONE", "SELECT 1 AS \"One\", 'ok' AS \"Message\"");

        var client = _factory.CreateClient();
        var requestXml = BuildSoapRequest("soap-secret", "PATIENTS_ONE");
        var response = await client.PostAsync("/soap/report", new StringContent(requestXml, Encoding.UTF8, "text/xml"));

        response.EnsureSuccessStatusCode();
        var xml = await response.Content.ReadAsStringAsync();
        Assert.Contains("<Column>One</Column>", xml, StringComparison.Ordinal);
        Assert.Contains("<Column>Message</Column>", xml, StringComparison.Ordinal);
        Assert.Contains("<Value>1</Value>", xml, StringComparison.Ordinal);
        Assert.Contains("<Value>ok</Value>", xml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunReport_Fails_WhenPasswordIsInvalid()
    {
        using var _ = new EnvironmentVariableScope("SOAP_REPORT_PASSWORD", "soap-secret");
        await SeedReportDefinitionAsync("PATIENTS_ONE", "SELECT 1 AS \"One\"");

        var client = _factory.CreateClient();
        var requestXml = BuildSoapRequest("wrong-password", "PATIENTS_ONE");
        var response = await client.PostAsync("/soap/report", new StringContent(requestXml, Encoding.UTF8, "text/xml"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var xml = await response.Content.ReadAsStringAsync();
        Assert.Contains("<faultcode>Client.Authentication</faultcode>", xml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunReport_Fails_WhenPkeyIsUnknown()
    {
        using var _ = new EnvironmentVariableScope("SOAP_REPORT_PASSWORD", "soap-secret");
        await SeedReportDefinitionAsync("KNOWN_REPORT", "SELECT 1 AS \"One\"");

        var client = _factory.CreateClient();
        var requestXml = BuildSoapRequest("soap-secret", "UNKNOWN_REPORT");
        var response = await client.PostAsync("/soap/report", new StringContent(requestXml, Encoding.UTF8, "text/xml"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var xml = await response.Content.ReadAsStringAsync();
        Assert.Contains("<faultcode>Client.Report</faultcode>", xml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunReport_Fails_WhenQueryIsNotSelect()
    {
        using var _ = new EnvironmentVariableScope("SOAP_REPORT_PASSWORD", "soap-secret");
        await SeedReportDefinitionAsync("INVALID_SQL", "UPDATE \"Patients\" SET \"Status\" = 'Inactive'");

        var client = _factory.CreateClient();
        var requestXml = BuildSoapRequest("soap-secret", "INVALID_SQL");
        var response = await client.PostAsync("/soap/report", new StringContent(requestXml, Encoding.UTF8, "text/xml"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var xml = await response.Content.ReadAsStringAsync();
        Assert.Contains("<faultcode>Client.Validation</faultcode>", xml, StringComparison.Ordinal);
        Assert.Contains("Only SELECT queries are allowed.", xml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunReport_AuditRecent_ReturnsExpectedAuditColumnsAndValues()
    {
        using var _ = new EnvironmentVariableScope("SOAP_REPORT_PASSWORD", "soap-secret");
        await SeedAuditReportDataAsync();

        var client = _factory.CreateClient();
        var requestXml = BuildSoapRequest("soap-secret", "AUDIT_RECENT");
        var response = await client.PostAsync("/soap/report", new StringContent(requestXml, Encoding.UTF8, "text/xml"));

        response.EnsureSuccessStatusCode();
        var xml = await response.Content.ReadAsStringAsync();

        Assert.Contains("<Column>AuditPKey</Column>", xml, StringComparison.Ordinal);
        Assert.Contains("<Column>AuditTypeCode</Column>", xml, StringComparison.Ordinal);
        Assert.Contains("<Column>AuditType</Column>", xml, StringComparison.Ordinal);
        Assert.Contains("<Column>StaffName</Column>", xml, StringComparison.Ordinal);
        Assert.Contains("<Value>LOGIN</Value>", xml, StringComparison.Ordinal);
        Assert.Contains("<Value>Login</Value>", xml, StringComparison.Ordinal);
        Assert.Contains("<Value>Alex Morgan</Value>", xml, StringComparison.Ordinal);
    }

    private async Task SeedReportDefinitionAsync(string pkey, string sqlQuery)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = await db.ReportQueryDefinitions.ToListAsync();
        if (existing.Count > 0)
        {
            db.ReportQueryDefinitions.RemoveRange(existing);
            await db.SaveChangesAsync();
        }

        db.ReportQueryDefinitions.Add(new ReportQueryDefinition
        {
            PKey = pkey,
            SqlQuery = sqlQuery,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedAuditReportDataAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.ReportQueryDefinitions.RemoveRange(await db.ReportQueryDefinitions.ToListAsync());
        db.AuditLogs.RemoveRange(await db.AuditLogs.ToListAsync());
        db.AuditEntryTypes.RemoveRange(await db.AuditEntryTypes.ToListAsync());
        db.Staff.RemoveRange(await db.Staff.ToListAsync());
        await db.SaveChangesAsync();

        db.AuditEntryTypes.Add(new AuditEntryType
        {
            Id = 11,
            Code = "LOGIN",
            DisplayName = "Login"
        });

        db.Staff.Add(new Staff
        {
            Id = 21,
            FirstName = "Alex",
            LastName = "Morgan",
            IsActive = true
        });

        db.AuditLogs.Add(new AuditLog
        {
            Id = 31,
            StaffPKey = 21,
            AuditEntryTypeId = 11,
            CreatedByUser = "alex.morgan",
            CreatedTimeUtc = DateTime.UtcNow,
            StudyPKey = "STUDY-100",
            Details = "Staff login"
        });

        db.ReportQueryDefinitions.Add(new ReportQueryDefinition
        {
            PKey = "AUDIT_RECENT",
            SqlQuery = "SELECT l.\"Id\" AS \"AuditPKey\", t.\"Code\" AS \"AuditTypeCode\", t.\"DisplayName\" AS \"AuditType\", s.\"FirstName\" || ' ' || s.\"LastName\" AS \"StaffName\" FROM \"AuditLogs\" AS l INNER JOIN \"AuditEntryTypes\" AS t ON l.\"AuditEntryTypeId\" = t.\"Id\" LEFT JOIN \"Staff\" AS s ON l.\"StaffPKey\" = s.\"Id\" ORDER BY l.\"CreatedTimeUtc\" DESC",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static string BuildSoapRequest(string password, string pkey)
    {
        return $"""
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" xmlns:rep="urn:mockhealthsystem:soap:report:v1">
  <soap:Body>
    <rep:RunReport>
      <rep:password>{password}</rep:password>
      <rep:pkey>{pkey}</rep:pkey>
    </rep:RunReport>
  </soap:Body>
</soap:Envelope>
""";
    }
}
