using System.Text.Json;
using Core.Auditoria.Domain;
using Core.Auditoria.Services;
using Core.Auth.Domain;
using Core.Auth.Exceptions;
using PCBuilderOps.Data;
using PCBuilderOps.Domain;
using PCBuilderOps.Services;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Services;

// RF-AUD-01, RF-AUD-07, RD-03 · criterios CA-HIS-11 y CA-HIS-12
public sealed class AuditoriaOrdenTests : IDisposable
{
    private static readonly LineaCotizacion[] PcCompatible =
    [
        new(ComponentesIniciales.CpuAm5, 1),
        new(ComponentesIniciales.PlacaAm5, 1),
        new(ComponentesIniciales.GpuMedia, 1),
        new(ComponentesIniciales.Fuente850W, 1)
    ];

    private static readonly EntidadesAuditables EntidadesDeLaApi = new([.. EntidadAuditada.DelCore, .. AuditoriaOrden.Entidades]);

    private readonly BaseDeDatosDelNegocio _bd = new();
    private readonly Guid _administradorId;
    private readonly Guid _clienteId;
    private readonly Guid _tecnicoId;
    private readonly Guid _ordenId;

    public AuditoriaOrdenTests()
    {
        _administradorId = _bd.CrearUsuario(RolUsuario.Administrador);
        _clienteId = _bd.CrearUsuario(RolUsuario.Estandar);
        _tecnicoId = _bd.CrearTecnicoConPermisoAsync(_administradorId).GetAwaiter().GetResult();
        _ordenId = _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, PcCompatible).GetAwaiter().GetResult().Id;
    }

    public void Dispose() => _bd.Dispose();

    private ActorTaller Tecnico => new(_tecnicoId, false);

    private Task<PaginaAuditoria> ConsultarAsync(string? entidad) =>
        new HistorialAuditoria(_bd.NuevoContextoCore(), EntidadesDeLaApi)
            .ConsultarAsync(new ActorAuditoria(_administradorId, true), new ConsultaAuditoria(null, entidad, null, null, null, "100"));

    private async Task<RegistroAuditoriaDetalle> UnicoRegistroAsync(string accion) =>
        Assert.Single((await ConsultarAsync(AuditoriaOrden.Entidad)).Registros, r => r.Accion == accion);

    private void VerificarCampos(RegistroAuditoriaDetalle registro, Guid usuarioId, string anterior, string nuevo)
    {
        Assert.NotEqual(Guid.Empty, registro.Id);
        Assert.Equal(usuarioId, registro.UsuarioId);
        Assert.False(string.IsNullOrWhiteSpace(registro.UsuarioNombre));
        Assert.Equal(AuditoriaOrden.Entidad, registro.Entidad);
        Assert.Equal(_ordenId.ToString(), registro.EntidadId);
        Assert.Equal(_bd.Reloj.GetUtcNow().UtcDateTime, registro.FechaUtc);
        Assert.Equal(DateTimeKind.Utc, registro.FechaUtc.Kind);
        Assert.Equal(anterior, JsonSerializer.Serialize(registro.ValorAnterior));
        Assert.Equal(nuevo, JsonSerializer.Serialize(registro.ValorNuevo));
    }

    private async Task AvanzarHastaEnPruebasAsync()
    {
        await _bd.NuevoServicioOrdenes().IniciarArmadoAsync(_administradorId, _ordenId, _tecnicoId);
        _bd.Reloj.Avanzar(TimeSpan.FromHours(1));
        await _bd.NuevoServicioOrdenes().PasarAPruebasAsync(Tecnico, _ordenId);
    }

    // CA-HIS-11
    [Fact]
    public async Task IniciarArmado_RegistraAlAdministradorElEstadoYElTecnicoAsignado()
    {
        // Actuar
        _bd.Reloj.Avanzar(TimeSpan.FromMinutes(5));
        await _bd.NuevoServicioOrdenes().IniciarArmadoAsync(_administradorId, _ordenId, _tecnicoId);

        // Verificar
        VerificarCampos(
            await UnicoRegistroAsync(AuditoriaOrden.ArmadoIniciado),
            _administradorId,
            """{"estado":"Cotizada","tecnicoAsignadoId":null}""",
            $$"""{"estado":"EnArmado","tecnicoAsignadoId":"{{_tecnicoId}}"}""");
    }

    // CA-HIS-11
    [Fact]
    public async Task PasarAPruebas_RegistraAlTecnicoYElCambioDeEstado()
    {
        // Actuar
        await AvanzarHastaEnPruebasAsync();

        // Verificar
        VerificarCampos(
            await UnicoRegistroAsync(AuditoriaOrden.OrdenEnPruebas),
            _tecnicoId,
            """{"estado":"EnArmado"}""",
            """{"estado":"EnPruebas"}""");
    }

    // CA-HIS-11
    [Fact]
    public async Task Entregar_RegistraAlTecnicoYElCambioDeEstado()
    {
        // Preparar
        await AvanzarHastaEnPruebasAsync();
        await _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(Tecnico, _ordenId, new DatosPrueba("Cinebench", 70m, 65m, 12000, true));

        // Actuar
        _bd.Reloj.Avanzar(TimeSpan.FromMinutes(20));
        await _bd.NuevoServicioOrdenes().EntregarAsync(Tecnico, _ordenId);

        // Verificar
        VerificarCampos(
            await UnicoRegistroAsync(AuditoriaOrden.OrdenEntregada),
            _tecnicoId,
            """{"estado":"EnPruebas"}""",
            """{"estado":"Entregada"}""");
    }

    // CA-HIS-12
    [Theory]
    [InlineData("OrdenEnsamble")]
    [InlineData("ordenensamble")]
    public async Task ConsultarPorLaEntidadDeLaOrden_SoloTraeRegistrosDeLaOrden(string entidad)
    {
        // Preparar
        await AvanzarHastaEnPruebasAsync();

        // Actuar
        var pagina = await ConsultarAsync(entidad);

        // Verificar
        Assert.Equal(2, pagina.Total);
        Assert.All(pagina.Registros, r => Assert.Equal(AuditoriaOrden.Entidad, r.Entidad));
        Assert.Contains((await ConsultarAsync(null)).Registros, r => r.Entidad == EntidadAuditada.SolicitudPermiso);
    }

    // CA-HIS-12
    [Fact]
    public async Task ConsultarUnaEntidadDesconocida_ListaLasValidasConLaOrden()
    {
        // Actuar
        var accion = () => ConsultarAsync("Factura");

        // Verificar
        var error = await Assert.ThrowsAsync<ValidacionException>(accion);
        Assert.Equal("La entidad no es válida. Usa SolicitudPermiso, Documento, Usuario, OrdenEnsamble.", error.Message);
    }
}
