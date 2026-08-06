using ConsultorioDental.API.Common;
using ConsultorioDental.API.Data;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.API.Services;

// ================= Especialidad =================

public interface IEspecialidadService
{
    Task<IEnumerable<EspecialidadDto>> ListarAsync(bool? activa, string? busqueda);
    Task<EspecialidadDto> ObtenerPorIdAsync(int id);
    Task<EspecialidadDto> CrearAsync(GuardarEspecialidadDto dto);
    Task<EspecialidadDto> ActualizarAsync(int id, GuardarEspecialidadDto dto);
    Task EliminarAsync(int id);
}

public class EspecialidadService : IEspecialidadService
{
    private readonly ConsultorioDbContext _db;

    public EspecialidadService(ConsultorioDbContext db) => _db = db;

    public async Task<IEnumerable<EspecialidadDto>> ListarAsync(bool? activa, string? busqueda)
    {
        var consulta = _db.Especialidades.AsNoTracking().AsQueryable();

        if (activa.HasValue) consulta = consulta.Where(e => e.Activa == activa.Value);
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(e => e.Nombre.Contains(texto));
        }

        var datos = await consulta.OrderBy(e => e.Nombre)
            .Select(e => new { Especialidad = e, Total = e.Dentistas.Count })
            .ToListAsync();

        return datos.Select(x => Mapeos.AEspecialidadDto(x.Especialidad, x.Total));
    }

    public async Task<EspecialidadDto> ObtenerPorIdAsync(int id)
    {
        var datos = await _db.Especialidades.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new { Especialidad = e, Total = e.Dentistas.Count })
            .FirstOrDefaultAsync() ?? throw new NoEncontradoException("especialidad", id);

        return Mapeos.AEspecialidadDto(datos.Especialidad, datos.Total);
    }

    public async Task<EspecialidadDto> CrearAsync(GuardarEspecialidadDto dto)
    {
        var nombre = dto.Nombre.Trim();

        if (await _db.Especialidades.AnyAsync(e => e.Nombre == nombre))
            throw new ConflictoException($"Ya existe una especialidad con el nombre '{nombre}'.");

        var especialidad = new Especialidad
        {
            Nombre = nombre,
            Descripcion = dto.Descripcion?.Trim(),
            Activa = dto.Activa
        };

        _db.Especialidades.Add(especialidad);
        await _db.SaveChangesAsync();

        return Mapeos.AEspecialidadDto(especialidad);
    }

    public async Task<EspecialidadDto> ActualizarAsync(int id, GuardarEspecialidadDto dto)
    {
        var especialidad = await _db.Especialidades.FirstOrDefaultAsync(e => e.Id == id)
                           ?? throw new NoEncontradoException("especialidad", id);

        var nombre = dto.Nombre.Trim();

        if (await _db.Especialidades.AnyAsync(e => e.Nombre == nombre && e.Id != id))
            throw new ConflictoException($"Ya existe otra especialidad con el nombre '{nombre}'.");

        especialidad.Nombre = nombre;
        especialidad.Descripcion = dto.Descripcion?.Trim();
        especialidad.Activa = dto.Activa;

        await _db.SaveChangesAsync();

        var total = await _db.Dentistas.CountAsync(d => d.EspecialidadId == id);
        return Mapeos.AEspecialidadDto(especialidad, total);
    }

    public async Task EliminarAsync(int id)
    {
        var especialidad = await _db.Especialidades.FirstOrDefaultAsync(e => e.Id == id)
                           ?? throw new NoEncontradoException("especialidad", id);

        var dentistas = await _db.Dentistas.CountAsync(d => d.EspecialidadId == id);
        if (dentistas > 0)
            throw new ConflictoException(
                $"No se puede eliminar la especialidad porque está asignada a {dentistas} dentista(s).");

        _db.Especialidades.Remove(especialidad);
        await _db.SaveChangesAsync();
    }
}

// ================= Motivo =================

public interface IMotivoService
{
    Task<IEnumerable<MotivoDto>> ListarAsync(bool? activo, string? busqueda);
    Task<MotivoDto> ObtenerPorIdAsync(int id);
    Task<MotivoDto> CrearAsync(GuardarMotivoDto dto);
    Task<MotivoDto> ActualizarAsync(int id, GuardarMotivoDto dto);
    Task EliminarAsync(int id);
}

public class MotivoService : IMotivoService
{
    private readonly ConsultorioDbContext _db;

    public MotivoService(ConsultorioDbContext db) => _db = db;

    public async Task<IEnumerable<MotivoDto>> ListarAsync(bool? activo, string? busqueda)
    {
        var consulta = _db.Motivos.AsNoTracking().AsQueryable();

        if (activo.HasValue) consulta = consulta.Where(m => m.Activo == activo.Value);
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(m => m.Nombre.Contains(texto));
        }

        var datos = await consulta.OrderByDescending(m => m.Prioridad).ThenBy(m => m.Nombre)
            .Select(m => new { Motivo = m, Total = m.Citas.Count })
            .ToListAsync();

        return datos.Select(x => Mapeos.AMotivoDto(x.Motivo, x.Total));
    }

    public async Task<MotivoDto> ObtenerPorIdAsync(int id)
    {
        var datos = await _db.Motivos.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new { Motivo = m, Total = m.Citas.Count })
            .FirstOrDefaultAsync() ?? throw new NoEncontradoException("motivo", id);

        return Mapeos.AMotivoDto(datos.Motivo, datos.Total);
    }

    public async Task<MotivoDto> CrearAsync(GuardarMotivoDto dto)
    {
        var nombre = dto.Nombre.Trim();

        if (await _db.Motivos.AnyAsync(m => m.Nombre == nombre))
            throw new ConflictoException($"Ya existe un motivo con el nombre '{nombre}'.");

        var motivo = new Motivo
        {
            Nombre = nombre,
            Descripcion = dto.Descripcion?.Trim(),
            Prioridad = dto.Prioridad,
            Activo = dto.Activo
        };

        _db.Motivos.Add(motivo);
        await _db.SaveChangesAsync();

        return Mapeos.AMotivoDto(motivo);
    }

    public async Task<MotivoDto> ActualizarAsync(int id, GuardarMotivoDto dto)
    {
        var motivo = await _db.Motivos.FirstOrDefaultAsync(m => m.Id == id)
                     ?? throw new NoEncontradoException("motivo", id);

        var nombre = dto.Nombre.Trim();

        if (await _db.Motivos.AnyAsync(m => m.Nombre == nombre && m.Id != id))
            throw new ConflictoException($"Ya existe otro motivo con el nombre '{nombre}'.");

        motivo.Nombre = nombre;
        motivo.Descripcion = dto.Descripcion?.Trim();
        motivo.Prioridad = dto.Prioridad;
        motivo.Activo = dto.Activo;

        await _db.SaveChangesAsync();

        var total = await _db.Citas.CountAsync(c => c.MotivoId == id);
        return Mapeos.AMotivoDto(motivo, total);
    }

    public async Task EliminarAsync(int id)
    {
        var motivo = await _db.Motivos.FirstOrDefaultAsync(m => m.Id == id)
                     ?? throw new NoEncontradoException("motivo", id);

        var citas = await _db.Citas.CountAsync(c => c.MotivoId == id);
        if (citas > 0)
            throw new ConflictoException($"No se puede eliminar el motivo porque está asociado a {citas} cita(s).");

        _db.Motivos.Remove(motivo);
        await _db.SaveChangesAsync();
    }
}

// ================= Servicio =================

public interface IServicioService
{
    Task<IEnumerable<ServicioDto>> ListarAsync(bool? activo, string? busqueda);
    Task<ServicioDto> ObtenerPorIdAsync(int id);
    Task<ServicioDto> CrearAsync(GuardarServicioDto dto);
    Task<ServicioDto> ActualizarAsync(int id, GuardarServicioDto dto);
    Task EliminarAsync(int id);
}

public class ServicioService : IServicioService
{
    private readonly ConsultorioDbContext _db;

    public ServicioService(ConsultorioDbContext db) => _db = db;

    public async Task<IEnumerable<ServicioDto>> ListarAsync(bool? activo, string? busqueda)
    {
        var consulta = _db.Servicios.AsNoTracking().AsQueryable();

        if (activo.HasValue) consulta = consulta.Where(s => s.Activo == activo.Value);
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(s => s.Nombre.Contains(texto));
        }

        var datos = await consulta.OrderBy(s => s.Nombre)
            .Select(s => new { Servicio = s, Total = s.Citas.Count })
            .ToListAsync();

        return datos.Select(x => Mapeos.AServicioDto(x.Servicio, x.Total));
    }

    public async Task<ServicioDto> ObtenerPorIdAsync(int id)
    {
        var datos = await _db.Servicios.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new { Servicio = s, Total = s.Citas.Count })
            .FirstOrDefaultAsync() ?? throw new NoEncontradoException("servicio", id);

        return Mapeos.AServicioDto(datos.Servicio, datos.Total);
    }

    public async Task<ServicioDto> CrearAsync(GuardarServicioDto dto)
    {
        var nombre = dto.Nombre.Trim();

        if (await _db.Servicios.AnyAsync(s => s.Nombre == nombre))
            throw new ConflictoException($"Ya existe un servicio con el nombre '{nombre}'.");

        var servicio = new Servicio
        {
            Nombre = nombre,
            Descripcion = dto.Descripcion?.Trim(),
            Precio = dto.Precio,
            DuracionMinutos = dto.DuracionMinutos,
            Activo = dto.Activo
        };

        _db.Servicios.Add(servicio);
        await _db.SaveChangesAsync();

        return Mapeos.AServicioDto(servicio);
    }

    public async Task<ServicioDto> ActualizarAsync(int id, GuardarServicioDto dto)
    {
        var servicio = await _db.Servicios.FirstOrDefaultAsync(s => s.Id == id)
                       ?? throw new NoEncontradoException("servicio", id);

        var nombre = dto.Nombre.Trim();

        if (await _db.Servicios.AnyAsync(s => s.Nombre == nombre && s.Id != id))
            throw new ConflictoException($"Ya existe otro servicio con el nombre '{nombre}'.");

        servicio.Nombre = nombre;
        servicio.Descripcion = dto.Descripcion?.Trim();
        servicio.Precio = dto.Precio;
        servicio.DuracionMinutos = dto.DuracionMinutos;
        servicio.Activo = dto.Activo;

        await _db.SaveChangesAsync();

        var total = await _db.Citas.CountAsync(c => c.ServicioId == id);
        return Mapeos.AServicioDto(servicio, total);
    }

    public async Task EliminarAsync(int id)
    {
        var servicio = await _db.Servicios.FirstOrDefaultAsync(s => s.Id == id)
                       ?? throw new NoEncontradoException("servicio", id);

        var citas = await _db.Citas.CountAsync(c => c.ServicioId == id);
        if (citas > 0)
            throw new ConflictoException($"No se puede eliminar el servicio porque está asociado a {citas} cita(s).");

        _db.Servicios.Remove(servicio);
        await _db.SaveChangesAsync();
    }
}

// ================= Consultorio =================

public interface IConsultorioService
{
    Task<IEnumerable<ConsultorioDto>> ListarAsync(bool? activo, string? busqueda);
    Task<ConsultorioDto> ObtenerPorIdAsync(int id);
    Task<ConsultorioDto> CrearAsync(GuardarConsultorioDto dto);
    Task<ConsultorioDto> ActualizarAsync(int id, GuardarConsultorioDto dto);
    Task EliminarAsync(int id);
}

public class ConsultorioService : IConsultorioService
{
    private readonly ConsultorioDbContext _db;

    public ConsultorioService(ConsultorioDbContext db) => _db = db;

    public async Task<IEnumerable<ConsultorioDto>> ListarAsync(bool? activo, string? busqueda)
    {
        var consulta = _db.Consultorios.AsNoTracking().AsQueryable();

        if (activo.HasValue) consulta = consulta.Where(c => c.Activo == activo.Value);
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(c => c.Nombre.Contains(texto) || c.Codigo.Contains(texto));
        }

        var datos = await consulta.OrderBy(c => c.Codigo)
            .Select(c => new { Consultorio = c, Total = c.Citas.Count })
            .ToListAsync();

        return datos.Select(x => Mapeos.AConsultorioDto(x.Consultorio, x.Total));
    }

    public async Task<ConsultorioDto> ObtenerPorIdAsync(int id)
    {
        var datos = await _db.Consultorios.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new { Consultorio = c, Total = c.Citas.Count })
            .FirstOrDefaultAsync() ?? throw new NoEncontradoException("consultorio", id);

        return Mapeos.AConsultorioDto(datos.Consultorio, datos.Total);
    }

    public async Task<ConsultorioDto> CrearAsync(GuardarConsultorioDto dto)
    {
        var codigo = dto.Codigo.Trim().ToUpperInvariant();

        if (await _db.Consultorios.AnyAsync(c => c.Codigo == codigo))
            throw new ConflictoException($"Ya existe un consultorio con el código '{codigo}'.");

        var consultorio = new Consultorio
        {
            Codigo = codigo,
            Nombre = dto.Nombre.Trim(),
            Ubicacion = dto.Ubicacion?.Trim(),
            Capacidad = dto.Capacidad,
            Activo = dto.Activo
        };

        _db.Consultorios.Add(consultorio);
        await _db.SaveChangesAsync();

        return Mapeos.AConsultorioDto(consultorio);
    }

    public async Task<ConsultorioDto> ActualizarAsync(int id, GuardarConsultorioDto dto)
    {
        var consultorio = await _db.Consultorios.FirstOrDefaultAsync(c => c.Id == id)
                          ?? throw new NoEncontradoException("consultorio", id);

        var codigo = dto.Codigo.Trim().ToUpperInvariant();

        if (await _db.Consultorios.AnyAsync(c => c.Codigo == codigo && c.Id != id))
            throw new ConflictoException($"Ya existe otro consultorio con el código '{codigo}'.");

        consultorio.Codigo = codigo;
        consultorio.Nombre = dto.Nombre.Trim();
        consultorio.Ubicacion = dto.Ubicacion?.Trim();
        consultorio.Capacidad = dto.Capacidad;
        consultorio.Activo = dto.Activo;

        await _db.SaveChangesAsync();

        var total = await _db.Citas.CountAsync(c => c.ConsultorioId == id);
        return Mapeos.AConsultorioDto(consultorio, total);
    }

    public async Task EliminarAsync(int id)
    {
        var consultorio = await _db.Consultorios.FirstOrDefaultAsync(c => c.Id == id)
                          ?? throw new NoEncontradoException("consultorio", id);

        var citas = await _db.Citas.CountAsync(c => c.ConsultorioId == id);
        if (citas > 0)
            throw new ConflictoException($"No se puede eliminar el consultorio porque está asociado a {citas} cita(s).");

        _db.Consultorios.Remove(consultorio);
        await _db.SaveChangesAsync();
    }
}
