<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';

import { clearAuthenticatedSession, getAccessToken } from '../authSession';
import { useRunStore } from '../../runs/stores/runStore';
import { playerApi } from '../../../shared/api/playerApi';

const router = useRouter();
const runStore = useRunStore();
const isOpen = ref(false);
const busy = ref(false);
const error = ref<string | null>(null);

async function logout() {
  const accessToken = getAccessToken();
  error.value = null;
  busy.value = true;

  try {
    if (accessToken) await playerApi.logout(accessToken);
    clearAuthenticatedSession();
    runStore.clearForLogout();
    await router.replace({ name: 'login' });
  } catch (cause) {
    error.value = cause instanceof Error
      ? cause.message
      : 'La déconnexion a échoué. Réessayez.';
  } finally {
    busy.value = false;
  }
}
</script>

<template>
  <aside class="session-menu">
    <button
      type="button"
      class="session-menu__toggle"
      aria-label="Ouvrir le menu de session"
      :aria-expanded="isOpen"
      @click="isOpen = !isOpen"
    >
      <span aria-hidden="true">◇</span>
      <span>Session</span>
    </button>

    <div v-if="isOpen" class="session-menu__panel">
      <button
        type="button"
        class="session-menu__logout"
        :disabled="busy"
        @click="logout"
      >
        {{ busy ? 'Déconnexion…' : 'Se déconnecter' }}
      </button>
      <p v-if="error" class="session-menu__error" role="alert">{{ error }}</p>
    </div>
  </aside>
</template>

<style scoped>
.session-menu {
  position: fixed;
  top: 10px;
  right: 10px;
  z-index: 9200;
  display: grid;
  justify-items: end;
  gap: 6px;
  font-family: var(--font);
}

.session-menu__toggle,
.session-menu__logout {
  border: 1px solid var(--line-soft);
  background: var(--panel);
  color: var(--ink-3);
  font: 600 10px var(--font);
  letter-spacing: .12em;
  text-transform: uppercase;
  cursor: pointer;
}

.session-menu__toggle {
  display: flex;
  align-items: center;
  gap: 7px;
  padding: 8px 10px;
}

.session-menu__panel {
  min-width: 180px;
  padding: 10px;
  border: 1px solid var(--line-soft);
  background: var(--panel);
  box-shadow: var(--shadow-panel);
}

.session-menu__logout {
  width: 100%;
  padding: 9px 10px;
  color: var(--danger);
  text-align: left;
}

.session-menu__toggle:hover,
.session-menu__logout:hover:not(:disabled) {
  border-color: var(--mint-dim);
  color: var(--mint);
}

.session-menu__logout:disabled { opacity: .55; cursor: wait; }
.session-menu__error { margin: 9px 0 0; color: var(--danger); font-size: 11px; line-height: 1.4; }
</style>
