using System.ComponentModel.DataAnnotations;
using ConsultorioDental.API.Common;

namespace ConsultorioDental.API.DTOs;

public class GuardarPacienteDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 80 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 80 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento de identidad es obligatorio.")]
    [StringLength(20, MinimumLength = 5, ErrorMessage = "El documento debe tener entre 5 y 20 caracteres.")]
    [RegularExpression(@"^[0-9A-Za-z\-]+$", ErrorMessage = "El documento solo admite letras, números y guiones.")]
    public string Documento { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    [FechaNoFutura]
    [FechaRazonable(AniosAtrasMaximo = 120)]
    public DateOnly FechaNacimiento { get; set; }

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [RegularExpression(@"^[0-9\+\-\(\) ]{7,20}$", ErrorMessage = "El teléfono debe tener entre 7 y 20 caracteres numéricos.")]
    public string Telefono { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(150)]
    public string? Correo { get; set; }

    [StringLength(250, ErrorMessage = "La dirección no puede superar los 250 caracteres.")]
    public string? Direccion { get; set; }

    [StringLength(500, ErrorMessage = "Las alergias no pueden superar los 500 caracteres.")]
    public string? Alergias { get; set; }

    public bool Activo { get; set; } = true;
}

public class PacienteDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
    /// <summary>Edad calculada automáticamente a partir de la fecha de nacimiento.</summary>
    public int Edad { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public string? Alergias { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int TotalCitas { get; set; }
}
