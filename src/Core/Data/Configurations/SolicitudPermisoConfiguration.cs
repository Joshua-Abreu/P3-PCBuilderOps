using Core.Auth.Domain;
using Core.Permisos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations;

public class SolicitudPermisoConfiguration : IEntityTypeConfiguration<SolicitudPermiso>
{
    public void Configure(EntityTypeBuilder<SolicitudPermiso> builder)
    {
        builder.ToTable("SolicitudesPermiso");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Permiso)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.FechaCreacionUtc)
            .IsRequired();

        builder.Property(s => s.MotivoRechazo)
            .HasMaxLength(SolicitudPermiso.MotivoRechazoMaximo);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(s => s.SolicitanteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(s => s.AprobadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.Estado)
            .HasDatabaseName("IX_SolicitudesPermiso_Estado");
    }
}
