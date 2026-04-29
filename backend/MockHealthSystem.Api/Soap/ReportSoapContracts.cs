using System.Xml.Serialization;

namespace MockHealthSystem.Api.Soap;

public static class ReportSoapConstants
{
    public const string ServiceNamespace = "urn:mockhealthsystem:soap:report:v1";
    public const string SoapEnvelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
}

[XmlRoot("RunReport", Namespace = ReportSoapConstants.ServiceNamespace)]
public sealed class RunReportSoapRequest
{
    [XmlElement("password")]
    public string Password { get; set; } = string.Empty;

    [XmlElement("pkey")]
    public string PKey { get; set; } = string.Empty;
}

[XmlRoot("RunReportResponse", Namespace = ReportSoapConstants.ServiceNamespace)]
public sealed class RunReportSoapResponse
{
    [XmlArray("Columns")]
    [XmlArrayItem("Column")]
    public List<string> Columns { get; set; } = [];

    [XmlArray("Rows")]
    [XmlArrayItem("Row")]
    public List<ReportSoapRow> Rows { get; set; } = [];
}

public sealed class ReportSoapRow
{
    [XmlArray("Values")]
    [XmlArrayItem("Value")]
    public List<string> Values { get; set; } = [];
}
