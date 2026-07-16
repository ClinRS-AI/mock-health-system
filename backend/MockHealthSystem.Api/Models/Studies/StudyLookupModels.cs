namespace MockHealthSystem.Api.Models.Studies;

public class StudyCategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class StudyCategoryEditModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class StudySubcategoryViewModel
{
    public int Id { get; set; }
    public int? StudyCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class StudySubcategoryEditModel
{
    public int? StudyCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class StudyTypeViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ForeColor { get; set; }
    public string? BackColor { get; set; }
}

public class StudyStatusTypeViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BackColor { get; set; }
    public bool IsActive { get; set; }
    public bool IsEnrollmentPermitted { get; set; }
    public string? StudyPhase { get; set; }
}

public class StudyGroupViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
