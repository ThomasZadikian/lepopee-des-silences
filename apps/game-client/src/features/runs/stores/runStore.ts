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
  type NodeDto,
  type ResolveCurrentEventResponse,
  type RunDto,
} from '../types/runTypes';

const demoPlayerId = '00000000-0000-0000-0000-000000000001';

export const useRunStore = defineStore('run', () => {
  const currentRun = ref<RunDto | null>(null);
  const pendingRewardOffer = ref<RewardOfferDto | null>(null);
  const lastOutcome = ref<ResolveCurrentEventResponse['outcome'] | null>(null);
  const activeCombat = ref<CombatInstanceDto | null>(null);
  const previewedNodeId = ref<string | null>(null);

  const isLoading = ref(false);
  const error = ref<string | null>(null);

  const currentRoom = computed(() => currentRun.value?.currentRoom ?? null);

  const allNodes = computed<NodeDto[]>(() => {
    const room = currentRoom.value;

    if (!room) {
      return [];
    }

    return room.nodeLayers.flatMap((layer) => layer.nodes);
  });

  const previewedNode = computed<NodeDto | null>(() => {
    if (!previewedNodeId.value) {
      return null;
    }

    return allNodes.value.find((node) => node.id === previewedNodeId.value) ?? null;
  });

  const selectedNode = computed<NodeDto | null>(() => {
    const room = currentRoom.value;

    if (!room) {
      return null;
    }

    if (previewedNode.value) {
      return previewedNode.value;
    }

    const selected = allNodes.value.find((node) => node.state === 'Selected');

    if (selected) {
      return selected;
    }

    const resolvedAtCurrentDepth = allNodes.value.find((node) =>
      node.state === 'Resolved' &&
      node.nodeDepth === room.currentNodeDepth
    );

    if (resolvedAtCurrentDepth) {
      return resolvedAtCurrentDepth;
    }

    const lastResolved = [...allNodes.value]
      .filter((node) => node.state === 'Resolved')
      .sort((left, right) => right.nodeDepth - left.nodeDepth)[0];

    return lastResolved ?? null;
  });

  const availableNodes = computed(() => {
    const room = currentRoom.value;

    if (!room) {
      return [];
    }

    return room.availableNodes;
  });

  function resetPreviewedNode() {
    previewedNodeId.value = null;
  }

  function previewNode(nodeId: string) {
    const node = allNodes.value.find((candidate) => candidate.id === nodeId);

    if (!node || node.state !== 'Available') {
      return;
    }

    previewedNodeId.value = nodeId;
  }

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

  async function refreshPendingRewardIfNeeded() {
    if (!currentRun.value?.pendingRewardOfferId) {
      pendingRewardOffer.value = null;
      return;
    }

    const response = await rewardApi.getPendingReward(currentRun.value.id);
    pendingRewardOffer.value = unwrapRewardOffer(response);
  }

  async function loadPendingReward() {
    if (!currentRun.value) {
      return;
    }

    await execute(async () => {
      await refreshPendingRewardIfNeeded();
    });
  }

  async function startRun() {
    await execute(async () => {
      const response = await runApi.startRun(demoPlayerId);
      const run = unwrapRunResponse(response);

      currentRun.value = run;
      pendingRewardOffer.value = null;
      lastOutcome.value = null;
      activeCombat.value = null;
      resetPreviewedNode();

      await refreshPendingRewardIfNeeded();
    });
  }

  async function loadRun(runId: string) {
    await execute(async () => {
      const response = await runApi.getRun(runId);
      const run = unwrapRunResponse(response);

      currentRun.value = run;
      resetPreviewedNode();

      if (!run.activeCombatId) {
        activeCombat.value = null;
      }

      await refreshPendingRewardIfNeeded();
    });
  }

  async function confirmAndResolveNode() {
    if (!currentRun.value || !selectedNode.value) {
      return;
    }

    const node = selectedNode.value;

    if (node.state === 'Resolved') {
      await progressRun();
      return;
    }

    if (node.state === 'Selected') {
      await resolveCurrentEvent();
      return;
    }

    if (node.state !== 'Available') {
      return;
    }

    await execute(async () => {
      const chooseResponse = await runApi.chooseNode(
        currentRun.value!.id,
        node.id,
      );

      currentRun.value = unwrapRunResponse(chooseResponse);
      resetPreviewedNode();

      const resolveResponse = await runApi.resolveCurrentEvent(
        currentRun.value!.id,
      );

      currentRun.value = resolveResponse.run;
      lastOutcome.value = resolveResponse.outcome;
      activeCombat.value = resolveResponse.startedCombat ?? null;

      await refreshPendingRewardIfNeeded();
    });
  }

  function chooseNode(nodeId: string) {
    previewNode(nodeId);
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
      resetPreviewedNode();

      await refreshPendingRewardIfNeeded();
    });
  }

  async function progressRun() {
    if (!currentRun.value) {
      return;
    }

    await execute(async () => {
      await refreshPendingRewardIfNeeded();

      if (pendingRewardOffer.value || currentRun.value?.pendingRewardOfferId) {
        throw new Error(
          'Une récompense est en attente : choisis une faveur avant de progresser.',
        );
      }

      const response = await runApi.progressRun(currentRun.value!.id);

      currentRun.value = unwrapRunResponse(response);
      lastOutcome.value = null;
      activeCombat.value = null;
      resetPreviewedNode();

      await refreshPendingRewardIfNeeded();
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
      resetPreviewedNode();

      await refreshPendingRewardIfNeeded();
    });
  }

  async function handleCombatCompleted() {
    if (!currentRun.value) {
      return;
    }

    await execute(async () => {
      const response = await runApi.getRun(currentRun.value!.id);
      const run = unwrapRunResponse(response);

      currentRun.value = run;
      activeCombat.value = null;
      lastOutcome.value = null;
      resetPreviewedNode();

      await refreshPendingRewardIfNeeded();
    });
  }

  async function selectReward(optionId: string) {
    if (!currentRun.value || !pendingRewardOffer.value) {
      return;
    }

    await execute(async () => {
      await rewardApi.selectReward(currentRun.value!.id, {
        rewardOfferId: pendingRewardOffer.value!.id,
        rewardChoiceId: optionId,
        choiceId: optionId,
        rewardOptionId: optionId,
        optionId,
      });

      pendingRewardOffer.value = null;

      const response = await runApi.getRun(currentRun.value!.id);
      currentRun.value = unwrapRunResponse(response);

      lastOutcome.value = null;
      activeCombat.value = null;
      resetPreviewedNode();

      await refreshPendingRewardIfNeeded();
    });
  }

const gameplayPhase = computed(() => {
  if (!currentRun.value) {
    return 'Loading';
  }

  if (pendingRewardOffer.value || currentRun.value.pendingRewardOfferId) {
    return 'Reward';
  }

  if (currentRun.value.activeCombatId) {
    return 'Combat';
  }

  if (currentRun.value.status === 'Completed' || currentRun.value.status === 'Failed') {
    return 'Completed';
  }

  if (lastOutcome.value) {
    return 'EventOutcome';
  }

  return 'Map';
});

async function continueAfterOutcome() {
  if (!lastOutcome.value) {
    return;
  }

  await progressRun();
}

  return {
    currentRun,
    currentRoom,
    allNodes,
    selectedNode,
    availableNodes,
    previewedNodeId,
    lastOutcome,
    activeCombat,
    pendingRewardOffer,
    isLoading,
    error,
    gameplayPhase,
    startRun,
    loadRun,
    chooseNode,
    previewNode,
    confirmAndResolveNode,
    progressRun,
    resolveCurrentEvent,
    generateNextNodes,
    loadPendingReward,
    selectReward,
    handleCombatCompleted,
    refreshPendingRewardIfNeeded,
    continueAfterOutcome,
  };
});