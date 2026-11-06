using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Application.Modules.Content.Validators;

public static class ImageFileValidator
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> ForbiddenExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".sh", ".ps1", ".php", ".js", ".mjs", ".cjs",
        ".html", ".htm", ".svg", ".msi", ".scr", ".vbs", ".jar", ".py", ".rb", ".pl"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "application/octet-stream"
    };

    public static string ValidateAndBuildSafeFileName(
        Stream? stream,
        string? clientFileName,
        string? contentType = null,
        long? declaredLength = null)
    {
        if (stream is null)
            throw new DomainException("File ảnh tải lên không được để trống.");

        if (string.IsNullOrWhiteSpace(clientFileName))
            throw new DomainException("Tên file ảnh không hợp lệ.");

        // Check double extensions or executable extensions
        var fileNameOnly = Path.GetFileName(clientFileName.Trim());
        var ext = Path.GetExtension(fileNameOnly)?.ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(ext) || ForbiddenExtensions.Contains(ext))
            throw new DomainException("Định dạng file không được phép tải lên. Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP.");

        // Also check any inner extension (e.g. malware.exe.jpg)
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileNameOnly);
        var innerExt = Path.GetExtension(nameWithoutExt);
        if (!string.IsNullOrWhiteSpace(innerExt) && ForbiddenExtensions.Contains(innerExt))
            throw new DomainException("Tên file chứa phần mở rộng thực thi không hợp lệ.");

        if (!AllowedExtensions.Contains(ext))
            throw new DomainException("Chỉ chấp nhận định dạng ảnh .jpg, .jpeg, .png hoặc .webp.");

        if (!string.IsNullOrWhiteSpace(contentType) && !AllowedContentTypes.Contains(contentType.Trim()))
            throw new DomainException($"Loại nội dung '{contentType}' không hợp lệ. Chỉ chấp nhận file hình ảnh.");

        var length = declaredLength ?? (stream.CanSeek ? stream.Length : -1);
        if (length == 0)
            throw new DomainException("File ảnh tải lên bị rỗng (0 byte).");

        if (length > MaxFileSizeBytes)
            throw new DomainException("Dung lượng ảnh vượt quá giới hạn tối đa 5 MB.");

        if (stream.CanSeek)
        {
            if (stream.Length == 0)
                throw new DomainException("File ảnh tải lên bị rỗng (0 byte).");

            if (stream.Length > MaxFileSizeBytes)
                throw new DomainException("Dung lượng ảnh vượt quá giới hạn tối đa 5 MB.");

            stream.Position = 0;
            Span<byte> header = stackalloc byte[12];
            var bytesRead = stream.Read(header);
            stream.Position = 0;

            // Reject PE executables ("MZ") or shell scripts ("#!") disguised as images
            if (bytesRead >= 2 &&
                ((header[0] == 0x4D && header[1] == 0x5A) ||
                 (header[0] == 0x23 && header[1] == 0x21) ||
                 (header[0] == 0x7F && header[1] == 0x45)))
            {
                throw new DomainException("Phát hiện nội dung file thực thi không hợp lệ.");
            }
        }

        var normalizedExt = ext == ".jpeg" ? ".jpg" : ext;
        return $"service-{Guid.NewGuid():N}{normalizedExt}";
    }
}
