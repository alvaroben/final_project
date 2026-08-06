using ConsultorioDental.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.API.Data;

/// <summary>
/// Carga inicial de datos. Se ejecuta al iniciar la aplicación y solo inserta
/// lo que aún no existe, por lo que es seguro ejecutarla varias veces.
/// </summary>
public static class SeedData
{
    public static async Task InicializarAsync(IServiceProvider proveedor)
    {
        using var alcance = proveedor.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<ConsultorioDbContext>();
        var config = alcance.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = alcance.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        await db.Database.MigrateAsync();

        await SembrarAdministradorAsync(db, config, logger);
        await SembrarCatalogosAsync(db, logger);
        await SembrarCitasEjemploAsync(db, logger);
    }

    private static async Task SembrarAdministradorAsync(ConsultorioDbContext db, IConfiguration config, ILogger logger)
    {
        var nombreUsuario = config["SeedAdmin:NombreUsuario"] ?? "admin";

        if (await db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario)) return;

        db.Usuarios.Add(new Usuario
        {
            NombreUsuario = nombreUsuario,
            NombreCompleto = config["SeedAdmin:NombreCompleto"] ?? "Administrador del Sistema",
            Correo = (config["SeedAdmin:Correo"] ?? "admin@consultoriodental.com").ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(config["SeedAdmin:Password"] ?? "Admin123*"),
            Rol = RolUsuario.Administrador,
            Activo = true,
            FechaCreacion = DateTime.Now
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Usuario administrador inicial creado: {Usuario}", nombreUsuario);
    }

    private static async Task SembrarCatalogosAsync(ConsultorioDbContext db, ILogger logger)
    {
        if (await db.Especialidades.AnyAsync()) return;

        var especialidades = new List<Especialidad>
        {
            new() { Nombre = "Odontología General", Descripcion = "Atención dental integral y preventiva." },
            new() { Nombre = "Ortodoncia", Descripcion = "Corrección de la posición de dientes y maxilares." },
            new() { Nombre = "Endodoncia", Descripcion = "Tratamiento de conductos radiculares." },
            new() { Nombre = "Periodoncia", Descripcion = "Tratamiento de encías y tejidos de soporte." },
            new() { Nombre = "Odontopediatría", Descripcion = "Atención dental para niños y adolescentes." }
        };
        db.Especialidades.AddRange(especialidades);

        var motivos = new List<Motivo>
        {
            new() { Nombre = "Dolor dental", Descripcion = "Molestia o dolor agudo en una o varias piezas.", Prioridad = 3 },
            new() { Nombre = "Limpieza de rutina", Descripcion = "Profilaxis y control preventivo.", Prioridad = 1 },
            new() { Nombre = "Revisión de ortodoncia", Descripcion = "Ajuste y seguimiento de brackets.", Prioridad = 2 },
            new() { Nombre = "Urgencia por trauma", Descripcion = "Golpe o fractura dental reciente.", Prioridad = 3 },
            new() { Nombre = "Evaluación inicial", Descripcion = "Primera consulta y diagnóstico.", Prioridad = 2 }
        };
        db.Motivos.AddRange(motivos);

        var servicios = new List<Servicio>
        {
            new() { Nombre = "Consulta general", Descripcion = "Evaluación y diagnóstico.", Precio = 1200m, DuracionMinutos = 30 },
            new() { Nombre = "Limpieza dental", Descripcion = "Profilaxis completa.", Precio = 2500m, DuracionMinutos = 45 },
            new() { Nombre = "Extracción simple", Descripcion = "Extracción de pieza sin complicación.", Precio = 3500m, DuracionMinutos = 45 },
            new() { Nombre = "Tratamiento de conducto", Descripcion = "Endodoncia unirradicular.", Precio = 9500m, DuracionMinutos = 90 },
            new() { Nombre = "Resina estética", Descripcion = "Restauración con resina del color del diente.", Precio = 3000m, DuracionMinutos = 60 },
            new() { Nombre = "Ajuste de brackets", Descripcion = "Control mensual de ortodoncia.", Precio = 2000m, DuracionMinutos = 30 }
        };
        db.Servicios.AddRange(servicios);

        var consultorios = new List<Consultorio>
        {
            new() { Codigo = "C-01", Nombre = "Consultorio 1", Ubicacion = "Primer nivel, ala norte", Capacidad = 1 },
            new() { Codigo = "C-02", Nombre = "Consultorio 2", Ubicacion = "Primer nivel, ala sur", Capacidad = 1 },
            new() { Codigo = "C-03", Nombre = "Sala de cirugía", Ubicacion = "Segundo nivel", Capacidad = 1 }
        };
        db.Consultorios.AddRange(consultorios);

        await db.SaveChangesAsync();

        var dentistas = new List<Dentista>
        {
            new()
            {
                Nombre = "Carolina", Apellido = "Méndez", NumeroLicencia = "EXQ-10234",
                Correo = "carolina.mendez@consultoriodental.com", Telefono = "809-555-0101",
                EspecialidadId = especialidades[0].Id
            },
            new()
            {
                Nombre = "Rafael", Apellido = "Peña", NumeroLicencia = "EXQ-20567",
                Correo = "rafael.pena@consultoriodental.com", Telefono = "809-555-0102",
                EspecialidadId = especialidades[1].Id
            },
            new()
            {
                Nombre = "Isabel", Apellido = "Guzmán", NumeroLicencia = "EXQ-30891",
                Correo = "isabel.guzman@consultoriodental.com", Telefono = "809-555-0103",
                EspecialidadId = especialidades[2].Id
            }
        };
        db.Dentistas.AddRange(dentistas);
        await db.SaveChangesAsync();

        // Lunes a viernes de 8:00 a 12:00 y de 14:00 a 18:00; sábados solo en la mañana.
        var horarios = new List<HorarioDentista>();
        foreach (var dentista in dentistas)
        {
            for (var dia = DayOfWeek.Monday; dia <= DayOfWeek.Friday; dia++)
            {
                horarios.Add(new HorarioDentista
                {
                    DentistaId = dentista.Id,
                    DiaSemana = dia,
                    HoraInicio = new TimeOnly(8, 0),
                    HoraFin = new TimeOnly(12, 0),
                    Observacion = "Jornada matutina"
                });
                horarios.Add(new HorarioDentista
                {
                    DentistaId = dentista.Id,
                    DiaSemana = dia,
                    HoraInicio = new TimeOnly(14, 0),
                    HoraFin = new TimeOnly(18, 0),
                    Observacion = "Jornada vespertina"
                });
            }

            horarios.Add(new HorarioDentista
            {
                DentistaId = dentista.Id,
                DiaSemana = DayOfWeek.Saturday,
                HoraInicio = new TimeOnly(8, 0),
                HoraFin = new TimeOnly(13, 0),
                Observacion = "Jornada sabatina"
            });
        }
        db.HorariosDentista.AddRange(horarios);

        var pacientes = new List<Paciente>
        {
            new()
            {
                Nombre = "Juan", Apellido = "Pérez", Documento = "001-1234567-8",
                FechaNacimiento = new DateOnly(1990, 5, 14), Telefono = "829-555-1010",
                Correo = "juan.perez@correo.com", Direccion = "Av. Independencia 45, Santo Domingo"
            },
            new()
            {
                Nombre = "María", Apellido = "Rodríguez", Documento = "402-9876543-1",
                FechaNacimiento = new DateOnly(1985, 11, 2), Telefono = "809-555-2020",
                Correo = "maria.rodriguez@correo.com", Alergias = "Penicilina"
            },
            new()
            {
                Nombre = "Luis", Apellido = "Fortuna", Documento = "031-5551234-9",
                FechaNacimiento = new DateOnly(2001, 3, 27), Telefono = "849-555-3030",
                Correo = "luis.fortuna@correo.com"
            },
            new()
            {
                Nombre = "Ana", Apellido = "Santos", Documento = "223-4445556-7",
                FechaNacimiento = new DateOnly(2015, 8, 9), Telefono = "809-555-4040",
                Direccion = "Calle Duarte 12, Santiago"
            }
        };
        db.Pacientes.AddRange(pacientes);

        await db.SaveChangesAsync();
        logger.LogInformation("Catálogos, dentistas, horarios y pacientes de ejemplo cargados.");
    }

    /// <summary>
    /// Crea tres citas que ilustran los tres estados calculados: una ya terminada,
    /// una transcurriendo ahora mismo y una futura.
    /// </summary>
    private static async Task SembrarCitasEjemploAsync(ConsultorioDbContext db, ILogger logger)
    {
        if (await db.Citas.AnyAsync()) return;

        var pacientes = await db.Pacientes.OrderBy(p => p.Id).Take(3).ToListAsync();
        var dentistas = await db.Dentistas.OrderBy(d => d.Id).Take(3).ToListAsync();
        var motivos = await db.Motivos.OrderBy(m => m.Id).Take(3).ToListAsync();
        var servicios = await db.Servicios.OrderBy(s => s.Id).Take(3).ToListAsync();
        var consultorios = await db.Consultorios.OrderBy(c => c.Id).Take(3).ToListAsync();

        if (pacientes.Count < 3 || dentistas.Count < 3 || consultorios.Count < 3) return;

        var ahora = DateTime.Now;
        var enProceso = ahora.AddMinutes(-15);           // empezó hace 15 minutos
        var finalizada = ahora.AddDays(-3);              // ocurrió hace tres días
        var vigente = ahora.AddDays(2).Date.AddHours(9); // dentro de dos días a las 9:00

        // Los dentistas no atienden domingos: se corre al lunes para que la cita quede dentro de horario.
        if (vigente.DayOfWeek == DayOfWeek.Sunday) vigente = vigente.AddDays(1);

        var citas = new List<Cita>
        {
            new()
            {
                PacienteId = pacientes[0].Id, DentistaId = dentistas[0].Id,
                MotivoId = motivos[1].Id, ServicioId = servicios[1].Id, ConsultorioId = consultorios[0].Id,
                Fecha = DateOnly.FromDateTime(finalizada), Hora = new TimeOnly(9, 0),
                DuracionMinutos = servicios[1].DuracionMinutos, CostoEstimado = servicios[1].Precio,
                Notas = "Cita de ejemplo ya finalizada.", FechaRegistro = finalizada.AddDays(-5)
            },
            new()
            {
                PacienteId = pacientes[1].Id, DentistaId = dentistas[1].Id,
                MotivoId = motivos[0].Id, ServicioId = servicios[0].Id, ConsultorioId = consultorios[1].Id,
                Fecha = DateOnly.FromDateTime(enProceso), Hora = new TimeOnly(enProceso.Hour, enProceso.Minute),
                DuracionMinutos = 60, CostoEstimado = servicios[0].Precio,
                Notas = "Cita de ejemplo en proceso.", FechaRegistro = ahora.AddDays(-2)
            },
            new()
            {
                PacienteId = pacientes[2].Id, DentistaId = dentistas[2].Id,
                MotivoId = motivos[2].Id, ServicioId = servicios[2].Id, ConsultorioId = consultorios[2].Id,
                Fecha = DateOnly.FromDateTime(vigente), Hora = TimeOnly.FromDateTime(vigente),
                DuracionMinutos = servicios[2].DuracionMinutos, CostoEstimado = servicios[2].Precio,
                Notas = "Cita de ejemplo vigente.", FechaRegistro = ahora
            }
        };

        db.Citas.AddRange(citas);
        await db.SaveChangesAsync();

        logger.LogInformation("Citas de ejemplo creadas (finalizada, en proceso y vigente).");
    }
}
