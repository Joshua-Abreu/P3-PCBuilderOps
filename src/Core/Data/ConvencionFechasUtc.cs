using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Core.Data;

// RD-11, Hallazgo 4 (Asignación 3)
public sealed class FechaUtcConverter() : ValueConverter<DateTime, DateTime>(
    fecha => fecha.Kind == DateTimeKind.Local ? fecha.ToUniversalTime() : DateTime.SpecifyKind(fecha, DateTimeKind.Utc),
    fecha => DateTime.SpecifyKind(fecha, DateTimeKind.Utc));

public static class ConvencionFechasUtc
{
    public static void UsarFechasUtc(this ModelConfigurationBuilder configuracion)
    {
        configuracion.Properties<DateTime>().HaveConversion<FechaUtcConverter>();
        configuracion.Properties<DateTime?>().HaveConversion<FechaUtcConverter>();
    }
}
