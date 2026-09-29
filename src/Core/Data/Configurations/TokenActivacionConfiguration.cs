using Core.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations;

public class TokenActivacionConfiguration : IEntityTypeConfiguration<TokenActivacion>
{
    public void Configure(EntityTypeBuilder<TokenActivacion> builder)
    {
        builder.ToTable("TokensActivacion");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UsuarioId).IsRequired();

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.ExpiracionUtc).IsRequired();

        builder.Property(t => t.FueUsado)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(t => t.UsuarioId)
            .HasDatabaseName("IX_TokensActivacion_UsuarioId");
    }
}
