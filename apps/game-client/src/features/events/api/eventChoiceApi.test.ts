import { describe, expect, it, vi, beforeEach } from 'vitest';
import { eventChoiceApi } from './eventChoiceApi';
import { gameEngineApi } from '../../../shared/api/gameEngineApi';

vi.mock('../../../shared/api/gameEngineApi', () => ({
  gameEngineApi: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

describe('eventChoiceApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('sends POST request to correct endpoint', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await eventChoiceApi.chooseCurrentEventOption('run-1', { choiceId: 'c1' });

    expect(gameEngineApi.post).toHaveBeenCalledWith(
      '/api/v2/runs/run-1/current-event/choice',
      { choiceId: 'c1' },
    );
  });

  it('includes all body fields in request', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    const body = { choiceId: 'c1', optionId: 'o1', eventChoiceId: 'ec1' };
    await eventChoiceApi.chooseCurrentEventOption('run-42', body);

    expect(gameEngineApi.post).toHaveBeenCalledWith(
      '/api/v2/runs/run-42/current-event/choice',
      body,
    );
  });

  it('returns the API response', async () => {
    const mockResponse = { run: { id: 'run-1' }, choiceResult: { id: 'cr-1' } };
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce(mockResponse);

    const result = await eventChoiceApi.chooseCurrentEventOption('run-1', { choiceId: 'c1' });
    expect(result).toEqual(mockResponse);
  });

  it('propagates errors from gameEngineApi', async () => {
    const error = new Error('Network error');
    vi.mocked(gameEngineApi.post).mockRejectedValueOnce(error);

    await expect(
      eventChoiceApi.chooseCurrentEventOption('run-1', { choiceId: 'c1' }),
    ).rejects.toThrow('Network error');
  });
});
