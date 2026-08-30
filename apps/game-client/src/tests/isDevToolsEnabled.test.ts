import { afterEach, describe, expect, it, vi } from 'vitest';

describe('isDevToolsEnabled', () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.resetModules();
  });

  it('requires dev mode and the explicit client flag', async () => {
    vi.stubEnv('DEV', true);
    vi.stubEnv('VITE_GAME_CLIENT_DEVTOOLS_ENABLED', 'true');

    const { isDevToolsEnabled } = await import('../features/devtools/utils/isDevToolsEnabled');

    expect(isDevToolsEnabled()).toBe(true);
  });

  it('stays disabled when the explicit client flag is absent', async () => {
    vi.stubEnv('DEV', true);
    vi.stubEnv('VITE_GAME_CLIENT_DEVTOOLS_ENABLED', 'false');

    const { isDevToolsEnabled } = await import('../features/devtools/utils/isDevToolsEnabled');

    expect(isDevToolsEnabled()).toBe(false);
  });

  it('stays disabled outside dev mode', async () => {
    vi.stubEnv('DEV', false);
    vi.stubEnv('VITE_GAME_CLIENT_DEVTOOLS_ENABLED', 'true');

    const { isDevToolsEnabled } = await import('../features/devtools/utils/isDevToolsEnabled');

    expect(isDevToolsEnabled()).toBe(false);
  });
});
