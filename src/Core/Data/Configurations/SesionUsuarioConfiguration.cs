using Core.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations;

public class SesionUsuarioConfiguration : IEntityTypeConfiguration<SesionUsuario>
{
    public void Configure(EntityTypeBuilder<SesionUsuario> builder)
    {
        builder.ToTable("SesionesUsuario");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UsuarioId).IsRequired();

        builder.Property(s => s.TokenJwt)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(s => s.FechaCreacionUtc).IsRequired();
        builder.Property(s => s.ExpiracionUtc).IsRequired();

        builder.Property(s => s.EstaRevocada)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(s => s.TokenJwt)
            .HasDatabaseName("IX_SesionesUsuario_TokenJwt");

        builder.HasIndex(s => s.UsuarioId)
            .HasDatabaseName("IX_SesionesUsuario_UsuarioId");
    }
}
