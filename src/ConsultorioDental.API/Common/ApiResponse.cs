namespace ConsultorioDental.API.Common;

/// <summary>
/// Envoltura estándar de todas las respuestas de la API para mantener un contrato uniforme.
/// </summary>
public class ApiResponse<T>
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public T? Datos { get; set; }
    public IEnumerable<string>? Errores { get; set; }

    public static ApiResponse<T> Ok(T datos, string mensaje = "Operación realizada correctamente") =>
        new() { Exito = true, Mensaje = mensaje, Datos = datos };

    public static ApiResponse<T> Fallo(string mensaje, IEnumerable<string>? errores = null) =>
        new() { Exito = false, Mensaje = mensaje, Errores = errores };
}

/// <summary>
/// Variante sin datos, usada en operaciones como eliminar.
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Correcto(string mensaje) => new() { Exito = true, Mensaje = mensaje };

    public static ApiResponse Error(string mensaje, IEnumerable<string>? errores = null) =>
        new() { Exito = false, Mensaje = mensaje, Errores = errores };
}
