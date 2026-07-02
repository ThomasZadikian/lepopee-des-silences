import { gameEngineApi } from '../../../shared/api/gameEngineApi';

import type { SkillDefinitionView } from '../types/skillTypes';

export const skillsApi = {
  listActive() {
    return gameEngineApi.get<{ skills: SkillDefinitionView[] }>('/api/v2/skills');
  },
};
