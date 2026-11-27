using Core.Auditoria.Domain;
using Core.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations;

public class RegistroAuditoriaConfiguration : IEntityTypeConfiguration<RegistroAuditoria>
{
    public const string Tabla = "RegistrosAuditoria";
    public const string DisparadorInmutable = "TR_RegistrosAuditoria_Inmutable";

    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        // RF-AUD-06
        builder.ToTable(Tabla, tabla => tabla.HasTrigger(DisparadorInmutable));
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Accion).IsRequired().HasMaxLength(RegistroAuditoria.AccionMaximo);
        builder.Property(r => r.Entidad).IsRequired().HasMaxLength(RegistroAuditoria.EntidadMaximo);
        builder.Property(r => r.EntidadId).IsRequired().HasMaxLength(RegistroAuditoria.EntidadIdMaximo);
        builder.Property(r => r.FechaUtc).IsRequired();
        builder.Property(r => r.ValorAnterior).IsRequired();
        builder.Property(r => r.ValorNuevo).IsRequired();

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(r => r.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.FechaUtc).HasDatabaseName("IX_RegistrosAuditoria_FechaUtc");
        builder.HasIndex(r => new { r.UsuarioId, r.FechaUtc }).HasDatabaseName("IX_RegistrosAuditoria_UsuarioId_FechaUtc");
        builder.HasIndex(r => new { r.Entidad, r.FechaUtc }).HasDatabaseName("IX_RegistrosAuditoria_Entidad_FechaUtc");
    }
}
