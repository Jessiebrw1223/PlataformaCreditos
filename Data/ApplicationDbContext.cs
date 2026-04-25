using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<SolicitudCredito> SolicitudesCredito => Set<SolicitudCredito>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Cliente>(entity =>
        {
            entity.Property(c => c.IngresosMensuales).HasColumnType("decimal(18,2)");
            entity.HasCheckConstraint("CK_Cliente_IngresosMensuales", "IngresosMensuales > 0");
            entity.HasIndex(c => c.UsuarioId).IsUnique();
        });

        builder.Entity<SolicitudCredito>(entity =>
        {
            entity.Property(s => s.MontoSolicitado).HasColumnType("decimal(18,2)");
            entity.Property(s => s.Estado).HasConversion<int>();
            entity.Property(s => s.MotivoRechazo).HasMaxLength(300);
            entity.HasCheckConstraint("CK_Solicitud_MontoSolicitado", "MontoSolicitado > 0");
            entity.HasOne(s => s.Cliente)
                  .WithMany(c => c.Solicitudes)
                  .HasForeignKey(s => s.ClienteId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(s => new { s.ClienteId, s.Estado })
                  .IsUnique()
                  .HasFilter("Estado = 1");
        });
    }
}
