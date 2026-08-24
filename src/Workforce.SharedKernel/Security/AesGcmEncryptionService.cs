using System;
using System.Security.Cryptography;
using System.Text;

namespace Workforce.SharedKernel.Security;

public class SecuritySettings
{
    public string? BankEncryptionKeyBase64 { get; set; }
    public int KeyVersion { get; set; } = 1;
}

public interface IBankEncryptionService
{
    int CurrentKeyVersion { get; }
    string Encrypt(string plaintext);
    string Decrypt(string encryptedData);
}

public class AesGcmEncryptionService : IBankEncryptionService
{
    private readonly byte[] _key;
    private readonly int _keyVersion;

    public int CurrentKeyVersion => _keyVersion;

    public AesGcmEncryptionService(string? keyBase64 = null, int keyVersion = 1)
    {
        _keyVersion = keyVersion > 0 ? keyVersion : 1;

        if (!string.IsNullOrWhiteSpace(keyBase64))
        {
            _key = Convert.FromBase64String(keyBase64);
        }
        else
        {
            // Provider-neutral external key fallback derived via HKDF context separation
            var seed = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("ZAINX_BANK_ENCRYPTION_SEED") ?? "ZainX_Development_Bank_Master_Seed_v1");
            _key = HKDF.DeriveKey(HashAlgorithmName.SHA256, seed, 32, Encoding.UTF8.GetBytes($"zainx-bank-aes-v{_keyVersion}"));
        }
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return string.Empty;

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        
        // 96-bit CSPRNG nonce per encryption operation (never reused)
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        // 128-bit authentication tag
        var tag = new byte[16];
        var ciphertext = new byte[plaintextBytes.Length];

        using var aesGcm = new AesGcm(_key, tagSizeInBytes: 16);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Format: v{version}:{nonce(base64)}:{tag(base64)}:{ciphertext(base64)}
        return $"v{_keyVersion}:{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(tag)}:{Convert.ToBase64String(ciphertext)}";
    }

    public string Decrypt(string encryptedData)
    {
        if (string.IsNullOrEmpty(encryptedData)) return string.Empty;

        var parts = encryptedData.Split(':');
        if (parts.Length != 4)
        {
            throw new FormatException("Invalid encrypted payload format.");
        }

        var versionPart = parts[0];
        if (!versionPart.StartsWith('v') || !int.TryParse(versionPart[1..], out var version) || version != _keyVersion)
        {
            throw new FormatException($"Unsupported or mismatched key version '{versionPart}'.");
        }

        var nonce = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var ciphertext = Convert.FromBase64String(parts[3]);

        if (nonce.Length != 12)
        {
            throw new CryptographicException("Invalid nonce length; expected 96-bit nonce.");
        }
        if (tag.Length != 16)
        {
            throw new CryptographicException("Invalid authentication tag length; expected 128-bit tag.");
        }

        var plaintextBytes = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(_key, tagSizeInBytes: 16);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    // Static default instance for backwards compatibility
    private static readonly Lazy<AesGcmEncryptionService> DefaultInstance = new(() => new AesGcmEncryptionService());

    public static string EncryptDefault(string plaintext) => DefaultInstance.Value.Encrypt(plaintext);
    public static string DecryptDefault(string encryptedData) => DefaultInstance.Value.Decrypt(encryptedData);
}
