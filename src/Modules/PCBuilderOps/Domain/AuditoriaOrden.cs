namespace PCBuilderOps.Domain;

// RF-AUD-01, RD-03
public static class AuditoriaOrden
{
    public const string Entidad = "OrdenEnsamble";

    public const string ArmadoIniciado = "ArmadoIniciado";
    public const string OrdenEnPruebas = "OrdenEnPruebas";
    public const string OrdenEntregada = "OrdenEntregada";

    public static readonly IReadOnlyList<string> Entidades = [Entidad];
}
