using System.Text;
using System.Xml.Linq;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Api.Soap;

namespace MockHealthSystem.Api.Controllers;

[ApiController]
[ApiVersionNeutral]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("soap/report")]
public sealed class ReportSoapController : ControllerBase
{
    private readonly IReportSoapService _reportSoapService;

    public ReportSoapController(IReportSoapService reportSoapService)
    {
        _reportSoapService = reportSoapService;
    }

    [HttpGet]
    public IActionResult GetWsdl([FromQuery(Name = "wsdl")] string? wsdl)
    {
        var hasWsdlFlag = Request.Query.ContainsKey("wsdl");
        var hasWsdlValue = wsdl is not null;
        if (!hasWsdlFlag && !hasWsdlValue)
        {
            return NotFound();
        }

        var serviceLocation = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/soap/report";
        var content = BuildWsdl(serviceLocation);
        return Content(content, "text/xml", Encoding.UTF8);
    }

    [HttpPost]
    [Consumes("text/xml", "application/soap+xml")]
    public async Task<IActionResult> RunReport(CancellationToken cancellationToken)
    {
        try
        {
            var requestBody = await ReadRequestBodyAsync(cancellationToken);
            var request = ParseSoapRequest(requestBody);
            var response = await _reportSoapService.RunReportAsync(request, cancellationToken);

            var envelope = BuildSuccessEnvelope(response);
            return Content(envelope, "text/xml", Encoding.UTF8);
        }
        catch (InvalidReportPasswordException ex)
        {
            return SoapFault("Client.Authentication", ex.Message);
        }
        catch (ReportPKeyNotFoundException ex)
        {
            return SoapFault("Client.Report", ex.Message);
        }
        catch (ReportQueryValidationException ex)
        {
            return SoapFault("Client.Validation", ex.Message);
        }
        catch (Exception ex)
        {
            return SoapFault("Server", $"Report execution failed: {ex.Message}");
        }
    }

    private async Task<string> ReadRequestBodyAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static RunReportSoapRequest ParseSoapRequest(string xml)
    {
        var document = XDocument.Parse(xml);
        XNamespace soapNs = ReportSoapConstants.SoapEnvelopeNamespace;
        XNamespace serviceNs = ReportSoapConstants.ServiceNamespace;

        var bodyElement = document.Root?.Element(soapNs + "Body");
        var runReportElement = bodyElement?.Element(serviceNs + "RunReport");
        if (runReportElement is null)
        {
            throw new ReportQueryValidationException("SOAP body is missing RunReport.");
        }

        var password = runReportElement.Element(serviceNs + "password")?.Value?.Trim() ?? string.Empty;
        var pkey = runReportElement.Element(serviceNs + "pkey")?.Value?.Trim() ?? string.Empty;

        return new RunReportSoapRequest
        {
            Password = password,
            PKey = pkey
        };
    }

    private static string BuildSuccessEnvelope(RunReportSoapResponse response)
    {
        XNamespace soapNs = ReportSoapConstants.SoapEnvelopeNamespace;
        XNamespace serviceNs = ReportSoapConstants.ServiceNamespace;

        var envelope = new XDocument(
            new XElement(soapNs + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", soapNs),
                new XElement(soapNs + "Body",
                    new XElement(serviceNs + "RunReportResponse",
                        new XElement(serviceNs + "Columns",
                            response.Columns.Select(c => new XElement(serviceNs + "Column", c))),
                        new XElement(serviceNs + "Rows",
                            response.Rows.Select(row =>
                                new XElement(serviceNs + "Row",
                                    new XElement(serviceNs + "Values",
                                        row.Values.Select(v => new XElement(serviceNs + "Value", v))))))))));

        return envelope.ToString(SaveOptions.DisableFormatting);
    }

    private ContentResult SoapFault(string code, string message)
    {
        XNamespace soapNs = ReportSoapConstants.SoapEnvelopeNamespace;

        var envelope = new XDocument(
            new XElement(soapNs + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", soapNs),
                new XElement(soapNs + "Body",
                    new XElement(soapNs + "Fault",
                        new XElement("faultcode", code),
                        new XElement("faultstring", message)))));

        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return Content(envelope.ToString(SaveOptions.DisableFormatting), "text/xml", Encoding.UTF8);
    }

    private static string BuildWsdl(string serviceLocation)
    {
        return $"""
<?xml version="1.0" encoding="utf-8"?>
<wsdl:definitions
    xmlns:wsdl="http://schemas.xmlsoap.org/wsdl/"
    xmlns:soap="http://schemas.xmlsoap.org/wsdl/soap/"
    xmlns:xsd="http://www.w3.org/2001/XMLSchema"
    xmlns:tns="{ReportSoapConstants.ServiceNamespace}"
    targetNamespace="{ReportSoapConstants.ServiceNamespace}">
  <wsdl:types>
    <xsd:schema targetNamespace="{ReportSoapConstants.ServiceNamespace}" elementFormDefault="qualified">
      <xsd:complexType name="RunReportRequestType">
        <xsd:sequence>
          <xsd:element name="password" type="xsd:string"/>
          <xsd:element name="pkey" type="xsd:string"/>
        </xsd:sequence>
      </xsd:complexType>
      <xsd:element name="RunReport" type="tns:RunReportRequestType"/>

      <xsd:complexType name="ReportRowType">
        <xsd:sequence>
          <xsd:element name="Values" minOccurs="0">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="Value" type="xsd:string" minOccurs="0" maxOccurs="unbounded"/>
              </xsd:sequence>
            </xsd:complexType>
          </xsd:element>
        </xsd:sequence>
      </xsd:complexType>

      <xsd:complexType name="RunReportResponseType">
        <xsd:sequence>
          <xsd:element name="Columns" minOccurs="0">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="Column" type="xsd:string" minOccurs="0" maxOccurs="unbounded"/>
              </xsd:sequence>
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="Rows" minOccurs="0">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="Row" type="tns:ReportRowType" minOccurs="0" maxOccurs="unbounded"/>
              </xsd:sequence>
            </xsd:complexType>
          </xsd:element>
        </xsd:sequence>
      </xsd:complexType>
      <xsd:element name="RunReportResponse" type="tns:RunReportResponseType"/>
    </xsd:schema>
  </wsdl:types>

  <wsdl:message name="RunReportSoapIn">
    <wsdl:part name="parameters" element="tns:RunReport"/>
  </wsdl:message>
  <wsdl:message name="RunReportSoapOut">
    <wsdl:part name="parameters" element="tns:RunReportResponse"/>
  </wsdl:message>

  <wsdl:portType name="ReportSoapPortType">
    <wsdl:operation name="RunReport">
      <wsdl:input message="tns:RunReportSoapIn"/>
      <wsdl:output message="tns:RunReportSoapOut"/>
    </wsdl:operation>
  </wsdl:portType>

  <wsdl:binding name="ReportSoapBinding" type="tns:ReportSoapPortType">
    <soap:binding transport="http://schemas.xmlsoap.org/soap/http" style="document"/>
    <wsdl:operation name="RunReport">
      <soap:operation soapAction="{ReportSoapConstants.ServiceNamespace}/RunReport" style="document"/>
      <wsdl:input>
        <soap:body use="literal"/>
      </wsdl:input>
      <wsdl:output>
        <soap:body use="literal"/>
      </wsdl:output>
    </wsdl:operation>
  </wsdl:binding>

  <wsdl:service name="ReportSoapService">
    <wsdl:port name="ReportSoapPort" binding="tns:ReportSoapBinding">
      <soap:address location="{serviceLocation}"/>
    </wsdl:port>
  </wsdl:service>
</wsdl:definitions>
""";
    }
}
