using RepairShop.Application.Common.Interfaces;

namespace RepairShop.IntegrationTests.TestDoubles;

/// <summary>
/// Thay thế CloudinaryFileStorageService khi chạy Integration Test — test không nên phụ thuộc
/// dịch vụ ngoài (Cloudinary) thật, tránh test fail vì lý do không liên quan (mất mạng, hết quota,
/// sai API key), và tránh tốn phí/quota Cloudinary mỗi lần chạy CI.
/// </summary>
public class FakeFileStorageService : IFileStorageService
{
    public Task<UploadedFileResult> UploadImageAsync(Stream fileStream, string fileName, string folder) =>
        Task.FromResult(new UploadedFileResult(
            Url: $"https://fake-storage.test/{folder}/{fileName}",
            PublicId: Guid.NewGuid().ToString()));
}