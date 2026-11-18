using Microsoft.EntityFrameworkCore;
using PlantillaBackend.Domain.Abstractions;
using PlantillaBackend.Domain.Entities;

namespace PlantillaBackend.Data.Persistence;

/// <summary>
/// Contexto de persistencia sobre el esquema existente PlantillaBackend.
/// No crea ni modifica el esquema de la base de datos.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public const string SchemaName = "PlantillaBackend";

    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Funcionario> Funcionarios => Set<Funcionario>();
    public DbSet<PreguntaSeguridad> PreguntasSeguridad => Set<PreguntaSeguridad>();
    public DbSet<FuncionarioPreguntaSeguridad> FuncionarioPreguntasSeguridad
        => Set<FuncionarioPreguntaSeguridad>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("roles", SchemaName);
            entity.HasKey(e => e.IdRol);

            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.Sdescripcion)
                .HasColumnName("sdescripcion")
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(e => e.Benabled).HasColumnName("benabled");
            entity.Property(e => e.NusuarioCreacion).HasColumnName("nusuario_creacion");
            entity.Property(e => e.DfechaCreacion).HasColumnName("dfecha_creacion");
            entity.Property(e => e.NusuarioActualizacion).HasColumnName("nusuario_actualizacion");
            entity.Property(e => e.DfechaActualizacion).HasColumnName("dfecha_actualizacion");
        });

        modelBuilder.Entity<Funcionario>(entity =>
        {
            entity.ToTable("funcionarios", SchemaName);
            entity.HasKey(e => e.IdFuncionario);

            entity.Property(e => e.IdFuncionario).HasColumnName("id_funcionario");
            entity.Property(e => e.Snacionalidad)
                .HasColumnName("snacionalidad")
                .HasMaxLength(1)
                .IsRequired();
            entity.Property(e => e.NcedulaIdent).HasColumnName("ncedula_ident");
            entity.Property(e => e.SprimerApellido)
                .HasColumnName("sprimer_apellido")
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(e => e.SsegundoApellido)
                .HasColumnName("ssegundo_apellido")
                .HasMaxLength(100);
            entity.Property(e => e.SprimerNombre)
                .HasColumnName("sprimer_nombre")
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(e => e.SsegundoNombre)
                .HasColumnName("ssegundo_nombre")
                .HasMaxLength(100);
            entity.Property(e => e.SpasswordHash)
                .HasColumnName("spassword_hash")
                .HasMaxLength(500)
                .IsRequired();
            entity.Property(e => e.RolId).HasColumnName("rol_id");
            entity.Property(e => e.Benabled).HasColumnName("benabled");
            entity.Property(e => e.NusuarioCreacion).HasColumnName("nusuario_creacion");
            entity.Property(e => e.DfechaCreacion).HasColumnName("dfecha_creacion");
            entity.Property(e => e.NusuarioActualizacion).HasColumnName("nusuario_actualizacion");
            entity.Property(e => e.DfechaActualizacion).HasColumnName("dfecha_actualizacion");

            entity.HasIndex(e => new { e.Snacionalidad, e.NcedulaIdent })
                .IsUnique()
                .HasDatabaseName("uq_funcionario_cedula");

            entity.HasOne(e => e.Rol)
                .WithMany(e => e.Funcionarios)
                .HasForeignKey(e => e.RolId)
                .HasConstraintName("funcionarios_roles")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PreguntaSeguridad>(entity =>
        {
            entity.ToTable("preguntas_seg", SchemaName);
            entity.HasKey(e => e.IdPreguntaSeg);

            entity.Property(e => e.IdPreguntaSeg).HasColumnName("id_preguntaseg");
            entity.Property(e => e.Sdescripcion)
                .HasColumnName("sdescripcion")
                .HasMaxLength(250)
                .IsRequired();
            entity.Property(e => e.Benabled).HasColumnName("benabled");
            entity.Property(e => e.NusuarioCreacion).HasColumnName("nusuario_creacion");
            entity.Property(e => e.DfechaCreacion).HasColumnName("dfecha_creacion");
            entity.Property(e => e.NusuarioActualizacion).HasColumnName("nusuario_actualizacion");
            entity.Property(e => e.DfechaActualizacion).HasColumnName("dfecha_actualizacion");
        });

        modelBuilder.Entity<FuncionarioPreguntaSeguridad>(entity =>
        {
            entity.ToTable("funcionario_pseguridad", SchemaName);
            entity.HasKey(e => e.IdFpseguridad);

            entity.Property(e => e.IdFpseguridad).HasColumnName("id_fpseguridad");
            entity.Property(e => e.IdFuncionario).HasColumnName("id_funcionario");
            entity.Property(e => e.IdPreguntaSeg).HasColumnName("id_preguntaseg");
            entity.Property(e => e.DfechaReg).HasColumnName("dfecha_reg");
            entity.Property(e => e.Srespuesta)
                .HasColumnName("srespuesta")
                .HasMaxLength(250)
                .IsRequired();
            entity.Property(e => e.NusuarioCreacion).HasColumnName("nusuario_creacion");
            entity.Property(e => e.DfechaCreacion).HasColumnName("dfecha_creacion");
            entity.Property(e => e.NusuarioActualizacion).HasColumnName("nusuario_actualizacion");
            entity.Property(e => e.DfechaActualizacion).HasColumnName("dfecha_actualizacion");

            entity.HasIndex(e => new { e.IdFuncionario, e.IdPreguntaSeg })
                .IsUnique()
                .HasDatabaseName("uq_funcionario_pregunta");

            entity.HasOne(e => e.Funcionario)
                .WithMany(e => e.PreguntasSeguridad)
                .HasForeignKey(e => e.IdFuncionario)
                .HasConstraintName("funcionario_pseguridad_funcionario_fkey")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PreguntaSeguridad)
                .WithMany(e => e.FuncionarioPreguntas)
                .HasForeignKey(e => e.IdPreguntaSeg)
                .HasConstraintName("funcionario_pseguridad_preguntas_seg_fkey")
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditInformation()
    {
        var usuarioAuditoria = _currentUserService?.Cedula ?? 1;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.DfechaCreacion == default)
                {
                    entry.Entity.DfechaCreacion = DateTime.UtcNow;
                }

                if (entry.Entity.NusuarioCreacion <= 0 || entry.Entity.NusuarioCreacion == 1)
                {
                    entry.Entity.NusuarioCreacion = usuarioAuditoria;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.DfechaActualizacion = DateTime.UtcNow;
                entry.Entity.NusuarioActualizacion = usuarioAuditoria;

                entry.Property(x => x.NusuarioCreacion).IsModified = false;
                entry.Property(x => x.DfechaCreacion).IsModified = false;
            }
        }
    }
}
