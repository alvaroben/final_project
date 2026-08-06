using System.ComponentModel.DataAnnotations;

namespace ConsultorioDental.API.Models;

public class Dentista
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Apellido { get; set; } = string.Empty;

    /// <summary>Número de licencia profesional (exequátur). Único.</summary>
    [Required, MaxLength(30)]
    public string NumeroLicencia { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Telefono { get; set; } = string.Empty;

    public int EspecialidadId { get; set; }
    public Especialidad? Especialidad { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<HorarioDentista> Horarios { get; set; } = new List<HorarioDentista>();
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}
