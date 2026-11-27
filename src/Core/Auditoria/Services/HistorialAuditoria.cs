using System.Globalization;
using System.Text.Json;
using Core.Auditoria.Exceptions;
using Core.Auth.Exceptions;
using Core.Data;
using Core.Reportes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Auditoria.Services;

// RF-AUD-06, RF-AUD-07, RD-06, RD-07
public class HistorialAuditoria : IHistorialAuditoria
{
    public const int TamanoPaginaPorDefecto = 50;
    public const int TamanoPaginaMaximo = 100;

    private readonly AppDbContext _dbContext;
    private readonly EntidadesAuditables _entidades;

    public HistorialAuditoria(AppDbContext dbContext, EntidadesAuditables entidades)
    {
        _dbContext = dbContext;
        _entidades = entidades;
    }

    public async Task<PaginaAuditoria> ConsultarAsync(ActorAuditoria actor, ConsultaAuditoria consulta, CancellationToken ct = default)
    {
        if (!actor.EsAdministrador)
            throw new AccesoAuditoriaDenegadoException();

        var usuarioId = LeerUsuario(consulta.UsuarioId);
        var entidad = LeerEntidad(consulta.Entidad);
        var rango = LeerRango(consulta.Desde, consulta.Hasta);
        var pagina = LeerEntero(consulta.Pagina, 1, 1, int.MaxValue, "La página debe ser un número mayor o igual a 1.");
        var tamanoPagina = LeerEntero(
            consulta.TamanoPagina,
            TamanoPaginaPorDefecto,
            1,
            TamanoPaginaMaximo,
            $"El tamaño de página debe ser un número entre 1 y {TamanoPaginaMaximo}.");

        var registros = _dbContext.RegistrosAuditoria.AsNoTracking();

        if (usuarioId is { } idUsuario)
            registros = registros.Where(r => r.UsuarioId == idUsuario);

        if (entidad is not null)
            registros = registros.Where(r => r.Entidad == entidad);

        if (rango is not null)
            registros = registros.Where(r => r.FechaUtc >= rango.DesdeUtc && r.FechaUtc <= rango.HastaInclusivoUtc);

        var total = await registros.CountAsync(ct);

        var filas = await (
                from r in registros
                join u in _dbContext.Usuarios.AsNoTracking() on r.UsuarioId equals u.Id
                orderby r.FechaUtc descending, r.Id
                select new { Registro = r, UsuarioNombre = u.Nombre })
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(ct);

        var detalles = filas
            .Select(f => new RegistroAuditoriaDetalle(
                f.Registro.Id,
                f.Registro.UsuarioId,
                f.UsuarioNombre,
                f.Registro.Accion,
                f.Registro.Entidad,
                f.Registro.EntidadId,
                f.Registro.FechaUtc,
                ComoJson(f.Registro.ValorAnterior),
                ComoJson(f.Registro.ValorNuevo)))
            .ToList();

        return new PaginaAuditoria(total, pagina, tamanoPagina, detalles);
    }

    private static Guid? LeerUsuario(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        return Guid.TryParse(valor.Trim(), out var id)
            ? id
            : throw new ValidacionException("El filtro usuarioId no es un identificador válido.");
    }

    private string? LeerEntidad(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        return _entidades.Nombres.FirstOrDefault(n => string.Equals(n, valor.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new ValidacionException($"La entidad no es válida. Usa {string.Join(", ", _entidades.Nombres)}.");
    }

    private static RangoFechas? LeerRango(string? desde, string? hasta)
    {
        var hayDesde = !string.IsNullOrWhiteSpace(desde);
        var hayHasta = !string.IsNullOrWhiteSpace(hasta);

        if (!hayDesde && !hayHasta)
            return null;

        if (hayDesde != hayHasta)
            throw new ValidacionException($"Indica desde y hasta juntos, con el formato {RangoFechas.Formato}.");

        return RangoFechas.Crear(desde, hasta);
    }

    private static int LeerEntero(string? valor, int porDefecto, int minimo, int maximo, string mensaje)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return porDefecto;

        return int.TryParse(valor.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var numero) && numero >= minimo && numero <= maximo
            ? numero
            : throw new ValidacionException(mensaje);
    }

    private static JsonElement ComoJson(string valor)
    {
        using var documento = JsonDocument.Parse(valor);
        return documento.RootElement.Clone();
    }
}
