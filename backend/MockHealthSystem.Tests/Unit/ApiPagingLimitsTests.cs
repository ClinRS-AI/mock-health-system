using MockHealthSystem.Api.Models.Patients;
using MockHealthSystem.Api.Monitoring;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

public sealed class ApiPagingLimitsTests
{
    [Theory]
    [InlineData(null, MonitoringRequestListLimits.DefaultTake)]
    [InlineData(0, MonitoringRequestListLimits.DefaultTake)]
    [InlineData(-1, MonitoringRequestListLimits.DefaultTake)]
    [InlineData(50, 50)]
    [InlineData(500, 500)]
    [InlineData(501, 500)]
    [InlineData(9999, 500)]
    public void MonitoringClampTake_MatchesContract(int? take, int expected) =>
        Assert.Equal(expected, MonitoringRequestListLimits.ClampTake(take));

    [Theory]
    [InlineData(null, PatientSearchLimits.DefaultLimit)]
    [InlineData(0, PatientSearchLimits.DefaultLimit)]
    [InlineData(-10, PatientSearchLimits.DefaultLimit)]
    [InlineData(1, 1)]
    [InlineData(5000, 5000)]
    [InlineData(5001, 5000)]
    [InlineData(int.MaxValue, 5000)]
    public void PatientSearchClampLimit_MatchesContract(int? limit, int expected) =>
        Assert.Equal(expected, PatientSearchLimits.ClampLimit(limit));
}
