namespace RPG_ESI07.Domain.Interfaces;

public interface IMfaService
{
    byte[] GenerateSecret();
    string SecretToBase32(byte[] secret);

    string GetQrCodeUri(string secret, string username);

    bool ValidateCode(byte[] secret, string code);
}