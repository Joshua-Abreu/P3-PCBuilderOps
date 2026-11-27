using Core.Auditoria.Domain;
using Core.Auditoria.Exceptions;
using Core.Auditoria.Services;
using Core.Auth.Domain;
using Core.Auth.Exceptions;
using Core.Tests.Soporte;

namespace Core.Tests.Auditoria;

// RF-AUD-07, RD-06, RD-07 · criterios CA-AUD-14 a CA-AUD-17
public sealed class ConsultaAuditoriaTests : IDisposable
{
    private static readonly DateTime Noviembre30Final = new(2026, 11, 30, 23, 59, 59, DateTimeKind.Utc);
    private static readonly DateTime Diciembre1Inicio = new(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Noviembre10 = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly BaseDeDatosEnMemoria _baseDeDatos = new();
    private readonly Guid _administrador1;
    private readonly Guid _administrador2;
    private readonly Guid _estandar;

    public ConsultaAuditoriaTests()
    {
        _administrador1 = _baseDeDatos.CrearUsuario(RolUsuario.Administrador, "Ana Administradora");
        _administrador2 = _baseDeDatos.CrearUsuario(RolUsuario.Administrador, "Alberto Administrador");
        _estandar = _baseDeDatos.CrearUsuario(RolUsuario.Estandar, "Elena Pérez");

        Guardar(_administrador1, AccionAuditoria.SolicitudAprobada, EntidadAuditada.SolicitudPermiso, Noviembre10);
        Guardar(_administrador1, AccionAuditoria.RolCambiado, EntidadAuditada.Usuario, Noviembre10.AddHours(1));
        Guardar(_administrador2, AccionAuditoria.DocumentoEliminado, EntidadAuditada.Documento, Noviembre10.AddHours(2));
        Guardar(_estandar, AccionAuditoria.DocumentoSubido, EntidadAuditada.Documento, Noviembre30Final);
        Guardar(_administrador2, AccionAuditoria.UsuarioDesactivado, EntidadAuditada.Usuario, Diciembre1Inicio);
    }

    public void Dispose() => _baseDeDatos.Dispose();

    private void Guardar(Guid usuarioId, string accion, string entidad, DateTime fechaUtc)
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        contexto.RegistrosAuditoria.Add(RegistroAuditoria.Crear(
            usuarioId, accion, entidad, Guid.NewGuid().ToString(), """{"antes":1}""", """{"despues":2}""", fechaUtc));
        contexto.SaveChanges();
    }

    private Task<PaginaAuditoria> ConsultarAsync(
        string? usuarioId = null,
        string? entidad = null,
        string? desde = null,
        string? hasta = null,
        string? pagina = null,
        string? tamanoPagina = null,
        bool comoAdministrador = true) =>
        new HistorialAuditoria(_baseDeDatos.NuevoContexto(), new EntidadesAuditables(EntidadAuditada.DelCore))
            .ConsultarAsync(
                new ActorAuditoria(comoAdministrador ? _administrador1 : _estandar, comoAdministrador),
                new ConsultaAuditoria(usuarioId, entidad, desde, hasta, pagina, tamanoPagina));

    // CA-AUD-14 · RF-AUD-07
    [Fact]
    public async Task SinFiltros_DevuelveTodoDelMasRecienteAlMasAntiguoConElNombreDeQuienActuo()
    {
        // Actuar
        var pagina = await ConsultarAsync();

        // Verificar
        Assert.Equal(5, pagina.Total);
        Assert.Equal(pagina.Registros.Select(r => r.FechaUtc).OrderByDescending(f => f), pagina.Registros.Select(r => r.FechaUtc));
        Assert.Equal("Alberto Administrador", pagina.Registros[0].UsuarioNombre);
        Assert.Equal(1, pagina.Registros[0].ValorAnterior.GetProperty("antes").GetInt32());
    }

    // CA-AUD-14 · RF-AUD-07
    [Fact]
    public async Task FiltrarPorUsuarioPorEntidadYCombinados_DaResultadosDistintosYCorrectos()
    {
        // Actuar
        var delAdministrador2 = await ConsultarAsync(usuarioId: _administrador2.ToString());
        var deDocumentos = await ConsultarAsync(entidad: "Documento");
        var combinados = await ConsultarAsync(usuarioId: _administrador1.ToString(), entidad: "usuario");

        // Verificar
        Assert.Equal(
            new[] { AccionAuditoria.UsuarioDesactivado, AccionAuditoria.DocumentoEliminado },
            delAdministrador2.Registros.Select(r => r.Accion));
        Assert.Equal(
            new[] { AccionAuditoria.DocumentoSubido, AccionAuditoria.DocumentoEliminado },
            deDocumentos.Registros.Select(r => r.Accion));
        Assert.Equal(new[] { AccionAuditoria.RolCambiado }, combinados.Registros.Select(r => r.Accion));
        Assert.Equal(1, combinados.Total);
    }

    // CA-AUD-15 · RF-AUD-07, RD-11
    [Fact]
    public async Task RangoDeUnDia_IncluyeSuUltimoSegundoYNadaDelDiaSiguiente()
    {
        // Actuar
        var del30 = await ConsultarAsync(desde: "2026-11-30", hasta: "2026-11-30");
        var hastaEl29 = await ConsultarAsync(desde: "2026-11-01", hasta: "2026-11-29");
        var calendarioCompleto = await ConsultarAsync(desde: "0001-01-01", hasta: "9999-12-31");

        // Verificar
        Assert.Equal(new[] { AccionAuditoria.DocumentoSubido }, del30.Registros.Select(r => r.Accion));
        Assert.Equal(3, hastaEl29.Total);
        Assert.Equal(5, calendarioCompleto.Total);
    }

    // CA-AUD-14 · RF-AUD-07
    [Fact]
    public async Task Paginacion_DevuelveLaPaginaPedidaConElTotal()
    {
        // Actuar
        var segunda = await ConsultarAsync(pagina: "2", tamanoPagina: "2");

        // Verificar
        Assert.Equal(5, segunda.Total);
        Assert.Equal(2, segunda.Pagina);
        Assert.Equal(new[] { AccionAuditoria.DocumentoEliminado, AccionAuditoria.RolCambiado }, segunda.Registros.Select(r => r.Accion));
    }

    // CA-AUD-16 · RF-AUD-07, RD-06
    [Fact]
    public async Task UnEstandar_NoConsultaElHistorial()
    {
        // Actuar
        var consulta = () => ConsultarAsync(comoAdministrador: false);

        // Verificar
        var error = await Assert.ThrowsAsync<AccesoAuditoriaDenegadoException>(consulta);
        Assert.Equal("Solo un Administrador puede consultar el historial de auditoría.", error.Message);
    }

    // CA-AUD-17 · RF-AUD-07, RD-07
    [Theory]
    [InlineData("abc", null, null, null, null, null, "El filtro usuarioId no es un identificador válido.")]
    [InlineData(null, "Factura", null, null, null, null, "La entidad no es válida. Usa SolicitudPermiso, Documento, Usuario.")]
    [InlineData(null, null, "2026-13-01", "2026-12-31", null, null, "La fecha desde no es válida. Usa el formato yyyy-MM-dd.")]
    [InlineData(null, null, "2026-11-01", null, null, null, "Indica desde y hasta juntos, con el formato yyyy-MM-dd.")]
    [InlineData(null, null, null, "2026-11-30", null, null, "Indica desde y hasta juntos, con el formato yyyy-MM-dd.")]
    [InlineData(null, null, "2026-12-01", "2026-11-01", null, null, "La fecha desde no puede ser posterior a hasta.")]
    [InlineData(null, null, null, null, "0", null, "La página debe ser un número mayor o igual a 1.")]
    [InlineData(null, null, null, null, "uno", null, "La página debe ser un número mayor o igual a 1.")]
    [InlineData(null, null, null, null, null, "1000", "El tamaño de página debe ser un número entre 1 y 100.")]
    public async Task FiltroInvalido_SeRechazaConUnMensajeQueDiceQueEstaMal(
        string? usuarioId, string? entidad, string? desde, string? hasta, string? pagina, string? tamanoPagina, string mensaje)
    {
        // Actuar
        var consulta = () => ConsultarAsync(usuarioId, entidad, desde, hasta, pagina, tamanoPagina);

        // Verificar
        var error = await Assert.ThrowsAsync<ValidacionException>(consulta);
        Assert.Equal(mensaje, error.Message);
    }
}
