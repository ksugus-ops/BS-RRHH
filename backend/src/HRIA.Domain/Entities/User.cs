using HRIA.Domain.Enums;

namespace HRIA.Domain.Entities;

public class User
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Imagen de perfil del usuario. Si es nula, la interfaz genera un avatar
    /// con las iniciales y un color derivado del nombre.
    /// </summary>
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
