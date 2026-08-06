using System.Text.Json;
using ConsultorioDental.API.Common;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.API.Middleware;

/// <summary>
/// Captura cualquier excepción no controlada del pipeline y la traduce a una respuesta
/// JSON uniforme. El detalle técnico solo queda en el log del servidor.
/// </summary>
public class ManejadorExcepcionesMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejadorExcepcionesMiddleware> _logger;
    private readonly IHostEnvironment _entorno;

    public ManejadorExcepcionesMiddleware(
        RequestDelegate siguiente,
        ILogger<ManejadorExcepcionesMiddleware> logger,
        IHostEnvironment entorno)
    {
        _siguiente = siguiente;
        _logger = logger;
        _entorno = entorno;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (Exception ex)
        {
            await EscribirRespuestaAsync(contexto, ex);
        }
    }

    private async Task EscribirRespuestaAsync(HttpContext contexto, Exception ex)
    {
        var (codigo, mensaje, errores) = Traducir(ex);

        if (codigo >= StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", contexto.Request.Method, contexto.Request.Path);
        else
            _logger.LogWarning("Solicitud rechazada ({Codigo}) en {Metodo} {Ruta}: {Mensaje}",
                codigo, contexto.Request.Method, contexto.Request.Path, mensaje);

        // Si la respuesta ya empezó a enviarse no se puede reescribir el encabezado.
        if (contexto.Response.HasStarted)
        {
            _logger.LogWarning("La respuesta ya había iniciado; no se pudo enviar el detalle del error.");
            return;
        }

        contexto.Response.Clear();
        contexto.Response.StatusCode = codigo;
        contexto.Response.ContentType = "application/json; charset=utf-8";

        // En desarrollo se adjunta el detalle técnico para facilitar la depuración.
        if (_entorno.IsDevelopment() && codigo >= StatusCodes.Status500InternalServerError)
            errores = new[] { ex.GetType().Name, ex.Message };

        var cuerpo = ApiResponse.Error(mensaje, errores);

        await contexto.Response.WriteAsync(JsonSerializer.Serialize(cuerpo, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }));
    }

    private static (int Codigo, string Mensaje, IEnumerable<string>? Errores) Traducir(Exception ex) => ex switch
    {
        AppException app => (app.CodigoHttp, app.Message, app.Errores),

        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
            "El registro fue modificado por otro usuario. Vuelva a consultarlo e intente nuevamente.", null),

        DbUpdateException dbEx => TraducirErrorDeBaseDeDatos(dbEx),

        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,
            "No cuenta con autorización para realizar esta operación.", null),

        TaskCanceledException or OperationCanceledException => (StatusCodes.Status499ClientClosedRequest,
            "La solicitud fue cancelada antes de completarse.", null),

        _ => (StatusCodes.Status500InternalServerError,
            "Ocurrió un error inesperado al procesar la solicitud. Intente nuevamente o contacte al administrador.", null)
    };

    private static (int, string, IEnumerable<string>?) TraducirErrorDeBaseDeDatos(DbUpdateException ex)
    {
        var detalle = ex.InnerException?.Message ?? ex.Message;

        // 2601/2627: índice único duplicado. 547: violación de llave foránea.
        if (detalle.Contains("2601") || detalle.Contains("2627") || detalle.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            return (StatusCodes.Status409Conflict,
                "Ya existe un registro con esos datos únicos. Verifique los campos que no admiten duplicados.", null);

        if (detalle.Contains("547") || detalle.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase))
            return (StatusCodes.Status409Conflict,
                "La operación viola una relación existente entre tablas. Verifique los registros asociados.", null);

        return (StatusCodes.Status400BadRequest,
            "No fue posible guardar los cambios en la base de datos. Revise los datos enviados.", null);
    }
}

public static class ManejadorExcepcionesExtensiones
{
    public static IApplicationBuilder UsarManejadorDeExcepciones(this IApplicationBuilder app) =>
        app.UseMiddleware<ManejadorExcepcionesMiddleware>();
}
