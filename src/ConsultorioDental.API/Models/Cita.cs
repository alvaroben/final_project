using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsultorioDental.API.Models;

/// <summary>Estado de la cita, siempre calculado a partir de fecha, hora y duración.</summary>
public enum EstadoCita
{
    Vigente = 1,
    EnProceso = 2,
    Finalizada = 3,
    Cancelada = 4
}

/// <summary>Entidad principal: cita del consultorio dental.</summary>
public class Cita
{
    public int Id { get; set; }

    public int PacienteId { get; set; }
    public Paciente? Paciente { get; set; }

    public DateOnly Fecha { get; set; }

    public TimeOnly Hora { get; set; }

    /// <summary>Tiempo estimado de la consulta en minutos.</summary>
    public int DuracionMinutos { get; set; }

    public int DentistaId { get; set; }
    public Dentista? Dentista { get; set; }

    public int MotivoId { get; set; }
    public Motivo? Motivo { get; set; }

    public int ServicioId { get; set; }
    public Servicio? Servicio { get; set; }

    public int ConsultorioId { get; set; }
    public Consultorio? Consultorio { get; set; }

    /// <summary>Precio congelado al momento de agendar, para que un cambio de tarifa no altere el historial.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostoEstimado { get; set; }

    [MaxLength(500)]
    public string? Notas { get; set; }

    public bool Cancelada { get; set; }

    [MaxLength(300)]
    public string? MotivoCancelacion { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    // ----- Campos calculados (no se persisten) -----

    [NotMapped]
    public DateTime FechaHoraInicio => Fecha.ToDateTime(Hora);

    [NotMapped]
    public DateTime FechaHoraFin => FechaHoraInicio.AddMinutes(DuracionMinutos);

    /// <summary>Estado calculado según el momento indicado (normalmente DateTime.Now).</summary>
    public EstadoCita CalcularEstado(DateTime referencia)
    {
        if (Cancelada) return EstadoCita.Cancelada;
        if (referencia < FechaHoraInicio) return EstadoCita.Vigente;
        return referencia < FechaHoraFin ? EstadoCita.EnProceso : EstadoCita.Finalizada;
    }
}
