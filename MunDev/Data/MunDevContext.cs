using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MunDev.Models;

namespace MunDev.Data;

public partial class MunDevContext : DbContext
{
    public MunDevContext()
    {
    }

    public MunDevContext(DbContextOptions<MunDevContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActividadUsuario> ActividadUsuarios { get; set; }

    public virtual DbSet<Comentario> Comentarios { get; set; }

    public virtual DbSet<Equipo> Equipos { get; set; }

    public virtual DbSet<Notificacion> Notificacions { get; set; }

    public virtual DbSet<Proyecto> Proyectos { get; set; }

    public virtual DbSet<ProyectoUsuario> ProyectoUsuarios { get; set; }

    public virtual DbSet<Repositorio> Repositorios { get; set; }

    public virtual DbSet<Rol> Rols { get; set; }

    public virtual DbSet<Tarea> Tareas { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public DbSet<EquipoUsuario> EquipoUsuarios { get; set; }
    public DbSet<Invitation> Invitations { get; set; }
    public DbSet<Perfil> Perfiles { get; set; } = null!;



    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Server=DESKTOP-9G14HAJ;Database=MunDev;Integrated Security=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Perfil) // Un Usuario tiene un Perfil
            .WithOne(p => p.Usuario) // Un Perfil pertenece a un Usuario
            .HasForeignKey<Perfil>(p => p.UsuarioId) // La FK en Perfil es UsuarioId
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Invitation>()
        .HasOne(i => i.InvitedByUser)
        .WithMany()
        .HasForeignKey(i => i.InvitedByUserId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invitation>()
            .HasOne(i => i.AcceptedByUser)
            .WithMany()
            .HasForeignKey(i => i.AcceptedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invitation>()
            .HasOne(i => i.Equipo)
            .WithMany()
            .HasForeignKey(i => i.EquipoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EquipoUsuario>()
        .HasOne(eu => eu.Equipo)
        .WithMany(e => e.EquipoUsuarios)
        .HasForeignKey(eu => eu.EquipoId)
         .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EquipoUsuario>()
            .HasOne(eu => eu.Usuario)
            .WithMany()
            .HasForeignKey(eu => eu.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ActividadUsuario>(entity =>

        {
            entity.HasKey(e => e.ActividadUsuarioId).HasName("PK__Activida__9FDCB5056E041739");

            entity.ToTable("ActividadUsuario");

            entity.Property(e => e.FechaActividad)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TipoActividad).HasMaxLength(100);

            entity.HasOne(d => d.Usuario).WithMany(p => p.ActividadUsuarios)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Actividad__Usuar__619B8048");
        });

        modelBuilder.Entity<Comentario>(entity =>
        {
            entity.HasKey(e => e.ComentarioId).HasName("PK__Comentar__F1844938DDF6317D");

            entity.ToTable("Comentario");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Tarea).WithMany(p => p.Comentarios)
                .HasForeignKey(d => d.TareaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Comentari__Tarea__5441852A");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Comentarios)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Comentari__Usuar__5535A963");
        });

        modelBuilder.Entity<Equipo>(entity =>
        {
            entity.HasKey(e => e.EquipoId).HasName("PK__Equipo__DE8A0BDF3B7E5681");

            entity.ToTable("Equipo");

            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.NombreEquipo).HasMaxLength(100);

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.Equipos)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .HasConstraintName("FK__Equipo__CreadoPo__3F466844");
        });

        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.HasKey(e => e.NotificacionId).HasName("PK__Notifica__BCC12024F14C67AA");

            entity.ToTable("Notificacion");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Leida)
                .HasDefaultValue(false)
                .HasColumnName("leida");
            entity.Property(e => e.Mensaje).HasMaxLength(500);
            entity.Property(e => e.TipoNotificacion).HasMaxLength(50);

            entity.HasOne(d => d.Usuario).WithMany(p => p.Notificacions)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificac__Usuar__5DCAEF64");
        });

        modelBuilder.Entity<Proyecto>(entity =>
        {
            entity.HasKey(e => e.ProyectoId).HasName("PK__Proyecto__CF241D65D3184D83");

            entity.ToTable("Proyecto");

            entity.Property(e => e.EstadoProyecto)
                .HasMaxLength(50)
                .HasDefaultValue("Activo");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NombreProyecto).HasMaxLength(200);

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.Proyectos)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .HasConstraintName("FK__Proyecto__Creado__440B1D61");

            entity.HasOne(d => d.Equipo).WithMany(p => p.Proyectos)
                .HasForeignKey(d => d.EquipoId)
                .HasConstraintName("FK__Proyecto__Equipo__44FF419A");
        });

        modelBuilder.Entity<ProyectoUsuario>(entity =>
        {
            entity.HasKey(e => e.ProyectoUsuarioId).HasName("PK__Proyecto__ADE3541B8CC1E85B");

            entity.ToTable("ProyectoUsuario");

            entity.HasIndex(e => new { e.ProyectoId, e.UsuarioId }, "UQ__Proyecto__5D97C31F62BAF76E").IsUnique();

            entity.Property(e => e.FechaAsignacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RolEnProyecto).HasMaxLength(50);

            entity.HasOne(d => d.Proyecto).WithMany(p => p.ProyectoUsuarios)
                .HasForeignKey(d => d.ProyectoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProyectoU__Proye__49C3F6B7");

            entity.HasOne(d => d.Usuario).WithMany(p => p.ProyectoUsuarios)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProyectoU__Usuar__4AB81AF0");
        });

        modelBuilder.Entity<Repositorio>(entity =>
        {
            entity.HasKey(e => e.RepositorioId).HasName("PK__Reposito__7C1D3FA2AA44FB1D");

            entity.ToTable("Repositorio");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NombreRepositorio).HasMaxLength(200);
            entity.Property(e => e.RepositorioUrl)
                .HasMaxLength(500)
                .HasColumnName("RepositorioURL");

            entity.HasOne(d => d.Proyecto).WithMany(p => p.Repositorios)
                .HasForeignKey(d => d.ProyectoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Repositor__Proye__59063A47");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.RolId).HasName("PK__Rol__F92302F1E4F132AE");

            entity.ToTable("Rol");

            entity.HasIndex(e => e.NombreRol, "UQ__Rol__4F0B537F513BCD0E").IsUnique();

            entity.Property(e => e.NombreRol).HasMaxLength(50);
        });

        modelBuilder.Entity<Tarea>(entity =>
        {
            entity.HasKey(e => e.TareaId).HasName("PK__Tarea__5CD8399179719ACE");

            entity.ToTable("Tarea");

            entity.Property(e => e.AsignadoAusuarioId).HasColumnName("AsignadoAUsuarioId");
            entity.Property(e => e.EstadoTarea)
                .HasMaxLength(50)
                .HasDefaultValue("Pendiente");
            entity.Property(e => e.Prioridad)
                .HasMaxLength(50)
                .HasDefaultValue("Media");
            entity.Property(e => e.Titulo).HasMaxLength(200);

            entity.HasOne(d => d.AsignadoAusuario).WithMany(p => p.Tareas)
                .HasForeignKey(d => d.AsignadoAusuarioId)
                .HasConstraintName("FK__Tarea__AsignadoA__5070F446");

            entity.HasOne(d => d.Proyecto).WithMany(p => p.Tareas)
                .HasForeignKey(d => d.ProyectoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tarea__ProyectoI__4F7CD00D");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.UsuarioId).HasName("PK__Usuario__2B3DE7B84410861C");

            entity.ToTable("Usuario");

            entity.HasIndex(e => e.NombreUsuario, "UQ__Usuario__6B0F5AE0F08876B1").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Usuario__A9D10534FFE4A6E4").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.ContrasenaHash).HasMaxLength(256);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.NombreUsuario).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
