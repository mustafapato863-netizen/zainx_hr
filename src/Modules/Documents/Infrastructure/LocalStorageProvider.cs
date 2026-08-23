using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
        _baseStoragePath = Path.GetFullPath(baseStoragePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage", "documents"));
        if (!Directory.Exists(_baseStoragePath))
        {
            Directory.CreateDirectory(_baseStoragePath);
        }
    }

    public async Task<string> SaveAsync(Stream content, string tenantId, string fileName, CancellationToken ct = default)
    {
        var sanitizedTenantId = DocumentSecurityValidator.SanitizeFileName(tenantId);
        var sanitizedFileName = DocumentSecurityValidator.SanitizeFileName(fileName);

        var tenantFolder = Path.Combine(_baseStoragePath, sanitizedTenantId);
        if (!Directory.Exists(tenantFolder))
        {
            Directory.CreateDirectory(tenantFolder);
        }

        var uniqueKey = $"{Guid.NewGuid():N}_{sanitizedFileName}";
        var fullPath = Path.Combine(tenantFolder, uniqueKey);

        // Path traversal sanity check
        var resolvedPath = Path.GetFullPath(fullPath);
        if (!resolvedPath.StartsWith(_baseStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Path traversal violation detected during document save.");
        }

        await using var fileStream = new FileStream(resolvedPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await content.CopyToAsync(fileStream, ct);

        // Return relative storage key (never expose physical server paths)
        return $"{sanitizedTenantId}/{uniqueKey}";
    }

    public Task<Stream?> ReadAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains(".."))
        {
            throw new ArgumentException("Invalid storage key format.");
        }

        var safeKey = storageKey.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.GetFullPath(Path.Combine(_baseStoragePath, safeKey));

        // Path traversal guard
        if (!fullPath.StartsWith(_baseStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Path traversal violation detected during document read.");
        }

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains(".."))
        {
            return Task.FromResult(false);
        }

        var safeKey = storageKey.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.GetFullPath(Path.Combine(_baseStoragePath, safeKey));

        if (!fullPath.StartsWith(_baseStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
