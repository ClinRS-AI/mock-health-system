# API Contracts: SOAP Report Endpoint

**Base URL**: `http://<host>/soap/report`
**Protocol**: SOAP 1.1 over HTTP POST
**Content-Type**: `text/xml; charset=utf-8`
**SOAPAction**: `RunReport`

---

## WSDL

### `GET /soap/report?wsdl`

Returns the WSDL document describing the report service.

**Auth required**: No (anonymous)
**Response** `200 text/xml`: WSDL document

---

## RunReport Operation

### `POST /soap/report`

Execute a named SQL report. The report must be registered as a `ReportQueryDefinition`
in the database (via `GET /test-data/soap/report-pkeys`).

**Auth required**: Password in SOAP envelope (see below)

**Request envelope**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
               xmlns:rep="http://clinrs.com/report">
  <soap:Body>
    <rep:RunReport>
      <rep:Password>your-soap-report-password</rep:Password>
      <rep:PKey>PatientReport</rep:PKey>
    </rep:RunReport>
  </soap:Body>
</soap:Envelope>
```

- `Password`: Must match `SOAP_REPORT_PASSWORD` environment variable
- `PKey`: Must match a registered `ReportQueryDefinition.PKey`

**Successful response envelope**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <RunReportResponse>
      <RunReportResult>
        <Columns>
          <Column>id</Column>
          <Column>firstName</Column>
          <Column>lastName</Column>
        </Columns>
        <Rows>
          <Row>
            <Value>42</Value>
            <Value>Jane</Value>
            <Value>Smith</Value>
          </Row>
        </Rows>
      </RunReportResult>
    </RunReportResponse>
  </soap:Body>
</soap:Envelope>
```

**SOAP fault — invalid password**:
```xml
<soap:Fault>
  <faultcode>Client</faultcode>
  <faultstring>Invalid password</faultstring>
</soap:Fault>
```

**SOAP fault — unknown PKey**:
```xml
<soap:Fault>
  <faultcode>Client</faultcode>
  <faultstring>Report not found: UnknownReport</faultstring>
</soap:Fault>
```

**SOAP fault — SQL execution error**:
```xml
<soap:Fault>
  <faultcode>Server</faultcode>
  <faultstring>Report execution failed</faultstring>
</soap:Fault>
```

---

## Configuration

The SOAP password and available reports are configured via:

| Setting | Source | Description |
|---------|--------|-------------|
| `SOAP_REPORT_PASSWORD` | Environment variable | Required; no default |
| Report PKeys | Database (`ReportQueryDefinition`) | Populated via EF migration or test data tools |

To list available report PKeys without executing them, use the admin REST endpoint:
`GET /api/v1/test-data/soap/report-pkeys`
