<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';

import AccountAccessShell from '../features/account/components/AccountAccessShell.vue';

const router = useRouter();
const characterName = ref('');
const selectedArchetype = ref('archetype.porteur');
const error = ref<string | null>(null);

const archetypes = [
  {
    key: 'archetype.porteur',
    name: 'Porteur',
    description: 'L’archétype actuellement disponible. Équilibré, résilient et conçu comme fondation du système de combat.',
    available: true,
    glyph: '◇',
  },
  {
    key: 'future-1',
    name: 'Archétype à venir',
    description: 'Un autre rapport au Palais prendra place ici dans une future version.',
    available: false,
    glyph: '○',
  },
  {
    key: 'future-2',
    name: 'Archétype à venir',
    description: 'Le système est déjà prévu pour accueillir plusieurs personnages par compte.',
    available: false,
    glyph: '○',
  },
] as const;

const selected = computed(() => archetypes.find((item) => item.key === selectedArchetype.value));

async function continueToPalace() {
  error.value = null;
  if (!characterName.value.trim()) {
    error.value = 'Donnez un nom à votre personnage.';
    return;
  }
  if (!selected.value?.available) {
    error.value = 'Cet archétype n’est pas encore disponible.';
    return;
  }

  // La commande CreateCharacter sera branchée sur l'API Player dans la verticale Application/API.
  await router.push({ name: 'threshold' });
}
</script>

<template>
  <AccountAccessShell
    kicker="Votre incarnation"
    title="Choisir un archétype"
    subtitle="Le Palais appartient au compte ; le nom, l’équipement et l’archétype appartiennent au personnage. L’archétype choisi est définitif."
  >
    <form class="character-selection" @submit.prevent="continueToPalace">
      <label class="character-name">
        <span class="character-name__label">Nom du personnage</span>
        <input v-model="characterName" class="character-name__input" maxlength="40" autocomplete="off" placeholder="Nommer votre personnage" />
      </label>

      <div class="archetype-grid" role="radiogroup" aria-label="Archétype du personnage">
        <button
          v-for="archetype in archetypes"
          :key="archetype.key"
          type="button"
          class="archetype-card"
          :class="{
            'archetype-card--selected': selectedArchetype === archetype.key,
            'archetype-card--locked': !archetype.available,
          }"
          :disabled="!archetype.available"
          @click="selectedArchetype = archetype.key"
        >
          <span class="archetype-card__glyph">{{ archetype.glyph }}</span>
          <span class="archetype-card__name">{{ archetype.name }}</span>
          <span class="archetype-card__description">{{ archetype.description }}</span>
          <span class="archetype-card__status">{{ archetype.available ? 'Disponible' : 'Verrouillé' }}</span>
        </button>
      </div>

      <div class="character-selection__summary">
        <span class="character-selection__summary-label">Sélection</span>
        <strong>{{ selected?.name }}</strong>
        <span>· archétype immuable après création</span>
      </div>

      <p v-if="error" class="character-selection__error" role="alert">{{ error }}</p>

      <button class="character-selection__submit" type="submit">
        <span>◈</span>
        <span>Entrer dans le Palais</span>
      </button>
    </form>
  </AccountAccessShell>
</template>

<style scoped>
.character-selection {
  padding: 30px;
  display: grid;
  gap: 28px;
  text-align: left;
}

.character-name {
  display: grid;
  gap: 8px;
  max-width: 420px;
}

.character-name__label,
.character-selection__summary-label {
  font-size: 10px;
  font-weight: 600;
  letter-spacing: .16em;
  text-transform: uppercase;
  color: var(--ink-3);
}

.character-name__input {
  box-sizing: border-box;
  width: 100%;
  padding: 12px 13px;
  border: 1px solid var(--line);
  outline: none;
  background: var(--bg-2);
  color: var(--ink);
  font: 14px var(--font);
}

.character-name__input:focus { border-color: var(--mint-dim); }

.archetype-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 14px;
}

.archetype-card {
  min-height: 220px;
  padding: 22px 18px;
  border: 1px solid var(--line);
  background: var(--bg-2);
  color: var(--ink-3);
  font-family: var(--font);
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  text-align: left;
  gap: 12px;
  cursor: pointer;
  transition: border-color .3s, background .3s, transform .3s;
}

.archetype-card:hover:not(:disabled) {
  border-color: var(--mint-dim);
  transform: translateY(-2px);
}

.archetype-card--selected {
  border-color: var(--mint-dim);
  background: color-mix(in srgb, var(--mint) 8%, var(--bg-2));
}

.archetype-card--locked {
  opacity: .42;
  cursor: not-allowed;
}

.archetype-card__glyph { color: var(--mint-dim); font-size: 24px; }
.archetype-card__name { color: var(--ink); font-family: var(--font-display); font-size: 22px; font-style: italic; }
.archetype-card__description { flex: 1; font-size: 12px; line-height: 1.6; }
.archetype-card__status { font: 10px var(--font-mono); letter-spacing: .12em; text-transform: uppercase; color: var(--ink-4); }

.character-selection__summary {
  display: flex;
  align-items: center;
  gap: 9px;
  flex-wrap: wrap;
  padding: 13px 0;
  border-top: 1px solid var(--line-soft);
  border-bottom: 1px solid var(--line-soft);
  color: var(--ink-4);
  font-size: 12px;
}

.character-selection__summary strong { color: var(--ink-2); font-weight: 500; }
.character-selection__error { color: var(--danger); font-size: 12px; }

.character-selection__submit {
  justify-self: end;
  padding: 12px 18px;
  border: 1px solid var(--mint-dim);
  background: transparent;
  color: var(--mint);
  display: flex;
  gap: 8px;
  font: 600 11px var(--font);
  letter-spacing: .13em;
  text-transform: uppercase;
  cursor: pointer;
}

@media (max-width: 760px) {
  .archetype-grid { grid-template-columns: 1fr; }
  .archetype-card { min-height: 150px; }
  .character-selection { padding: 20px; }
}
</style>
