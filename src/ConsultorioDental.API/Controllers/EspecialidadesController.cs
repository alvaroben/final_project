using ConsultorioDental.API.Common;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultorioDental.API.Controllers;

/// <summary>CRUD de especialidades asociadas a los dentistas.</summary>
[Authorize]
public class EspecialidadesController : ApiControllerBase
{
    private readonly IEspecialidadService _especialidades;

    public EspecialidadesController(IEspecialidadService especialidades) => _especialidades = especialidades;

    /// <summary>Lista las especialidades registradas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EspecialidadDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] bool? activa, [FromQuery] string? busqueda)
    {
        var datos = await _especialidades.ListarAsync(activa, busqueda);
        return Exito(datos, "Listado de especialidades obtenido correctamente.");
    }

    /// <summary>Obtiene una especialidad por su ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<EspecialidadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id)
    {
        var especialidad = await _especialidades.ObtenerPorIdAsync(id);
        return Exito(especialidad, "Especialidad obtenida correctamente.");
    }

    /// <summary>Registra una nueva especialidad.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EspecialidadDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] GuardarEspecialidadDto dto)
    {
        var especialidad = await _especialidades.CrearAsync(dto);
        return Creado(nameof(Obtener), new { id = especialidad.Id }, especialidad, "Especialidad registrada correctamente.");
    }

    /// <summary>Actualiza una especialidad existente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<EspecialidadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarEspecialidadDto dto)
    {
        var especialidad = await _especialidades.ActualizarAsync(id, dto);
        return Exito(especialidad, "Especialidad actualizada correctamente.");
    }

    /// <summary>Elimina una especialidad que no tenga dentistas asignados.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _especialidades.EliminarAsync(id);
        return Ok(ApiResponse.Correcto($"Especialidad con ID {id} eliminada correctamente."));
    }
}
