using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Workforce.SharedKernel.Security;

public interface IPiiEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string ComputeSearchHash(string plaintext);
    string MaskNationalId(string raw);
    string MaskDateOfBirth(DateOnly dob);
}

public class AesPiiEncryptionService : IPiiEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _hmacKey;

    public AesPiiEncryptionService(string? masterKeyBase64 = null, string? hmacKeyBase64 = null)
    {
        // 256-bit AES Master Key
        if (!string.IsNullOrWhiteSpace(masterKeyBase64))
        {
            _key = Convert.FromBase64String(masterKeyBase64);
        }
        else
        {
            // Standard deterministic sandbox key (in production: AWS KMS / Azure Key Vault / HashiCorp Vault)
            _key = SHA256.HashData(Encoding.UTF8.GetBytes("ZainX_Workforce_Enterprise_PII_Encryption_MasterKey_v1"));
        }

        // HMAC key for blind indexing
        if (!string.IsNullOrWhiteSpace(hmacKeyBase64))
        {
            _hmacKey = Convert.FromBase64String(hmacKeyBase64);
        }
        else
        {
            _hmacKey = SHA256.HashData(Encoding.UTF8.GetBytes("ZainX_Workforce_BlindIndex_Hmac_SaltKey_v1"));
        }
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return string.Empty;

        var nonce = new byte[12]; // 96-bit nonce for AES-GCM
        RandomNumberGenerator.Fill(nonce);

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16]; // 128-bit authentication tag

        using var aesGcm = new AesGcm(_key, 16);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Package: [12 bytes nonce][16 bytes tag][cipherBytes]
        var resultBytes = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, resultBytes, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, resultBytes, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, resultBytes, nonce.Length + tag.Length, cipherBytes.Length);

        return Convert.ToBase64String(resultBytes);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrWhiteSpace(ciphertext)) return string.Empty;

        var combinedBytes = Convert.FromBase64String(ciphertext);
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
        using var aesGcm = new AesGcm(_key, 16);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public string ComputeSearchHash(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext)) return string.Empty;
        var normalized = plaintext.Trim();
        using var hmac = new HMACSHA256(_hmacKey);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public string MaskNationalId(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "**********";
        if (raw.Length <= 4) return new string('*', raw.Length);
        return string.Concat(raw.AsSpan(0, 3), new string('*', raw.Length - 4), raw.AsSpan(raw.Length - 1, 1));
    }

    public string MaskDateOfBirth(DateOnly dob)
    {
        return $"****-**-{dob.Day:D2}";
    }
}
