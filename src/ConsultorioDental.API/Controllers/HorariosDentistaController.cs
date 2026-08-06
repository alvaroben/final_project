using ConsultorioDental.API.Common;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultorioDental.API.Controllers;

/// <summary>CRUD de los bloques de disponibilidad de cada dentista.</summary>
[Authorize]
[Route("api/horarios-dentista")]
public class HorariosDentistaController : ApiControllerBase
{
    private readonly IHorarioDentistaService _horarios;

    public HorariosDentistaController(IHorarioDentistaService horarios) => _horarios = horarios;

    /// <summary>Lista los horarios. Se puede filtrar por dentista y día de la semana (0 = Domingo).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<HorarioDentistaDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] int? dentistaId, [FromQuery] int? diaSemana, [FromQuery] bool? activo)
    {
        var datos = await _horarios.ListarAsync(dentistaId, diaSemana, activo);
        return Exito(datos, "Listado de horarios obtenido correctamente.");
    }

    /// <summary>Obtiene un horario por su ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<HorarioDentistaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id)
    {
        var horario = await _horarios.ObtenerPorIdAsync(id);
        return Exito(horario, "Horario obtenido correctamente.");
    }

    /// <summary>Registra un bloque de disponibilidad. Valida que no se solape con otro del mismo día.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<HorarioDentistaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] GuardarHorarioDentistaDto dto)
    {
        var horario = await _horarios.CrearAsync(dto);
        return Creado(nameof(Obtener), new { id = horario.Id }, horario, "Horario registrado correctamente.");
    }

    /// <summary>Actualiza un bloque de disponibilidad existente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<HorarioDentistaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarHorarioDentistaDto dto)
    {
        var horario = await _horarios.ActualizarAsync(id, dto);
        return Exito(horario, "Horario actualizado correctamente.");
    }

    /// <summary>Elimina un bloque de disponibilidad sin citas futuras asociadas.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _horarios.EliminarAsync(id);
        return Ok(ApiResponse.Correcto($"Horario con ID {id} eliminado correctamente."));
    }
}
