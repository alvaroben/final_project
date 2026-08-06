using System.ComponentModel.DataAnnotations;
using ConsultorioDental.API.Models;

namespace ConsultorioDental.API.DTOs;

public class GuardarCitaDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un paciente válido.")]
    public int PacienteId { get; set; }

    [Required(ErrorMessage = "La fecha de la cita es obligatoria.")]
    public DateOnly Fecha { get; set; }

    [Required(ErrorMessage = "La hora de la cita es obligatoria.")]
    public TimeOnly Hora { get; set; }

    /// <summary>Si se omite (0), se toma la duración sugerida del servicio.</summary>
    [Range(0, 480, ErrorMessage = "La duración debe estar entre 5 y 480 minutos (0 para usar la del servicio).")]
    public int DuracionMinutos { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un dentista válido.")]
    public int DentistaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un motivo válido.")]
    public int MotivoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un servicio válido.")]
    public int ServicioId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un consultorio válido.")]
    public int ConsultorioId { get; set; }

    [StringLength(500, ErrorMessage = "Las notas no pueden superar los 500 caracteres.")]
    public string? Notas { get; set; }
}

public class CancelarCitaDto
{
    [Required(ErrorMessage = "Debe indicar el motivo de la cancelación.")]
    [StringLength(300, MinimumLength = 5, ErrorMessage = "El motivo de cancelación debe tener entre 5 y 300 caracteres.")]
    public string MotivoCancelacion { get; set; } = string.Empty;
}

/// <summary>Filtros opcionales para el listado de citas.</summary>
public class FiltroCitasDto
{
    public int? PacienteId { get; set; }
    public int? DentistaId { get; set; }
    public int? ConsultorioId { get; set; }
    public int? ServicioId { get; set; }
    public int? MotivoId { get; set; }
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    /// <summary>Vigente, EnProceso, Finalizada o Cancelada.</summary>
    public EstadoCita? Estado { get; set; }
}

/// <summary>Desglose del tiempo que falta para que inicie la cita.</summary>
public class TiempoRestanteDto
{
    public int Dias { get; set; }
    public int Horas { get; set; }
    public int Minutos { get; set; }
    public double TotalMinutos { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public class CitaDto
{
    public int Id { get; set; }

    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public string PacienteDocumento { get; set; } = string.Empty;

    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public int DuracionMinutos { get; set; }

    public int DentistaId { get; set; }
    public string DentistaNombre { get; set; } = string.Empty;
    public string DentistaEspecialidad { get; set; } = string.Empty;

    public int MotivoId { get; set; }
    public string MotivoNombre { get; set; } = string.Empty;

    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = string.Empty;

    public int ConsultorioId { get; set; }
    public string ConsultorioNombre { get; set; } = string.Empty;

    public decimal CostoEstimado { get; set; }
    public string? Notas { get; set; }

    // ----- Campos calculados automáticamente -----
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public EstadoCita Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;
    public TiempoRestanteDto TiempoRestante { get; set; } = new();
    /// <summary>Minutos que faltan para que termine la consulta; solo aplica cuando está en proceso.</summary>
    public int? MinutosParaFinalizar { get; set; }

    public bool Cancelada { get; set; }
    public string? MotivoCancelacion { get; set; }
    public DateTime FechaRegistro { get; set; }
}
