using System.ComponentModel.DataAnnotations;

namespace ConsultorioDental.API.Models;

/// <summary>Área, sala o unidad donde se realiza la consulta.</summary>
public class Consultorio
{
    public int Id { get; set; }

    /// <summary>Código corto de la sala. Único.</summary>
    [Required, MaxLength(20)]
    public string Codigo { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Ubicacion { get; set; }

    /// <summary>Sillones o unidades dentales disponibles en el área.</summary>
    public int Capacidad { get; set; } = 1;

    public bool Activo { get; set; } = true;

    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}
