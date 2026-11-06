namespace ArchivePieces.Services
{
    public class ImageUploadService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public ImageUploadService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<(bool Success, string? Path, string? Error)> UploadAsync(
            IFormFile file, int productId)
        {
            if (file == null || file.Length == 0)
                return (false, null, "No file provided.");

            if (file.Length > MaxFileSize)
                return (false, null, "File exceeds 5MB limit.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
                return (false, null, "Only JPG, PNG, and WEBP files are allowed.");

            var folder = Path.Combine(_env.WebRootPath, "uploads", "products", productId.ToString());
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            // Return relative web path
            return (true, $"/uploads/products/{productId}/{fileName}", null);
        }

        public void Delete(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
