using RepairShop.Application.Common.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace RepairShop.Infrastructure.ExternalServices;

public class CloudinaryFileStorageService : IFileStorageService
{
    private readonly Cloudinary? _cloudinary;
    private readonly bool _hasCloudinaryConfig;
    private readonly ILogger<CloudinaryFileStorageService> _logger;

    public CloudinaryFileStorageService(
        IOptions<CloudinarySettings> settings,
        ILogger<CloudinaryFileStorageService> logger)
    {
        var s = settings.Value;
        _logger = logger;
        _hasCloudinaryConfig =
            !string.IsNullOrWhiteSpace(s.CloudName) &&
            !string.IsNullOrWhiteSpace(s.ApiKey) &&
            !string.IsNullOrWhiteSpace(s.ApiSecret);

        if (_hasCloudinaryConfig)
        {
            var account = new Account(s.CloudName, s.ApiKey, s.ApiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Timeout = s.TimeoutMilliseconds > 0 ? s.TimeoutMilliseconds : 30000;
        }
    }

    public async Task<UploadedFileResult> UploadImageAsync(Stream fileStream, string fileName, string folder)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = ".jpg";
        }
        var safeFileName = $"{Guid.NewGuid():N}{ext}";

        if (!_hasCloudinaryConfig || _cloudinary is null)
        {
            return await SaveLocallyAsync(fileStream, safeFileName, folder);
        }

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(safeFileName, fileStream),
            Folder = folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        try
        {
            if (fileStream.CanSeek)
                fileStream.Position = 0;

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error is not null)
                throw new InvalidOperationException($"Upload ảnh thất bại: {result.Error.Message}");

            return new UploadedFileResult(result.SecureUrl.ToString(), result.PublicId);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Cloudinary upload timeout cho file {FileName} trong folder {Folder}", safeFileName, folder);
            throw new TimeoutException("Cloudinary không phản hồi kịp thời khi upload ảnh.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Không thể kết nối Cloudinary khi upload file {FileName}", safeFileName);
            throw new InvalidOperationException("Không thể kết nối dịch vụ lưu trữ ảnh.", ex);
        }
    }

    public async Task DeleteImageAsync(string? imageUrlOrPublicId)
    {
        if (string.IsNullOrWhiteSpace(imageUrlOrPublicId))
            return;

        try
        {
            if (imageUrlOrPublicId.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                var relative = imageUrlOrPublicId.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);
                if (File.Exists(localPath))
                {
                    File.Delete(localPath);
                }
                return;
            }

            if (_hasCloudinaryConfig && _cloudinary is not null && imageUrlOrPublicId.Contains("cloudinary.com", StringComparison.OrdinalIgnoreCase))
            {
                var publicId = TryExtractPublicId(imageUrlOrPublicId);
                if (!string.IsNullOrWhiteSpace(publicId))
                {
                    await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể xóa ảnh cũ {ImageUrl}", imageUrlOrPublicId);
        }
    }

    private static async Task<UploadedFileResult> SaveLocallyAsync(Stream fileStream, string safeFileName, string folder)
    {
        var normalizedFolder = folder.Trim('/').Replace('\\', '/');
        var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", normalizedFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(wwwroot);
        var fullPath = Path.Combine(wwwroot, safeFileName);

        if (fileStream.CanSeek)
            fileStream.Position = 0;

        await using var outStream = File.Create(fullPath);
        await fileStream.CopyToAsync(outStream);

        var url = $"/uploads/{normalizedFolder}/{safeFileName}";
        return new UploadedFileResult(url, $"{normalizedFolder}/{safeFileName}");
    }

    private static string? TryExtractPublicId(string cloudinaryUrl)
    {
        if (!Uri.TryCreate(cloudinaryUrl, UriKind.Absolute, out var uri))
            return null;

        var path = uri.AbsolutePath;
        var uploadIdx = path.IndexOf("/upload/", StringComparison.OrdinalIgnoreCase);
        if (uploadIdx < 0)
            return null;

        var afterUpload = path[(uploadIdx + "/upload/".Length)..];
        var segments = afterUpload.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return null;

        var startIdx = segments[0].StartsWith('v') && segments[0].Length > 1 && segments[0][1..].All(char.IsDigit)
            ? 1
            : 0;

        var publicIdWithExt = string.Join("/", segments.Skip(startIdx));
        var lastDot = publicIdWithExt.LastIndexOf('.');
        return lastDot > 0 ? publicIdWithExt[..lastDot] : publicIdWithExt;
    }
}
