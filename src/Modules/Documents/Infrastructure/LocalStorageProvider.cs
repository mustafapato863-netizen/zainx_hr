namespace Workforce.Modules.Documents.Infrastructure;

public interface IStorageProvider
{
    Task<string> SaveAsync(Stream content, string tenantId, string fileName, CancellationToken ct = default);
    Task<Stream?> ReadAsync(string storageKey, CancellationToken ct = default);
    Task<bool> DeleteAsync(string storageKey, CancellationToken ct = default);
}

public class LocalStorageProvider : IStorageProvider
{
    private readonly string _baseStoragePath;

    public LocalStorageProvider(string? baseStoragePath = null)
    {
        _baseStoragePath = baseStoragePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage", "documents");
        if (!Directory.Exists(_baseStoragePath))
        {
            Directory.CreateDirectory(_baseStoragePath);
        }
    }

    public async Task<string> SaveAsync(Stream content, string tenantId, string fileName, CancellationToken ct = default)
    {
        var tenantFolder = Path.Combine(_baseStoragePath, tenantId);
        if (!Directory.Exists(tenantFolder))
        {
            Directory.CreateDirectory(tenantFolder);
        }

        var uniqueKey = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(tenantFolder, uniqueKey);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await content.CopyToAsync(fileStream, ct);

        // Return relative storage key for portability
        return $"{tenantId}/{uniqueKey}";
    }

    public Task<Stream?> ReadAsync(string storageKey, CancellationToken ct = default)
    {
        var safeKey = storageKey.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.Combine(_baseStoragePath, safeKey);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var safeKey = storageKey.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.Combine(_baseStoragePath, safeKey);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
