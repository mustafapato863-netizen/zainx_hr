using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Workforce.Modules.Documents.Infrastructure;

public interface IMalwareScanner
{
    Task<MalwareScanResult> ScanAsync(Stream fileStream, string fileName, CancellationToken ct = default);
}

public record MalwareScanResult(bool IsClean, string Status, string? ThreatName = null);

public class PassThroughMalwareScanner : IMalwareScanner
{
    public Task<MalwareScanResult> ScanAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        // Baseline scanner returns Clean; integrates with ClamAV / Windows Defender / VirusTotal in production
        return Task.FromResult(new MalwareScanResult(true, "ScannedClean"));
    }
}

public static class DocumentSecurityValidator
{
    public const long MaxFileSizeBytes = 15 * 1024 * 1024; // 15 MB

    private static readonly string[] AllowedExtensions = { ".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx" };

    public static void ValidateFileName(string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("File name cannot be empty.");
        }

        if (originalFileName.Contains("..") || originalFileName.Contains('/') || originalFileName.Contains('\\'))
        {
            throw new ArgumentException("Path traversal characters in file name are strictly prohibited.");
        }

        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            throw new ArgumentException($"File extension '{ext}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}");
        }
    }

    public static async Task ValidateContentSignatureAsync(Stream stream, string originalFileName, CancellationToken ct = default)
    {
        if (stream.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException($"File size exceeds the maximum limit of 15 MB (actual: {stream.Length / (1024 * 1024):F1} MB).");
        }

        if (stream.Length < 4)
        {
            throw new ArgumentException("File content is too small to be a valid document.");
        }

        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        var buffer = new byte[8];
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 8), ct);
        stream.Position = 0; // Reset stream

        if (bytesRead < 4)
        {
            throw new ArgumentException("Unable to read file content signature.");
        }

        // Magic byte verification
        switch (ext)
        {
            case ".pdf":
                // PDF header starts with '%PDF' (0x25, 0x50, 0x44, 0x46)
                if (buffer[0] != 0x25 || buffer[1] != 0x50 || buffer[2] != 0x44 || buffer[3] != 0x46)
                {
                    throw new ArgumentException("Invalid file content: header does not match PDF signature.");
                }
                break;

            case ".png":
                // PNG header starts with 0x89, 0x50, 0x4E, 0x47
                if (buffer[0] != 0x89 || buffer[1] != 0x50 || buffer[2] != 0x4E || buffer[3] != 0x47)
                {
                    throw new ArgumentException("Invalid file content: header does not match PNG signature.");
                }
                break;

            case ".jpg" or ".jpeg":
                // JPEG header starts with 0xFF, 0xD8, 0xFF
                if (buffer[0] != 0xFF || buffer[1] != 0xD8 || buffer[2] != 0xFF)
                {
                    throw new ArgumentException("Invalid file content: header does not match JPEG signature.");
                }
                break;
        }
    }

    public static string SanitizeFileName(string fileName)
    {
        var rawName = Path.GetFileName(fileName);
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(rawName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? $"doc_{Guid.NewGuid():N}.pdf" : sanitized;
    }
}
