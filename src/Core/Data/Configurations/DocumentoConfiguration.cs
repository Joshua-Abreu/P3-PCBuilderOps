using Core.Auth.Domain;
using Core.Documentos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations;

public class DocumentoConfiguration : IEntityTypeConfiguration<Documento>
{
    public void Configure(EntityTypeBuilder<Documento> builder)
    {
        builder.ToTable("Documentos");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.NombreOriginal)
            .IsRequired()
            .HasMaxLength(Documento.NombreMaximo);

        builder.Property(d => d.TipoContenido)
            .IsRequired()
            .HasMaxLength(Documento.TipoContenidoMaximo);

        builder.Property(d => d.TamanoBytes)
            .IsRequired();

        builder.Property(d => d.FechaSubidaUtc)
            .IsRequired();

        builder.Property(d => d.NombreAlmacenado)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(d => d.NombreAlmacenado)
            .IsUnique()
            .HasDatabaseName("IX_Documentos_NombreAlmacenado");

        builder.Property(d => d.EstaEliminado)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(d => d.SubidoPorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(d => d.EliminadoPorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.SubidoPorId, d.EstaEliminado })
            .HasDatabaseName("IX_Documentos_SubidoPorId_EstaEliminado");
    }
}
