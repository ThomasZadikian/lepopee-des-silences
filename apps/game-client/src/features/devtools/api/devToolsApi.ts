import { HttpError, httpRequest } from '../../../shared/api/httpClient';
import type {
  DevToolsCombatResponse,
  DevToolsRunPsycheResponse,
  DevToolsRunResponse,
  DevToolsStatusResponse,
  PalaceRoomStateKey,
  RoomClimateKey,
} from '../types/devToolsTypes';

const tokenHeaderName = 'X-Leds-DevTools-Token';
const unavailableMessage = 'Devtools indisponibles ou endpoint non implemente.';

function tokenHeaders(token: string): HeadersInit {
  return { [tokenHeaderName]: token };
}

async function request<TResponse>(
  token: string,
  path: string,
  options: RequestInit = {},
): Promise<TResponse> {
  try {
    return await httpRequest<TResponse>(path, {
      ...options,
      headers: {
        ...tokenHeaders(token),
        ...options.headers,
      },
    });
  } catch (error) {
    if (error instanceof HttpError && [403, 404, 501].includes(error.status)) {
      throw new Error(unavailableMessage);
    }

    throw error;
  }
}

function post<TResponse, TBody = unknown>(
  token: string,
  path: string,
  body?: TBody,
): Promise<TResponse> {
  return request<TResponse>(token, path, {
    method: 'POST',
    body: body === undefined ? undefined : JSON.stringify(body),
  });
}

export const devToolsApi = {
  getStatus(token: string) {
    return request<DevToolsStatusResponse>(token, '/api/dev/v2/status', { method: 'GET' });
  },

  advanceRoom(token: string, runId: string) {
    return post<DevToolsRunResponse>(token, `/api/dev/v2/runs/${runId}/advance-room`);
  },

  advanceRooms(token: string, runId: string, count: number) {
    return post<DevToolsRunResponse>(token, `/api/dev/v2/runs/${runId}/advance-rooms`, { count });
  },

  forcePalaceRoomState(token: string, runId: string, state: PalaceRoomStateKey) {
    return post<DevToolsRunResponse>(
      token,
      `/api/dev/v2/runs/${runId}/current-room/palace-state`,
      { state },
    );
  },

  forceRoomClimate(token: string, runId: string, climate: RoomClimateKey) {
    return post<DevToolsRunResponse>(
      token,
      `/api/dev/v2/runs/${runId}/current-room/climate`,
      { climate },
    );
  },

  activateLaw(token: string, runId: string, lawKey: string) {
    return post<DevToolsRunResponse>(token, `/api/dev/v2/runs/${runId}/laws/activate`, { lawKey });
  },

  clearLaws(token: string, runId: string) {
    return post<DevToolsRunResponse>(token, `/api/dev/v2/runs/${runId}/laws/clear`);
  },

  activateCurse(token: string, runId: string, curseKey: string) {
    return post<DevToolsRunResponse>(token, `/api/dev/v2/runs/${runId}/curses/activate`, { curseKey });
  },

  clearCurses(token: string, runId: string) {
    return post<DevToolsRunResponse>(token, `/api/dev/v2/runs/${runId}/curses/clear`);
  },

  killEnemies(token: string, runId: string) {
    return post<DevToolsCombatResponse>(token, `/api/dev/v2/runs/${runId}/combats/current/kill-enemies`);
  },

  killEnemy(token: string, runId: string, combatantId: string) {
    return post<DevToolsCombatResponse>(
      token,
      `/api/dev/v2/runs/${runId}/combats/current/enemies/${combatantId}/kill`,
    );
  },

  setVitals(token: string, runId: string, combatantId: string, vitality: number, guard: number) {
    return post<DevToolsCombatResponse>(
      token,
      `/api/dev/v2/runs/${runId}/combats/current/combatants/${combatantId}/set-vitals`,
      { vitality, guard },
    );
  },

    getPsyche(token: string, runId: string) {
    return request<DevToolsRunPsycheResponse>(
      token,
      `/api/dev/v2/runs/${runId}/psyche`,
      { method: 'GET' },
    );
  },

  applyStatus(
    token: string,
    runId: string,
    combatantId: string,
    statusKey: string,
    stacks: number,
    duration: number,
  ) {
    return post<DevToolsCombatResponse>(
      token,
      `/api/dev/v2/runs/${runId}/combats/current/combatants/${combatantId}/status`,
      { statusKey, stacks, duration },
    );
  },
};

export const devToolsUnavailableMessage = unavailableMessage;
