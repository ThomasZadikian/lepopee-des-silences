using OtpNet;
using RPG_ESI07.Domain.Interfaces;
using System.Text;

namespace RPG_ESI07.Infrastructure.Services;

public class TotpMfaService : IMfaService
{
    public byte[] GenerateSecret()
    {
        return KeyGeneration.GenerateRandomKey(20);
    }
    public string SecretToBase32(byte[] secret)
    {
        return Base32Encoding.ToString(secret);
    }

    public string GetQrCodeUri(string secret, string username)
    {
        return $"otpauth://totp/RPG_ESI07:{username}?secret={secret}&issuer=RPG_ESI07";
    }

    public bool ValidateCode(
    byte[] secret, string code)
    {
        var totp = new Totp(secret);
        return totp.VerifyTotp(code,
        out _, new VerificationWindow(1, 1));
    }
}