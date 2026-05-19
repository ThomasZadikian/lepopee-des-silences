<template>
  <div style="min-height: 100vh; background: var(--rpg-cream);">
    <!-- ── Header ──────────────────────────────────────────────── -->
    <div style="padding: 32px 48px; border-bottom: 1px solid var(--rpg-border); display: flex; align-items: flex-end; justify-content: space-between;">
      <div>
        <div class="editorial-label mb-2">Enclos du Codex</div>
        <div style="font-family: var(--font-serif); font-size: 2rem; font-weight: 900; letter-spacing: -0.03em; line-height: 1;">
          Neige <span style="font-style: italic; color: var(--rpg-ink-muted);">— renard des glaces</span>
        </div>
      </div>
      <div style="font-size: 11px; color: var(--rpg-ink-muted); text-align: right;">
        <div>Mis à jour toutes les 5 min</div>
        <div v-if="lastUpdated">Dernière sync : {{ lastUpdated }}</div>
      </div>
    </div>

    <!-- ── Corps ──────────────────────────────────────────────── -->
    <div style="display: grid; grid-template-columns: 1fr 400px; min-height: calc(100vh - 120px);">

      <!-- Zone de jeu -->
      <div style="padding: 48px; border-right: 1px solid var(--rpg-border); display: flex; flex-direction: column; align-items: center; justify-content: center; position: relative; overflow: hidden;">

        <!-- Fond enclos -->
        <div style="width: 100%; max-width: 600px; height: 320px; background: linear-gradient(180deg, #e8f4f8 0%, #d4ecf5 60%, #b8dce8 100%); border: 1px solid var(--rpg-border); position: relative; overflow: hidden;">

          <!-- Sol -->
          <div style="position: absolute; bottom: 0; left: 0; right: 0; height: 60px; background: linear-gradient(180deg, #c8e8f0 0%, #a8d4e0 100%); border-top: 2px solid rgba(0,0,0,0.06);"/>

          <!-- Flocons décoratifs -->
          <div v-for="i in 8" :key="i"
            style="position: absolute; font-size: 12px; opacity: 0.4; animation: snowfall linear infinite;"
            :style="{
              left: `${10 + i * 11}%`,
              top: `-${i * 5}px`,
              animationDuration: `${3 + i * 0.5}s`,
              animationDelay: `${i * 0.3}s`
            }">
            ❄
          </div>

          <!-- Renard animé -->
          <div
            style="position: absolute; bottom: 60px;"
            :style="{ left: foxPosition }"
          >
            <div
              class="fox-sprite"
              :style="{
                backgroundImage: `url(${foxSprite})`,
                backgroundPosition: spritePosition,
                width: '117px',
                height: '128px',
                imageRendering: 'pixelated',
                transform: `scale(${foxScale})`,
                transformOrigin: 'bottom center'
              }"
            />
          </div>
        </div>

        <!-- État actuel -->
        <div style="margin-top: 24px; text-align: center;">
          <div style="display: inline-flex; align-items: center; gap: 10px; padding: 8px 20px; border: 1px solid var(--rpg-border); background: white;">
            <span style="font-size: 18px;">{{ stateEmoji }}</span>
            <div>
              <div style="font-size: 11px; font-weight: 700; letter-spacing: 0.1em; text-transform: uppercase;">{{ stateLabel }}</div>
              <div style="font-size: 10px; color: var(--rpg-ink-muted);">{{ stateDescription }}</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Panneau info -->
      <div style="padding: 48px 32px;">
        <div class="editorial-label mb-6">Journal de Neige</div>

        <!-- Stats globales -->
        <div style="border-top: 1px solid var(--rpg-border); margin-bottom: 32px;">
          <div v-for="stat in stats" :key="stat.label"
            style="padding: 14px 0; border-bottom: 1px solid var(--rpg-border); display: flex; justify-content: space-between; align-items: baseline;">
            <div style="font-size: 12px; color: var(--rpg-ink-muted);">{{ stat.label }}</div>
            <div style="font-family: var(--font-serif); font-size: 1.1rem; font-weight: 700;">{{ stat.value }}</div>
          </div>
        </div>

        <!-- Humeur -->
        <div class="editorial-label mb-4">Humeur actuelle</div>
        <div style="padding: 16px; border: 1px solid var(--rpg-border); margin-bottom: 32px;">
          <div style="font-size: 13px; line-height: 1.7; color: var(--rpg-ink-muted); font-style: italic;">
            "{{ moodText }}"
          </div>
        </div>

        <!-- Légende états -->
        <div class="editorial-label mb-4">États possibles</div>
        <div style="border-top: 1px solid var(--rpg-border);">
          <div v-for="s in stateList" :key="s.key"
            style="padding: 10px 0; border-bottom: 1px solid var(--rpg-border); display: flex; align-items: center; gap: 10px;"
            :style="{ opacity: currentState === s.key ? 1 : 0.4 }">
            <span>{{ s.emoji }}</span>
            <div>
              <div style="font-size: 12px; font-weight: 600;">{{ s.label }}</div>
              <div style="font-size: 10px; color: var(--rpg-ink-muted);">{{ s.desc }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { companionApi } from '@/api/companion'
import foxSprite from '@/assets/white_fox.png'
import { computed, onMounted, onUnmounted, ref } from 'vue'

const currentState  = ref('REPOS')
const victoriesToday = ref(0)
const defeatesToday  = ref(0)
const lastUpdated    = ref('')
const frame          = ref(0)
const foxPosition    = ref('40%')

// Mapping état → row du sprite sheet
// Image 1408x768, 6 rows de 128px, frames de largeur variable
const stateConfig: Record<string, { row: number; frames: number; frameWidth: number }> = {
    REPOS:   { row: 0, frames: 4, frameWidth: 234 },
    JEU:     { row: 1, frames: 6, frameWidth: 234 },
    MANGER:  { row: 2, frames: 4, frameWidth: 234 },
    EXCITE:  { row: 3, frames: 6, frameWidth: 234 },
    TRISTE:  { row: 4, frames: 4, frameWidth: 234 },
    ENDORMI: { row: 5, frames: 4, frameWidth: 234 },
}

const spritePosition = computed(() => {
    const config = stateConfig[currentState.value] ?? stateConfig['REPOS']
    const x = -(frame.value * config.frameWidth)
    const y = -(config.row * 128)
    return `${x}px ${y}px`
})

const foxScale = computed(() => {
    if (currentState.value === 'EXCITE') return 2.5
    if (currentState.value === 'ENDORMI') return 2.0
    return 2.2
})

const stateEmoji = computed(() => stateList.find(s => s.key === currentState.value)?.emoji ?? '🦊')
const stateLabel = computed(() => stateList.find(s => s.key === currentState.value)?.label ?? '')
const stateDescription = computed(() => stateList.find(s => s.key === currentState.value)?.desc ?? '')

const moodText = computed(() => {
    const texts: Record<string, string> = {
        REPOS:   'Neige se repose tranquillement, la queue enroulée autour de ses pattes.',
        JEU:     'Neige saute et bondit joyeusement dans l\'enclos.',
        MANGER:  'Neige mange sa gamelle avec appétit. Les aventuriers ont bien chassé aujourd\'hui.',
        EXCITE:  'Les victoires des joueurs rendent Neige frénétique ! Elle tourne en rond.',
        TRISTE:  'Trop de défaites... Neige a la queue basse et les oreilles couchées.',
        ENDORMI: 'Neige dort paisiblement. L\'enclos est calme depuis un moment.',
    }
    return texts[currentState.value] ?? ''
})

const stats = computed(() => [
    { label: 'Victoires du jour',  value: victoriesToday.value },
    { label: 'Défaites du jour',   value: defeatesToday.value },
    { label: 'Humeur',             value: stateLabel.value },
])

const stateList = [
    { key: 'REPOS',   emoji: '😌', label: 'Repos',    desc: 'Neige se détend' },
    { key: 'JEU',     emoji: '🎮', label: 'Jeu',      desc: 'Neige s\'amuse' },
    { key: 'MANGER',  emoji: '🍖', label: 'Repas',    desc: 'Neige mange' },
    { key: 'EXCITE',  emoji: '⚡', label: 'Excitée',  desc: 'Beaucoup de victoires' },
    { key: 'TRISTE',  emoji: '😢', label: 'Triste',   desc: 'Trop de défaites' },
    { key: 'ENDORMI', emoji: '💤', label: 'Dort',     desc: 'Inactivité prolongée' },
]

// Animation frames
let frameInterval: ReturnType<typeof setInterval> | null = null
let pollInterval:  ReturnType<typeof setInterval> | null = null
let moveInterval:  ReturnType<typeof setInterval> | null = null

function startAnimation() {
    if (frameInterval) clearInterval(frameInterval)
    frame.value = 0
    const config = stateConfig[currentState.value] ?? stateConfig['REPOS']
    const speed = currentState.value === 'EXCITE' ? 80 : currentState.value === 'ENDORMI' ? 400 : 150

    frameInterval = setInterval(() => {
        frame.value = (frame.value + 1) % config.frames
    }, speed)
}

function startMovement() {
    if (moveInterval) clearInterval(moveInterval)
    if (['ENDORMI', 'MANGER'].includes(currentState.value)) return

    moveInterval = setInterval(() => {
        const current = parseFloat(foxPosition.value)
        const delta   = currentState.value === 'EXCITE' ? (Math.random() - 0.5) * 20 : (Math.random() - 0.5) * 8
        const next    = Math.max(5, Math.min(75, current + delta))
        foxPosition.value = `${next}%`
    }, currentState.value === 'EXCITE' ? 600 : 2000)
}

async function fetchState() {
    try {
        const res        = await companionApi.getState()
        currentState.value   = res.data.currentState
        victoriesToday.value = res.data.victoriesToday
        defeatesToday.value  = res.data.defeatesToday
        lastUpdated.value    = new Date(res.data.lastUpdated).toLocaleTimeString('fr-FR')
        startAnimation()
        startMovement()
    } catch {
        // Silencieux — le companion reste dans son dernier état connu
    }
}

onMounted(async () => {
    await fetchState()
    pollInterval = setInterval(fetchState, 5 * 60 * 1000)
})

onUnmounted(() => {
    if (frameInterval) clearInterval(frameInterval)
    if (pollInterval)  clearInterval(pollInterval)
    if (moveInterval)  clearInterval(moveInterval)
})
</script>

<style scoped>
.fox-sprite {
    background-repeat: no-repeat;
    transition: left 1s ease-in-out;
    mix-blend-mode: multiply;
}

@keyframes snowfall {
    from { transform: translateY(-10px) rotate(0deg); opacity: 0.4; }
    to   { transform: translateY(380px) rotate(360deg); opacity: 0; }
}
</style>