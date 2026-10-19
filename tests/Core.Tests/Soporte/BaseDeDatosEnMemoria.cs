using Core.Auth.Domain;
using Core.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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

        using var contexto = NuevoContexto();
        contexto.Usuarios.Add(usuario);
        contexto.SaveChanges();

        return usuario.Id;
    }

    public void Dispose() => _conexion.Dispose();
}
