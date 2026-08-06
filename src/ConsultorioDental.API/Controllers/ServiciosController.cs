using ConsultorioDental.API.Common;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultorioDental.API.Controllers;

/// <summary>CRUD de servicios dentales ofrecidos por el consultorio.</summary>
[Authorize]
public class ServiciosController : ApiControllerBase
{
    private readonly IServicioService _servicios;

    public ServiciosController(IServicioService servicios) => _servicios = servicios;

    /// <summary>Lista los servicios registrados.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ServicioDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] bool? activo, [FromQuery] string? busqueda)
    {
        var datos = await _servicios.ListarAsync(activo, busqueda);
        return Exito(datos, "Listado de servicios obtenido correctamente.");
    }

    /// <summary>Obtiene un servicio por su ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ServicioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id)
    {
        var servicio = await _servicios.ObtenerPorIdAsync(id);
        return Exito(servicio, "Servicio obtenido correctamente.");
    }

    /// <summary>Registra un nuevo servicio dental.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ServicioDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] GuardarServicioDto dto)
    {
        var servicio = await _servicios.CrearAsync(dto);
        return Creado(nameof(Obtener), new { id = servicio.Id }, servicio, "Servicio registrado correctamente.");
    }

    /// <summary>Actualiza un servicio existente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ServicioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarServicioDto dto)
    {
        var servicio = await _servicios.ActualizarAsync(id, dto);
        return Exito(servicio, "Servicio actualizado correctamente.");
    }

    /// <summary>Elimina un servicio que no esté asociado a citas.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicios.EliminarAsync(id);
        return Ok(ApiResponse.Correcto($"Servicio con ID {id} eliminado correctamente."));
    }
}
