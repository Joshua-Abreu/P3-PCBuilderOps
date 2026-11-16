using PCBuilderOps.Services;

namespace PCBuilderOps.Tests.Services;

// RD-07
public class DetectorImagenTests
{
    public static TheoryData<byte[], string?> Firmas => new()
    {
        { [0xFF, 0xD8, 0xFF, 0xE0], "image/jpeg" },
        { [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "image/png" },
        { [.. "RIFF"u8, 0, 0, 0, 0, .. "WEBP"u8], "image/webp" },
        { [.. "RIFF"u8, 0, 0, 0, 0, .. "WAVE"u8], null },
        { [.. "GIF89a"u8], null },
        { [.. "<svg"u8], null },
        { [], null }
    };

    [Theory]
    [MemberData(nameof(Firmas))]
    public void TipoContenido_ReconoceSoloJpegPngYWebp(byte[] contenido, string? esperado)
    {
        // Actuar
        var tipo = DetectorImagen.TipoContenido(contenido);

        // Verificar
        Assert.Equal(esperado, tipo);
    }
}
