namespace HRIA.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public string Position { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Usuario de acceso asociado (1:0..1).
    public User? User { get; set; }

    public ICollection<Workday> Workdays { get; set; } = new List<Workday>();
    public ICollection<ScheduleAssignment> ScheduleAssignments { get; set; } = new List<ScheduleAssignment>();
    public ICollection<AbsenceRequest> AbsenceRequests { get; set; } = new List<AbsenceRequest>();
    public ICollection<VacationAllowance> VacationAllowances { get; set; } = new List<VacationAllowance>();

    public string FullName => $"{FirstName} {LastName}";
}
