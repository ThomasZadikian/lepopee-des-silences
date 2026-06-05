import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

import { rewardApi } from '../../rewards/api/rewardApi';
import {
  unwrapRewardOffer,
  type RewardOfferDto,
} from '../../rewards/types/rewardTypes';
import { runApi } from '../api/runApi';
import {
  unwrapRunResponse,
  type CombatInstanceDto,
  type ResolveCurrentEventResponse,
  type RunDto,
} from '../types/runTypes';

const demoPlayerId = '00000000-0000-0000-0000-000000000001';
const pendingRewardOffer = ref<RewardOfferDto | null>(null);

export const useRunStore = defineStore('run', () => {
  const currentRun = ref<RunDto | null>(null);
  const lastOutcome = ref<ResolveCurrentEventResponse['outcome'] | null>(null);
  const activeCombat = ref<CombatInstanceDto | null>(null);

  const isLoading = ref(false);
  const error = ref<string | null>(null);

  const currentRoom = computed(() => currentRun.value?.currentRoom ?? null);

const allNodes = computed(() => {
  const room = currentRoom.value;

  if (!room) {
    return [];
  }

  return room.nodeLayers.flatMap((layer) => layer.nodes);
});

async function loadPendingReward() {
  if (!currentRun.value) {
    return;
  }

  await execute(async () => {
    const response = await rewardApi.getPendingReward(currentRun.value!.id);

    pendingRewardOffer.value = unwrapRewardOffer(response);
  });
}

async function selectReward(optionId: string) {
  if (!currentRun.value || !pendingRewardOffer.value) {
    return;
  }

  await execute(async () => {
    await rewardApi.selectReward(currentRun.value!.id, {
      rewardOfferId: pendingRewardOffer.value!.id,
      rewardOptionId: optionId,
      optionId,
    });

    pendingRewardOffer.value = null;

    const response = await runApi.progressRun(currentRun.value!.id);
    currentRun.value = unwrapRunResponse(response);
    lastOutcome.value = null;
    activeCombat.value = null;
  });
}

const selectedNode = computed(() => {
  return allNodes.value.find((node) => node.state === 'Selected') ?? null;
});

const availableNodes = computed(() => {
  const room = currentRoom.value;

  if (!room) {
    return [];
  }

  return room.availableNodes;
});

async function execute(action: () => Promise<void>) {
  isLoading.value = true;
  error.value = null;

  try {
    await action();
  } catch (caught) {
    error.value = caught instanceof Error
      ? caught.message
      : 'Une erreur inconnue est survenue.';

    throw caught;
  } finally {
    isLoading.value = false;
  }
}

async function startRun() {
  await execute(async () => {
    const response = await runApi.startRun(demoPlayerId);

    console.log('[game-client] raw startRun response', response);

    const run = unwrapRunResponse(response);

    console.log('[game-client] normalized run', run);

    currentRun.value = run;
    lastOutcome.value = null;
    activeCombat.value = null;
  });
}

  async function loadRun(runId: string) {
    await execute(async () => {
      currentRun.value = await runApi.getRun(runId);
    });
  }

async function chooseNode(nodeId: string) {
  if (!currentRun.value) {
    return;
  }

  await execute(async () => {
    const response = await runApi.chooseNode(
      currentRun.value!.id,
      nodeId,
    );

    currentRun.value = unwrapRunResponse(response);
  });
}

  async function resolveCurrentEvent() {
    if (!currentRun.value) {
      return;
    }

    await execute(async () => {
      const response = await runApi.resolveCurrentEvent(currentRun.value!.id);

      currentRun.value = response.run;
      lastOutcome.value = response.outcome;
      activeCombat.value = response.startedCombat ?? null;
    });
  }

  async function progressRun() {
  if (!currentRun.value) {
    return;
  }

  await execute(async () => {
    const response = await runApi.progressRun(currentRun.value!.id);

    currentRun.value = unwrapRunResponse(response);
    lastOutcome.value = null;
    activeCombat.value = null;
  });
}

async function generateNextNodes() {
  if (!currentRun.value) {
    return;
  }

  await execute(async () => {
    const response = await runApi.generateNextNodes(currentRun.value!.id);

    currentRun.value = unwrapRunResponse(response);
    lastOutcome.value = null;
  });
}

return {
  currentRun,
  currentRoom,
  allNodes,
  selectedNode,
  availableNodes,
  lastOutcome,
  activeCombat,
  isLoading,
  error,
  startRun,
  loadRun,
  chooseNode,
  progressRun,
  resolveCurrentEvent,
  generateNextNodes,
  pendingRewardOffer,
  loadPendingReward,
  selectReward,
};
});