using Core.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations;

/// <summary>
/// Configuración Fluent API de la entidad Usuario.
/// RF-CA-01: Índice único en Correo.
/// </summary>
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Correo)
            .IsRequired()
            .HasMaxLength(256);

        // RF-CA-01: Correo único
        builder.HasIndex(u => u.Correo)
            .IsUnique()
            .HasDatabaseName("IX_Usuarios_Correo");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.Rol)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(u => u.EstaActivo)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.FechaCreacionUtc)
            .IsRequired();

        builder.Property(u => u.IntentosFallidos)
            .IsRequired()
            .HasDefaultValue(0);
    }
}
