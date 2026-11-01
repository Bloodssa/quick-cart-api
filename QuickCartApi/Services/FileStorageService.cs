using QuickCartApi.Services.Interfaces;

namespace QuickCartApi.Services
{
    public class FileStorageService(IWebHostEnvironment env) : IFileStorageService
    {
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
        private const long MaxFileSizeBytes = 4 * 1024 * 1024; // 4mb limit

        // magic byte signatures for every allowed format
        private static readonly Dictionary<string, byte[][]> Signatures = new()
        {
            [".jpg"] = [[0xFF, 0xD8, 0xFF]],
            [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
            [".png"] = [[0x89, 0x50, 0x4E, 0x47]],
            [".webp"] = [[0x52, 0x49, 0x46, 0x46]]
        };

        public async Task<(bool Success, string? FileName, string? Error)> SaveImageAsync(IFormFile file, string folder = "products")
        {
            if (file.Length == 0)
                return (false, null, "File is Empty.");

            if (file.Length > MaxFileSizeBytes)
                return (false, null, "File size exceeds the 4mb limit.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // validate if the file is not allowed extension
            if (!AllowedExtensions.Contains(extension))
                return (false, null, "Only .jpg, .jpeg, .png, and .webp files are allowed.");

            if (!AllowedContentTypes.Contains(file.ContentType))
                return (false, null, "Invalid file content type.");

            await using (var stream = file.OpenReadStream())
            {
                var header = new byte[8];
                var bytesRead = await stream.ReadAsync(header.AsMemory(0, 8));

                var validSignature = Signatures[extension]
                    .Any(sig => bytesRead >= sig.Length && sig.SequenceEqual(header.Take(sig.Length)));

                if (!validSignature)
                    return (false, null, "File content does not match its extension.");
            }

            // random guid to store in string image url
            var fileName = $"{Guid.NewGuid():N}{extension}";

            // wwwroot/uploads/{folder}
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return (true, fileName, null);
        }

        public void DeleteImage(string fileName, string folder = "products")
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            // protects from traversal of root deletion
            if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
                return;

            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            var path = Path.Combine(webRoot, "uploads", folder, fileName);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}