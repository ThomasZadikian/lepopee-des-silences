import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';

describe('environment', () => {
  const originalEnv = import.meta.env;

  beforeEach(() => {
    vi.resetModules();
  });

  afterEach(() => {
    import.meta.env = originalEnv;
  });

  it('uses fallback URL when VITE_GAME_ENGINE_API_URL is not defined', async () => {
    import.meta.env = { ...originalEnv, VITE_GAME_ENGINE_API_URL: undefined };
    const { environment } = await import('./environment');
    expect(environment.gameEngineApiUrl).toBe('http://localhost:5187');
  });

  it('uses VITE_GAME_ENGINE_API_URL when defined', async () => {
    import.meta.env = { ...originalEnv, VITE_GAME_ENGINE_API_URL: 'http://custom-api:9999' };
    const { environment } = await import('./environment');
    expect(environment.gameEngineApiUrl).toBe('http://custom-api:9999');
  });

  it('exports gameEngineApiUrl property', async () => {
    const { environment } = await import('./environment');
    expect(environment).toHaveProperty('gameEngineApiUrl');
  });

  it('is a const object', async () => {
    const { environment } = await import('./environment');
    expect(Object.isFrozen(environment) || !Object.isExtensible(environment)).toBe(false);
    // The 'as const' makes it readonly, but not frozen
    expect(typeof environment.gameEngineApiUrl).toBe('string');
  });
});
