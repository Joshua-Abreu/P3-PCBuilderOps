using Core.Auth.Domain;
using Core.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations;

public class PreferenciaNotificacionConfiguration : IEntityTypeConfiguration<PreferenciaNotificacion>
{
    public void Configure(EntityTypeBuilder<PreferenciaNotificacion> builder)
    {
        builder.ToTable("PreferenciasNotificacion");
        builder.HasKey(p => p.UsuarioId);

        builder.Property(p => p.CorreoActivo).IsRequired();
        builder.Property(p => p.FechaActualizacionUtc).IsRequired();

        builder.HasOne<Usuario>()
            .WithOne()
            .HasForeignKey<PreferenciaNotificacion>(p => p.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
