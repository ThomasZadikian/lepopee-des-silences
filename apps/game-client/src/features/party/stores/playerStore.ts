import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

import { getActivePlayerId } from '../../runs/stores/runStore';
import { playerApi } from '../api/playerApi';
import type { EquipmentChangePlanView, EquipmentPosition, PlayerProfileView } from '../types/playerTypes';

export const usePlayerStore = defineStore('player', () => {
  const profile = ref<PlayerProfileView | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  const mainCharacter = computed(() => profile.value?.characters[0] ?? null);
  const permanentItems = computed(() => profile.value?.permanentItems ?? []);

  async function execute(action: () => Promise<void>) {
    isLoading.value = true;
    error.value = null;
    try {
      await action();
    } catch (caught) {
      error.value = caught instanceof Error
        ? caught.message
        : 'Une erreur inconnue est survenue.';
    } finally {
      isLoading.value = false;
    }
  }

  async function loadProfile(playerId: string = getActivePlayerId()) {
    await execute(async () => {
      profile.value = await playerApi.getProfile(playerId);
    });
  }

  async function equipSkill(characterId: string, skillKey: string) {
    await execute(async () => {
      profile.value = await playerApi.equipSkill(getActivePlayerId(), characterId, skillKey);
    });
  }

  async function unequipSkill(characterId: string, skillKey: string) {
    await execute(async () => {
      profile.value = await playerApi.unequipSkill(getActivePlayerId(), characterId, skillKey);
    });
  }

  async function equipItem(characterId: string, itemKey: string) {
    await execute(async () => {
      profile.value = await playerApi.equipItem(getActivePlayerId(), characterId, itemKey);
    });
  }

  async function unequipItem(characterId: string, itemKey: string) {
    await execute(async () => {
      profile.value = await playerApi.unequipItem(getActivePlayerId(), characterId, itemKey);
    });
  }

  async function previewEquipmentChange(
    characterId: string, itemInstanceId: string, position: EquipmentPosition,
    resources?: { currentVitality?: number; currentMana?: number },
  ): Promise<EquipmentChangePlanView | null> {
    let plan: EquipmentChangePlanView | null = null;
    await execute(async () => {
      plan = await playerApi.previewEquipmentChange(
        getActivePlayerId(), characterId, itemInstanceId, position, resources,
      );
    });
    return plan;
  }

  async function equipItemInstance(
    characterId: string, itemInstanceId: string, position: EquipmentPosition,
    resources?: { currentVitality?: number; currentMana?: number },
  ) {
    await execute(async () => {
      profile.value = await playerApi.equipItemInstance(
        getActivePlayerId(), characterId, itemInstanceId, position, resources,
      );
    });
  }

  async function unequipItemInstance(characterId: string, itemInstanceId: string) {
    await execute(async () => {
      profile.value = await playerApi.unequipItemInstance(getActivePlayerId(), characterId, itemInstanceId);
    });
  }

  return {
    profile,
    isLoading,
    error,
    mainCharacter,
    permanentItems,
    loadProfile,
    equipSkill,
    unequipSkill,
    equipItem,
    unequipItem,
    previewEquipmentChange,
    equipItemInstance,
    unequipItemInstance,
  };
});
