using ConsultorioDental.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.API.Data;

public class ConsultorioDbContext : DbContext
{
    public ConsultorioDbContext(DbContextOptions<ConsultorioDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();
    public DbSet<Dentista> Dentistas => Set<Dentista>();
    public DbSet<HorarioDentista> HorariosDentista => Set<HorarioDentista>();
    public DbSet<Motivo> Motivos => Set<Motivo>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Consultorio> Consultorios => Set<Consultorio>();
    public DbSet<Cita> Citas => Set<Cita>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- Usuario ----------
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.NombreUsuario).IsUnique();
            e.HasIndex(u => u.Correo).IsUnique();
            e.Property(u => u.Rol).HasConversion<int>();
        });

        // ---------- Paciente ----------
        modelBuilder.Entity<Paciente>(e =>
        {
            e.ToTable("Pacientes");
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Documento).IsUnique();
        });

        // ---------- Especialidad ----------
        modelBuilder.Entity<Especialidad>(e =>
        {
            e.ToTable("Especialidades");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        // ---------- Dentista ----------
        modelBuilder.Entity<Dentista>(e =>
        {
            e.ToTable("Dentistas");
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.NumeroLicencia).IsUnique();
            e.HasIndex(d => d.Correo).IsUnique();

            e.HasOne(d => d.Especialidad)
             .WithMany(x => x.Dentistas)
             .HasForeignKey(d => d.EspecialidadId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- HorarioDentista ----------
        modelBuilder.Entity<HorarioDentista>(e =>
        {
            e.ToTable("HorariosDentista");
            e.HasKey(h => h.Id);
            e.Property(h => h.DiaSemana).HasConversion<int>();

            // Los horarios sí desaparecen con el dentista: no tienen sentido por sí solos.
            e.HasOne(h => h.Dentista)
             .WithMany(d => d.Horarios)
             .HasForeignKey(h => h.DentistaId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(h => new { h.DentistaId, h.DiaSemana });
        });

        // ---------- Motivo ----------
        modelBuilder.Entity<Motivo>(e =>
        {
            e.ToTable("Motivos");
            e.HasKey(m => m.Id);
            e.HasIndex(m => m.Nombre).IsUnique();
        });

        // ---------- Servicio ----------
        modelBuilder.Entity<Servicio>(e =>
        {
            e.ToTable("Servicios");
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Nombre).IsUnique();
            e.Property(s => s.Precio).HasColumnType("decimal(18,2)");
        });

        // ---------- Consultorio ----------
        modelBuilder.Entity<Consultorio>(e =>
        {
            e.ToTable("Consultorios");
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Codigo).IsUnique();
        });

        // ---------- Cita ----------
        modelBuilder.Entity<Cita>(e =>
        {
            e.ToTable("Citas");
            e.HasKey(c => c.Id);

            // Restrict en todas: no se permite borrar un catálogo que ya tiene citas asociadas.
            e.HasOne(c => c.Paciente)
             .WithMany(p => p.Citas)
             .HasForeignKey(c => c.PacienteId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.Dentista)
             .WithMany(d => d.Citas)
             .HasForeignKey(c => c.DentistaId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.Motivo)
             .WithMany(m => m.Citas)
             .HasForeignKey(c => c.MotivoId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.Servicio)
             .WithMany(s => s.Citas)
             .HasForeignKey(c => c.ServicioId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.Consultorio)
             .WithMany(x => x.Citas)
             .HasForeignKey(c => c.ConsultorioId)
             .OnDelete(DeleteBehavior.Restrict);

            // Índices que sostienen las búsquedas de solapamiento.
            e.HasIndex(c => new { c.DentistaId, c.Fecha });
            e.HasIndex(c => new { c.ConsultorioId, c.Fecha });
            e.HasIndex(c => new { c.PacienteId, c.Fecha });
        });
    }
}
