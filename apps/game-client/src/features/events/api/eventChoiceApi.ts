import { gameEngineApi } from '../../../shared/api/gameEngineApi';

import type {
  ChooseCurrentEventOptionRequest,
  ChooseCurrentEventOptionResponse,
} from '../types/eventTypes';

export const eventChoiceApi = {
  chooseCurrentEventOption(
    runId: string,
    body: ChooseCurrentEventOptionRequest,
  ) {
    return gameEngineApi.post<
      ChooseCurrentEventOptionResponse,
      ChooseCurrentEventOptionRequest
    >(
      `/api/v2/runs/${runId}/current-event/choice`,
      body,
    );
  },
};