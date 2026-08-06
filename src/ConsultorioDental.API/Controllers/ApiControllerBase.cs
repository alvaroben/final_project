using System.Security.Claims;
using ConsultorioDental.API.Common;
using Microsoft.AspNetCore.Mvc;

namespace ConsultorioDental.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Id del usuario autenticado, tomado del claim del token.</summary>
    protected int UsuarioIdActual =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    protected IActionResult Exito<T>(T datos, string mensaje = "Consulta realizada correctamente") =>
        Ok(ApiResponse<T>.Ok(datos, mensaje));

    protected IActionResult Creado<T>(string accion, object valoresRuta, T datos, string mensaje) =>
        CreatedAtAction(accion, valoresRuta, ApiResponse<T>.Ok(datos, mensaje));
}
