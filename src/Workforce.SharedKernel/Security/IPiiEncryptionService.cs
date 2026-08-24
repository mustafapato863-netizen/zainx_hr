using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Workforce.SharedKernel.Security;

public interface IPiiEncryptionService
{
    int CurrentKeyVersion { get; }
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string ComputeSearchHash(string plaintext);
    string NormalizeForSearch(string raw);
    string MaskNationalId(string raw);
    string MaskDateOfBirth(DateOnly dob);
}

public class AesPiiEncryptionService : IPiiEncryptionService
{
    private readonly int _currentKeyVersion;
    private readonly IReadOnlyDictionary<int, byte[]> _encryptionKeys;
    private readonly byte[] _hmacKey;

    public int CurrentKeyVersion => _currentKeyVersion;

    public AesPiiEncryptionService(
        string? masterKeyBase64 = null,
        string? hmacKeyBase64 = null,
        int currentKeyVersion = 1,
        IDictionary<int, string>? historicalKeysBase64 = null)
    {
        _currentKeyVersion = currentKeyVersion > 0 ? currentKeyVersion : 1;

        var keysDict = new Dictionary<int, byte[]>();

        // Load or derive current AES-256 Master Key (256-bit)
        if (!string.IsNullOrWhiteSpace(masterKeyBase64))
        {
            keysDict[_currentKeyVersion] = Convert.FromBase64String(masterKeyBase64);
        }
        else
        {
            // Provider-neutral development fallback derived via HKDF context separation
            var devSeed = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("ZAINX_PII_ENCRYPTION_SEED") ?? "ZainX_Development_PII_Master_Seed_v1");
            keysDict[_currentKeyVersion] = HKDF.DeriveKey(HashAlgorithmName.SHA256, devSeed, 32, Encoding.UTF8.GetBytes($"zainx-pii-aes-v{_currentKeyVersion}"));
        }

        // Load historical key versions for seamless rotation
        if (historicalKeysBase64 != null)
        {
            foreach (var (ver, b64) in historicalKeysBase64)
            {
                if (!string.IsNullOrWhiteSpace(b64))
                {
                    keysDict[ver] = Convert.FromBase64String(b64);
                }
            }
        }

        _encryptionKeys = keysDict;

        // Load or derive distinct HMAC-SHA256 Blind Index Key (256-bit) - MUST NOT be the same as AES key
        if (!string.IsNullOrWhiteSpace(hmacKeyBase64))
        {
            _hmacKey = Convert.FromBase64String(hmacKeyBase64);
        }
        else
        {
            var devHmacSeed = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("ZAINX_PII_HMAC_SEED") ?? "ZainX_Development_PII_Hmac_Seed_v1");
            _hmacKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, devHmacSeed, 32, Encoding.UTF8.GetBytes("zainx-pii-blind-index-hmac-v1"));
        }
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return string.Empty;

        // 1. Fresh unique 96-bit (12 bytes) nonce per operation - never reused
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16]; // 128-bit authentication tag

        if (!_encryptionKeys.TryGetValue(_currentKeyVersion, out var keyBytes))
        {
            throw new InvalidOperationException($"Encryption key for version {_currentKeyVersion} is not configured.");
        }

        using (var aesGcm = new AesGcm(keyBytes, 16))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        // 2. Binary payload: [12 bytes nonce][16 bytes auth tag][cipher bytes]
        var payloadBytes = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, payloadBytes, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payloadBytes, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, payloadBytes, nonce.Length + tag.Length, cipherBytes.Length);

        // 3. Envelope: v{version}${base64}
        return $"v{_currentKeyVersion}${Convert.ToBase64String(payloadBytes)}";
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrWhiteSpace(ciphertext)) return string.Empty;

        int keyVersion = _currentKeyVersion;
        string base64Payload = ciphertext;

        // Parse key version from envelope if present (e.g. v1$...)
        if (ciphertext.StartsWith("v", StringComparison.OrdinalIgnoreCase) && ciphertext.Contains('$'))
        {
            var parts = ciphertext.Split('$', 2);
            if (int.TryParse(parts[0].AsSpan(1), out var parsedVer))
            {
                keyVersion = parsedVer;
            }
            base64Payload = parts[1];
        }

        if (!_encryptionKeys.TryGetValue(keyVersion, out var keyBytes))
        {
            throw new InvalidOperationException($"Decryption key for version '{keyVersion}' is not available in keyring.");
        }

        var combinedBytes = Convert.FromBase64String(base64Payload);
        if (combinedBytes.Length < 28) // 12 (nonce) + 16 (tag)
        {
            throw new ArgumentException("Invalid encrypted PII ciphertext length.");
        }

        var nonce = new byte[12];
        var tag = new byte[16];
        var cipherBytes = new byte[combinedBytes.Length - 28];

        Buffer.BlockCopy(combinedBytes, 0, nonce, 0, 12);
        Buffer.BlockCopy(combinedBytes, 12, tag, 0, 16);
        Buffer.BlockCopy(combinedBytes, 28, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = new byte[cipherBytes.Length];
        using (var aesGcm = new AesGcm(keyBytes, 16))
        {
            // Authenticated decrypt: Throws CryptographicException if tag or ciphertext has been tampered
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }

    public string NormalizeForSearch(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        // 1. Unicode NFKC normalization and trim
        var normalized = raw.Normalize(NormalizationForm.FormKC).Trim();

        // 2. Strip non-alphanumeric formatting characters (hyphens, spaces, dots) for stable exact matches
        normalized = Regex.Replace(normalized, @"[\s\-\._]", string.Empty);

        // 3. Uppercase alphanumeric representations (e.g. Passport IDs, Tax IDs)
        return normalized.ToUpperInvariant();
    }

    public string ComputeSearchHash(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext)) return string.Empty;
        var canonical = NormalizeForSearch(plaintext);
        using var hmac = new HMACSHA256(_hmacKey);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public string MaskNationalId(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "**********";
        var clean = raw.Trim();
        if (clean.Length <= 4) return new string('*', clean.Length);
        return string.Concat(clean.AsSpan(0, 3), new string('*', clean.Length - 4), clean.AsSpan(clean.Length - 1, 1));
    }

    public string MaskDateOfBirth(DateOnly dob)
    {
        return $"****-**-{dob.Day:D2}";
    }
}
