using RepairShop.Application.Common.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace RepairShop.Infrastructure.ExternalServices;

public class CloudinaryFileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryFileStorageService> _logger;

    public CloudinaryFileStorageService(
        IOptions<CloudinarySettings> settings,
        ILogger<CloudinaryFileStorageService> logger)
    {
        var s = settings.Value;
        var account = new Account(s.CloudName, s.ApiKey, s.ApiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Timeout = s.TimeoutMilliseconds > 0 ? s.TimeoutMilliseconds : 30000;
        _logger = logger;
    }

    public async Task<UploadedFileResult> UploadImageAsync(Stream fileStream, string fileName, string folder)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = folder, // VD: "repairshop/tickets/{ticketId}"
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
            _logger.LogError(ex, "Cloudinary upload timeout cho file {FileName} trong folder {Folder}", fileName, folder);
            throw new TimeoutException("Cloudinary không phản hồi kịp thời khi upload ảnh.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Không thể kết nối Cloudinary khi upload file {FileName}", fileName);
            throw new InvalidOperationException("Không thể kết nối dịch vụ lưu trữ ảnh.", ex);
        }
    }
}