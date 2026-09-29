using Core.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations;

/// <summary>
/// Configuración Fluent API de la entidad TokenActivacion.
/// RF-CA-15: Token de activación con expiración.
/// </summary>
public class TokenActivacionConfiguration : IEntityTypeConfiguration<TokenActivacion>
{
    public void Configure(EntityTypeBuilder<TokenActivacion> builder)
    {
        builder.ToTable("TokensActivacion");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UsuarioId)
            .IsRequired();

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.ExpiracionUtc)
            .IsRequired();

        builder.Property(t => t.FueUsado)
            .IsRequired()
            .HasDefaultValue(false);

        // Índice para búsquedas por UsuarioId
        builder.HasIndex(t => t.UsuarioId)
            .HasDatabaseName("IX_TokensActivacion_UsuarioId");
    }
}
