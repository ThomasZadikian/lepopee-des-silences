import { gameEngineApi } from '../../../shared/api/gameEngineApi';
import { useEmotionalRegisterCatalog } from '../../emotional-registers/store';

import type { SkillDefinitionView } from '../types/skillTypes';

export const skillsApi = {
  async listActive() {
    const response = await gameEngineApi.get<{ skills: SkillDefinitionView[] }>('/api/v2/skills');
    const registerCatalog = useEmotionalRegisterCatalog();

    if (!Array.isArray(response.skills)) {
      throw new Error('Le contrat Catalog des compétences est invalide.');
    }

    for (const skill of response.skills) {
      if (!registerCatalog.definitionOf(skill.emotionalRegister)) {
        throw new Error(`La compétence '${skill.key}' référence un registre Catalog inconnu.`);
      }
      if (!Array.isArray(skill.compatibleCharacterDefinitionKeys)) {
        throw new Error(`La compétence '${skill.key}' ne fournit pas sa compatibilité Catalog.`);
      }
    }

    return response;
  },
};
