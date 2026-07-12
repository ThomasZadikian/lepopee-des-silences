<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
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

// ── Typewriter ──────────────────────────────────────────────────────────────
const displayed = ref('');
const lineIndex = ref(0);
const typing = ref(false);
const showChoices = ref(false);
let timer: ReturnType<typeof setInterval> | undefined;

function clearTimer() { if (timer) { clearInterval(timer); timer = undefined; } }

function typeLine(text: string) {
  clearTimer();
  typing.value = true;
  displayed.value = '';
  let i = 0;
  timer = setInterval(() => {
    i++;
    displayed.value = text.slice(0, i);
    if (i >= text.length) { clearTimer(); typing.value = false; afterLine(); }
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
    displayed.value = lines.value[lineIndex.value] ?? '';
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
  showChoices.value = false;
  emit('selectChoice', choiceId);
}

const hasMoreLines = computed(() => !typing.value && lineIndex.value < lines.value.length - 1);

// ── Screen reactions on consequence ─────────────────────────────────────────
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

watch(() => props.dialogue, () => { startNode(); }, { immediate: true });
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
    <div class="es-grain" />
    <div class="npc-vignette" />
    <div class="npc-aura" />

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
    </div>

    <!-- Name -->
    <div class="npc-name">
      <div class="es-kicker npc-name__kick">Rencontre · une entité du Palais</div>
      <h1 class="npc-name__title">{{ speaker }}</h1>
    </div>

    <!-- Bottom stack -->
    <div class="npc-wrap">
      <div class="npc-box" @click="advance">
        <span class="es-corner tl" /><span class="es-corner tr" />
        <span class="es-corner bl" /><span class="es-corner br" />

        <div class="npc-speaker-tag">{{ speaker }}</div>

        <p class="npc-line">
          {{ displayed }}<span v-if="typing" class="npc-caret">▌</span>
        </p>

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
          <span class="npc-choice__g">◆</span>
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
            <span class="npc-exit__g">◆</span>
            <span class="npc-exit__l">Se retirer du seuil</span>
            <span class="npc-exit__a">→</span>
          </button>
        </template>
      </div>

      <!-- Ended -->
      <div v-else-if="ended" class="npc-end">
        <button class="npc-exit" :disabled="isLoading" @click="emit('continue')">
          <span class="npc-exit__g">◆</span>
          <span class="npc-exit__l">Se retirer du seuil</span>
          <span class="npc-exit__a">→</span>
        </button>
      </div>
    </div>

    <!-- Flash overlay -->
    <div class="npc-flash" :class="['k-' + flashKind]" :key="'flash-' + flashKey" />
  </div>
</template>

<style scoped>
.npc-root {
  position: relative; width: 100%; height: 100%; overflow: hidden;
  color: var(--ink); font-family: var(--font); -webkit-font-smoothing: antialiased;
  background:
    radial-gradient(60% 55% at 20% 16%, rgba(125,92,255,.14), transparent 60%),
    radial-gradient(55% 50% at 84% 84%, rgba(96,120,255,.10), transparent 60%),
    radial-gradient(150% 130% at 50% -10%, #221a38 0%, var(--bg) 52%, var(--void) 100%);
  transition: background 1.1s ease;
}
.npc-root.mood-rompu {
  background:
    radial-gradient(70% 60% at 50% 24%, rgba(255,60,60,.20), transparent 70%),
    radial-gradient(150% 130% at 50% -10%, #2a1622 0%, #120810 55%, var(--void) 100%);
}
.npc-root.npc-shake { animation: npcShake .5s; }
@keyframes npcShake {
  0%,100% { transform: translate(0,0); } 20% { transform: translate(-6px,3px); }
  40% { transform: translate(5px,-4px); } 60% { transform: translate(-4px,-2px); } 80% { transform: translate(4px,3px); }
}

.npc-vignette { position: absolute; inset: 0; pointer-events: none; box-shadow: inset 0 0 240px 60px rgba(0,0,0,.7); }
.npc-aura { position: absolute; inset: 0; z-index: 1; pointer-events: none; opacity: .6; transition: opacity 1s ease; }
.npc-root.mood-rompu .npc-aura { opacity: .9; animation: npcPulse 3.2s ease-in-out infinite; }
@keyframes npcPulse { 0%,100% { opacity: .7; } 50% { opacity: 1; } }

/* Figure */
.npc-figure { position: absolute; top: 6vh; left: 50%; transform: translateX(-50%); width: 280px; height: 60vh; z-index: 2; }
.npc-figure svg { width: 100%; height: 100%; overflow: visible; }
.npc-silhouette {
  fill: url(#npcFigGrad); stroke: rgba(183,155,255,.5); stroke-width: 1;
  filter: drop-shadow(0 0 40px rgba(125,92,255,.32));
  animation: npcSway 7s ease-in-out infinite; transform-origin: center bottom;
}
.mood-rompu .npc-silhouette { stroke: rgba(255,90,90,.6); filter: drop-shadow(0 0 46px rgba(255,60,60,.5)); animation-duration: 5s; }
@keyframes npcSway { 0%,100% { transform: rotate(-.6deg) translateY(0); } 50% { transform: rotate(.6deg) translateY(-6px); } }
.npc-mask { fill: none; stroke: var(--violet, #b79bff); stroke-width: 1.4; opacity: .9; filter: drop-shadow(0 0 14px rgba(183,155,255,.8)); animation: npcBreathe 4s ease-in-out infinite; }
.mood-rompu .npc-mask { stroke: #ff5a5a; filter: drop-shadow(0 0 16px rgba(255,90,90,.9)); }
@keyframes npcBreathe { 0%,100% { opacity: .7; } 50% { opacity: 1; } }
.npc-eyes { fill: #b79bff; filter: drop-shadow(0 0 8px #b79bff); }
.mood-rompu .npc-eyes { fill: #ff5a5a; filter: drop-shadow(0 0 10px #ff5a5a); animation: npcFlicker .14s steps(2) infinite; }
@keyframes npcFlicker { 0%,100% { opacity: 1; } 50% { opacity: .55; } }

/* Name */
.npc-name { position: absolute; top: 4vh; left: 50%; transform: translateX(-50%); text-align: center; z-index: 4;
  opacity: 0; animation: npcNameIn 1s ease .2s forwards; }
@keyframes npcNameIn { from { opacity: 0; letter-spacing: .6em; filter: blur(6px); } to { opacity: 1; letter-spacing: .04em; filter: blur(0); } }
.npc-name__kick { color: var(--ink-4); }
.npc-name__title { margin: .3em 0 0; font-family: var(--display); font-size: 38px; font-weight: 400; color: var(--ink); letter-spacing: .04em; }

/* Bottom wrap */
.npc-wrap { position: relative; z-index: 5; height: 100%; display: flex; flex-direction: column; align-items: center; justify-content: flex-end; padding: 0 0 5vh; gap: 14px; }

/* Box */
.npc-box {
  position: relative; width: min(820px, 92vw); min-height: 184px; padding: 30px 38px 28px; cursor: pointer;
  background: linear-gradient(180deg, rgba(20,16,34,.86), rgba(12,9,22,.92));
  border: 1px solid var(--line); border-radius: 18px;
  box-shadow: 0 30px 80px -30px #000, inset 0 1px 0 rgba(255,255,255,.04); backdrop-filter: blur(4px);
  transition: border-color .5s ease, box-shadow .5s ease;
}
.mood-rompu .npc-box { border-color: rgba(255,90,90,.4); box-shadow: 0 30px 80px -30px #000, 0 0 50px -18px rgba(255,60,60,.5); }
.npc-speaker-tag { font-family: var(--caps, var(--display)); font-size: 11px; letter-spacing: .26em; text-transform: uppercase; color: #b79bff; opacity: .85; margin-bottom: 12px; }
.mood-rompu .npc-speaker-tag { color: #ff5a5a; }
.npc-line { margin: 0; font-family: var(--display); font-size: 25px; line-height: 1.5; color: var(--ink); font-style: italic; min-height: 1.5em; }
.npc-caret { display: inline-block; width: .5ch; color: #b79bff; animation: npcBlink 1s steps(1) infinite; }
.mood-rompu .npc-caret { color: #ff5a5a; }
@keyframes npcBlink { 50% { opacity: 0; } }
.npc-echoes { margin-top: 14px; }
.npc-echo { margin: 0 0 6px; padding-left: 14px; border-left: 2px solid rgba(183,155,255,.45); color: var(--ink-3); font-size: 14.5px; line-height: 1.55; opacity: 0; animation: npcRise .5s ease forwards; }
.npc-echo.warm { border-color: rgba(215,181,109,.6); color: #e7d6a8; }
.npc-echo.bad { border-color: rgba(255,90,90,.6); color: #ffb0b0; }
.npc-cont { position: absolute; right: 22px; bottom: 16px; color: var(--ink-4); font-size: 13px; animation: npcBob 1.4s ease-in-out infinite; }
@keyframes npcBob { 0%,100% { transform: translateY(0); } 50% { transform: translateY(4px); } }

/* Choices */
.npc-choices { width: min(820px, 92vw); display: flex; flex-direction: column; gap: 10px; }
.npc-choice {
  display: flex; align-items: center; gap: 14px; width: 100%; padding: 15px 20px; cursor: pointer; text-align: left;
  color: var(--ink-2); border: 1px solid var(--line); border-radius: 13px;
  background: linear-gradient(135deg, rgba(125,92,255,.06), transparent 60%), rgba(18,14,30,.6);
  opacity: 0; transform: translateY(10px); animation: npcRise .45s ease forwards;
  transition: transform .16s ease, border-color .2s ease, color .2s ease, box-shadow .2s ease;
}
.npc-choice:hover:not(:disabled) { transform: translateX(6px); color: var(--ink); border-color: rgba(183,155,255,.6); box-shadow: 0 10px 30px -16px rgba(125,92,255,.8); }
.mood-rompu .npc-choice:hover:not(:disabled) { border-color: rgba(255,90,90,.6); box-shadow: 0 10px 30px -16px rgba(255,60,60,.8); }
.npc-choice:disabled { opacity: .5; cursor: default; }
.npc-choice__g { color: #b79bff; font-size: 12px; }
.mood-rompu .npc-choice__g { color: #ff5a5a; }
.npc-choice__l { flex: 1; font-family: var(--display); font-size: 18px; }
.npc-choice__a { color: var(--ink-4); transition: transform .16s ease; }
.npc-choice:hover:not(:disabled) .npc-choice__a { transform: translateX(5px); color: #b79bff; }
@keyframes npcRise { to { opacity: 1; transform: none; } }
.npc-empty { color: var(--ink-4); font-style: italic; text-align: center; }

.npc-end { width: min(820px, 92vw); display: flex; justify-content: center; }
.npc-exit {
  display: flex; align-items: center; justify-content: center; gap: 14px;
  min-width: 280px; padding: 15px 28px; cursor: pointer; text-align: center;
  color: var(--ink-2); border: 1px solid var(--line); border-radius: 13px;
  background: linear-gradient(135deg, rgba(125,92,255,.06), transparent 60%), rgba(18,14,30,.6);
  opacity: 0; transform: translateY(10px); animation: npcRise .45s ease forwards;
  transition: transform .16s ease, border-color .2s ease, color .2s ease, box-shadow .2s ease;
}
.npc-exit:hover:not(:disabled) { transform: translateY(-2px); color: var(--ink); border-color: rgba(183,155,255,.6); box-shadow: 0 10px 30px -16px rgba(125,92,255,.8); }
.mood-rompu .npc-exit:hover:not(:disabled) { border-color: rgba(255,90,90,.6); box-shadow: 0 10px 30px -16px rgba(255,60,60,.8); }
.npc-exit:disabled { opacity: .5; cursor: default; }
.npc-exit__g { color: #b79bff; font-size: 12px; }
.mood-rompu .npc-exit__g { color: #ff5a5a; }
.npc-exit__l { font-family: var(--display); font-size: 18px; font-style: italic; }
.npc-exit__a { color: var(--ink-4); transition: transform .16s ease; }
.npc-exit:hover:not(:disabled) .npc-exit__a { transform: translateX(5px); color: #b79bff; }

/* Flash */
.npc-flash { position: absolute; inset: 0; z-index: 9; pointer-events: none; opacity: 0; }
.npc-flash.k-warm { background: radial-gradient(circle at 50% 40%, rgba(215,181,109,.5), transparent 60%); animation: npcFlash .6s ease; }
.npc-flash.k-bad { background: radial-gradient(circle at 50% 40%, rgba(255,60,60,.55), transparent 60%); animation: npcFlash .6s ease; }
@keyframes npcFlash { 0% { opacity: 0; } 30% { opacity: .55; } 100% { opacity: 0; } }
</style>