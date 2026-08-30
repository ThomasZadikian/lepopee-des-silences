import { describe, expect, it, vi, beforeEach } from 'vitest';
import { upcomingRoomsApi } from './upcomingRoomsApi';
import { gameEngineApi } from '../../../shared/api/gameEngineApi';

vi.mock('../../../shared/api/gameEngineApi', () => ({
  gameEngineApi: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

describe('upcomingRoomsApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('getUpcomingRooms sends GET request to the run upcoming-rooms route', async () => {
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce({ runId: 'run-1', isRevealed: false, rooms: [] });
    await upcomingRoomsApi.getUpcomingRooms('run-1');
    expect(gameEngineApi.get).toHaveBeenCalledWith('/api/v2/runs/run-1/upcoming-rooms');
  });

  it('returns the API response for getUpcomingRooms', async () => {
    const mockResponse = {
      runId: 'run-1',
      isRevealed: true,
      rooms: [{ roomIndex: 1, key: 'room.a', displayName: 'Salle A' }],
    };
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce(mockResponse);

    const result = await upcomingRoomsApi.getUpcomingRooms('run-1');
    expect(result).toEqual(mockResponse);
  });

  it('propagates errors from gameEngineApi', async () => {
    const error = new Error('Network error');
    vi.mocked(gameEngineApi.get).mockRejectedValueOnce(error);

    await expect(upcomingRoomsApi.getUpcomingRooms('run-1')).rejects.toThrow('Network error');
  });
});
