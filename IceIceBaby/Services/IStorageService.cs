using Microsoft.AspNetCore.Http;

namespace IceIceBaby.Services;

public interface IStorageService
{
    // Saves POD image to non-public storage and returns the absolute path
    Task<string> SavePodAsync(int orderId, IFormFile file, CancellationToken ct = default);
}
