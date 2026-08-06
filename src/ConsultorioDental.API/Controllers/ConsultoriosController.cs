using ConsultorioDental.API.Common;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultorioDental.API.Controllers;

/// <summary>CRUD de consultorios (áreas o salas de atención).</summary>
[Authorize]
public class ConsultoriosController : ApiControllerBase
{
    private readonly IConsultorioService _consultorios;

    public ConsultoriosController(IConsultorioService consultorios) => _consultorios = consultorios;

    /// <summary>Lista los consultorios registrados.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ConsultorioDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] bool? activo, [FromQuery] string? busqueda)
    {
        var datos = await _consultorios.ListarAsync(activo, busqueda);
        return Exito(datos, "Listado de consultorios obtenido correctamente.");
    }

    /// <summary>Obtiene un consultorio por su ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ConsultorioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id)
    {
        var consultorio = await _consultorios.ObtenerPorIdAsync(id);
        return Exito(consultorio, "Consultorio obtenido correctamente.");
    }

    /// <summary>Registra un nuevo consultorio.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ConsultorioDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] GuardarConsultorioDto dto)
    {
        var consultorio = await _consultorios.CrearAsync(dto);
        return Creado(nameof(Obtener), new { id = consultorio.Id }, consultorio, "Consultorio registrado correctamente.");
    }

    /// <summary>Actualiza un consultorio existente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ConsultorioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarConsultorioDto dto)
    {
        var consultorio = await _consultorios.ActualizarAsync(id, dto);
        return Exito(consultorio, "Consultorio actualizado correctamente.");
    }

    /// <summary>Elimina un consultorio que no esté asociado a citas.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _consultorios.EliminarAsync(id);
        return Ok(ApiResponse.Correcto($"Consultorio con ID {id} eliminado correctamente."));
    }
}
