using System.ComponentModel.DataAnnotations;

namespace ConsultorioDental.API.DTOs;

// ---------------- Especialidad ----------------

public class GuardarEspecialidadDto
{
    [Required(ErrorMessage = "El nombre de la especialidad es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "La descripción no puede superar los 300 caracteres.")]
    public string? Descripcion { get; set; }

    public bool Activa { get; set; } = true;
}

public class EspecialidadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activa { get; set; }
    public int TotalDentistas { get; set; }
}

// ---------------- Motivo ----------------

public class GuardarMotivoDto
{
    [Required(ErrorMessage = "El nombre del motivo es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "La descripción no puede superar los 300 caracteres.")]
    public string? Descripcion { get; set; }

    [Range(1, 3, ErrorMessage = "La prioridad debe ser 1 (Baja), 2 (Media) o 3 (Alta).")]
    public int Prioridad { get; set; } = 1;

    public bool Activo { get; set; } = true;
}

public class MotivoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Prioridad { get; set; }
    public string PrioridadNombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int TotalCitas { get; set; }
}

// ---------------- Servicio ----------------

public class GuardarServicioDto
{
    [Required(ErrorMessage = "El nombre del servicio es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "La descripción no puede superar los 300 caracteres.")]
    public string? Descripcion { get; set; }

    [Range(0.01, 1_000_000, ErrorMessage = "El precio debe ser mayor que cero y menor que 1,000,000.")]
    public decimal Precio { get; set; }

    [Range(5, 480, ErrorMessage = "La duración sugerida debe estar entre 5 y 480 minutos.")]
    public int DuracionMinutos { get; set; } = 30;

    public bool Activo { get; set; } = true;
}

public class ServicioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int DuracionMinutos { get; set; }
    public bool Activo { get; set; }
    public int TotalCitas { get; set; }
}

// ---------------- Consultorio ----------------

public class GuardarConsultorioDto
{
    [Required(ErrorMessage = "El código del consultorio es obligatorio.")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "El código debe tener entre 1 y 20 caracteres.")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del consultorio es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "La ubicación no puede superar los 150 caracteres.")]
    public string? Ubicacion { get; set; }

    [Range(1, 20, ErrorMessage = "La capacidad debe estar entre 1 y 20 unidades dentales.")]
    public int Capacidad { get; set; } = 1;

    public bool Activo { get; set; } = true;
}

public class ConsultorioDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Ubicacion { get; set; }
    public int Capacidad { get; set; }
    public bool Activo { get; set; }
    public int TotalCitas { get; set; }
}
