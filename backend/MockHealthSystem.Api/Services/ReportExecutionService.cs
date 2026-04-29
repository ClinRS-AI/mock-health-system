using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Infrastructure.Data;

namespace MockHealthSystem.Api.Services;

public interface IReportExecutionService
{
    Task<ReportExecutionResult> ExecuteAsync(string password, string pkey, CancellationToken cancellationToken = default);
}

public sealed class ReportExecutionResult
{
    public required IReadOnlyList<string> Columns { get; init; }
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
}

public sealed class InvalidReportPasswordException : Exception
{
    public InvalidReportPasswordException()
        : base("Invalid SOAP report password.")
    {
    }
}

public sealed class ReportPKeyNotFoundException : Exception
{
    public ReportPKeyNotFoundException(string pkey)
        : base($"No SQL report is configured for pkey '{pkey}'.")
    {
    }
}

public sealed class ReportQueryValidationException : Exception
{
    public ReportQueryValidationException(string message)
        : base(message)
    {
    }
}

public sealed class ReportExecutionService : IReportExecutionService
{
    private static readonly Regex DisallowedSqlTokensRegex = new(
        @"\b(insert|update|delete|drop|alter|truncate|create|grant|revoke|merge|call|execute|exec|copy|vacuum|analyze)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public ReportExecutionService(AppDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<ReportExecutionResult> ExecuteAsync(string password, string pkey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidReportPasswordException();
        }

        if (string.IsNullOrWhiteSpace(pkey))
        {
            throw new ReportQueryValidationException("pkey is required.");
        }

        ValidatePassword(password);

        var normalizedPkey = pkey.Trim();
        var queryDefinition = await _dbContext.ReportQueryDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PKey == normalizedPkey, cancellationToken);

        if (queryDefinition is null)
        {
            throw new ReportPKeyNotFoundException(normalizedPkey);
        }

        EnsureSelectOnly(queryDefinition.SqlQuery);

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = queryDefinition.SqlQuery;
        command.CommandType = System.Data.CommandType.Text;
        command.CommandTimeout = 30;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columns = Enumerable.Range(0, reader.FieldCount)
            .Select(reader.GetName)
            .ToList();

        var rows = new List<IReadOnlyList<string>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var values = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (reader.IsDBNull(i))
                {
                    values[i] = string.Empty;
                    continue;
                }

                var value = reader.GetValue(i);
                values[i] = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }

            rows.Add(values);
        }

        return new ReportExecutionResult
        {
            Columns = columns,
            Rows = rows
        };
    }

    private void ValidatePassword(string providedPassword)
    {
        var configuredPassword = _configuration["SOAP_REPORT_PASSWORD"];
        if (string.IsNullOrWhiteSpace(configuredPassword))
        {
            throw new ReportQueryValidationException("SOAP report password is not configured.");
        }

        var providedBytes = Encoding.UTF8.GetBytes(providedPassword);
        var configuredBytes = Encoding.UTF8.GetBytes(configuredPassword);
        var isValid = CryptographicOperations.FixedTimeEquals(providedBytes, configuredBytes);
        if (!isValid)
        {
            throw new InvalidReportPasswordException();
        }
    }

    private static void EnsureSelectOnly(string sqlQuery)
    {
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            throw new ReportQueryValidationException("Configured SQL query is empty.");
        }

        var trimmed = sqlQuery.Trim();
        var normalized = trimmed.TrimEnd();
        if (normalized.EndsWith(';'))
        {
            normalized = normalized[..^1].TrimEnd();
        }

        if (normalized.Contains(';'))
        {
            throw new ReportQueryValidationException("Only single SELECT statements are allowed.");
        }

        if (!normalized.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            throw new ReportQueryValidationException("Only SELECT/CTE queries are allowed.");
        }

        if (DisallowedSqlTokensRegex.IsMatch(normalized))
        {
            throw new ReportQueryValidationException("Only SELECT queries are allowed.");
        }
    }
}
