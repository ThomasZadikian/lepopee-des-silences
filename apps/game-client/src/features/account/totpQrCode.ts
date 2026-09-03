import QRCode from 'qrcode';

const QR_CODE_DATA_URL_PREFIX = 'data:image/svg+xml;charset=utf-8,';

export async function createTotpQrCodeDataUrl(otpAuthUri: string): Promise<string> {
  const uri = new URL(otpAuthUri);
  if (uri.protocol !== 'otpauth:' || uri.hostname !== 'totp') {
    throw new Error('Invalid TOTP enrollment URI.');
  }

  const svg = await QRCode.toString(otpAuthUri, {
    type: 'svg',
    errorCorrectionLevel: 'M',
    margin: 4,
    width: 256,
    color: {
      dark: '#10161d',
      light: '#ffffff',
    },
  });

  return `${QR_CODE_DATA_URL_PREFIX}${encodeURIComponent(svg)}`;
}
