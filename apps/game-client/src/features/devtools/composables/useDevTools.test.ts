// @vitest-environment jsdom
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { useDevTools } from './useDevTools';
import { devToolsApi } from '../api/devToolsApi';

vi.mock('../api/devToolsApi', () => ({
  devToolsApi: {
    getStatus: vi.fn(),
  },
}));

describe('useDevTools', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Clear localStorage between tests
    try { window.localStorage.clear(); } catch {}
  });

  it('returns initial state with no token', () => {
    const devTools = useDevTools();
    expect(devTools.hasToken.value).toBe(false);
    expect(devTools.status.value).toBe('unknown');
    expect(devTools.token.value).toBe('');
  });

  it('saves token to memory and localStorage', () => {
    const devTools = useDevTools();
    devTools.saveToken('test-token');
    expect(devTools.token.value).toBe('test-token');
    expect(devTools.hasToken.value).toBe(true);
    expect(window.localStorage.getItem('leds:game-client:devtools-token')).toBe('test-token');
  });

  it('clears token from memory and localStorage', () => {
    const devTools = useDevTools();
    devTools.saveToken('test-token');
    devTools.clearToken();
    expect(devTools.token.value).toBe('');
    expect(devTools.hasToken.value).toBe(false);
    expect(window.localStorage.getItem('leds:game-client:devtools-token')).toBeNull();
  });

  it('trims whitespace when saving token', () => {
    const devTools = useDevTools();
    devTools.saveToken('  test-token  ');
    expect(devTools.token.value).toBe('test-token');
  });

  it('reads stored token on initialization', () => {
    window.localStorage.setItem('leds:game-client:devtools-token', 'existing-token');
    const devTools = useDevTools();
    expect(devTools.token.value).toBe('existing-token');
    expect(devTools.hasToken.value).toBe(true);
  });

  it('checkStatus sets error when no token', async () => {
    const devTools = useDevTools();
    await devTools.checkStatus();
    expect(devTools.status.value).toBe('unknown');
    expect(devTools.error.value).toBe('Token devtools absent.');
  });

  it('checkStatus calls API and sets status on success', async () => {
    vi.mocked(devToolsApi.getStatus).mockResolvedValueOnce({
      enabled: true,
      environment: 'local',
    });

    const devTools = useDevTools();
    devTools.saveToken('test-token');
    await devTools.checkStatus();

    expect(devTools.status.value).toBe('available');
    expect(devTools.statusEnvironment.value).toBe('local');
    expect(devTools.message.value).toBe('Devtools backend disponibles.');
  });

  it('checkStatus sets unavailable when backend disabled', async () => {
    vi.mocked(devToolsApi.getStatus).mockResolvedValueOnce({
      enabled: false,
      environment: 'local',
    });

    const devTools = useDevTools();
    devTools.saveToken('test-token');
    await devTools.checkStatus();

    expect(devTools.status.value).toBe('unavailable');
  });

  it('checkStatus sets error on API failure', async () => {
    vi.mocked(devToolsApi.getStatus).mockRejectedValueOnce(new Error('Connection refused'));

    const devTools = useDevTools();
    devTools.saveToken('test-token');
    await devTools.checkStatus();

    expect(devTools.status.value).toBe('unavailable');
    expect(devTools.error.value).toBe('Connection refused');
  });

  it('runAction returns false when no token', async () => {
    const devTools = useDevTools();
    const result = await devTools.runAction(async () => {}, 'success');
    expect(result).toBe(false);
    expect(devTools.error.value).toBe('Token devtools absent.');
  });

  it('runAction calls action and returns true on success', async () => {
    const action = vi.fn().mockResolvedValue(undefined);
    const devTools = useDevTools();
    devTools.saveToken('test-token');

    const result = await devTools.runAction(action, 'Action succeeded');

    expect(result).toBe(true);
    expect(action).toHaveBeenCalledWith('test-token');
    expect(devTools.message.value).toBe('Action succeeded');
    expect(devTools.error.value).toBeNull();
  });

  it('runAction sets error on action failure', async () => {
    const action = vi.fn().mockRejectedValue(new Error('Action failed'));
    const devTools = useDevTools();
    devTools.saveToken('test-token');

    const result = await devTools.runAction(action, 'success');

    expect(result).toBe(false);
    expect(devTools.error.value).toBe('Action failed');
  });

  it('runAction sets isLoading during execution', async () => {
    let isLoadingDuringAction = false;
    const action = vi.fn().mockImplementation(async () => {
      isLoadingDuringAction = true;
    });
    const devTools = useDevTools();
    devTools.saveToken('test-token');

    const promise = devTools.runAction(action, 'success');
    expect(devTools.isLoading.value).toBe(true);

    await promise;
    expect(devTools.isLoading.value).toBe(false);
  });

  it('checkStatus sets isLoading during execution', async () => {
    const devTools = useDevTools();
    devTools.saveToken('test-token');

    const promise = devTools.checkStatus();
    expect(devTools.isLoading.value).toBe(true);

    await promise;
    expect(devTools.isLoading.value).toBe(false);
  });

  it('runAction clears previous message and error', async () => {
    const action = vi.fn().mockResolvedValue(undefined);
    const devTools = useDevTools();
    devTools.saveToken('test-token');
    devTools.message.value = 'old message';
    devTools.error.value = 'old error';

    await devTools.runAction(action, 'new success');

    expect(devTools.message.value).toBe('new success');
    expect(devTools.error.value).toBeNull();
  });
});
