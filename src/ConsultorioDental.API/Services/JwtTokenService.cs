using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ConsultorioDental.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace ConsultorioDental.API.Services;

/// <summary>Opciones de firma y vigencia del token, leídas de appsettings.json.</summary>
public class JwtOptions
{
    public const string Seccion = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int MinutosExpiracion { get; set; } = 120;
}

public interface IJwtTokenService
{
    (string Token, DateTime Expiracion) GenerarToken(Usuario usuario);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opciones;

    public JwtTokenService(Microsoft.Extensions.Options.IOptions<JwtOptions> opciones) => _opciones = opciones.Value;

    public (string Token, DateTime Expiracion) GenerarToken(Usuario usuario)
    {
        var expiracion = DateTime.UtcNow.AddMinutes(_opciones.MinutosExpiracion);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new(ClaimTypes.Email, usuario.Correo),
            new(ClaimTypes.Role, usuario.Rol.ToString())
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opciones.Issuer,
            audience: _opciones.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiracion,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiracion);
    }
}
