using System.Text;
using Core.Auth.Domain;
using Core.Auth.Exceptions;
using Core.Documentos.Exceptions;
using Core.Documentos.Services;
using Core.Tests.Soporte;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests.Documentos;

public sealed class DocumentosServiceTests : IDisposable
{
    private static readonly DateTime FechaSubida = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private readonly BaseDeDatosEnMemoria _baseDeDatos = new();
    private readonly RelojFijo _reloj = new(FechaSubida);
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "pcbuilderops-pruebas", Guid.NewGuid().ToString("N"));
    private readonly Guid _duenoId;
    private readonly Guid _otroEstandarId;
    private readonly Guid _administradorId;

    public DocumentosServiceTests()
    {
        _duenoId = _baseDeDatos.CrearUsuario(RolUsuario.Estandar);
        _otroEstandarId = _baseDeDatos.CrearUsuario(RolUsuario.Estandar);
        _administradorId = _baseDeDatos.CrearUsuario(RolUsuario.Administrador);
    }

    public void Dispose()
    {
        _baseDeDatos.Dispose();

        if (Directory.Exists(_carpeta))
            Directory.Delete(_carpeta, recursive: true);
    }

    private ActorDocumento Dueno => new(_duenoId, false);
    private ActorDocumento OtroEstandar => new(_otroEstandarId, false);
    private ActorDocumento Administrador => new(_administradorId, true);

    private DocumentosService NuevoServicio() =>
        new(_baseDeDatos.NuevoContexto(), new AlmacenamientoArchivosLocal(_carpeta), _reloj);

    private static ArchivoSubido Archivo(string nombre = "benchmark.txt", string contenido = "Cinebench R23: 15000", string? tipo = "text/plain")
    {
        var bytes = Encoding.UTF8.GetBytes(contenido);
        return new ArchivoSubido(nombre, tipo, bytes.Length, new MemoryStream(bytes));
    }

    private Task<DocumentoDetalle> SubirDelDuenoAsync() => NuevoServicio().SubirAsync(_duenoId, Archivo());

    private static string Leer(Stream contenido)
    {
        using var lector = new StreamReader(contenido);
        return lector.ReadToEnd();
    }

    // RF-DOC-01
    [Fact]
    public async Task Subir_ArchivoValido_GuardaNombreTipoTamanoFechaYQuienLoSubio()
    {
        // Preparar
        var servicio = NuevoServicio();

        // Actuar
        var documento = await servicio.SubirAsync(_duenoId, Archivo());

        // Verificar
        using var contexto = _baseDeDatos.NuevoContexto();
        var guardado = await contexto.Documentos.AsNoTracking().SingleAsync(d => d.Id == documento.Id);
        Assert.Equal("benchmark.txt", guardado.NombreOriginal);
        Assert.Equal("text/plain", guardado.TipoContenido);
        Assert.Equal(Encoding.UTF8.GetByteCount("Cinebench R23: 15000"), guardado.TamanoBytes);
        Assert.Equal(FechaSubida, guardado.FechaSubidaUtc);
        Assert.Equal(_duenoId, guardado.SubidoPorId);
        Assert.False(guardado.EstaEliminado);
        Assert.Equal("Usuario de prueba", documento.SubidoPor);
    }

    // RF-DOC-01, RF-DOC-05
    [Fact]
    public async Task Subir_ArchivoValido_GuardaElContenidoConUnNombreGeneradoDentroDeLaCarpeta()
    {
        // Preparar
        var servicio = NuevoServicio();

        // Actuar
        await servicio.SubirAsync(_duenoId, Archivo(nombre: "../../fuera.txt"));

        // Verificar
        var archivo = Assert.Single(Directory.GetFiles(_carpeta));
        Assert.DoesNotContain("fuera", Path.GetFileName(archivo));
        Assert.Equal("Cinebench R23: 15000", await File.ReadAllTextAsync(archivo));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(_carpeta)!));
    }

    // RF-DOC-01
    [Fact]
    public async Task Subir_NombreConRuta_GuardaSoloElNombreDelArchivo()
    {
        // Preparar
        var servicio = NuevoServicio();

        // Actuar
        var documento = await servicio.SubirAsync(_duenoId, Archivo(nombre: @"..\carpeta\reporte.pdf"));

        // Verificar
        Assert.Equal("reporte.pdf", documento.Nombre);
    }

    // RF-DOC-01
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no es un tipo")]
    public async Task Subir_TipoAusenteOInvalido_GuardaTipoGenerico(string? tipo)
    {
        // Preparar
        var servicio = NuevoServicio();

        // Actuar
        var documento = await servicio.SubirAsync(_duenoId, Archivo(tipo: tipo));

        // Verificar
        Assert.Equal("application/octet-stream", documento.TipoContenido);
    }

    // RD-07
    [Fact]
    public async Task Subir_SinArchivo_SeRechazaYNoGuardaNada()
    {
        // Preparar
        var servicio = NuevoServicio();

        // Actuar
        var accion = () => servicio.SubirAsync(_duenoId, null);

        // Verificar
        await Assert.ThrowsAsync<ValidacionException>(accion);
        using var contexto = _baseDeDatos.NuevoContexto();
        Assert.Empty(await contexto.Documentos.ToListAsync());
    }

    // RD-07
    [Fact]
    public async Task Subir_ArchivoVacio_SeRechazaYNoGuardaNada()
    {
        // Preparar
        var servicio = NuevoServicio();

        // Actuar
        var accion = () => servicio.SubirAsync(_duenoId, Archivo(contenido: ""));

        // Verificar
        await Assert.ThrowsAsync<ValidacionException>(accion);
        Assert.False(Directory.Exists(_carpeta) && Directory.GetFiles(_carpeta).Length > 0);
    }

    // RD-07
    [Fact]
    public async Task Subir_ArchivoMayorAlMaximo_SeRechaza()
    {
        // Preparar
        var servicio = NuevoServicio();
        var archivo = new ArchivoSubido("grande.bin", "application/octet-stream", DocumentosService.TamanoMaximoBytes + 1, Stream.Null);

        // Actuar
        var accion = () => servicio.SubirAsync(_duenoId, archivo);

        // Verificar
        await Assert.ThrowsAsync<ValidacionException>(accion);
    }

    // RF-DOC-02
    [Fact]
    public async Task Listar_Estandar_SoloDevuelveLosSuyos()
    {
        // Preparar
        var propio = await SubirDelDuenoAsync();
        await NuevoServicio().SubirAsync(_otroEstandarId, Archivo(nombre: "ajeno.txt"));

        // Actuar
        var listado = await NuevoServicio().ListarAsync(Dueno);

        // Verificar
        Assert.Equal(propio.Id, Assert.Single(listado).Id);
    }

    // RF-DOC-02
    [Fact]
    public async Task Listar_Administrador_DevuelveLosDeTodos()
    {
        // Preparar
        await SubirDelDuenoAsync();
        await NuevoServicio().SubirAsync(_otroEstandarId, Archivo(nombre: "ajeno.txt"));

        // Actuar
        var listado = await NuevoServicio().ListarAsync(Administrador);

        // Verificar
        Assert.Equal(2, listado.Count);
    }

    // RF-DOC-02, RF-DOC-04
    [Fact]
    public async Task Listar_DocumentoEliminado_NoAparece()
    {
        // Preparar
        var documento = await SubirDelDuenoAsync();
        await NuevoServicio().EliminarAsync(Dueno, documento.Id);

        // Actuar
        var delDueno = await NuevoServicio().ListarAsync(Dueno);
        var delAdministrador = await NuevoServicio().ListarAsync(Administrador);

        // Verificar
        Assert.Empty(delDueno);
        Assert.Empty(delAdministrador);
    }

    // RF-DOC-03
    [Fact]
    public async Task Descargar_Dueno_RecibeElContenidoYElNombreOriginal()
    {
        // Preparar
        var documento = await SubirDelDuenoAsync();

        // Actuar
        var descarga = await NuevoServicio().DescargarAsync(Dueno, documento.Id);

        // Verificar
        Assert.Equal("benchmark.txt", descarga.Nombre);
        Assert.Equal("Cinebench R23: 15000", Leer(descarga.Contenido));
    }

    // RF-DOC-03
    [Fact]
    public async Task Descargar_Administrador_RecibeElContenidoDeUnDocumentoAjeno()
    {
        // Preparar
        var documento = await SubirDelDuenoAsync();

        // Actuar
        var descarga = await NuevoServicio().DescargarAsync(Administrador, documento.Id);

        // Verificar
        Assert.Equal("Cinebench R23: 15000", Leer(descarga.Contenido));
    }

    // RF-DOC-03, RD-06
    [Fact]
    public async Task Descargar_OtroEstandar_SeRechaza()
    {
        // Preparar
        var documento = await SubirDelDuenoAsync();

        // Actuar
        var accion = () => NuevoServicio().DescargarAsync(OtroEstandar, documento.Id);

        // Verificar
        await Assert.ThrowsAsync<AccesoDocumentoDenegadoException>(accion);
    }

    // RF-DOC-01, RD-06
    [Fact]
    public async Task Obtener_OtroEstandar_SeRechaza()
    {
        // Preparar
        var documento = await SubirDelDuenoAsync();

        // Actuar
        var accion = () => NuevoServicio().ObtenerAsync(OtroEstandar, documento.Id);

        // Verificar
        await Assert.ThrowsAsync<AccesoDocumentoDenegadoException>(accion);
    }

    // RF-DOC-03, RF-DOC-04
    [Fact]
    public async Task Descargar_DocumentoEliminado_SeRechazaComoNoEncontrado()
    {
        // Preparar
        var documento = await SubirDelDuenoAsync();
        await NuevoServicio().EliminarAsync(Dueno, documento.Id);

        // Actuar
        var accion = () => NuevoServicio().DescargarAsync(Dueno, documento.Id);

        // Verificar
        await Assert.ThrowsAsync<DocumentoNoEncontradoException>(accion);
    }

    // RF-DOC-03
    [Fact]
    public async Task Descargar_DocumentoQueNoExiste_SeRechazaComoNoEncontrado()
    {
        // Preparar
        var servicio = NuevoServicio();

        // Actuar
        var accion = () => servicio.DescargarAsync(Dueno, Guid.NewGuid());

        // Verificar
        await Assert.ThrowsAsync<DocumentoNoEncontradoException>(accion);
    }

    // RF-DOC-04
    [Fact]
    public async Task Eliminar_Dueno_MarcaElRegistroYConservaRegistroYArchivo()
    {
        // Preparar
        var documento = await SubirDelDuenoAsync();
        _reloj.Fijar(FechaSubida.AddHours(1));

        // Actuar
        await NuevoServicio().EliminarAsync(Dueno, documento.Id);

        // Verificar
        using var contexto = _baseDeDatos.NuevoContexto();
        var guardado = await contexto.Documentos.AsNoTracking().SingleAsync(d => d.Id == documento.Id);
        Assert.True(guardado.EstaEliminado);
        Assert.Equal(FechaSubida.AddHours(1), guardado.FechaEliminacionUtc);
        Assert.Equal(_duenoId, guardado.EliminadoPorId);
        Assert.Single(Directory.GetFiles(_carpeta));
    }

    // RF-DOC-04
    [Fact]
    public async Task Eliminar_Administrador_EliminaUnDocumentoAjeno()
    {
        // Preparar
        var documento = await SubirDelDuenoAsync();

        // Actuar
        await NuevoServicio().EliminarAsync(Administrador, documento.Id);

        // Verificar
        Assert.Empty(await NuevoServicio().ListarAsync(Dueno));
    }

    // RF-DOC-04, RD-06
    [Fact]
    public async Task Eliminar_OtroEstandar_SeRechazaYElDocumentoSigueVisible()
    {
        // Preparar
        var documento = await SubirDelDuenoAsync();

        // Actuar
        var accion = () => NuevoServicio().EliminarAsync(OtroEstandar, documento.Id);

        // Verificar
        await Assert.ThrowsAsync<AccesoDocumentoDenegadoException>(accion);
        Assert.Equal(documento.Id, Assert.Single(await NuevoServicio().ListarAsync(Dueno)).Id);
    }

    // RF-DOC-04
    [Fact]
    public async Task Eliminar_DosVeces_LaSegundaSeRechazaComoNoEncontrado()
    {
        // Preparar
        var documento = await SubirDelDuenoAsync();
        await NuevoServicio().EliminarAsync(Dueno, documento.Id);

        // Actuar
        var accion = () => NuevoServicio().EliminarAsync(Dueno, documento.Id);

        // Verificar
        await Assert.ThrowsAsync<DocumentoNoEncontradoException>(accion);
    }
}
