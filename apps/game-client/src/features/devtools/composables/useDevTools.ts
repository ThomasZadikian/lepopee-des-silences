import { computed, ref } from 'vue';

import { devToolsApi } from '../api/devToolsApi';
import type { DevToolsStatusKey } from '../types/devToolsTypes';

const tokenStorageKey = 'leds:game-client:devtools-token';

function readStoredToken(): string {
  try {
    return window.localStorage.getItem(tokenStorageKey) ?? '';
  } catch {
    return '';
  }
}

function writeStoredToken(token: string): void {
  try {
    if (token) window.localStorage.setItem(tokenStorageKey, token);
    else window.localStorage.removeItem(tokenStorageKey);
  } catch {
    // Storage may be unavailable in private contexts; the in-memory ref still works.
  }
}

export function useDevTools() {
  const token = ref(readStoredToken());
  const status = ref<DevToolsStatusKey>('unknown');
  const statusEnvironment = ref<string | null>(null);
  const isLoading = ref(false);
  const message = ref<string | null>(null);
  const error = ref<string | null>(null);

  const hasToken = computed(() => token.value.trim().length > 0);

  function saveToken(nextToken: string) {
    token.value = nextToken.trim();
    writeStoredToken(token.value);
  }

  function clearToken() {
    token.value = '';
    writeStoredToken('');
  }

  async function checkStatus() {
    if (!hasToken.value) {
      status.value = 'unknown';
      error.value = 'Token devtools absent.';
      return;
    }

    isLoading.value = true;
    error.value = null;
    try {
      const response = await devToolsApi.getStatus(token.value);
      status.value = response.enabled ? 'available' : 'unavailable';
      statusEnvironment.value = response.environment;
      message.value = 'Devtools backend disponibles.';
    } catch (caught) {
      status.value = 'unavailable';
      error.value = caught instanceof Error ? caught.message : 'Devtools indisponibles.';
    } finally {
      isLoading.value = false;
    }
  }

  async function runAction(action: (token: string) => Promise<unknown>, successMessage: string) {
    if (!hasToken.value) {
      error.value = 'Token devtools absent.';
      return false;
    }

    isLoading.value = true;
    error.value = null;
    message.value = null;
    try {
      await action(token.value);
      message.value = successMessage;
      return true;
    } catch (caught) {
      error.value = caught instanceof Error ? caught.message : 'Action devtools echouee.';
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  return {
    token,
    hasToken,
    status,
    statusEnvironment,
    isLoading,
    message,
    error,
    saveToken,
    clearToken,
    checkStatus,
    runAction,
  };
}
