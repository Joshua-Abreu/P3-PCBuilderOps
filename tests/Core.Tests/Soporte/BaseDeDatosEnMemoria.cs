using Core.Auth.Domain;
using Core.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Core.Tests.Soporte;

public sealed class BaseDeDatosEnMemoria : IDisposable
{
    private readonly SqliteConnection _conexion;

    public BaseDeDatosEnMemoria()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        using var contexto = NuevoContexto();
        contexto.Database.EnsureCreated();
    }

    public AppDbContext NuevoContexto() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexion).Options);

    public AppDbContext NuevoContexto(IInterceptor interceptor) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexion).AddInterceptors(interceptor).Options);

    public AppDbContext NuevoContextoQueRegistraSql(ICollection<string> sentencias) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_conexion)
            .LogTo(sentencias.Add, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
            .Options);

    public Guid CrearUsuario(RolUsuario rol, string nombre = "Usuario de prueba", bool estaActivo = true)
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = nombre,
            Correo = $"{Guid.NewGuid():N}@prueba.com",
            PasswordHash = "hash",
            Rol = rol,
            EstaActivo = estaActivo,
            FechaCreacionUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        using var contexto = NuevoContexto();
        contexto.Usuarios.Add(usuario);
        contexto.SaveChanges();

        return usuario.Id;
    }

    public void Dispose() => _conexion.Dispose();
}
