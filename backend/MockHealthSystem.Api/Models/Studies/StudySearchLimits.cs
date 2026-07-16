namespace MockHealthSystem.Api.Models.Studies;

/// <summary>Centralizes study list paging limits so boundaries are unit-tested without huge DB seeds.</summary>
public static class StudySearchLimits
{
    public const int DefaultLimit = 100;
    public const int MaxLimit = 5000;

    public static int ClampLimit(int? limit) => limit switch
    {
        null => DefaultLimit,
        <= 0 => DefaultLimit,
        > MaxLimit => MaxLimit,
        var n => n.Value
    };
}
