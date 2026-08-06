using System.ComponentModel.DataAnnotations;

namespace ConsultorioDental.API.Models;

/// <summary>
/// Bloque de disponibilidad de un dentista en un día de la semana.
/// Un dentista puede tener varios bloques por día (mañana y tarde, por ejemplo).
/// </summary>
public class HorarioDentista
{
    public int Id { get; set; }

    public int DentistaId { get; set; }
    public Dentista? Dentista { get; set; }

    /// <summary>Domingo = 0 ... Sábado = 6 (coincide con System.DayOfWeek).</summary>
    public DayOfWeek DiaSemana { get; set; }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public bool Activo { get; set; } = true;

    [MaxLength(200)]
    public string? Observacion { get; set; }
}
