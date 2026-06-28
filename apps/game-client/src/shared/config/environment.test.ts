import { describe, expect, it, vi, beforeEach } from 'vitest';

describe('environment', () => {
  beforeEach(() => {
    vi.resetModules();
    vi.unstubAllEnvs();
  });

  it('uses fallback URL when VITE_GAME_ENGINE_API_URL is not defined', async () => {
    vi.stubEnv('VITE_GAME_ENGINE_API_URL', undefined);
    const { environment } = await import('./environment');
    expect(environment.gameEngineApiUrl).toBe('http://localhost:5187');
  });

  it('uses VITE_GAME_ENGINE_API_URL when defined', async () => {
    vi.stubEnv('VITE_GAME_ENGINE_API_URL', 'http://custom-api:9999');
    const { environment } = await import('./environment');
    expect(environment.gameEngineApiUrl).toBe('http://custom-api:9999');
  });

  it('exports gameEngineApiUrl property', async () => {
    const { environment } = await import('./environment');
    expect(environment).toHaveProperty('gameEngineApiUrl');
  });

  it('is a const object', async () => {
    const { environment } = await import('./environment');
    expect(typeof environment.gameEngineApiUrl).toBe('string');
  });
});
