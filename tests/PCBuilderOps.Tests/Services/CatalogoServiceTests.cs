using Core.Auth.Domain;
using Core.Auth.Exceptions;
using Core.Documentos.Services;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;
using PCBuilderOps.Domain;
using PCBuilderOps.Exceptions;
using PCBuilderOps.Services;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Services;

// RF-NEG-01, FN-01
public sealed class CatalogoServiceTests : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 5, 6, 7];

    private static readonly DatosComponente CpuValida = new(
        "  Ryzen 5 7600  ", "Cpu", "AMD", "AM5", 65, null, 199.99m, 8, null, null);

    private readonly BaseDeDatosDelNegocio _bd = new();
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "pcbuilderops-pruebas", Guid.NewGuid().ToString("N"));
    private readonly AlmacenamientoArchivosLocal _almacenamiento;

    public CatalogoServiceTests()
    {
        _almacenamiento = new AlmacenamientoArchivosLocal(_carpeta);
    }

    public void Dispose()
    {
        _bd.Dispose();

        if (Directory.Exists(_carpeta))
            Directory.Delete(_carpeta, recursive: true);
    }

    private CatalogoService Catalogo() => _bd.NuevoServicioCatalogo(_almacenamiento);

    private static ImagenSubida Imagen(byte[] contenido) => new(contenido.Length, new MemoryStream(contenido));

    private string[] ArchivosGuardados() => Directory.GetFiles(_carpeta);

    [Fact]
    public async Task Listar_ParaElCliente_DevuelveElCatalogoInicialActivo()
    {
        // Actuar
        var componentes = await Catalogo().ListarAsync(incluirRetirados: false);

        // Verificar
        Assert.Equal(ComponentesIniciales.Todos.Length, componentes.Count);
        Assert.All(componentes, c => Assert.True(c.EstaActivo));
        Assert.All(componentes, c => Assert.Null(c.ImagenUrl));
    }

    [Fact]
    public async Task Crear_DatosValidos_GuardaElComponenteActivoYNormalizado()
    {
        // Actuar
        var creado = await Catalogo().CrearAsync(CpuValida);

        // Verificar
        var guardado = await _bd.NuevoContextoNegocio().Componentes.SingleAsync(c => c.Id == creado.Id);
        Assert.Equal("Ryzen 5 7600", guardado.Nombre);
        Assert.Equal(CategoriaComponente.Cpu, guardado.Categoria);
        Assert.Equal(199.99m, guardado.Precio);
        Assert.True(guardado.EstaActivo);
    }

    [Fact]
    public async Task Crear_PlacaConMemoriaEnMinusculas_GuardaElTipoEnMayusculas()
    {
        // Preparar
        var placa = new DatosComponente("B760M", "placabase", "MSI", "LGA1700", 30, null, 129m, 3, " ddr5 ", 2);

        // Actuar
        var creada = await Catalogo().CrearAsync(placa);

        // Verificar
        Assert.Equal("PlacaBase", creada.Categoria);
        Assert.Equal("DDR5", creada.TipoMemoria);
        Assert.Equal(2, creada.RanurasMemoria);
    }

    public static TheoryData<DatosComponente, string> DatosInvalidos => new()
    {
        { CpuValida with { Nombre = "   " }, "El nombre es obligatorio." },
        { CpuValida with { Nombre = new string('x', 151) }, "El nombre no puede superar 150 caracteres." },
        { CpuValida with { Categoria = "Teclado" }, "La categoría no es válida. Usa Cpu, PlacaBase, Gpu, Ram, Almacenamiento, Fuente." },
        { CpuValida with { Categoria = "1" }, "La categoría no es válida. Usa Cpu, PlacaBase, Gpu, Ram, Almacenamiento, Fuente." },
        { CpuValida with { Fabricante = null }, "El fabricante es obligatorio." },
        { CpuValida with { SocketOInterfaz = "" }, "El socket o interfaz es obligatorio." },
        { CpuValida with { ConsumoWatts = -1 }, "El consumo debe estar entre 0 y 2000 W." },
        { CpuValida with { ConsumoWatts = null }, "El consumo debe estar entre 0 y 2000 W." },
        { CpuValida with { PotenciaWatts = 500 }, "Solo las fuentes de poder tienen potencia." },
        { CpuValida with { Categoria = "Fuente", PotenciaWatts = null }, "La potencia de la fuente es obligatoria y debe estar entre 1 y 3000 W." },
        { CpuValida with { TipoMemoria = "DDR5" }, "Solo las placas base tienen tipo de memoria y ranuras." },
        { CpuValida with { Categoria = "PlacaBase", RanurasMemoria = 0 }, "Las ranuras de memoria deben estar entre 1 y 16." },
        { CpuValida with { Precio = 0m }, "El precio debe ser mayor que 0 y menor que 100000." },
        { CpuValida with { Precio = 10.999m }, "El precio admite como máximo dos decimales." },
        { CpuValida with { StockDisponible = -1 }, "Las existencias deben estar entre 0 y 100000." }
    };

    [Theory]
    [MemberData(nameof(DatosInvalidos))]
    public async Task Crear_DatosInvalidos_SeRechazaConSuMensajeYNoGuarda(DatosComponente datos, string mensaje)
    {
        // Actuar
        var accion = () => Catalogo().CrearAsync(datos);

        // Verificar
        var error = await Assert.ThrowsAsync<ValidacionException>(accion);
        Assert.Equal(mensaje, error.Message);
        Assert.Equal(ComponentesIniciales.Todos.Length, await _bd.NuevoContextoNegocio().Componentes.CountAsync());
    }

    [Fact]
    public async Task Actualizar_PrecioYExistencias_NoCambiaLasOrdenesYaCotizadas()
    {
        // Preparar
        var clienteId = _bd.CrearUsuario(RolUsuario.Estandar);
        var orden = await _bd.NuevoServicioOrdenes().CotizarAsync(clienteId,
        [
            new(ComponentesIniciales.CpuAm5, 1),
            new(ComponentesIniciales.PlacaAm5, 1),
            new(ComponentesIniciales.Fuente850W, 1)
        ]);
        var datos = new DatosComponente("Ryzen 7 7700X", "Cpu", "AMD", "AM5", 105, null, 249.00m, 3, null, null);

        // Actuar
        var actualizado = await Catalogo().ActualizarAsync(ComponentesIniciales.CpuAm5, datos);

        // Verificar
        Assert.Equal(249.00m, actualizado.Precio);
        Assert.Equal(3, actualizado.StockDisponible);
        var detalle = await _bd.NuevoContextoNegocio().DetallesEnsamble
            .SingleAsync(d => d.OrdenEnsambleId == orden.Id && d.ComponenteId == ComponentesIniciales.CpuAm5);
        Assert.Equal(299.00m, detalle.PrecioUnitario);
        Assert.Equal(orden.CostoTotal, (await _bd.NuevoContextoNegocio().OrdenesEnsamble.SingleAsync()).CostoTotal);
    }

    [Fact]
    public async Task Actualizar_OtraCategoria_SeRechaza()
    {
        // Preparar
        var datos = new DatosComponente("Ryzen 7 7700X", "Gpu", "AMD", "AM5", 105, null, 299m, 10, null, null);

        // Actuar
        var accion = () => Catalogo().ActualizarAsync(ComponentesIniciales.CpuAm5, datos);

        // Verificar
        var error = await Assert.ThrowsAsync<ValidacionException>(accion);
        Assert.Equal("La categoría de un componente no se puede cambiar; retíralo y crea uno nuevo.", error.Message);
    }

    [Fact]
    public async Task Actualizar_ComponenteInexistente_LanzaNoEncontrado()
    {
        // Actuar
        var accion = () => Catalogo().ActualizarAsync(Guid.NewGuid(), CpuValida);

        // Verificar
        await Assert.ThrowsAsync<ComponenteNoEncontradoException>(accion);
    }

    [Fact]
    public async Task Retirar_LoOcultaAlClienteYSeSigueViendoComoAdministrador()
    {
        // Actuar
        await Catalogo().CambiarEstadoAsync(ComponentesIniciales.GpuAlta, estaActivo: false);

        // Verificar
        var delCliente = await Catalogo().ListarAsync(incluirRetirados: false);
        var delAdministrador = await Catalogo().ListarAsync(incluirRetirados: true);
        Assert.DoesNotContain(delCliente, c => c.Id == ComponentesIniciales.GpuAlta);
        Assert.Contains(delAdministrador, c => c.Id == ComponentesIniciales.GpuAlta && !c.EstaActivo);
    }

    [Fact]
    public async Task Retirar_YaNoSePuedeCotizarNiValidar()
    {
        // Preparar
        await Catalogo().CambiarEstadoAsync(ComponentesIniciales.Fuente850W, estaActivo: false);
        LineaCotizacion[] lineas =
        [
            new(ComponentesIniciales.CpuAm5, 1),
            new(ComponentesIniciales.PlacaAm5, 1),
            new(ComponentesIniciales.Fuente850W, 1)
        ];

        // Actuar
        var cotizar = () => _bd.NuevoServicioOrdenes().CotizarAsync(_bd.CrearUsuario(RolUsuario.Estandar), lineas);
        var validar = () => _bd.NuevoServicioOrdenes().ValidarArmadoAsync(lineas);

        // Verificar
        var error = await Assert.ThrowsAsync<ValidacionException>(cotizar);
        Assert.Equal("El componente RM850x ya no está disponible.", error.Message);
        await Assert.ThrowsAsync<ValidacionException>(validar);
    }

    [Fact]
    public async Task Reactivar_VuelveAlCatalogoDelCliente()
    {
        // Preparar
        await Catalogo().CambiarEstadoAsync(ComponentesIniciales.GpuAlta, estaActivo: false);

        // Actuar
        await Catalogo().CambiarEstadoAsync(ComponentesIniciales.GpuAlta, estaActivo: true);

        // Verificar
        Assert.Contains(await Catalogo().ListarAsync(incluirRetirados: false), c => c.Id == ComponentesIniciales.GpuAlta);
    }

    [Fact]
    public async Task CambiarImagen_Png_LaGuardaYSePuedeDescargar()
    {
        // Actuar
        var resumen = await Catalogo().CambiarImagenAsync(ComponentesIniciales.CpuAm5, Imagen(Png));

        // Verificar
        Assert.StartsWith($"/api/componentes/{ComponentesIniciales.CpuAm5}/imagen?v=componente-", resumen.ImagenUrl);
        var imagen = await Catalogo().ObtenerImagenAsync(ComponentesIniciales.CpuAm5);
        await using var contenido = imagen.Contenido;
        using var copia = new MemoryStream();
        await contenido.CopyToAsync(copia);
        Assert.Equal("image/png", imagen.TipoContenido);
        Assert.Equal(Png, copia.ToArray());
    }

    [Fact]
    public async Task CambiarImagen_ArchivoQueNoEsImagen_SeRechazaYNoGuardaNada()
    {
        // Actuar
        var accion = () => Catalogo().CambiarImagenAsync(ComponentesIniciales.CpuAm5, Imagen("hola"u8.ToArray()));

        // Verificar
        var error = await Assert.ThrowsAsync<ValidacionException>(accion);
        Assert.Equal("La imagen debe ser JPG, PNG o WebP.", error.Message);
        Assert.Empty(ArchivosGuardados());
    }

    [Fact]
    public async Task CambiarImagen_TamanoDeclaradoMayorAlLimite_SeRechaza()
    {
        // Preparar
        var imagen = new ImagenSubida(CatalogoService.TamanoMaximoImagenBytes + 1, new MemoryStream(Png));

        // Actuar
        var accion = () => Catalogo().CambiarImagenAsync(ComponentesIniciales.CpuAm5, imagen);

        // Verificar
        var error = await Assert.ThrowsAsync<ValidacionException>(accion);
        Assert.Equal("La imagen supera el máximo de 2 MB.", error.Message);
    }

    [Fact]
    public async Task CambiarImagen_ContenidoMayorAlLimiteAunqueDeclareMenos_SeRechazaYNoGuarda()
    {
        // Preparar
        var grande = new byte[CatalogoService.TamanoMaximoImagenBytes + 1];
        Png.CopyTo(grande, 0);
        var imagen = new ImagenSubida(Png.Length, new MemoryStream(grande));

        // Actuar
        var accion = () => Catalogo().CambiarImagenAsync(ComponentesIniciales.CpuAm5, imagen);

        // Verificar
        await Assert.ThrowsAsync<ValidacionException>(accion);
        Assert.Empty(ArchivosGuardados());
    }

    [Fact]
    public async Task CambiarImagen_Reemplazar_BorraLaAnterior()
    {
        // Preparar
        await Catalogo().CambiarImagenAsync(ComponentesIniciales.CpuAm5, Imagen(Png));

        // Actuar
        await Catalogo().CambiarImagenAsync(ComponentesIniciales.CpuAm5, Imagen(Jpeg));

        // Verificar
        Assert.Single(ArchivosGuardados());
        Assert.Equal("image/jpeg", (await Catalogo().ObtenerImagenAsync(ComponentesIniciales.CpuAm5)).TipoContenido);
    }

    [Fact]
    public async Task QuitarImagen_BorraElArchivoYLaUrl()
    {
        // Preparar
        await Catalogo().CambiarImagenAsync(ComponentesIniciales.CpuAm5, Imagen(Png));

        // Actuar
        var resumen = await Catalogo().QuitarImagenAsync(ComponentesIniciales.CpuAm5);

        // Verificar
        Assert.Null(resumen.ImagenUrl);
        Assert.Empty(ArchivosGuardados());
        await Assert.ThrowsAsync<ImagenNoEncontradaException>(() => Catalogo().ObtenerImagenAsync(ComponentesIniciales.CpuAm5));
    }

    [Fact]
    public async Task ObtenerImagen_ComponenteInexistente_LanzaNoEncontrado()
    {
        // Actuar
        var accion = () => Catalogo().ObtenerImagenAsync(Guid.NewGuid());

        // Verificar
        await Assert.ThrowsAsync<ComponenteNoEncontradoException>(accion);
    }
}
