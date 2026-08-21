namespace RepairShop.Application.Common.Interfaces;

public record UploadedFileResult(string Url, string PublicId);

public interface IFileStorageService
{
    Task<UploadedFileResult> UploadImageAsync(Stream fileStream, string fileName, string folder);
}