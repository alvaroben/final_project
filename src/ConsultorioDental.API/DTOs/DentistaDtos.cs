using System.ComponentModel.DataAnnotations;

namespace ConsultorioDental.API.DTOs;

public class GuardarDentistaDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 80 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 80 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de licencia es obligatorio.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "El número de licencia debe tener entre 3 y 30 caracteres.")]
    public string NumeroLicencia { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [RegularExpression(@"^[0-9\+\-\(\) ]{7,20}$", ErrorMessage = "El teléfono debe tener entre 7 y 20 caracteres numéricos.")]
    public string Telefono { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una especialidad válida.")]
    public int EspecialidadId { get; set; }

    public bool Activo { get; set; } = true;
}

public class DentistaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string NumeroLicencia { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public int EspecialidadId { get; set; }
    public string EspecialidadNombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int TotalCitas { get; set; }
    public List<HorarioDentistaDto> Horarios { get; set; } = new();
}

// ---------------- Horario ----------------

public class GuardarHorarioDentistaDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un dentista válido.")]
    public int DentistaId { get; set; }

    [Range(0, 6, ErrorMessage = "El día de la semana debe estar entre 0 (Domingo) y 6 (Sábado).")]
    public int DiaSemana { get; set; }

    [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
    public TimeOnly HoraInicio { get; set; }

    [Required(ErrorMessage = "La hora de fin es obligatoria.")]
    public TimeOnly HoraFin { get; set; }

    [StringLength(200, ErrorMessage = "La observación no puede superar los 200 caracteres.")]
    public string? Observacion { get; set; }

    public bool Activo { get; set; } = true;
}

public class HorarioDentistaDto
{
    public int Id { get; set; }
    public int DentistaId { get; set; }
    public string DentistaNombre { get; set; } = string.Empty;
    public int DiaSemana { get; set; }
    public string DiaSemanaNombre { get; set; } = string.Empty;
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int MinutosDisponibles { get; set; }
    public string? Observacion { get; set; }
    public bool Activo { get; set; }
}
