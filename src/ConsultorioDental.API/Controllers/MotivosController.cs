using ConsultorioDental.API.Common;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultorioDental.API.Controllers;

/// <summary>CRUD de motivos de consulta.</summary>
[Authorize]
public class MotivosController : ApiControllerBase
{
    private readonly IMotivoService _motivos;

    public MotivosController(IMotivoService motivos) => _motivos = motivos;

    /// <summary>Lista los motivos registrados, ordenados por prioridad.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<MotivoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] bool? activo, [FromQuery] string? busqueda)
    {
        var datos = await _motivos.ListarAsync(activo, busqueda);
        return Exito(datos, "Listado de motivos obtenido correctamente.");
    }

    /// <summary>Obtiene un motivo por su ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MotivoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id)
    {
        var motivo = await _motivos.ObtenerPorIdAsync(id);
        return Exito(motivo, "Motivo obtenido correctamente.");
    }

    /// <summary>Registra un nuevo motivo de consulta.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MotivoDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] GuardarMotivoDto dto)
    {
        var motivo = await _motivos.CrearAsync(dto);
        return Creado(nameof(Obtener), new { id = motivo.Id }, motivo, "Motivo registrado correctamente.");
    }

    /// <summary>Actualiza un motivo existente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MotivoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarMotivoDto dto)
    {
        var motivo = await _motivos.ActualizarAsync(id, dto);
        return Exito(motivo, "Motivo actualizado correctamente.");
    }

    /// <summary>Elimina un motivo que no esté asociado a citas.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _motivos.EliminarAsync(id);
        return Ok(ApiResponse.Correcto($"Motivo con ID {id} eliminado correctamente."));
    }
}
