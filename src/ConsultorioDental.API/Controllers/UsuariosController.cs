using ConsultorioDental.API.Common;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultorioDental.API.Controllers;

/// <summary>CRUD de usuarios del sistema. Requiere token JWT.</summary>
[Authorize]
public class UsuariosController : ApiControllerBase
{
    private readonly IUsuarioService _usuarios;

    public UsuariosController(IUsuarioService usuarios) => _usuarios = usuarios;

    /// <summary>Lista los usuarios registrados, con filtros opcionales.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UsuarioDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] bool? activo, [FromQuery] string? busqueda)
    {
        var datos = await _usuarios.ListarAsync(activo, busqueda);
        return Exito(datos, "Listado de usuarios obtenido correctamente.");
    }

    /// <summary>Obtiene un usuario por su ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(id);
        return Exito(usuario, "Usuario obtenido correctamente.");
    }

    /// <summary>Registra un nuevo usuario. Solo administradores.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(ApiResponse<UsuarioDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] CrearUsuarioDto dto)
    {
        var usuario = await _usuarios.CrearAsync(dto);
        return Creado(nameof(Obtener), new { id = usuario.Id }, usuario, "Usuario registrado correctamente.");
    }

    /// <summary>Actualiza los datos de un usuario existente. Solo administradores.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(ApiResponse<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarUsuarioDto dto)
    {
        var usuario = await _usuarios.ActualizarAsync(id, dto);
        return Exito(usuario, "Usuario actualizado correctamente.");
    }

    /// <summary>Elimina un usuario. Solo administradores.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _usuarios.EliminarAsync(id, UsuarioIdActual);
        return Ok(ApiResponse.Correcto($"Usuario con ID {id} eliminado correctamente."));
    }

    /// <summary>Cambia la contraseña del usuario autenticado.</summary>
    [HttpPost("cambiar-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto dto)
    {
        await _usuarios.CambiarPasswordAsync(UsuarioIdActual, dto);
        return Ok(ApiResponse.Correcto("Contraseña actualizada correctamente."));
    }
}
