import { gameEngineApi } from '../../../shared/api/gameEngineApi';
import type { GetUpcomingRoomsResponse } from '../types/upcomingRoomsTypes';

export const upcomingRoomsApi = {
  getUpcomingRooms(runId: string) {
    return gameEngineApi.get<GetUpcomingRoomsResponse>(
      `/api/v2/runs/${runId}/upcoming-rooms`,
    );
  },
};
