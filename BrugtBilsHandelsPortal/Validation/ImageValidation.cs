namespace BrugtBilsHandelsPortal.Validation;

public static class ImageValidation
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static bool TryGetExtension(ReadOnlySpan<byte> content, string contentType, out string extension)
    {
        extension = string.Empty;

        if (IsJpeg(content) && string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
        {
            extension = "jpg";
            return true;
        }

        if (IsPng(content) && string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase))
        {
            extension = "png";
            return true;
        }

        if (IsWebP(content) && string.Equals(contentType, "image/webp", StringComparison.OrdinalIgnoreCase))
        {
            extension = "webp";
            return true;
        }

        return false;
    }

    private static bool IsJpeg(ReadOnlySpan<byte> content)
    {
        return content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF;
    }

    private static bool IsPng(ReadOnlySpan<byte> content)
    {
        return content.Length >= PngSignature.Length && content[..PngSignature.Length].SequenceEqual(PngSignature);
    }

    private static bool IsWebP(ReadOnlySpan<byte> content)
    {
        return content.Length >= 12 && content[..4].SequenceEqual("RIFF"u8) && content.Slice(8, 4).SequenceEqual("WEBP"u8);
    }
}