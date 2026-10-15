namespace Core.Data;

// RD-09, RD-10
public static class CadenaConexion
{
    public const string LocalPorDefecto =
        "Server=localhost;Database=PCBuilderOps;Trusted_Connection=True;TrustServerCertificate=True";

    public static string Obtener(string? desdeConfiguracion = null) =>
        Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? desdeConfiguracion
            ?? LocalPorDefecto;
}
