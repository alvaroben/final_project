using System.ComponentModel.DataAnnotations;

namespace ConsultorioDental.API.Models;

/// <summary>Razón principal por la cual se agenda la cita.</summary>
public class Motivo
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descripcion { get; set; }

    /// <summary>1 = Baja, 2 = Media, 3 = Alta. Permite priorizar la agenda.</summary>
    public int Prioridad { get; set; } = 1;

    public bool Activo { get; set; } = true;

    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}
