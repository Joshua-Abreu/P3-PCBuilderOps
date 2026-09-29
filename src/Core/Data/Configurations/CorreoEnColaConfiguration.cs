using Core.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations;

public class CorreoEnColaConfiguration : IEntityTypeConfiguration<CorreoEnCola>
{
    public void Configure(EntityTypeBuilder<CorreoEnCola> builder)
    {
        builder.ToTable("CorreosEnCola");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Destinatario)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.Asunto)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(c => c.Cuerpo).IsRequired();

        builder.Property(c => c.Estado)
            .IsRequired()
            .HasMaxLength(32)
            .HasDefaultValue("Pendiente");

        builder.Property(c => c.FechaCreacionUtc).IsRequired();

        builder.HasIndex(c => c.Estado)
            .HasDatabaseName("IX_CorreosEnCola_Estado");
    }
}
