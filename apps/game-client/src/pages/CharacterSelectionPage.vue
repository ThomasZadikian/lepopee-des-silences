<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

import AccountAccessShell from '../features/account/components/AccountAccessShell.vue';
import { getAccessToken } from '../features/account/authSession';
import { selectCharacter } from '../features/account/selectedCharacter';
import { playerApi, type AccountCharacterResponse } from '../shared/api/playerApi';

const router = useRouter();
const characterName = ref('');
const selectedArchetype = ref('archetype.porteur');
const error = ref<string | null>(null);
const accountError = ref<string | null>(null);
const busy = ref(false);
const accountLoaded = ref(false);
const characters = ref<AccountCharacterResponse[]>([]);
const showCreation = ref(false);

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

async function loadAccountCharacters() {
  const accessToken = getAccessToken();
  if (!accessToken) {
    accountError.value = 'Votre session a expiré. Reconnectez-vous.';
    accountLoaded.value = true;
    await router.replace({ name: 'login' });
    return;
  }

  try {
    const account = await playerApi.getAccount(accessToken);
    characters.value = account.characters;
    showCreation.value = account.characters.length === 0;
  } catch (cause) {
    accountError.value = cause instanceof Error
      ? cause.message
      : 'Impossible de charger les personnages du compte.';
  } finally {
    accountLoaded.value = true;
  }
}

onMounted(loadAccountCharacters);

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

  const accessToken = getAccessToken();
  if (!accessToken) {
    error.value = 'Votre session a expiré. Reconnectez-vous avant de créer un personnage.';
    return;
  }

  try {
    busy.value = true;
    const existingIds = new Set(characters.value.map((character) => character.id));
    const profile = await playerApi.createCharacter(accessToken, {
      displayName: characterName.value.trim(),
      archetypeKey: selected.value.key,
    });
    const createdCharacter = profile.characters.find((character) =>
      character.characterType === 'Player' && !existingIds.has(character.id));
    if (!createdCharacter) {
      throw new Error('Le personnage a été créé, mais il n’a pas pu être sélectionné. Rechargez la page.');
    }
    selectCharacter(createdCharacter.id);
    await router.push({ name: 'threshold' });
  } catch (cause) {
    error.value = cause instanceof Error
      ? cause.message
      : 'Impossible de créer ce personnage pour le moment.';
  } finally {
    busy.value = false;
  }
}

async function playWithCharacter(characterId: string) {
  selectCharacter(characterId);
  await router.push({ name: 'threshold' });
}

function beginCharacterCreation() {
  error.value = null;
  showCreation.value = true;
}

function cancelCharacterCreation() {
  error.value = null;
  showCreation.value = false;
}

function archetypeName(character: AccountCharacterResponse): string {
  return archetypes.find((archetype) => archetype.key === character.archetypeKey)?.name
    ?? character.archetypeKey
    ?? 'Archétype inconnu';
}
</script>

<template>
  <AccountAccessShell
    kicker="Votre incarnation"
    :title="showCreation ? 'Choisir un archétype' : 'Choisir un personnage'"
    :subtitle="showCreation
      ? 'Le Palais appartient au compte ; le nom, l’équipement et l’archétype appartiennent au personnage. L’archétype choisi est définitif.'
      : 'Votre compte peut abriter plusieurs personnages. Choisissez celui avec lequel vous souhaitez jouer.'"
  >
    <p v-if="!accountLoaded" class="character-selection__loading" role="status">
      Recherche de votre personnage…
    </p>

    <p v-else-if="accountError" class="character-selection__account-error" role="alert">
      {{ accountError }}
    </p>

    <section v-else-if="!showCreation" class="character-list" aria-label="Personnages du compte">
      <header class="character-list__header">
        <div>
          <span class="character-selection__summary-label">Personnages disponibles</span>
          <p>Choisissez l’incarnation qui franchira le seuil du Palais.</p>
        </div>
        <button type="button" class="character-list__create" @click="beginCharacterCreation">
          Créer un nouveau personnage
        </button>
      </header>

      <div class="character-list__grid">
        <button
          v-for="character in characters"
          :key="character.id"
          type="button"
          class="character-card"
          :data-character-id="character.id"
          @click="playWithCharacter(character.id)"
        >
          <span class="character-card__glyph">◇</span>
          <span class="character-card__identity">
            <strong>{{ character.displayName }}</strong>
            <span>{{ archetypeName(character) }}</span>
          </span>
          <span class="character-card__action">Jouer</span>
        </button>
      </div>
    </section>

    <form v-else class="character-selection" @submit.prevent="continueToPalace">
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
          :disabled="!archetype.available || busy"
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

      <div class="character-selection__actions">
        <button
          v-if="characters.length > 0"
          class="character-selection__cancel"
          type="button"
          :disabled="busy"
          @click="cancelCharacterCreation"
        >
          Retour à la liste
        </button>
        <button class="character-selection__submit" type="submit" :disabled="busy">
          <span>◈</span>
          <span>{{ busy ? 'Création…' : 'Créer et entrer dans le Palais' }}</span>
        </button>
      </div>
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

.character-list {
  padding: 30px;
  display: grid;
  gap: 24px;
}

.character-list__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
}

.character-list__header p {
  margin: 7px 0 0;
  color: var(--ink-3);
  font-size: 12px;
}

.character-list__create,
.character-selection__cancel {
  padding: 10px 14px;
  border: 1px solid var(--line);
  background: transparent;
  color: var(--ink-2);
  font: 600 10px var(--font);
  letter-spacing: .1em;
  text-transform: uppercase;
  cursor: pointer;
}

.character-list__create:hover,
.character-selection__cancel:hover { border-color: var(--mint-dim); color: var(--mint); }

.character-list__grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.character-card {
  min-height: 92px;
  padding: 18px;
  border: 1px solid var(--line);
  background: var(--bg-2);
  color: var(--ink-2);
  display: flex;
  align-items: center;
  gap: 14px;
  text-align: left;
  cursor: pointer;
  transition: border-color .3s, transform .3s, background .3s;
}

.character-card:hover {
  border-color: var(--mint-dim);
  background: color-mix(in srgb, var(--mint) 7%, var(--bg-2));
  transform: translateY(-2px);
}

.character-card__glyph { color: var(--mint-dim); font-size: 22px; }
.character-card__identity { min-width: 0; flex: 1; display: grid; gap: 5px; }
.character-card__identity strong { color: var(--ink); font-family: var(--font-display); font-size: 21px; font-style: italic; font-weight: 400; }
.character-card__identity span { color: var(--ink-4); font: 10px var(--font-mono); text-transform: uppercase; }
.character-card__action { color: var(--mint); font: 600 10px var(--font); letter-spacing: .12em; text-transform: uppercase; }

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
.character-selection__loading { padding: 30px; color: var(--ink-3); font-size: 12px; }
.character-selection__account-error { padding: 30px; color: var(--danger); font-size: 12px; }

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

.character-selection__submit:disabled { opacity: .55; cursor: wait; }
.character-selection__actions { display: flex; justify-content: flex-end; gap: 12px; }

@media (max-width: 760px) {
  .archetype-grid { grid-template-columns: 1fr; }
  .archetype-card { min-height: 150px; }
  .character-selection { padding: 20px; }
  .character-list { padding: 20px; }
  .character-list__header { align-items: stretch; flex-direction: column; }
  .character-list__grid { grid-template-columns: 1fr; }
}
</style>
