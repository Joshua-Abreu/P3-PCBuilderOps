using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Core.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Mantener igual que AddDbContext en src/API/Program.cs: toda opción nueva de UseSqlServer va en ambos.
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(CadenaConexion.Obtener())
            .Options;

        return new AppDbContext(opciones);
    }
}
