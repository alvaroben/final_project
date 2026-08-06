using System.ComponentModel.DataAnnotations;

namespace ConsultorioDental.API.Models;

/// <summary>Tipo de servicio dental ofrecido por el consultorio.</summary>
public class Servicio
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descripcion { get; set; }

    /// <summary>Precio base del servicio.</summary>
    public decimal Precio { get; set; }

    /// <summary>Duración sugerida en minutos; se usa cuando la cita no especifica una.</summary>
    public int DuracionMinutos { get; set; } = 30;

    public bool Activo { get; set; } = true;

    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}
