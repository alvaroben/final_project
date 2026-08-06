using ConsultorioDental.API.Common;
using ConsultorioDental.API.Data;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.API.Services;

public interface ICitaService
{
    Task<IEnumerable<CitaDto>> ListarAsync(FiltroCitasDto filtro);
    Task<CitaDto> ObtenerPorIdAsync(int id);
    Task<CitaDto> CrearAsync(GuardarCitaDto dto);
    Task<CitaDto> ActualizarAsync(int id, GuardarCitaDto dto);
    Task<CitaDto> CancelarAsync(int id, CancelarCitaDto dto);
    Task EliminarAsync(int id);
    Task<object> ObtenerResumenAsync();
    Task<object> ObtenerDisponibilidadAsync(int dentistaId, DateOnly fecha);
}

public class CitaService : ICitaService
{
    private const int DuracionMinima = 5;
    private const int DuracionMaxima = 480;
    private const int AniosMaximosAFuturo = 2;

    private readonly ConsultorioDbContext _db;

    public CitaService(ConsultorioDbContext db) => _db = db;

    // Punto único de "ahora": facilita que todos los cálculos usen la misma referencia.
    private static DateTime Ahora => DateTime.Now;

    private IQueryable<Cita> CitasConRelaciones() => _db.Citas
        .Include(c => c.Paciente)
        .Include(c => c.Dentista).ThenInclude(d => d!.Especialidad)
        .Include(c => c.Motivo)
        .Include(c => c.Servicio)
        .Include(c => c.Consultorio);

    public async Task<IEnumerable<CitaDto>> ListarAsync(FiltroCitasDto filtro)
    {
        if (filtro.FechaDesde.HasValue && filtro.FechaHasta.HasValue && filtro.FechaDesde > filtro.FechaHasta)
            throw new ReglaNegocioException("La fecha inicial del filtro no puede ser posterior a la fecha final.");

        var consulta = CitasConRelaciones().AsNoTracking();

        if (filtro.PacienteId.HasValue) consulta = consulta.Where(c => c.PacienteId == filtro.PacienteId.Value);
        if (filtro.DentistaId.HasValue) consulta = consulta.Where(c => c.DentistaId == filtro.DentistaId.Value);
        if (filtro.ConsultorioId.HasValue) consulta = consulta.Where(c => c.ConsultorioId == filtro.ConsultorioId.Value);
        if (filtro.ServicioId.HasValue) consulta = consulta.Where(c => c.ServicioId == filtro.ServicioId.Value);
        if (filtro.MotivoId.HasValue) consulta = consulta.Where(c => c.MotivoId == filtro.MotivoId.Value);
        if (filtro.FechaDesde.HasValue) consulta = consulta.Where(c => c.Fecha >= filtro.FechaDesde.Value);
        if (filtro.FechaHasta.HasValue) consulta = consulta.Where(c => c.Fecha <= filtro.FechaHasta.Value);

        var citas = await consulta.OrderBy(c => c.Fecha).ThenBy(c => c.Hora).ToListAsync();

        var referencia = Ahora;

        // El estado es calculado, por eso este filtro se aplica en memoria.
        if (filtro.Estado.HasValue)
            citas = citas.Where(c => c.CalcularEstado(referencia) == filtro.Estado.Value).ToList();

        return citas.Select(c => Mapeos.ACitaDto(c, referencia));
    }

    public async Task<CitaDto> ObtenerPorIdAsync(int id)
    {
        var cita = await CitasConRelaciones().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id)
                   ?? throw new NoEncontradoException("cita", id);

        return Mapeos.ACitaDto(cita, Ahora);
    }

    public async Task<CitaDto> CrearAsync(GuardarCitaDto dto)
    {
        dto.Hora = TruncarAMinutos(dto.Hora);
        var (servicio, duracion) = await ValidarReferenciasAsync(dto);

        ValidarFechaHora(dto.Fecha, dto.Hora, duracion);
        await ValidarHorarioDentistaAsync(dto.DentistaId, dto.Fecha, dto.Hora, duracion);
        await ValidarDisponibilidadAsync(dto, duracion, null);

        var cita = new Cita
        {
            PacienteId = dto.PacienteId,
            Fecha = dto.Fecha,
            Hora = dto.Hora,
            DuracionMinutos = duracion,
            DentistaId = dto.DentistaId,
            MotivoId = dto.MotivoId,
            ServicioId = dto.ServicioId,
            ConsultorioId = dto.ConsultorioId,
            CostoEstimado = servicio.Precio,
            Notas = dto.Notas?.Trim(),
            Cancelada = false,
            FechaRegistro = Ahora
        };

        _db.Citas.Add(cita);
        await _db.SaveChangesAsync();

        return await ObtenerPorIdAsync(cita.Id);
    }

    public async Task<CitaDto> ActualizarAsync(int id, GuardarCitaDto dto)
    {
        var cita = await _db.Citas.FirstOrDefaultAsync(c => c.Id == id)
                   ?? throw new NoEncontradoException("cita", id);

        var estadoActual = cita.CalcularEstado(Ahora);

        if (estadoActual is EstadoCita.EnProceso or EstadoCita.Finalizada)
            throw new ReglaNegocioException(
                $"No se puede modificar una cita en estado '{Mapeos.NombreEstado(estadoActual)}'.");

        if (cita.Cancelada)
            throw new ReglaNegocioException("No se puede modificar una cita cancelada. Registre una nueva cita.");

        dto.Hora = TruncarAMinutos(dto.Hora);
        var (servicio, duracion) = await ValidarReferenciasAsync(dto);

        ValidarFechaHora(dto.Fecha, dto.Hora, duracion);
        await ValidarHorarioDentistaAsync(dto.DentistaId, dto.Fecha, dto.Hora, duracion);
        await ValidarDisponibilidadAsync(dto, duracion, id);

        cita.PacienteId = dto.PacienteId;
        cita.Fecha = dto.Fecha;
        cita.Hora = dto.Hora;
        cita.DuracionMinutos = duracion;
        cita.DentistaId = dto.DentistaId;
        cita.MotivoId = dto.MotivoId;
        cita.ServicioId = dto.ServicioId;
        cita.ConsultorioId = dto.ConsultorioId;
        cita.CostoEstimado = servicio.Precio;
        cita.Notas = dto.Notas?.Trim();

        await _db.SaveChangesAsync();

        return await ObtenerPorIdAsync(id);
    }

    public async Task<CitaDto> CancelarAsync(int id, CancelarCitaDto dto)
    {
        var cita = await _db.Citas.FirstOrDefaultAsync(c => c.Id == id)
                   ?? throw new NoEncontradoException("cita", id);

        if (cita.Cancelada)
            throw new ReglaNegocioException("La cita ya se encuentra cancelada.");

        var estado = cita.CalcularEstado(Ahora);
        if (estado == EstadoCita.Finalizada)
            throw new ReglaNegocioException("No se puede cancelar una cita que ya finalizó.");

        cita.Cancelada = true;
        cita.MotivoCancelacion = dto.MotivoCancelacion.Trim();

        await _db.SaveChangesAsync();

        return await ObtenerPorIdAsync(id);
    }

    public async Task EliminarAsync(int id)
    {
        var cita = await _db.Citas.FirstOrDefaultAsync(c => c.Id == id)
                   ?? throw new NoEncontradoException("cita", id);

        var estado = cita.CalcularEstado(Ahora);

        // Las citas atendidas son historial clínico: no se borran.
        if (estado is EstadoCita.EnProceso or EstadoCita.Finalizada)
            throw new ConflictoException(
                $"No se puede eliminar una cita en estado '{Mapeos.NombreEstado(estado)}'. " +
                "Solo pueden eliminarse citas que aún no han iniciado.");

        _db.Citas.Remove(cita);
        await _db.SaveChangesAsync();
    }

    public async Task<object> ObtenerResumenAsync()
    {
        var referencia = Ahora;
        var citas = await _db.Citas.AsNoTracking().ToListAsync();
        var hoy = DateOnly.FromDateTime(referencia);

        return new
        {
            Total = citas.Count,
            Vigentes = citas.Count(c => c.CalcularEstado(referencia) == EstadoCita.Vigente),
            EnProceso = citas.Count(c => c.CalcularEstado(referencia) == EstadoCita.EnProceso),
            Finalizadas = citas.Count(c => c.CalcularEstado(referencia) == EstadoCita.Finalizada),
            Canceladas = citas.Count(c => c.Cancelada),
            CitasDeHoy = citas.Count(c => c.Fecha == hoy && !c.Cancelada),
            IngresoEstimadoPendiente = citas
                .Where(c => !c.Cancelada && c.CalcularEstado(referencia) != EstadoCita.Finalizada)
                .Sum(c => c.CostoEstimado),
            FechaConsulta = referencia
        };
    }

    public async Task<object> ObtenerDisponibilidadAsync(int dentistaId, DateOnly fecha)
    {
        var dentista = await _db.Dentistas.AsNoTracking()
            .Include(d => d.Horarios)
            .FirstOrDefaultAsync(d => d.Id == dentistaId) ?? throw new NoEncontradoException("dentista", dentistaId);

        var bloques = dentista.Horarios
            .Where(h => h.Activo && h.DiaSemana == fecha.DayOfWeek)
            .OrderBy(h => h.HoraInicio)
            .ToList();

        var ocupadas = await _db.Citas.AsNoTracking()
            .Where(c => c.DentistaId == dentistaId && c.Fecha == fecha && !c.Cancelada)
            .OrderBy(c => c.Hora)
            .Select(c => new { c.Id, c.Hora, c.DuracionMinutos })
            .ToListAsync();

        return new
        {
            DentistaId = dentistaId,
            Dentista = $"{dentista.Nombre} {dentista.Apellido}",
            Fecha = fecha,
            DiaSemana = Mapeos.NombreDia(fecha.DayOfWeek),
            AtiendeEseDia = bloques.Count > 0,
            BloquesDeTrabajo = bloques.Select(b => new
            {
                b.HoraInicio,
                b.HoraFin,
                MinutosDisponibles = (int)(b.HoraFin - b.HoraInicio).TotalMinutes
            }),
            CitasAgendadas = ocupadas.Select(o => new
            {
                o.Id,
                HoraInicio = o.Hora,
                HoraFin = o.Hora.AddMinutes(o.DuracionMinutos),
                o.DuracionMinutos
            }),
            MinutosOcupados = ocupadas.Sum(o => o.DuracionMinutos)
        };
    }

    // ---------------- Validaciones ----------------

    /// <summary>La agenda trabaja al minuto: los segundos que llegue el cliente se descartan.</summary>
    private static TimeOnly TruncarAMinutos(TimeOnly hora) => new(hora.Hour, hora.Minute);

    /// <summary>Verifica que todas las llaves foráneas existan, estén activas, y resuelve la duración final.</summary>
    private async Task<(Servicio Servicio, int Duracion)> ValidarReferenciasAsync(GuardarCitaDto dto)
    {
        var paciente = await _db.Pacientes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == dto.PacienteId)
                       ?? throw new NoEncontradoException("paciente", dto.PacienteId);
        if (!paciente.Activo)
            throw new ReglaNegocioException($"El paciente {paciente.Nombre} {paciente.Apellido} está inactivo.");

        var dentista = await _db.Dentistas.AsNoTracking().FirstOrDefaultAsync(d => d.Id == dto.DentistaId)
                       ?? throw new NoEncontradoException("dentista", dto.DentistaId);
        if (!dentista.Activo)
            throw new ReglaNegocioException($"El dentista {dentista.Nombre} {dentista.Apellido} está inactivo.");

        var motivo = await _db.Motivos.AsNoTracking().FirstOrDefaultAsync(m => m.Id == dto.MotivoId)
                     ?? throw new NoEncontradoException("motivo", dto.MotivoId);
        if (!motivo.Activo)
            throw new ReglaNegocioException($"El motivo '{motivo.Nombre}' está inactivo.");

        var servicio = await _db.Servicios.AsNoTracking().FirstOrDefaultAsync(s => s.Id == dto.ServicioId)
                       ?? throw new NoEncontradoException("servicio", dto.ServicioId);
        if (!servicio.Activo)
            throw new ReglaNegocioException($"El servicio '{servicio.Nombre}' está inactivo.");

        var consultorio = await _db.Consultorios.AsNoTracking().FirstOrDefaultAsync(c => c.Id == dto.ConsultorioId)
                          ?? throw new NoEncontradoException("consultorio", dto.ConsultorioId);
        if (!consultorio.Activo)
            throw new ReglaNegocioException($"El consultorio '{consultorio.Codigo}' está inactivo.");

        var duracion = dto.DuracionMinutos > 0 ? dto.DuracionMinutos : servicio.DuracionMinutos;

        if (duracion is < DuracionMinima or > DuracionMaxima)
            throw new ReglaNegocioException(
                $"La duración de la cita debe estar entre {DuracionMinima} y {DuracionMaxima} minutos.");

        return (servicio, duracion);
    }

    private static void ValidarFechaHora(DateOnly fecha, TimeOnly hora, int duracion)
    {
        var inicio = fecha.ToDateTime(hora);

        if (inicio <= Ahora)
            throw new ReglaNegocioException(
                $"No se puede agendar una cita en el pasado. La fecha y hora indicadas ({inicio:dd/MM/yyyy HH:mm}) ya transcurrieron.");

        if (fecha > DateOnly.FromDateTime(Ahora.AddYears(AniosMaximosAFuturo)))
            throw new ReglaNegocioException(
                $"No se pueden agendar citas con más de {AniosMaximosAFuturo} años de anticipación.");

        // Una cita no puede cruzar la medianoche: fecha y hora quedarían inconsistentes.
        if (inicio.AddMinutes(duracion).Date != inicio.Date)
            throw new ReglaNegocioException(
                "La cita no puede extenderse más allá del final del día. Reduzca la duración o adelante la hora.");
    }

    private async Task ValidarHorarioDentistaAsync(int dentistaId, DateOnly fecha, TimeOnly hora, int duracion)
    {
        var horarios = await _db.HorariosDentista.AsNoTracking()
            .Where(h => h.DentistaId == dentistaId && h.Activo)
            .ToListAsync();

        var delDia = horarios.Where(h => h.DiaSemana == fecha.DayOfWeek).ToList();

        if (delDia.Count == 0)
            throw new ReglaNegocioException(
                $"El dentista no tiene horario de atención los días {Mapeos.NombreDia(fecha.DayOfWeek)}.");

        var horaFin = hora.AddMinutes(duracion);
        var cabe = delDia.Any(h => hora >= h.HoraInicio && horaFin <= h.HoraFin);

        if (!cabe)
        {
            var disponibles = string.Join(", ", delDia.OrderBy(h => h.HoraInicio)
                .Select(h => $"{h.HoraInicio:HH\\:mm}-{h.HoraFin:HH\\:mm}"));

            throw new ReglaNegocioException(
                $"La cita ({hora:HH\\:mm}-{horaFin:HH\\:mm}) queda fuera del horario del dentista " +
                $"para el {Mapeos.NombreDia(fecha.DayOfWeek)}. Horario disponible: {disponibles}.");
        }
    }

    /// <summary>Impide choques de agenda para el dentista, el consultorio y el paciente.</summary>
    private async Task ValidarDisponibilidadAsync(GuardarCitaDto dto, int duracion, int? idExcluido)
    {
        var inicio = dto.Hora;
        var fin = dto.Hora.AddMinutes(duracion);

        var mismasFecha = await _db.Citas.AsNoTracking()
            .Include(c => c.Paciente)
            .Include(c => c.Consultorio)
            .Where(c => c.Fecha == dto.Fecha && !c.Cancelada && (idExcluido == null || c.Id != idExcluido))
            .Where(c => c.DentistaId == dto.DentistaId
                        || c.ConsultorioId == dto.ConsultorioId
                        || c.PacienteId == dto.PacienteId)
            .ToListAsync();

        foreach (var otra in mismasFecha)
        {
            var otroInicio = otra.Hora;
            var otroFin = otra.Hora.AddMinutes(otra.DuracionMinutos);

            var seSolapan = inicio < otroFin && otroInicio < fin;
            if (!seSolapan) continue;

            var rango = $"{otroInicio:HH\\:mm} - {otroFin:HH\\:mm}";

            if (otra.DentistaId == dto.DentistaId)
                throw new ConflictoException(
                    $"El dentista ya tiene la cita #{otra.Id} agendada el {dto.Fecha:dd/MM/yyyy} de {rango}.");

            if (otra.ConsultorioId == dto.ConsultorioId)
                throw new ConflictoException(
                    $"El consultorio '{otra.Consultorio?.Codigo}' está ocupado el {dto.Fecha:dd/MM/yyyy} de {rango} (cita #{otra.Id}).");

            throw new ConflictoException(
                $"El paciente ya tiene la cita #{otra.Id} agendada el {dto.Fecha:dd/MM/yyyy} de {rango}.");
        }
    }
}
