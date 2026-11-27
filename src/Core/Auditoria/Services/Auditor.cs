using System.Text.Json;
using Core.Auditoria.Domain;
using Core.Data;

namespace Core.Auditoria.Services;

// RF-AUD-01, RF-AUD-02 a 05
public class Auditor : IAuditor
{
    private static readonly JsonSerializerOptions FormatoValores = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _reloj;

    public Auditor(AppDbContext dbContext, TimeProvider reloj)
    {
        _dbContext = dbContext;
        _reloj = reloj;
    }

    public void Registrar(Guid usuarioId, string accion, string entidad, string entidadId, object valorAnterior, object valorNuevo) =>
        _dbContext.RegistrosAuditoria.Add(RegistroAuditoria.Crear(
            usuarioId,
            accion,
            entidad,
            entidadId,
            JsonSerializer.Serialize(valorAnterior, FormatoValores),
            JsonSerializer.Serialize(valorNuevo, FormatoValores),
            _reloj.GetUtcNow().UtcDateTime));
}
