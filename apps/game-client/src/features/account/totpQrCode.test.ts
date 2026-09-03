import { describe, expect, it } from 'vitest';

import { createTotpQrCodeDataUrl } from './totpQrCode';

describe('createTotpQrCodeDataUrl', () => {
  it('creates an embeddable QR code from a TOTP enrollment URI', async () => {
    const dataUrl = await createTotpQrCodeDataUrl(
      'otpauth://totp/Palais:player@example.fr?secret=JBSWY3DPEHPK3PXP&issuer=Palais',
    );

    expect(dataUrl).toMatch(/^data:image\/svg\+xml;charset=utf-8,/);
    expect(decodeURIComponent(dataUrl)).toContain('<svg');
  });

  it('rejects non-TOTP URIs', async () => {
    await expect(createTotpQrCodeDataUrl('https://example.fr/secret')).rejects.toThrow(
      'Invalid TOTP enrollment URI.',
    );
  });
});
