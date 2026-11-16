namespace PCBuilderOps.Services;

// RD-07
public static class DetectorImagen
{
    private static readonly byte[] FirmaJpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] FirmaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] FirmaRiff = "RIFF"u8.ToArray();
    private static readonly byte[] FirmaWebp = "WEBP"u8.ToArray();

    public static string? TipoContenido(ReadOnlySpan<byte> contenido)
    {
        if (contenido.StartsWith(FirmaJpeg))
            return "image/jpeg";

        if (contenido.StartsWith(FirmaPng))
            return "image/png";

        if (contenido.Length >= 12 && contenido.StartsWith(FirmaRiff) && contenido[8..12].SequenceEqual(FirmaWebp))
            return "image/webp";

        return null;
    }
}
