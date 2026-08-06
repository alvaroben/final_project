using System.ComponentModel.DataAnnotations;

namespace ConsultorioDental.API.Models;

/// <summary>Roles disponibles en el sistema.</summary>
public enum RolUsuario
{
    Administrador = 1,
    Recepcionista = 2,
    Dentista = 3
}

public class Usuario
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public RolUsuario Rol { get; set; } = RolUsuario.Recepcionista;

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public DateTime? UltimoAcceso { get; set; }
}
