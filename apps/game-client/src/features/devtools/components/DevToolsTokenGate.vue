<script setup lang="ts">
import { ref } from 'vue';

import type { DevToolsStatusKey } from '../types/devToolsTypes';

const props = defineProps<{
  hasToken: boolean;
  status: DevToolsStatusKey;
  environment: string | null;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  saveToken: [token: string];
  clearToken: [];
  checkStatus: [];
}>();

const draftToken = ref('');

function save() {
  emit('saveToken', draftToken.value);
  draftToken.value = '';
}
</script>

<template>
  <section class="devtools-card">
    <div class="devtools-card__header">
      <span class="devtools-kicker">DEV ONLY</span>
      <strong>Connexion</strong>
    </div>

    <dl class="devtools-status-list">
      <div>
        <dt>Devtools backend</dt>
        <dd :class="`devtools-status devtools-status--${props.status}`">
          {{ props.status === 'available' ? 'disponible' : props.status === 'unavailable' ? 'indisponible' : 'inconnu' }}
        </dd>
      </div>
      <div>
        <dt>Token</dt>
        <dd>{{ props.hasToken ? 'defini' : 'absent' }}</dd>
      </div>
      <div v-if="props.environment">
        <dt>Environment</dt>
        <dd>{{ props.environment }}</dd>
      </div>
    </dl>

    <div class="devtools-token-row">
      <input
        v-model="draftToken"
        type="password"
        autocomplete="off"
        placeholder="Token local devtools"
        class="devtools-input"
      >
      <button class="devtools-btn" :disabled="draftToken.trim().length === 0" @click="save">
        Enregistrer
      </button>
    </div>

    <div class="devtools-actions-row">
      <button class="devtools-btn" :disabled="props.isLoading || !props.hasToken" @click="emit('checkStatus')">
        Tester
      </button>
      <button class="devtools-btn devtools-btn--ghost" :disabled="!props.hasToken" @click="emit('clearToken')">
        Effacer le token
      </button>
    </div>
  </section>
</template>
