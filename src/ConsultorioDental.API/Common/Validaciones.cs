using System.ComponentModel.DataAnnotations;

namespace ConsultorioDental.API.Common;

/// <summary>Rechaza fechas futuras (fecha de nacimiento, por ejemplo).</summary>
public class FechaNoFuturaAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is null) return ValidationResult.Success;

        var fecha = value switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            _ => (DateOnly?)null
        };

        if (fecha is null) return new ValidationResult($"El campo {context.DisplayName} no tiene un formato de fecha válido.");

        return fecha > DateOnly.FromDateTime(DateTime.Now)
            ? new ValidationResult($"El campo {context.DisplayName} no puede ser una fecha futura.")
            : ValidationResult.Success;
    }
}

/// <summary>Valida que la fecha sea real y esté dentro de un rango de años razonable.</summary>
public class FechaRazonableAttribute : ValidationAttribute
{
    public int AniosAtrasMaximo { get; set; } = 120;

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is null) return ValidationResult.Success;

        var fecha = value switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            _ => (DateOnly?)null
        };

        if (fecha is null) return new ValidationResult($"El campo {context.DisplayName} no tiene un formato de fecha válido.");

        var limite = DateOnly.FromDateTime(DateTime.Now.AddYears(-AniosAtrasMaximo));
        return fecha < limite
            ? new ValidationResult($"El campo {context.DisplayName} no puede ser anterior a {limite:dd/MM/yyyy}.")
            : ValidationResult.Success;
    }
}
