<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import type { NpcDialogueViewDto, NarrativeFragmentDto } from '../../runs/types/runTypes';

const props = defineProps<{
  dialogue: NpcDialogueViewDto | null;
  echoes: NarrativeFragmentDto[];
  ended: boolean;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  selectChoice: [choiceId: string];
  continue: [];
}>();

// Felt, never named (mystery preserved).
const mood = computed(() => {
  const s = (props.dialogue?.aggregateState ?? '').toLowerCase();
  if (s === 'rompu') return 'rompu';
  if (s === 'tendu') return 'tendu';
  return 'latent';
});
const speaker = computed(() => props.dialogue?.speaker ?? 'Une présence');
const lines = computed(() => props.dialogue?.lines ?? []);
const choices = computed(() => props.dialogue?.choices ?? []);

// ── Bubble history ───────────────────────────────────────────────────────────
// Chaque réplique devient une bulle qui reste affichée — les précédentes s'estompent
// mais ne disparaissent pas — au lieu de s'écraser à chaque nouvelle ligne/nœud.
type Bubble = { id: number; kind: 'npc' | 'player'; text: string };
const bubbles = ref<Bubble[]>([]);
let bubbleSeq = 0;
let lastNpcKey: string | null | undefined;

const historyEl = ref<HTMLElement | null>(null);
function scrollToLatest() {
  nextTick(() => {
    if (historyEl.value) historyEl.value.scrollTop = historyEl.value.scrollHeight;
  });
}

function pushBubble(kind: Bubble['kind'], text: string) {
  if (!text) return;
  bubbles.value = [...bubbles.value, { id: bubbleSeq++, kind, text }];
  scrollToLatest();
}

// ── Typewriter (ligne en cours de frappe, pas encore une bulle scellée) ──────
const typingText = ref('');
const lineIndex = ref(0);
const typing = ref(false);
const showChoices = ref(false);
let timer: ReturnType<typeof setInterval> | undefined;

function clearTimer() { if (timer) { clearInterval(timer); timer = undefined; } }

function typeLine(text: string) {
  clearTimer();
  typing.value = true;
  typingText.value = '';
  let i = 0;
  timer = setInterval(() => {
    i++;
    typingText.value = text.slice(0, i);
    scrollToLatest();
    if (i >= text.length) {
      clearTimer();
      typing.value = false;
      pushBubble('npc', typingText.value);
      typingText.value = '';
      afterLine();
    }
  }, 26);
}

function afterLine() {
  if (lineIndex.value >= lines.value.length - 1 && !props.ended) {
    showChoices.value = true;
  }
}

function startNode() {
  lineIndex.value = 0;
  showChoices.value = false;
  if (lines.value.length) typeLine(lines.value[0]);
  else if (!props.ended) showChoices.value = true;
}

function advance() {
  if (props.isLoading) return;
  if (typing.value) {
    clearTimer();
    typing.value = false;
    const fullLine = lines.value[lineIndex.value] ?? '';
    pushBubble('npc', fullLine);
    typingText.value = '';
    afterLine();
    return;
  }
  if (lineIndex.value < lines.value.length - 1) {
    lineIndex.value++;
    typeLine(lines.value[lineIndex.value]);
  }
}

function pick(choiceId: string) {
  if (props.isLoading) return;
  const choice = choices.value.find((c) => c.choiceId === choiceId);
  if (choice) pushBubble('player', choice.label);
  showChoices.value = false;
  emit('selectChoice', choiceId);
}

const hasMoreLines = computed(() => !typing.value && lineIndex.value < lines.value.length - 1);

// ── Reaction on consequence (kept minimal — no full-screen wash) ────────────
const shakeKey = ref(0);
const flashKind = ref<'none' | 'warm' | 'bad'>('none');
const flashKey = ref(0);

function react() {
  if (!props.echoes?.length) return;
  if (mood.value === 'rompu') {
    shakeKey.value++;
    flashKind.value = 'bad';
  } else {
    flashKind.value = 'warm';
  }
  flashKey.value++;
  setTimeout(() => { flashKind.value = 'none'; }, 620);
}

watch(() => props.dialogue, (dialogue) => {
  if (!dialogue) return;
  // Une nouvelle rencontre (PNJ différent, ou reprise après une rencontre déjà refermée)
  // efface l'historique — un nouveau nœud de la même rencontre s'y ajoute.
  if (dialogue.npcKey !== lastNpcKey) {
    bubbles.value = [];
    bubbleSeq = 0;
    lastNpcKey = dialogue.npcKey;
  }
  startNode();
}, { immediate: true });
watch(() => props.echoes, (e) => { if (e && e.length) react(); });
watch(() => props.ended, (v) => { if (v) showChoices.value = false; });

function onKey(e: KeyboardEvent) {
  if (e.code === 'Space') { e.preventDefault(); advance(); }
}
onMounted(() => window.addEventListener('keydown', onKey));
onBeforeUnmount(() => { clearTimer(); window.removeEventListener('keydown', onKey); });
</script>

<template>
  <div class="npc-root" :class="['mood-' + mood, { 'npc-shake': shakeKey }]" :key="shakeKey">
    <!-- The figure -->
    <div class="npc-figure">
      <svg viewBox="0 0 300 480" preserveAspectRatio="xMidYMax meet" aria-hidden="true">
        <defs>
          <linearGradient id="npcFigGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stop-color="rgba(40,30,70,.65)" />
            <stop offset="1" stop-color="rgba(10,8,20,.9)" />
          </linearGradient>
        </defs>
        <g class="npc-silhouette">
          <path d="M150 60 C120 60 108 86 108 120 C108 150 92 168 86 210
                   C80 250 70 300 64 470 L236 470 C230 300 220 250 214 210
                   C208 168 192 150 192 120 C192 86 180 60 150 60 Z" />
        </g>
        <g transform="translate(150 96)">
          <ellipse class="npc-mask" rx="26" ry="32" />
          <circle class="npc-eyes" cx="-9" cy="-2" r="2.6" />
          <circle class="npc-eyes" cx="9" cy="-2" r="2.6" />
        </g>
      </svg>
      <span class="npc-figure__name">{{ speaker }}</span>
    </div>

    <!-- Dialogue column, centered -->
    <div class="npc-wrap">
      <div ref="historyEl" class="npc-history" @click="advance">
        <div
          v-for="(bubble, i) in bubbles"
          :key="bubble.id"
          class="npc-bubble"
          :class="[
            bubble.kind === 'player' ? 'npc-bubble--player' : 'npc-bubble--npc',
            (i < bubbles.length - 1 || typing) && 'npc-bubble--dim',
          ]"
        >
          <span class="npc-bubble__speaker">{{ bubble.kind === 'player' ? 'Toi' : speaker }}</span>
          <p class="npc-bubble__text">{{ bubble.text }}</p>
        </div>

        <div v-if="typing" class="npc-bubble npc-bubble--npc">
          <span class="npc-bubble__speaker">{{ speaker }}</span>
          <p class="npc-bubble__text">{{ typingText }}<span class="npc-caret">▌</span></p>
        </div>

        <div v-if="echoes && echoes.length" class="npc-echoes">
          <p
            v-for="(echo, i) in echoes"
            :key="'echo-' + i"
            class="npc-echo"
            :class="mood === 'rompu' ? 'bad' : 'warm'"
            :style="{ animationDelay: (i * 90) + 'ms' }"
          >{{ echo.text }}</p>
        </div>

        <div v-if="hasMoreLines" class="npc-cont">continuer ▾</div>
      </div>

      <!-- Choices -->
      <div v-if="showChoices && !ended" class="npc-choices">
        <button
          v-for="(choice, idx) in choices"
          :key="choice.choiceId"
          class="npc-choice"
          :style="{ animationDelay: (idx * 80) + 'ms' }"
          :disabled="isLoading"
          @click="pick(choice.choiceId)"
        >
          <span class="npc-choice__l">{{ choice.label }}</span>
          <span class="npc-choice__a">→</span>
        </button>
        <template v-if="!choices.length">
          <p class="npc-empty">Le silence s'installe…</p>
          <!-- Defensive fallback: if the backend ever returns an empty choice
               list while the encounter isn't formally "ended" (e.g. every
               choice was filtered out by an unmet requirement), still offer
               a way out instead of a silent dead end. -->
          <button class="npc-exit" :disabled="isLoading" @click="emit('continue')">
            <span class="npc-exit__l">Se retirer du seuil</span>
            <span class="npc-exit__a">→</span>
          </button>
        </template>
      </div>

      <!-- Ended -->
      <div v-else-if="ended" class="npc-end">
        <button class="npc-exit" :disabled="isLoading" @click="emit('continue')">
          <span class="npc-exit__l">Se retirer du seuil</span>
          <span class="npc-exit__a">→</span>
        </button>
      </div>
    </div>

    <!-- Reaction flash (kept minimal — a brief tint, not a wash) -->
    <div class="npc-flash" :class="['k-' + flashKind]" :key="'flash-' + flashKey" />
  </div>
</template>

<style scoped>
.npc-root {
  position: relative; width: 100%; height: 100%; overflow: hidden;
  color: var(--ink); font-family: var(--font); -webkit-font-smoothing: antialiased;
}
.npc-root.npc-shake { animation: npcShake .5s; }
@keyframes npcShake {
  0%,100% { transform: translate(0,0); } 20% { transform: translate(-6px,3px); }
  40% { transform: translate(5px,-4px); } 60% { transform: translate(-4px,-2px); } 80% { transform: translate(4px,3px); }
}

/* Figure — asset du PNJ, un peu à gauche du centre */
.npc-figure { position: absolute; top: 6vh; left: 30%; transform: translateX(-50%); width: 280px; height: 60vh; z-index: 2; display: flex; flex-direction: column; align-items: center; }
.npc-figure svg { width: 100%; height: 100%; overflow: visible; }
.npc-silhouette {
  fill: url(#npcFigGrad); stroke: var(--mint-dim); stroke-width: 1; opacity: .85;
  animation: npcSway 7s ease-in-out infinite; transform-origin: center bottom;
}
.mood-rompu .npc-silhouette { stroke: var(--danger-dim); }
@keyframes npcSway { 0%,100% { transform: rotate(-.6deg) translateY(0); } 50% { transform: rotate(.6deg) translateY(-6px); } }
.npc-mask { fill: none; stroke: var(--mint-dim); stroke-width: 1.4; opacity: .9; animation: npcBreathe 4s ease-in-out infinite; }
.mood-rompu .npc-mask { stroke: var(--danger-dim); }
@keyframes npcBreathe { 0%,100% { opacity: .7; } 50% { opacity: 1; } }
.npc-eyes { fill: var(--mint-dim); }
.mood-rompu .npc-eyes { fill: var(--danger-dim); animation: npcFlicker .14s steps(2) infinite; }
@keyframes npcFlicker { 0%,100% { opacity: 1; } 50% { opacity: .55; } }

.npc-figure__name {
  margin-top: 8px;
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: .18em;
  text-transform: uppercase;
  color: var(--ink-3);
}

/* Dialogue column — centrée, sans fond hors la carte */
.npc-wrap {
  position: relative; z-index: 5; height: 100%; width: min(560px, 46vw);
  margin-left: auto; margin-right: 8vw;
  display: flex; flex-direction: column; align-items: stretch; justify-content: flex-end;
  padding: 0 0 6vh; gap: 14px;
}

.npc-history {
  max-height: 50vh;
  overflow-y: auto;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.npc-bubble { transition: opacity .4s ease; }
.npc-bubble--dim { opacity: .45; }
.npc-bubble--player { align-self: flex-end; text-align: right; max-width: 82%; }

.npc-bubble__speaker { display: block; font-family: var(--font-mono); font-size: 10px; letter-spacing: .16em; text-transform: uppercase; color: var(--mint-dim); opacity: .85; margin-bottom: 6px; }
.mood-rompu .npc-bubble__speaker { color: var(--danger-dim); }
.npc-bubble--player .npc-bubble__speaker { color: var(--ink-4); }

.npc-bubble__text {
  margin: 0; padding: 10px 14px;
  background: rgba(12, 13, 18, 0.55);
  font-family: var(--font-display); font-size: 18px; line-height: 1.5; color: var(--ink); font-style: italic; min-height: 1.4em;
}
.npc-bubble--player .npc-bubble__text { color: var(--ink-2); font-size: 14px; background: rgba(12, 13, 18, 0.4); }

.npc-caret { display: inline-block; width: .5ch; color: var(--mint-dim); animation: npcBlink 1s steps(1) infinite; }
.mood-rompu .npc-caret { color: var(--danger-dim); }
@keyframes npcBlink { 50% { opacity: 0; } }
.npc-echoes { margin-top: 6px; }
.npc-echo { margin: 0 0 6px; padding-left: 12px; border-left: 2px solid var(--mint-dim); color: var(--ink-3); font-size: 13px; line-height: 1.55; opacity: 0; animation: npcRise .5s ease forwards; }
.npc-echo.bad { border-color: var(--danger-dim); color: var(--ink-2); }
.npc-cont { align-self: flex-end; color: var(--ink-4); font-size: 12px; animation: npcBob 1.4s ease-in-out infinite; }
@keyframes npcBob { 0%,100% { transform: translateY(0); } 50% { transform: translateY(4px); } }

/* Choices */
.npc-choices { display: flex; flex-direction: column; gap: 8px; }
.npc-choice {
  display: flex; align-items: center; gap: 12px; width: 100%; padding: 12px 16px; cursor: pointer; text-align: left;
  color: var(--ink-2); border: 1px solid var(--line); background: rgba(12, 13, 18, 0.55);
  opacity: 0; transform: translateY(10px); animation: npcRise .45s ease forwards;
  transition: border-color .2s ease, color .2s ease;
}
.npc-choice:hover:not(:disabled) { color: var(--ink); border-color: var(--mint-dim); }
.npc-choice:disabled { opacity: .5; cursor: default; }
.npc-choice__l { flex: 1; font-family: var(--font-display); font-style: italic; font-size: 15px; }
.npc-choice__a { color: var(--ink-4); transition: transform .16s ease; }
.npc-choice:hover:not(:disabled) .npc-choice__a { transform: translateX(5px); color: var(--mint-dim); }
@keyframes npcRise { to { opacity: 1; transform: none; } }
.npc-empty { color: var(--ink-4); font-style: italic; text-align: center; }

.npc-end { display: flex; justify-content: flex-end; }
.npc-exit {
  display: flex; align-items: center; justify-content: center; gap: 12px;
  padding: 12px 22px; cursor: pointer; text-align: center;
  color: var(--ink-2); border: 1px solid var(--line); background: rgba(12, 13, 18, 0.55);
  opacity: 0; transform: translateY(10px); animation: npcRise .45s ease forwards;
  transition: border-color .2s ease, color .2s ease;
}
.npc-exit:hover:not(:disabled) { color: var(--ink); border-color: var(--mint-dim); }
.npc-exit:disabled { opacity: .5; cursor: default; }
.npc-exit__l { font-family: var(--font-display); font-size: 15px; font-style: italic; }
.npc-exit__a { color: var(--ink-4); transition: transform .16s ease; }
.npc-exit:hover:not(:disabled) .npc-exit__a { transform: translateX(5px); color: var(--mint-dim); }

/* Reaction flash — brief tint tied to a consequence, not an ambient wash */
.npc-flash { position: absolute; inset: 0; z-index: 9; pointer-events: none; opacity: 0; }
.npc-flash.k-warm { background: radial-gradient(circle at 50% 40%, color-mix(in oklch, var(--mint), transparent 55%), transparent 60%); animation: npcFlash .6s ease; }
.npc-flash.k-bad { background: radial-gradient(circle at 50% 40%, color-mix(in oklch, var(--danger), transparent 45%), transparent 60%); animation: npcFlash .6s ease; }
@keyframes npcFlash { 0% { opacity: 0; } 30% { opacity: .55; } 100% { opacity: 0; } }

@media (max-width: 900px) {
  .npc-figure { left: 18%; width: 200px; }
  .npc-wrap { width: min(460px, 60vw); margin-right: 4vw; }
}
</style>
