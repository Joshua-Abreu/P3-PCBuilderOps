using Core.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;
using PCBuilderOps.Domain;
using PCBuilderOps.Exceptions;
using PCBuilderOps.Services;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Services;

// Hallazgo 1 (Asignación 3) · RF-NOT-03, RF-NEG-07: la misma regla de concurrencia en las transiciones de la orden.
public sealed class TransicionSimultaneaOrdenTests : IDisposable
{
    private static readonly LineaCotizacion[] PcCompatible =
    [
        new(ComponentesIniciales.CpuAm5, 1),
        new(ComponentesIniciales.PlacaAm5, 1),
        new(ComponentesIniciales.GpuMedia, 1),
        new(ComponentesIniciales.Fuente850W, 1)
    ];

    private readonly BaseDeDatosDelNegocio _bd = new();
    private readonly Guid _administradorId;
    private readonly Guid _clienteId;

    public TransicionSimultaneaOrdenTests()
    {
        _administradorId = _bd.CrearUsuario(RolUsuario.Administrador);
        _clienteId = _bd.CrearUsuario(RolUsuario.Estandar);
    }

    public void Dispose() => _bd.Dispose();

    private OrdenesService ServicioSobre(PCBuilderOpsDbContext contexto) => _bd.NuevoServicioOrdenes(contexto);

    private int AvisosDelCliente() =>
        _bd.NuevoContextoCore().Notificaciones.Count(n => n.UsuarioId == _clienteId);

    // Hallazgo 1
    [Fact]
    public async Task PasarAPruebasDosVecesALaVez_LaSegundaSeRechazaYElClienteRecibeUnSoloAviso()
    {
        // Preparar
        var orden = await _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, PcCompatible);
        var tecnicoId = await _bd.CrearTecnicoConPermisoAsync(_administradorId);
        await _bd.NuevoServicioOrdenes().IniciarArmadoAsync(_administradorId, orden.Id, tecnicoId);
        var tecnico = new ActorTaller(tecnicoId, false);
        var contextoDelSegundo = _bd.NuevoContextoNegocio();
        await contextoDelSegundo.OrdenesEnsamble
            .Include(o => o.Detalles).ThenInclude(d => d.Componente)
            .Include(o => o.Pruebas)
            .SingleAsync(o => o.Id == orden.Id);

        // Actuar
        await ServicioSobre(_bd.NuevoContextoNegocio()).PasarAPruebasAsync(tecnico, orden.Id);
        var pasoTardio = () => ServicioSobre(contextoDelSegundo).PasarAPruebasAsync(tecnico, orden.Id);

        // Verificar
        await Assert.ThrowsAsync<TransicionOrdenNoPermitidaException>(pasoTardio);
        Assert.Equal(1, AvisosDelCliente());

        // CA-HIS-04
        Assert.Equal(1, _bd.NuevoContextoNegocio().TransicionesOrden
            .Count(t => t.OrdenEnsambleId == orden.Id && t.EstadoNuevo == EstadoOrdenEnsamble.EnPruebas));
        Assert.Equal(1, _bd.NuevoContextoCore().RegistrosAuditoria
            .Count(r => r.EntidadId == orden.Id.ToString() && r.Accion == AuditoriaOrden.OrdenEnPruebas));
    }
}
