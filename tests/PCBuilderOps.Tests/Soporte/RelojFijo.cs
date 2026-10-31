namespace PCBuilderOps.Tests.Soporte;

public sealed class RelojFijo : TimeProvider
{
    private DateTimeOffset _ahora;

    public RelojFijo(DateTime ahoraUtc) => _ahora = new DateTimeOffset(ahoraUtc, TimeSpan.Zero);

    public void Avanzar(TimeSpan tiempo) => _ahora = _ahora.Add(tiempo);

    public override DateTimeOffset GetUtcNow() => _ahora;
}
