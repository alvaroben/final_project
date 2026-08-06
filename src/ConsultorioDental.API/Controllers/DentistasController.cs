using ConsultorioDental.API.Common;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultorioDental.API.Controllers;

/// <summary>CRUD de dentistas y consulta de su agenda.</summary>
[Authorize]
public class DentistasController : ApiControllerBase
{
    private readonly IDentistaService _dentistas;
    private readonly ICitaService _citas;

    public DentistasController(IDentistaService dentistas, ICitaService citas)
    {
        _dentistas = dentistas;
        _citas = citas;
    }

    /// <summary>Lista los dentistas con su especialidad y horarios.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DentistaDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] bool? activo, [FromQuery] int? especialidadId, [FromQuery] string? busqueda)
    {
        var datos = await _dentistas.ListarAsync(activo, especialidadId, busqueda);
        return Exito(datos, "Listado de dentistas obtenido correctamente.");
    }

    /// <summary>Obtiene un dentista por su ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<DentistaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id)
    {
        var dentista = await _dentistas.ObtenerPorIdAsync(id);
        return Exito(dentista, "Dentista obtenido correctamente.");
    }

    /// <summary>Muestra los bloques de trabajo y las citas ya agendadas de un dentista en una fecha.</summary>
    [HttpGet("{id:int}/disponibilidad")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Disponibilidad(int id, [FromQuery] DateOnly fecha)
    {
        var datos = await _citas.ObtenerDisponibilidadAsync(id, fecha);
        return Exito(datos, "Disponibilidad obtenida correctamente.");
    }

    /// <summary>Registra un nuevo dentista.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DentistaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] GuardarDentistaDto dto)
    {
        var dentista = await _dentistas.CrearAsync(dto);
        return Creado(nameof(Obtener), new { id = dentista.Id }, dentista, "Dentista registrado correctamente.");
    }

    /// <summary>Actualiza un dentista existente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<DentistaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarDentistaDto dto)
    {
        var dentista = await _dentistas.ActualizarAsync(id, dto);
        return Exito(dentista, "Dentista actualizado correctamente.");
    }

    /// <summary>Elimina un dentista que no tenga citas registradas.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _dentistas.EliminarAsync(id);
        return Ok(ApiResponse.Correcto($"Dentista con ID {id} eliminado correctamente."));
    }
}
