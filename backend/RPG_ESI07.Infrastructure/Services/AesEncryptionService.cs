using Microsoft.Extensions.Configuration;
using RPG_ESI07.Domain.Interfaces;
using System.Security.Cryptography;

namespace RPG_ESI07.Infrastructure.Services;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration.GetValue<string>("Encryption:Key");
        if (string.IsNullOrWhiteSpace(keyBase64))
            throw new InvalidOperationException(
                "Encryption:Key is required. Generate a 256-bit key in base64.");
        _key = Convert.FromBase64String(keyBase64);
    }

    public byte[] Encrypt(byte[] plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        var result = new byte[aes.IV.Length + ciphertext.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(ciphertext, 0, result, aes.IV.Length, ciphertext.Length);
        return result;
    }

    public byte[] Decrypt(byte[] ciphertext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[aes.IV.Length];
        Buffer.BlockCopy(ciphertext, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, iv.Length, ciphertext.Length - iv.Length);
    }
}
