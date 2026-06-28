import { describe, expect, it, vi, beforeEach } from 'vitest';
import { HttpError, httpRequest } from './httpClient';
import { environment } from '../config/environment';

vi.mock('../config/environment', () => ({
  environment: {
    gameEngineApiUrl: 'http://localhost:5187',
  },
}));

describe('HttpError', () => {
  it('creates error with status and body', () => {
    const error = new HttpError('Not found', 404, { detail: 'Resource missing' });
    expect(error.message).toBe('Not found');
    expect(error.status).toBe(404);
    expect(error.body).toEqual({ detail: 'Resource missing' });
    expect(error.name).toBe('HttpError');
  });
});

describe('httpRequest', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('sends request to correct URL', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      headers: { get: () => 'application/json' },
      json: () => Promise.resolve({ data: 'test' }),
    });

    await httpRequest('/api/test');

    expect(fetch).toHaveBeenCalledWith('http://localhost:5187/api/test', expect.any(Object));
  });

  it('sends with correct headers', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      headers: { get: () => 'application/json' },
      json: () => Promise.resolve({}),
    });

    await httpRequest('/api/test');

    expect(fetch).toHaveBeenCalledWith(expect.any(String), expect.objectContaining({
      headers: expect.objectContaining({
        Accept: 'application/json',
        'Content-Type': 'application/json',
      }),
    }));
  });

  it('sends POST with body', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      headers: { get: () => 'application/json' },
      json: () => Promise.resolve({}),
    });

    await httpRequest('/api/test', {
      method: 'POST',
      body: JSON.stringify({ key: 'value' }),
    });

    expect(fetch).toHaveBeenCalledWith(expect.any(String), expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ key: 'value' }),
    }));
  });

  it('returns parsed JSON response', async () => {
    const mockData = { id: 1, name: 'test' };
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      headers: { get: () => 'application/json' },
      json: () => Promise.resolve(mockData),
    });

    const result = await httpRequest('/api/test');
    expect(result).toEqual(mockData);
  });

  it('throws HttpError on non-ok response with message field', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 400,
      headers: { get: () => 'application/json' },
      json: () => Promise.resolve({ message: 'Bad request' }),
    });

    await expect(httpRequest('/api/test')).rejects.toThrow('Bad request');
    await expect(httpRequest('/api/test')).rejects.toBeInstanceOf(HttpError);
  });

  it('throws HttpError with title field fallback', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 400,
      headers: { get: () => 'application/json' },
      json: () => Promise.resolve({ title: 'Error title' }),
    });

    await expect(httpRequest('/api/test')).rejects.toThrow('Error title');
  });

  it('throws HttpError with detail field fallback', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 400,
      headers: { get: () => 'application/json' },
      json: () => Promise.resolve({ detail: 'Error detail' }),
    });

    await expect(httpRequest('/api/test')).rejects.toThrow('Error detail');
  });

  it('throws HttpError with string body fallback', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 500,
      headers: { get: () => 'text/plain' },
      text: () => Promise.resolve('Server error'),
    });

    await expect(httpRequest('/api/test')).rejects.toThrow('Server error');
  });

  it('throws HttpError with generic fallback message', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 500,
      headers: { get: () => 'application/json' },
      json: () => Promise.resolve({}),
    });

    await expect(httpRequest('/api/test')).rejects.toThrow('Game Engine API request failed with status 500');
  });

  it('handles 204 No Content response', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 204,
      headers: { get: () => null },
    });

    const result = await httpRequest('/api/test');
    expect(result).toBeNull();
  });

  it('merges custom headers', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      headers: { get: () => 'application/json' },
      json: () => Promise.resolve({}),
    });

    await httpRequest('/api/test', {
      headers: { 'X-Custom': 'value' },
    });

    expect(fetch).toHaveBeenCalledWith(expect.any(String), expect.objectContaining({
      headers: expect.objectContaining({
        'X-Custom': 'value',
      }),
    }));
  });
});
