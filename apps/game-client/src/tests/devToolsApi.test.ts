import { beforeEach, describe, expect, it, vi } from 'vitest';

import { HttpError, httpRequest } from '../shared/api/httpClient';
import { devToolsApi, devToolsUnavailableMessage } from '../features/devtools/api/devToolsApi';

vi.mock('../shared/api/httpClient', async () => {
  class MockHttpError extends Error {
    public readonly status: number;
    public readonly body: unknown;

    public constructor(message: string, status: number, body: unknown) {
      super(message);
      this.name = 'HttpError';
      this.status = status;
      this.body = body;
    }
  }

  return {
    HttpError: MockHttpError,
    httpRequest: vi.fn(),
  };
});

describe('devToolsApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(httpRequest).mockResolvedValue({});
  });

  it('sends the devtools token header', async () => {
    await devToolsApi.getStatus('local-token');

    expect(httpRequest).toHaveBeenCalledWith('/api/dev/v2/status', {
      method: 'GET',
      headers: {
        'X-Leds-DevTools-Token': 'local-token',
      },
    });
  });

  it('posts action payloads to backend devtools endpoints', async () => {
    await devToolsApi.advanceRooms('local-token', 'run-1', 4);

    expect(httpRequest).toHaveBeenCalledWith(
      '/api/dev/v2/runs/run-1/advance-rooms',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ count: 4 }),
        headers: expect.objectContaining({
          'X-Leds-DevTools-Token': 'local-token',
        }),
      }),
    );
  });

  it('maps protected or unavailable endpoints to a generic devtools error', async () => {
    vi.mocked(httpRequest).mockRejectedValueOnce(new HttpError('Forbidden', 403, null));

    await expect(devToolsApi.getStatus('bad-token')).rejects.toThrow(devToolsUnavailableMessage);
  });
});
