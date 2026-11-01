namespace QuickCartApi.Services.Interfaces
{
    public interface IFileStorageService
    {
        Task<(bool Success, string? FileName, string? Error)> SaveImageAsync(IFormFile file, string folder = "products");
        void DeleteImage(string fileName, string folder = "products");
    }
}
