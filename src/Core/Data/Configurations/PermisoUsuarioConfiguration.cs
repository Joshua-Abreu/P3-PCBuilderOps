using Core.Auth.Domain;
using Core.Permisos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations;

public class PermisoUsuarioConfiguration : IEntityTypeConfiguration<PermisoUsuario>
{
    public void Configure(EntityTypeBuilder<PermisoUsuario> builder)
    {
        builder.ToTable("PermisosUsuario");
        builder.HasKey(p => new { p.UsuarioId, p.Permiso });

        builder.Property(p => p.Permiso)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.FechaOtorgadoUtc)
            .IsRequired();

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(p => p.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<SolicitudPermiso>()
            .WithMany()
            .HasForeignKey(p => p.SolicitudId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
