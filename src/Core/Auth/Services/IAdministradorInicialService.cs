namespace Core.Auth.Services;

public interface IAdministradorInicialService
{
    Task<ResultadoAdministradorInicial> AsegurarAsync(string? nombre, string? correo, string? password, CancellationToken ct = default);
}

public enum ResultadoAdministradorInicial
{
    Creado,
    Promovido,
    YaExisteAdministrador,
    SinConfiguracion
}
