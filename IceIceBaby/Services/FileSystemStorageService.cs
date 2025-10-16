using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace IceIceBaby.Services;

public class FileSystemStorageService : IStorageService
{
    private readonly IWebHostEnvironment _env;

    public FileSystemStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SavePodAsync(int orderId, IFormFile file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            throw new InvalidDataException("No file uploaded.");

        // Validate MIME type
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };
        if (!allowed.Contains(file.ContentType))
            throw new InvalidDataException("Unsupported file type.");

        // Non-public root under ContentRoot/App_Data/pod/yyyy/MM
        var root = Path.Combine(_env.ContentRootPath, "App_Data", "pod", DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
        Directory.CreateDirectory(root);

        var ext = Path.GetExtension(file.FileName);
        var rand = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
        var name = $"{DateTime.UtcNow:yyyyMMdd_HHmmssfff}_order{orderId}_{rand}{ext}";
        var fullPath = Path.Combine(root, name);

        await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true);
        await file.CopyToAsync(stream, ct);
        return fullPath;
    }
}
