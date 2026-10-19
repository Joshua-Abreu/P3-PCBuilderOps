namespace Core.Tests.Soporte;

public sealed class RelojFijo : TimeProvider
{
    private DateTimeOffset _ahora;

    public RelojFijo(DateTime ahoraUtc) => _ahora = new DateTimeOffset(ahoraUtc, TimeSpan.Zero);

    public void Fijar(DateTime ahoraUtc) => _ahora = new DateTimeOffset(ahoraUtc, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _ahora;
}
