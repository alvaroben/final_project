using System.ComponentModel.DataAnnotations;

namespace ConsultorioDental.API.Models;

public class Especialidad
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descripcion { get; set; }

    public bool Activa { get; set; } = true;

    public ICollection<Dentista> Dentistas { get; set; } = new List<Dentista>();
}
