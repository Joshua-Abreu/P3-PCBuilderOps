using Core.Auth.Domain;
using Core.Data;
using Core.Permisos.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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

    public GestionPermisosService NuevaGestionPermisos() => new(NuevoContextoCore(), Reloj);

    public OrdenesService NuevoServicioOrdenes() => new(NuevoContextoNegocio(), NuevaGestionPermisos(), Reloj);

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
