namespace ConsultorioDental.API.Common;

/// <summary>
/// Excepción base de la aplicación. Lleva el código HTTP que le corresponde
/// para que el middleware de errores pueda traducirla sin conocer cada caso.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string mensaje, int codigoHttp) : base(mensaje) => CodigoHttp = codigoHttp;

    public int CodigoHttp { get; }
    public IEnumerable<string>? Errores { get; init; }
}

/// <summary>Recurso inexistente. 404.</summary>
public class NoEncontradoException : AppException
{
    public NoEncontradoException(string mensaje) : base(mensaje, StatusCodes.Status404NotFound) { }

    public NoEncontradoException(string entidad, int id)
        : base($"No existe un registro de {entidad} con el ID {id}.", StatusCodes.Status404NotFound) { }
}

/// <summary>Datos inválidos o regla de negocio incumplida. 400.</summary>
public class ReglaNegocioException : AppException
{
    public ReglaNegocioException(string mensaje) : base(mensaje, StatusCodes.Status400BadRequest) { }
}

/// <summary>Conflicto con el estado actual: duplicados, solapamientos. 409.</summary>
public class ConflictoException : AppException
{
    public ConflictoException(string mensaje) : base(mensaje, StatusCodes.Status409Conflict) { }
}

/// <summary>Credenciales inválidas o usuario inactivo. 401.</summary>
public class NoAutorizadoException : AppException
{
    public NoAutorizadoException(string mensaje) : base(mensaje, StatusCodes.Status401Unauthorized) { }
}
