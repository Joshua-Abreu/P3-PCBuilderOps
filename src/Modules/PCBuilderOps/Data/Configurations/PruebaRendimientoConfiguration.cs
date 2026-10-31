using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCBuilderOps.Domain;

namespace PCBuilderOps.Data.Configurations;

public class PruebaRendimientoConfiguration : IEntityTypeConfiguration<PruebaRendimiento>
{
    public void Configure(EntityTypeBuilder<PruebaRendimiento> builder)
    {
        builder.ToTable("PruebasRendimiento");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.TipoSoftwarePrueba)
            .IsRequired()
            .HasMaxLength(PruebaRendimiento.TipoSoftwarePruebaMaximo);

        builder.Property(p => p.TemperaturaMaximaCpu)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(p => p.TemperaturaMaximaGpu)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(p => p.FechaEjecucionUtc)
            .IsRequired();
    }
}
