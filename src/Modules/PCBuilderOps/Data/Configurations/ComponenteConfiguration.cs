using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCBuilderOps.Domain;

namespace PCBuilderOps.Data.Configurations;

public class ComponenteConfiguration : IEntityTypeConfiguration<Componente>
{
    public void Configure(EntityTypeBuilder<Componente> builder)
    {
        builder.ToTable("Componentes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(Componente.NombreMaximo);

        builder.Property(c => c.Categoria)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(c => c.Fabricante)
            .IsRequired()
            .HasMaxLength(Componente.FabricanteMaximo);

        builder.Property(c => c.SocketOInterfaz)
            .IsRequired()
            .HasMaxLength(Componente.SocketOInterfazMaximo);

        builder.Property(c => c.Precio)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasData(ComponentesIniciales.Todos);
    }
}
