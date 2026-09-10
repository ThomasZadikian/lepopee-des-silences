import { describe, expect, it, vi, beforeEach } from 'vitest';
import { gameEngineApi } from './gameEngineApi';
import { httpRequest } from './httpClient';

vi.mock('./httpClient', () => ({
  httpRequest: vi.fn(),
}));

describe('gameEngineApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('get calls httpRequest with GET method', async () => {
    vi.mocked(httpRequest).mockResolvedValueOnce({});
    await gameEngineApi.get('/api/test');
    expect(httpRequest).toHaveBeenCalledWith('/api/test', { method: 'GET' });
  });

  it('post calls httpRequest with POST method and body', async () => {
    vi.mocked(httpRequest).mockResolvedValueOnce({});
    await gameEngineApi.post('/api/test', { key: 'value' });
    expect(httpRequest).toHaveBeenCalledWith('/api/test', {
      method: 'POST',
      body: JSON.stringify({ key: 'value' }),
    });
  });

  it('post calls httpRequest without body when undefined', async () => {
    vi.mocked(httpRequest).mockResolvedValueOnce({});
    await gameEngineApi.post('/api/test');
    expect(httpRequest).toHaveBeenCalledWith('/api/test', {
      method: 'POST',
      body: undefined,
    });
  });

  it('postWithHeaders forwards authentication headers with the serialized body', async () => {
    vi.mocked(httpRequest).mockResolvedValueOnce({});

    await gameEngineApi.postWithHeaders(
      '/api/dev/test',
      { characterId: 'character-2' },
      { Authorization: 'Bearer access-token' },
    );

    expect(httpRequest).toHaveBeenCalledWith('/api/dev/test', {
      method: 'POST',
      body: JSON.stringify({ characterId: 'character-2' }),
      headers: { Authorization: 'Bearer access-token' },
    });
  });

  it('returns the response from httpRequest for get', async () => {
    const mockResponse = { data: 'test' };
    vi.mocked(httpRequest).mockResolvedValueOnce(mockResponse);

    const result = await gameEngineApi.get('/api/test');
    expect(result).toEqual(mockResponse);
  });

  it('returns the response from httpRequest for post', async () => {
    const mockResponse = { created: true };
    vi.mocked(httpRequest).mockResolvedValueOnce(mockResponse);

    const result = await gameEngineApi.post('/api/test', {});
    expect(result).toEqual(mockResponse);
  });

  it('propagates errors from httpRequest', async () => {
    const error = new Error('Request failed');
    vi.mocked(httpRequest).mockRejectedValueOnce(error);

    await expect(gameEngineApi.get('/api/test')).rejects.toThrow('Request failed');
  });
});
