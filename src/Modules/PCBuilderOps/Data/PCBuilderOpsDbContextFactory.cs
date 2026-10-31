using Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PCBuilderOps.Data;

public class PCBuilderOpsDbContextFactory : IDesignTimeDbContextFactory<PCBuilderOpsDbContext>
{
    public PCBuilderOpsDbContext CreateDbContext(string[] args)
    {
        // Mantener igual que AddDbContext en src/API/Program.cs: toda opción nueva de UseSqlServer va en ambos.
        var opciones = new DbContextOptionsBuilder<PCBuilderOpsDbContext>()
            .UseSqlServer(
                CadenaConexion.Obtener(),
                sql => sql.MigrationsHistoryTable(PCBuilderOpsDbContext.TablaHistorialMigraciones))
            .Options;

        return new PCBuilderOpsDbContext(opciones);
    }
}
