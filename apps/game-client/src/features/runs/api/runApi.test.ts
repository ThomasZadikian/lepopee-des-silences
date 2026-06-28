import { describe, expect, it, vi, beforeEach } from 'vitest';
import { runApi } from './runApi';
import { gameEngineApi } from '../../../shared/api/gameEngineApi';

vi.mock('../../../shared/api/gameEngineApi', () => ({
  gameEngineApi: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

describe('runApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('startRun sends POST with playerId', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await runApi.startRun('player-1');
    expect(gameEngineApi.post).toHaveBeenCalledWith('/api/v2/runs', { playerId: 'player-1' });
  });

  it('getRun sends GET request', async () => {
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce({});
    await runApi.getRun('run-1');
    expect(gameEngineApi.get).toHaveBeenCalledWith('/api/v2/runs/run-1');
  });

  it('chooseNode sends POST request', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await runApi.chooseNode('run-1', 'node-1');
    expect(gameEngineApi.post).toHaveBeenCalledWith('/api/v2/runs/run-1/nodes/node-1/choose');
  });

  it('resolveCurrentEvent sends POST request', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await runApi.resolveCurrentEvent('run-1');
    expect(gameEngineApi.post).toHaveBeenCalledWith('/api/v2/runs/run-1/current-event/resolve');
  });

  it('progressRun sends POST request', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await runApi.progressRun('run-1');
    expect(gameEngineApi.post).toHaveBeenCalledWith('/api/v2/runs/run-1/progress');
  });

  it('generateNextNodes sends POST request', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await runApi.generateNextNodes('run-1');
    expect(gameEngineApi.post).toHaveBeenCalledWith('/api/v2/runs/run-1/nodes/next');
  });

  it('enterInterlude sends POST request', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await runApi.enterInterlude('run-1');
    expect(gameEngineApi.post).toHaveBeenCalledWith('/api/v2/runs/run-1/interlude/enter');
  });

  it('getInterlude sends GET request', async () => {
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce({});
    await runApi.getInterlude('run-1');
    expect(gameEngineApi.get).toHaveBeenCalledWith('/api/v2/runs/run-1/interlude');
  });

  it('enterNextRoom sends POST request', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await runApi.enterNextRoom('run-1');
    expect(gameEngineApi.post).toHaveBeenCalledWith('/api/v2/runs/run-1/rooms/next');
  });

  it('saveAndExitRun sends POST request', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await runApi.saveAndExitRun('run-1');
    expect(gameEngineApi.post).toHaveBeenCalledWith('/api/v2/runs/run-1/save-and-exit');
  });

  it('resumeRun sends POST request', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await runApi.resumeRun('run-1');
    expect(gameEngineApi.post).toHaveBeenCalledWith('/api/v2/runs/run-1/resume');
  });

  it('exitMidRoom sends POST request', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await runApi.exitMidRoom('run-1');
    expect(gameEngineApi.post).toHaveBeenCalledWith('/api/v2/runs/run-1/exit-mid-room');
  });

  it('abandonRun sends POST request', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await runApi.abandonRun('run-1');
    expect(gameEngineApi.post).toHaveBeenCalledWith('/api/v2/runs/run-1/abandon');
  });

  it('returns the API response', async () => {
    const mockResponse = { id: 'run-1', status: 'Active' };
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce(mockResponse);

    const result = await runApi.getRun('run-1');
    expect(result).toEqual(mockResponse);
  });
});
