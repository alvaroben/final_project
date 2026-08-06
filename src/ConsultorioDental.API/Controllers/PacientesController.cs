using ConsultorioDental.API.Common;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultorioDental.API.Controllers;

/// <summary>CRUD de pacientes del consultorio.</summary>
[Authorize]
public class PacientesController : ApiControllerBase
{
    private readonly IPacienteService _pacientes;

    public PacientesController(IPacienteService pacientes) => _pacientes = pacientes;

    /// <summary>Lista los pacientes. Permite filtrar por estado y buscar por nombre, apellido o documento.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PacienteDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] bool? activo, [FromQuery] string? busqueda)
    {
        var datos = await _pacientes.ListarAsync(activo, busqueda);
        return Exito(datos, "Listado de pacientes obtenido correctamente.");
    }

    /// <summary>Obtiene un paciente por su ID, con la edad calculada.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PacienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id)
    {
        var paciente = await _pacientes.ObtenerPorIdAsync(id);
        return Exito(paciente, "Paciente obtenido correctamente.");
    }

    /// <summary>Registra un nuevo paciente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PacienteDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] GuardarPacienteDto dto)
    {
        var paciente = await _pacientes.CrearAsync(dto);
        return Creado(nameof(Obtener), new { id = paciente.Id }, paciente, "Paciente registrado correctamente.");
    }

    /// <summary>Actualiza un paciente existente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PacienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarPacienteDto dto)
    {
        var paciente = await _pacientes.ActualizarAsync(id, dto);
        return Exito(paciente, "Paciente actualizado correctamente.");
    }

    /// <summary>Elimina un paciente que no tenga citas registradas.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _pacientes.EliminarAsync(id);
        return Ok(ApiResponse.Correcto($"Paciente con ID {id} eliminado correctamente."));
    }
}
