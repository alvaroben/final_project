using ConsultorioDental.API.Common;
using ConsultorioDental.API.Data;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.API.Services;

public interface IPacienteService
{
    Task<IEnumerable<PacienteDto>> ListarAsync(bool? activo, string? busqueda);
    Task<PacienteDto> ObtenerPorIdAsync(int id);
    Task<PacienteDto> CrearAsync(GuardarPacienteDto dto);
    Task<PacienteDto> ActualizarAsync(int id, GuardarPacienteDto dto);
    Task EliminarAsync(int id);
}

public class PacienteService : IPacienteService
{
    private readonly ConsultorioDbContext _db;

    public PacienteService(ConsultorioDbContext db) => _db = db;

    public async Task<IEnumerable<PacienteDto>> ListarAsync(bool? activo, string? busqueda)
    {
        var consulta = _db.Pacientes.AsNoTracking().AsQueryable();

        if (activo.HasValue)
            consulta = consulta.Where(p => p.Activo == activo.Value);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(p =>
                p.Nombre.Contains(texto) || p.Apellido.Contains(texto) || p.Documento.Contains(texto));
        }

        var datos = await consulta
            .OrderBy(p => p.Apellido).ThenBy(p => p.Nombre)
            .Select(p => new { Paciente = p, Total = p.Citas.Count })
            .ToListAsync();

        return datos.Select(x => Mapeos.APacienteDto(x.Paciente, x.Total));
    }

    public async Task<PacienteDto> ObtenerPorIdAsync(int id)
    {
        var datos = await _db.Pacientes.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { Paciente = p, Total = p.Citas.Count })
            .FirstOrDefaultAsync() ?? throw new NoEncontradoException("paciente", id);

        return Mapeos.APacienteDto(datos.Paciente, datos.Total);
    }

    public async Task<PacienteDto> CrearAsync(GuardarPacienteDto dto)
    {
        var documento = dto.Documento.Trim();

        if (await _db.Pacientes.AnyAsync(p => p.Documento == documento))
            throw new ConflictoException($"Ya existe un paciente registrado con el documento '{documento}'.");

        var paciente = new Paciente
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Documento = documento,
            FechaNacimiento = dto.FechaNacimiento,
            Telefono = dto.Telefono.Trim(),
            Correo = dto.Correo?.Trim().ToLowerInvariant(),
            Direccion = dto.Direccion?.Trim(),
            Alergias = dto.Alergias?.Trim(),
            Activo = dto.Activo,
            FechaRegistro = DateTime.Now
        };

        _db.Pacientes.Add(paciente);
        await _db.SaveChangesAsync();

        return Mapeos.APacienteDto(paciente);
    }

    public async Task<PacienteDto> ActualizarAsync(int id, GuardarPacienteDto dto)
    {
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.Id == id)
                       ?? throw new NoEncontradoException("paciente", id);

        var documento = dto.Documento.Trim();

        if (await _db.Pacientes.AnyAsync(p => p.Documento == documento && p.Id != id))
            throw new ConflictoException($"Ya existe otro paciente registrado con el documento '{documento}'.");

        paciente.Nombre = dto.Nombre.Trim();
        paciente.Apellido = dto.Apellido.Trim();
        paciente.Documento = documento;
        paciente.FechaNacimiento = dto.FechaNacimiento;
        paciente.Telefono = dto.Telefono.Trim();
        paciente.Correo = dto.Correo?.Trim().ToLowerInvariant();
        paciente.Direccion = dto.Direccion?.Trim();
        paciente.Alergias = dto.Alergias?.Trim();
        paciente.Activo = dto.Activo;

        await _db.SaveChangesAsync();

        var total = await _db.Citas.CountAsync(c => c.PacienteId == id);
        return Mapeos.APacienteDto(paciente, total);
    }

    public async Task EliminarAsync(int id)
    {
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.Id == id)
                       ?? throw new NoEncontradoException("paciente", id);

        var citas = await _db.Citas.CountAsync(c => c.PacienteId == id);
        if (citas > 0)
            throw new ConflictoException(
                $"No se puede eliminar el paciente porque tiene {citas} cita(s) registrada(s). Puede desactivarlo en su lugar.");

        _db.Pacientes.Remove(paciente);
        await _db.SaveChangesAsync();
    }
}
