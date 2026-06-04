namespace RPG_ESI07.Domain.Interfaces;

public interface IEncryptionService
{
    byte[] Encrypt(byte[] plaintext);
    byte[] Decrypt(byte[] ciphertext);
}
