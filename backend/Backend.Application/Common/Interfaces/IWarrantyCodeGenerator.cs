public interface IWarrantyCodeGenerator
{
    Task<string> GenerateUniqueCodeAsync();
}