using ConsultorioDental.API.Common;
using ConsultorioDental.API.Data;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.API.Services;

public interface IDentistaService
{
    Task<IEnumerable<DentistaDto>> ListarAsync(bool? activo, int? especialidadId, string? busqueda);
    Task<DentistaDto> ObtenerPorIdAsync(int id);
    Task<DentistaDto> CrearAsync(GuardarDentistaDto dto);
    Task<DentistaDto> ActualizarAsync(int id, GuardarDentistaDto dto);
    Task EliminarAsync(int id);
}

public class DentistaService : IDentistaService
{
    private readonly ConsultorioDbContext _db;

    public DentistaService(ConsultorioDbContext db) => _db = db;

    public async Task<IEnumerable<DentistaDto>> ListarAsync(bool? activo, int? especialidadId, string? busqueda)
    {
        var consulta = _db.Dentistas.AsNoTracking()
            .Include(d => d.Especialidad)
            .Include(d => d.Horarios)
            .AsQueryable();

        if (activo.HasValue) consulta = consulta.Where(d => d.Activo == activo.Value);
        if (especialidadId.HasValue) consulta = consulta.Where(d => d.EspecialidadId == especialidadId.Value);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(d =>
                d.Nombre.Contains(texto) || d.Apellido.Contains(texto) || d.NumeroLicencia.Contains(texto));
        }

        var datos = await consulta
            .OrderBy(d => d.Apellido).ThenBy(d => d.Nombre)
            .Select(d => new { Dentista = d, Total = d.Citas.Count })
            .ToListAsync();

        return datos.Select(x => Mapeos.ADentistaDto(x.Dentista, x.Total));
    }

    public async Task<DentistaDto> ObtenerPorIdAsync(int id)
    {
        var datos = await _db.Dentistas.AsNoTracking()
            .Include(d => d.Especialidad)
            .Include(d => d.Horarios)
            .Where(d => d.Id == id)
            .Select(d => new { Dentista = d, Total = d.Citas.Count })
            .FirstOrDefaultAsync() ?? throw new NoEncontradoException("dentista", id);

        return Mapeos.ADentistaDto(datos.Dentista, datos.Total);
    }

    public async Task<DentistaDto> CrearAsync(GuardarDentistaDto dto)
    {
        var licencia = dto.NumeroLicencia.Trim().ToUpperInvariant();
        var correo = dto.Correo.Trim().ToLowerInvariant();

        await ValidarEspecialidadAsync(dto.EspecialidadId);

        if (await _db.Dentistas.AnyAsync(d => d.NumeroLicencia == licencia))
            throw new ConflictoException($"Ya existe un dentista con el número de licencia '{licencia}'.");

        if (await _db.Dentistas.AnyAsync(d => d.Correo == correo))
            throw new ConflictoException($"Ya existe un dentista registrado con el correo '{correo}'.");

        var dentista = new Dentista
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            NumeroLicencia = licencia,
            Correo = correo,
            Telefono = dto.Telefono.Trim(),
            EspecialidadId = dto.EspecialidadId,
            Activo = dto.Activo
        };

        _db.Dentistas.Add(dentista);
        await _db.SaveChangesAsync();

        return await ObtenerPorIdAsync(dentista.Id);
    }

    public async Task<DentistaDto> ActualizarAsync(int id, GuardarDentistaDto dto)
    {
        var dentista = await _db.Dentistas.FirstOrDefaultAsync(d => d.Id == id)
                       ?? throw new NoEncontradoException("dentista", id);

        var licencia = dto.NumeroLicencia.Trim().ToUpperInvariant();
        var correo = dto.Correo.Trim().ToLowerInvariant();

        await ValidarEspecialidadAsync(dto.EspecialidadId);

        if (await _db.Dentistas.AnyAsync(d => d.NumeroLicencia == licencia && d.Id != id))
            throw new ConflictoException($"Ya existe otro dentista con el número de licencia '{licencia}'.");

        if (await _db.Dentistas.AnyAsync(d => d.Correo == correo && d.Id != id))
            throw new ConflictoException($"Ya existe otro dentista registrado con el correo '{correo}'.");

        dentista.Nombre = dto.Nombre.Trim();
        dentista.Apellido = dto.Apellido.Trim();
        dentista.NumeroLicencia = licencia;
        dentista.Correo = correo;
        dentista.Telefono = dto.Telefono.Trim();
        dentista.EspecialidadId = dto.EspecialidadId;
        dentista.Activo = dto.Activo;

        await _db.SaveChangesAsync();

        return await ObtenerPorIdAsync(id);
    }

    public async Task EliminarAsync(int id)
    {
        var dentista = await _db.Dentistas.FirstOrDefaultAsync(d => d.Id == id)
                       ?? throw new NoEncontradoException("dentista", id);

        var citas = await _db.Citas.CountAsync(c => c.DentistaId == id);
        if (citas > 0)
            throw new ConflictoException(
                $"No se puede eliminar el dentista porque tiene {citas} cita(s) registrada(s). Puede desactivarlo en su lugar.");

        _db.Dentistas.Remove(dentista);
        await _db.SaveChangesAsync();
    }

    private async Task ValidarEspecialidadAsync(int especialidadId)
    {
        var especialidad = await _db.Especialidades.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == especialidadId)
            ?? throw new NoEncontradoException("especialidad", especialidadId);

        if (!especialidad.Activa)
            throw new ReglaNegocioException($"La especialidad '{especialidad.Nombre}' está inactiva y no puede asignarse.");
    }
}

// ================= Horario del dentista =================

public interface IHorarioDentistaService
{
    Task<IEnumerable<HorarioDentistaDto>> ListarAsync(int? dentistaId, int? diaSemana, bool? activo);
    Task<HorarioDentistaDto> ObtenerPorIdAsync(int id);
    Task<HorarioDentistaDto> CrearAsync(GuardarHorarioDentistaDto dto);
    Task<HorarioDentistaDto> ActualizarAsync(int id, GuardarHorarioDentistaDto dto);
    Task EliminarAsync(int id);
}

public class HorarioDentistaService : IHorarioDentistaService
{
    private const int MinutosMinimosBloque = 15;

    private readonly ConsultorioDbContext _db;

    public HorarioDentistaService(ConsultorioDbContext db) => _db = db;

    public async Task<IEnumerable<HorarioDentistaDto>> ListarAsync(int? dentistaId, int? diaSemana, bool? activo)
    {
        var consulta = _db.HorariosDentista.AsNoTracking().Include(h => h.Dentista).AsQueryable();

        if (dentistaId.HasValue) consulta = consulta.Where(h => h.DentistaId == dentistaId.Value);
        if (activo.HasValue) consulta = consulta.Where(h => h.Activo == activo.Value);

        if (diaSemana.HasValue)
        {
            if (diaSemana is < 0 or > 6)
                throw new ReglaNegocioException("El día de la semana debe estar entre 0 (Domingo) y 6 (Sábado).");

            var dia = (DayOfWeek)diaSemana.Value;
            consulta = consulta.Where(h => h.DiaSemana == dia);
        }

        var horarios = await consulta
            .OrderBy(h => h.DentistaId).ThenBy(h => h.DiaSemana).ThenBy(h => h.HoraInicio)
            .ToListAsync();

        return horarios.Select(Mapeos.AHorarioDto);
    }

    public async Task<HorarioDentistaDto> ObtenerPorIdAsync(int id)
    {
        var horario = await _db.HorariosDentista.AsNoTracking()
            .Include(h => h.Dentista)
            .FirstOrDefaultAsync(h => h.Id == id) ?? throw new NoEncontradoException("horario", id);

        return Mapeos.AHorarioDto(horario);
    }

    public async Task<HorarioDentistaDto> CrearAsync(GuardarHorarioDentistaDto dto)
    {
        await ValidarDentistaAsync(dto.DentistaId);
        ValidarRangoHorario(dto);

        var dia = (DayOfWeek)dto.DiaSemana;
        await ValidarSolapamientoAsync(dto.DentistaId, dia, dto.HoraInicio, dto.HoraFin, null);

        var horario = new HorarioDentista
        {
            DentistaId = dto.DentistaId,
            DiaSemana = dia,
            HoraInicio = dto.HoraInicio,
            HoraFin = dto.HoraFin,
            Observacion = dto.Observacion?.Trim(),
            Activo = dto.Activo
        };

        _db.HorariosDentista.Add(horario);
        await _db.SaveChangesAsync();

        return await ObtenerPorIdAsync(horario.Id);
    }

    public async Task<HorarioDentistaDto> ActualizarAsync(int id, GuardarHorarioDentistaDto dto)
    {
        var horario = await _db.HorariosDentista.FirstOrDefaultAsync(h => h.Id == id)
                      ?? throw new NoEncontradoException("horario", id);

        await ValidarDentistaAsync(dto.DentistaId);
        ValidarRangoHorario(dto);

        var dia = (DayOfWeek)dto.DiaSemana;
        await ValidarSolapamientoAsync(dto.DentistaId, dia, dto.HoraInicio, dto.HoraFin, id);

        horario.DentistaId = dto.DentistaId;
        horario.DiaSemana = dia;
        horario.HoraInicio = dto.HoraInicio;
        horario.HoraFin = dto.HoraFin;
        horario.Observacion = dto.Observacion?.Trim();
        horario.Activo = dto.Activo;

        await _db.SaveChangesAsync();

        return await ObtenerPorIdAsync(id);
    }

    public async Task EliminarAsync(int id)
    {
        var horario = await _db.HorariosDentista.FirstOrDefaultAsync(h => h.Id == id)
                      ?? throw new NoEncontradoException("horario", id);

        // Si el dentista ya tiene citas futuras en ese día, quitar el bloque las dejaría fuera de horario.
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var citasFuturas = await _db.Citas
            .Where(c => c.DentistaId == horario.DentistaId && !c.Cancelada && c.Fecha >= hoy)
            .ToListAsync();

        var afectadas = citasFuturas.Count(c =>
            c.Fecha.DayOfWeek == horario.DiaSemana &&
            c.Hora >= horario.HoraInicio && c.Hora < horario.HoraFin);

        if (afectadas > 0)
            throw new ConflictoException(
                $"No se puede eliminar el horario porque hay {afectadas} cita(s) futura(s) agendada(s) dentro de ese bloque.");

        _db.HorariosDentista.Remove(horario);
        await _db.SaveChangesAsync();
    }

    private async Task ValidarDentistaAsync(int dentistaId)
    {
        var dentista = await _db.Dentistas.AsNoTracking().FirstOrDefaultAsync(d => d.Id == dentistaId)
                       ?? throw new NoEncontradoException("dentista", dentistaId);

        if (!dentista.Activo)
            throw new ReglaNegocioException($"El dentista {dentista.Nombre} {dentista.Apellido} está inactivo.");
    }

    private static void ValidarRangoHorario(GuardarHorarioDentistaDto dto)
    {
        if (dto.HoraFin <= dto.HoraInicio)
            throw new ReglaNegocioException("La hora de fin debe ser posterior a la hora de inicio.");

        var minutos = (dto.HoraFin - dto.HoraInicio).TotalMinutes;
        if (minutos < MinutosMinimosBloque)
            throw new ReglaNegocioException($"El bloque de horario debe tener al menos {MinutosMinimosBloque} minutos.");
    }

    private async Task ValidarSolapamientoAsync(int dentistaId, DayOfWeek dia, TimeOnly inicio, TimeOnly fin, int? idExcluido)
    {
        var existentes = await _db.HorariosDentista.AsNoTracking()
            .Where(h => h.DentistaId == dentistaId && h.DiaSemana == dia && (idExcluido == null || h.Id != idExcluido))
            .ToListAsync();

        var choque = existentes.FirstOrDefault(h => inicio < h.HoraFin && h.HoraInicio < fin);

        if (choque is not null)
            throw new ConflictoException(
                $"El horario se solapa con otro bloque del mismo dentista el {Mapeos.NombreDia(dia)} " +
                $"({choque.HoraInicio:HH\\:mm} - {choque.HoraFin:HH\\:mm}).");
    }
}
