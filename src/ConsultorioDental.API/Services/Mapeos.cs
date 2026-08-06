using System.Globalization;
using ConsultorioDental.API.DTOs;
using ConsultorioDental.API.Models;

namespace ConsultorioDental.API.Services;

/// <summary>
/// Conversión de entidades a DTOs. Aquí viven también los cálculos automáticos
/// que se exponen al cliente (edad, estado de la cita, tiempo restante).
/// </summary>
public static class Mapeos
{
    private static readonly CultureInfo Cultura = new("es-DO");

    public static UsuarioDto AUsuarioDto(Usuario u) => new()
    {
        Id = u.Id,
        NombreUsuario = u.NombreUsuario,
        NombreCompleto = u.NombreCompleto,
        Correo = u.Correo,
        Rol = u.Rol,
        RolNombre = u.Rol.ToString(),
        Activo = u.Activo,
        FechaCreacion = u.FechaCreacion,
        UltimoAcceso = u.UltimoAcceso
    };

    public static int CalcularEdad(DateOnly fechaNacimiento, DateOnly hoy)
    {
        var edad = hoy.Year - fechaNacimiento.Year;
        if (fechaNacimiento > hoy.AddYears(-edad)) edad--;
        return edad < 0 ? 0 : edad;
    }

    public static PacienteDto APacienteDto(Paciente p, int totalCitas = 0) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Apellido = p.Apellido,
        NombreCompleto = $"{p.Nombre} {p.Apellido}",
        Documento = p.Documento,
        FechaNacimiento = p.FechaNacimiento,
        Edad = CalcularEdad(p.FechaNacimiento, DateOnly.FromDateTime(DateTime.Now)),
        Telefono = p.Telefono,
        Correo = p.Correo,
        Direccion = p.Direccion,
        Alergias = p.Alergias,
        Activo = p.Activo,
        FechaRegistro = p.FechaRegistro,
        TotalCitas = totalCitas
    };

    public static EspecialidadDto AEspecialidadDto(Especialidad e, int totalDentistas = 0) => new()
    {
        Id = e.Id,
        Nombre = e.Nombre,
        Descripcion = e.Descripcion,
        Activa = e.Activa,
        TotalDentistas = totalDentistas
    };

    public static string NombrePrioridad(int prioridad) => prioridad switch
    {
        3 => "Alta",
        2 => "Media",
        _ => "Baja"
    };

    public static MotivoDto AMotivoDto(Motivo m, int totalCitas = 0) => new()
    {
        Id = m.Id,
        Nombre = m.Nombre,
        Descripcion = m.Descripcion,
        Prioridad = m.Prioridad,
        PrioridadNombre = NombrePrioridad(m.Prioridad),
        Activo = m.Activo,
        TotalCitas = totalCitas
    };

    public static ServicioDto AServicioDto(Servicio s, int totalCitas = 0) => new()
    {
        Id = s.Id,
        Nombre = s.Nombre,
        Descripcion = s.Descripcion,
        Precio = s.Precio,
        DuracionMinutos = s.DuracionMinutos,
        Activo = s.Activo,
        TotalCitas = totalCitas
    };

    public static ConsultorioDto AConsultorioDto(Consultorio c, int totalCitas = 0) => new()
    {
        Id = c.Id,
        Codigo = c.Codigo,
        Nombre = c.Nombre,
        Ubicacion = c.Ubicacion,
        Capacidad = c.Capacidad,
        Activo = c.Activo,
        TotalCitas = totalCitas
    };

    public static string NombreDia(DayOfWeek dia) =>
        Cultura.DateTimeFormat.GetDayName(dia) is { Length: > 0 } nombre
            ? char.ToUpper(nombre[0], Cultura) + nombre[1..]
            : dia.ToString();

    public static HorarioDentistaDto AHorarioDto(HorarioDentista h) => new()
    {
        Id = h.Id,
        DentistaId = h.DentistaId,
        DentistaNombre = h.Dentista is null ? string.Empty : $"{h.Dentista.Nombre} {h.Dentista.Apellido}",
        DiaSemana = (int)h.DiaSemana,
        DiaSemanaNombre = NombreDia(h.DiaSemana),
        HoraInicio = h.HoraInicio,
        HoraFin = h.HoraFin,
        MinutosDisponibles = (int)(h.HoraFin - h.HoraInicio).TotalMinutes,
        Observacion = h.Observacion,
        Activo = h.Activo
    };

    public static DentistaDto ADentistaDto(Dentista d, int totalCitas = 0) => new()
    {
        Id = d.Id,
        Nombre = d.Nombre,
        Apellido = d.Apellido,
        NombreCompleto = $"{d.Nombre} {d.Apellido}",
        NumeroLicencia = d.NumeroLicencia,
        Correo = d.Correo,
        Telefono = d.Telefono,
        EspecialidadId = d.EspecialidadId,
        EspecialidadNombre = d.Especialidad?.Nombre ?? string.Empty,
        Activo = d.Activo,
        TotalCitas = totalCitas,
        Horarios = d.Horarios.OrderBy(h => h.DiaSemana).ThenBy(h => h.HoraInicio).Select(AHorarioDto).ToList()
    };

    public static string NombreEstado(EstadoCita estado) => estado switch
    {
        EstadoCita.Vigente => "Vigente",
        EstadoCita.EnProceso => "En proceso",
        EstadoCita.Finalizada => "Finalizada",
        _ => "Cancelada"
    };

    /// <summary>Días, horas y minutos que faltan para el inicio de la cita.</summary>
    public static TiempoRestanteDto CalcularTiempoRestante(Cita cita, DateTime referencia)
    {
        var estado = cita.CalcularEstado(referencia);

        if (estado != EstadoCita.Vigente)
        {
            return new TiempoRestanteDto
            {
                Descripcion = estado switch
                {
                    EstadoCita.EnProceso => "La cita está en proceso.",
                    EstadoCita.Finalizada => "La cita ya finalizó.",
                    _ => "La cita fue cancelada."
                }
            };
        }

        var restante = cita.FechaHoraInicio - referencia;
        var dias = restante.Days;
        var horas = restante.Hours;
        var minutos = restante.Minutes;

        var partes = new List<string>();
        if (dias > 0) partes.Add($"{dias} día{(dias == 1 ? "" : "s")}");
        if (horas > 0) partes.Add($"{horas} hora{(horas == 1 ? "" : "s")}");
        if (minutos > 0 || partes.Count == 0) partes.Add($"{minutos} minuto{(minutos == 1 ? "" : "s")}");

        return new TiempoRestanteDto
        {
            Dias = dias,
            Horas = horas,
            Minutos = minutos,
            TotalMinutos = Math.Round(restante.TotalMinutes, 2),
            Descripcion = $"Faltan {string.Join(", ", partes)} para la cita."
        };
    }

    public static CitaDto ACitaDto(Cita c, DateTime referencia)
    {
        var estado = c.CalcularEstado(referencia);

        return new CitaDto
        {
            Id = c.Id,
            PacienteId = c.PacienteId,
            PacienteNombre = c.Paciente is null ? string.Empty : $"{c.Paciente.Nombre} {c.Paciente.Apellido}",
            PacienteDocumento = c.Paciente?.Documento ?? string.Empty,
            Fecha = c.Fecha,
            Hora = c.Hora,
            DuracionMinutos = c.DuracionMinutos,
            DentistaId = c.DentistaId,
            DentistaNombre = c.Dentista is null ? string.Empty : $"{c.Dentista.Nombre} {c.Dentista.Apellido}",
            DentistaEspecialidad = c.Dentista?.Especialidad?.Nombre ?? string.Empty,
            MotivoId = c.MotivoId,
            MotivoNombre = c.Motivo?.Nombre ?? string.Empty,
            ServicioId = c.ServicioId,
            ServicioNombre = c.Servicio?.Nombre ?? string.Empty,
            ConsultorioId = c.ConsultorioId,
            ConsultorioNombre = c.Consultorio is null ? string.Empty : $"{c.Consultorio.Codigo} - {c.Consultorio.Nombre}",
            CostoEstimado = c.CostoEstimado,
            Notas = c.Notas,
            FechaHoraInicio = c.FechaHoraInicio,
            FechaHoraFin = c.FechaHoraFin,
            Estado = estado,
            EstadoNombre = NombreEstado(estado),
            TiempoRestante = CalcularTiempoRestante(c, referencia),
            MinutosParaFinalizar = estado == EstadoCita.EnProceso
                ? (int)Math.Ceiling((c.FechaHoraFin - referencia).TotalMinutes)
                : null,
            Cancelada = c.Cancelada,
            MotivoCancelacion = c.MotivoCancelacion,
            FechaRegistro = c.FechaRegistro
        };
    }
}
