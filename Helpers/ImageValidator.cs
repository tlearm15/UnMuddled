namespace GroupProject.Helpers;

public static class ImageValidator
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/png", "image/webp"];

    public static (bool IsValid, string? ErrorMessage) ValidateImage(IFormFile file)
    {
        if (file.Length == 0)
            return (false, "The uploaded file is empty.");

        if (file.Length > MaxFileSize)
            return (false, "Image must be smaller than 5 MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            return (false, "Invalid file extension. Only JPG, PNG, and WebP images are allowed.");

        if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            return (false, "Invalid file MIME type. Only image files are allowed.");

        if (!HasValidImageSignature(file, extension))
            return (false, "The file content does not match a valid image format.");

        return (true, null);
    }

    private static bool HasValidImageSignature(IFormFile file, string extension)
    {
        using var stream = file.OpenReadStream();
        if (stream.Length < 12) return false;

        Span<byte> header = stackalloc byte[12];
        int bytesRead = stream.Read(header);
        if (bytesRead < 12) return false;

        return extension switch
        {
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            ".webp" => header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                       header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
            _ => false
        };
    }
}