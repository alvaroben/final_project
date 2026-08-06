using System.ComponentModel.DataAnnotations;

namespace ConsultorioDental.API.Models;

public class Paciente
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Apellido { get; set; } = string.Empty;

    /// <summary>Documento de identidad. Único.</summary>
    [Required, MaxLength(20)]
    public string Documento { get; set; } = string.Empty;

    public DateOnly FechaNacimiento { get; set; }

    [Required, MaxLength(20)]
    public string Telefono { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Correo { get; set; }

    [MaxLength(250)]
    public string? Direccion { get; set; }

    [MaxLength(500)]
    public string? Alergias { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}
