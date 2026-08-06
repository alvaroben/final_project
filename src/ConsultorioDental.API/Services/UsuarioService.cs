using ConsultorioDental.API.Common;
using ConsultorioDental.API.Data;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.API.Services;

public interface IUsuarioService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
    Task<IEnumerable<UsuarioDto>> ListarAsync(bool? activo, string? busqueda);
    Task<UsuarioDto> ObtenerPorIdAsync(int id);
    Task<UsuarioDto> CrearAsync(CrearUsuarioDto dto);
    Task<UsuarioDto> ActualizarAsync(int id, ActualizarUsuarioDto dto);
    Task EliminarAsync(int id, int idUsuarioActual);
    Task CambiarPasswordAsync(int id, CambiarPasswordDto dto);
}

public class UsuarioService : IUsuarioService
{
    private readonly ConsultorioDbContext _db;
    private readonly IJwtTokenService _jwt;

    public UsuarioService(ConsultorioDbContext db, IJwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario == dto.NombreUsuario);

        // Mismo mensaje para usuario inexistente y contraseña incorrecta: no revelamos cuál falló.
        if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
            throw new NoAutorizadoException("Usuario o contraseña incorrectos.");

        if (!usuario.Activo)
            throw new NoAutorizadoException("El usuario se encuentra inactivo. Contacte al administrador.");

        usuario.UltimoAcceso = DateTime.Now;
        await _db.SaveChangesAsync();

        var (token, expiracion) = _jwt.GenerarToken(usuario);

        return new LoginResponseDto
        {
            Token = token,
            ExpiraEn = expiracion,
            Usuario = Mapeos.AUsuarioDto(usuario)
        };
    }

    public async Task<IEnumerable<UsuarioDto>> ListarAsync(bool? activo, string? busqueda)
    {
        var consulta = _db.Usuarios.AsNoTracking().AsQueryable();

        if (activo.HasValue)
            consulta = consulta.Where(u => u.Activo == activo.Value);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(u =>
                u.NombreUsuario.Contains(texto) ||
                u.NombreCompleto.Contains(texto) ||
                u.Correo.Contains(texto));
        }

        var usuarios = await consulta.OrderBy(u => u.NombreUsuario).ToListAsync();
        return usuarios.Select(Mapeos.AUsuarioDto);
    }

    public async Task<UsuarioDto> ObtenerPorIdAsync(int id)
    {
        var usuario = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id)
                      ?? throw new NoEncontradoException("usuario", id);

        return Mapeos.AUsuarioDto(usuario);
    }

    public async Task<UsuarioDto> CrearAsync(CrearUsuarioDto dto)
    {
        var nombreUsuario = dto.NombreUsuario.Trim();
        var correo = dto.Correo.Trim().ToLowerInvariant();

        if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario))
            throw new ConflictoException($"Ya existe un usuario registrado con el nombre '{nombreUsuario}'.");

        if (await _db.Usuarios.AnyAsync(u => u.Correo == correo))
            throw new ConflictoException($"Ya existe un usuario registrado con el correo '{correo}'.");

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            NombreCompleto = dto.NombreCompleto.Trim(),
            Correo = correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Rol = dto.Rol,
            Activo = dto.Activo,
            FechaCreacion = DateTime.Now
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return Mapeos.AUsuarioDto(usuario);
    }

    public async Task<UsuarioDto> ActualizarAsync(int id, ActualizarUsuarioDto dto)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id)
                      ?? throw new NoEncontradoException("usuario", id);

        var correo = dto.Correo.Trim().ToLowerInvariant();

        if (await _db.Usuarios.AnyAsync(u => u.Correo == correo && u.Id != id))
            throw new ConflictoException($"Ya existe otro usuario registrado con el correo '{correo}'.");

        // Evita quedarse sin administradores activos al degradar o desactivar el último.
        if (usuario.Rol == RolUsuario.Administrador && (dto.Rol != RolUsuario.Administrador || !dto.Activo))
            await ValidarQueQuedeUnAdministradorAsync(id);

        usuario.NombreCompleto = dto.NombreCompleto.Trim();
        usuario.Correo = correo;
        usuario.Rol = dto.Rol;
        usuario.Activo = dto.Activo;

        await _db.SaveChangesAsync();
        return Mapeos.AUsuarioDto(usuario);
    }

    public async Task EliminarAsync(int id, int idUsuarioActual)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id)
                      ?? throw new NoEncontradoException("usuario", id);

        if (id == idUsuarioActual)
            throw new ReglaNegocioException("Un usuario no puede eliminar su propia cuenta mientras está autenticado.");

        if (usuario.Rol == RolUsuario.Administrador)
            await ValidarQueQuedeUnAdministradorAsync(id);

        _db.Usuarios.Remove(usuario);
        await _db.SaveChangesAsync();
    }

    public async Task CambiarPasswordAsync(int id, CambiarPasswordDto dto)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id)
                      ?? throw new NoEncontradoException("usuario", id);

        if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
            throw new ReglaNegocioException("La contraseña actual no es correcta.");

        if (dto.PasswordActual == dto.PasswordNuevo)
            throw new ReglaNegocioException("La nueva contraseña debe ser distinta de la actual.");

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNuevo);
        await _db.SaveChangesAsync();
    }

    private async Task ValidarQueQuedeUnAdministradorAsync(int idExcluido)
    {
        var otrosAdmins = await _db.Usuarios
            .CountAsync(u => u.Rol == RolUsuario.Administrador && u.Activo && u.Id != idExcluido);

        if (otrosAdmins == 0)
            throw new ReglaNegocioException("El sistema debe conservar al menos un usuario administrador activo.");
    }
}
