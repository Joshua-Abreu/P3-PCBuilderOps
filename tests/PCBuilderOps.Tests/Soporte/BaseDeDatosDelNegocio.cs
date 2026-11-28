using Core.Auditoria.Services;
using Core.Auth.Domain;
using Core.Data;
using Core.Documentos.Services;
using Core.Notifications.Services;
using Core.Permisos.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using PCBuilderOps.Data;
using PCBuilderOps.Services;

namespace PCBuilderOps.Tests.Soporte;

// Base nueva por prueba, compartida por el contexto del Core y el del módulo, igual que en producción.
public sealed class BaseDeDatosDelNegocio : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly List<DbContext> _contextos = [];

    public BaseDeDatosDelNegocio()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        NuevoContextoCore().Database.EnsureCreated();
        NuevoContextoNegocio().GetService<IRelationalDatabaseCreator>().CreateTables();
    }

    public RelojFijo Reloj { get; } = new(new DateTime(2026, 10, 31, 9, 0, 0, DateTimeKind.Utc));

    public AppDbContext NuevoContextoCore() =>
        Registrar(new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexion).Options));

    public PCBuilderOpsDbContext NuevoContextoNegocio() =>
        Registrar(new PCBuilderOpsDbContext(new DbContextOptionsBuilder<PCBuilderOpsDbContext>().UseSqlite(_conexion).Options));

    public PCBuilderOpsDbContext NuevoContextoNegocioQueRegistraSql(ICollection<string> sentencias) =>
        Registrar(new PCBuilderOpsDbContext(new DbContextOptionsBuilder<PCBuilderOpsDbContext>()
            .UseSqlite(_conexion)
            .LogTo(sentencias.Add, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
            .Options));

    public GestionPermisosService NuevaGestionPermisos()
    {
        var contexto = NuevoContextoCore();
        return new(contexto, Reloj, new Notificador(contexto, new EncoladorCorreos(contexto, Reloj), Reloj), new Auditor(contexto, Reloj));
    }

    // CA-HIS-01: el auditor, el notificador y la operación comparten el contexto del Core, como en una petición.
    public OrdenesService NuevoServicioOrdenes(PCBuilderOpsDbContext? contextoNegocio = null, Func<AppDbContext, INotificador>? notificador = null)
    {
        var contextoCore = NuevoContextoCore();

        return new(
            contextoNegocio ?? NuevoContextoNegocio(),
            NuevaGestionPermisos(),
            Reloj,
            notificador?.Invoke(contextoCore) ?? new Notificador(contextoCore, new EncoladorCorreos(contextoCore, Reloj), Reloj),
            new Auditor(contextoCore, Reloj),
            new OperacionCompartida(contextoCore));
    }

    public ReporteTiempoEnsambleService NuevoReporteTiempoEnsamble(PCBuilderOpsDbContext? contexto = null) =>
        new(contexto ?? NuevoContextoNegocio());

    public Notificador NuevoNotificador()
    {
        var contexto = NuevoContextoCore();
        return new(contexto, new EncoladorCorreos(contexto, Reloj), Reloj);
    }

    public CatalogoService NuevoServicioCatalogo(IAlmacenamientoArchivos almacenamiento) =>
        new(NuevoContextoNegocio(), almacenamiento);

    public Guid CrearUsuario(RolUsuario rol)
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Usuario de prueba",
            Correo = $"{Guid.NewGuid():N}@prueba.com",
            PasswordHash = "hash",
            Rol = rol,
            EstaActivo = true,
            FechaCreacionUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var contexto = NuevoContextoCore();
        contexto.Usuarios.Add(usuario);
        contexto.SaveChanges();

        return usuario.Id;
    }

    public async Task<Guid> CrearTecnicoConPermisoAsync(Guid administradorId)
    {
        var tecnicoId = CrearUsuario(RolUsuario.Estandar);
        var solicitud = await NuevaGestionPermisos().CrearAsync(tecnicoId, Core.Permisos.Domain.TipoPermiso.OperarTaller);

        await NuevaGestionPermisos().AprobarAsync(solicitud.Id, administradorId);

        return tecnicoId;
    }

    public void Dispose()
    {
        foreach (var contexto in _contextos)
            contexto.Dispose();

        _conexion.Dispose();
    }

    private T Registrar<T>(T contexto) where T : DbContext
    {
        _contextos.Add(contexto);
        return contexto;
    }
}
