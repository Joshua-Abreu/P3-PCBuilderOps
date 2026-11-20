using Core.Auth.Domain;
using Core.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations;

public class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        builder.ToTable("Notificaciones");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Asunto)
            .IsRequired()
            .HasMaxLength(Notificacion.LargoMaximoAsunto);

        builder.Property(n => n.Mensaje)
            .IsRequired()
            .HasMaxLength(Notificacion.LargoMaximoMensaje);

        builder.Property(n => n.FechaCreacionUtc).IsRequired();

        builder.Ignore(n => n.EsLeida);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(n => n.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.UsuarioId, n.FechaCreacionUtc })
            .HasDatabaseName("IX_Notificaciones_UsuarioId_FechaCreacionUtc");
    }
}
