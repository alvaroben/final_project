using ConsultorioDental.API.Common;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultorioDental.API.Controllers;

/// <summary>
/// Entidad principal del sistema. El estado y el tiempo restante nunca se envían:
/// la API los calcula a partir de la fecha, la hora y la duración.
/// </summary>
[Authorize]
public class CitasController : ApiControllerBase
{
    private readonly ICitaService _citas;

    public CitasController(ICitaService citas) => _citas = citas;

    /// <summary>Lista las citas con filtros opcionales por paciente, dentista, fechas y estado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CitaDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar([FromQuery] FiltroCitasDto filtro)
    {
        var datos = await _citas.ListarAsync(filtro);
        return Exito(datos, "Listado de citas obtenido correctamente.");
    }

    /// <summary>Resumen de la agenda: totales por estado, citas de hoy e ingreso estimado pendiente.</summary>
    [HttpGet("resumen")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Resumen()
    {
        var datos = await _citas.ObtenerResumenAsync();
        return Exito(datos, "Resumen de citas obtenido correctamente.");
    }

    /// <summary>Obtiene una cita por su ID, con estado y tiempo restante calculados.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CitaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id)
    {
        var cita = await _citas.ObtenerPorIdAsync(id);
        return Exito(cita, "Cita obtenida correctamente.");
    }

    /// <summary>
    /// Agenda una nueva cita. Valida fecha futura, horario del dentista y que no exista
    /// solapamiento para el dentista, el consultorio ni el paciente.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CitaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] GuardarCitaDto dto)
    {
        var cita = await _citas.CrearAsync(dto);
        return Creado(nameof(Obtener), new { id = cita.Id }, cita, "Cita agendada correctamente.");
    }

    /// <summary>Reprograma o modifica una cita que aún no ha iniciado.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CitaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarCitaDto dto)
    {
        var cita = await _citas.ActualizarAsync(id, dto);
        return Exito(cita, "Cita actualizada correctamente.");
    }

    /// <summary>Cancela una cita sin borrarla, dejando registrado el motivo.</summary>
    [HttpPatch("{id:int}/cancelar")]
    [ProducesResponseType(typeof(ApiResponse<CitaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarCitaDto dto)
    {
        var cita = await _citas.CancelarAsync(id, dto);
        return Exito(cita, "Cita cancelada correctamente.");
    }

    /// <summary>Elimina una cita que aún no ha iniciado. Las atendidas se conservan como historial.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _citas.EliminarAsync(id);
        return Ok(ApiResponse.Correcto($"Cita con ID {id} eliminada correctamente."));
    }
}
