using ConsultorioDental.API.Common;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultorioDental.API.Controllers;

/// <summary>Inicio de sesión y datos del usuario autenticado.</summary>
public class AuthController : ApiControllerBase
{
    private readonly IUsuarioService _usuarios;

    public AuthController(IUsuarioService usuarios) => _usuarios = usuarios;

    /// <summary>Valida las credenciales y devuelve el JWT que protege el resto de los endpoints.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var respuesta = await _usuarios.LoginAsync(dto);
        return Exito(respuesta, "Inicio de sesión exitoso.");
    }

    /// <summary>Devuelve los datos del usuario dueño del token enviado.</summary>
    [HttpGet("perfil")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UsuarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Perfil()
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(UsuarioIdActual);
        return Exito(usuario, "Perfil obtenido correctamente.");
    }
}
