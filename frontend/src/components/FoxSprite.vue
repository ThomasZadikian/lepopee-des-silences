<template>
  <canvas
    ref="canvas"
    :width="displayW"
    :height="displayH"
    style="image-rendering: pixelated; display: block;"
  />
</template>

<script setup lang="ts">
import foxSpriteUrl from '@/assets/white_fox.png'
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'

const props = defineProps<{ state: string }>()

// Positions exactes de chaque frame (x de départ, largeur)
const stateConfig: Record<string, {
    sy: number; sh: number; speed: number;
    frames: Array<{ x: number; w: number }>
}> = {
    REPOS: {
        sy: 8, sh: 122, speed: 150,
        frames: [
            { x: 56,  w: 152 },
            { x: 210, w: 150 },
            { x: 368, w: 143 },
            { x: 525, w: 140 },
            { x: 666, w: 153 },
            { x: 823, w: 150 },
        ]
    },
    JEU: {
        sy: 201, sh: 125, speed: 100,
        frames: [
            { x: 79,  w: 161 },
            { x: 240, w: 181 },
            { x: 421, w: 156 },
            { x: 608, w: 208 },
            { x: 816, w: 170 },
        ]
    },
    MANGER: {
        sy: 411, sh: 97, speed: 200,
        frames: [
            { x: 56,  w: 159 },
            { x: 215, w: 170 },
            { x: 385, w: 171 },
            { x: 556, w: 149 },
            { x: 705, w: 152 },
            { x: 857, w: 162 },
        ]
    },
    EXCITE: {
        sy: 594, sh: 131, speed: 80,
        frames: [
            { x: 57,  w: 176 },
            { x: 233, w: 175 },
            { x: 408, w: 185 },
            { x: 593, w: 201 },
            { x: 794, w: 182 },
        ]
    },
    TRISTE: {
        sy: 808, sh: 104, speed: 300,
        frames: [
            { x: 55,  w: 174 },
            { x: 229, w: 193 },
            { x: 422, w: 167 },
            { x: 589, w: 195 },
            { x: 784, w: 188 },
        ]
    },
    ENDORMI: {
        sy: 1008, sh: 103, speed: 500,
        frames: [
            { x: 48,  w: 173 },
            { x: 221, w: 180 },
            { x: 401, w: 205 },
            { x: 606, w: 183 },
            { x: 789, w: 202 },
        ]
    },
}

const SCALE = 2

const currentConfig = computed(() => stateConfig[props.state] ?? stateConfig['REPOS'])
const maxFrameW     = computed(() => Math.max(...currentConfig.value.frames.map(f => f.w)))
const displayW      = computed(() => maxFrameW.value * SCALE)
const displayH      = computed(() => currentConfig.value.sh * SCALE)

const canvas = ref<HTMLCanvasElement | null>(null)
const img    = new Image()
img.src      = foxSpriteUrl

let frameIdx = 0
let rafId    = 0
let lastTime = 0

function draw(timestamp: number) {
    const config = currentConfig.value
    const ctx    = canvas.value?.getContext('2d')
    if (!ctx) { rafId = requestAnimationFrame(draw); return }

    if (timestamp - lastTime > config.speed) {
        frameIdx = (frameIdx + 1) % config.frames.length
        lastTime = timestamp
    }

    const f  = config.frames[frameIdx]
    const dw = f.w * SCALE
    const dh = config.sh * SCALE
    const dx = 0
    ctx.clearRect(0, 0, displayW.value, displayH.value)
    ctx.drawImage(img, f.x, config.sy, f.w, config.sh, dx, 0, dw, dh)

    rafId = requestAnimationFrame(draw)
}

function startAnimation() {
    cancelAnimationFrame(rafId)
    frameIdx = 0
    lastTime = 0
    const start = () => { rafId = requestAnimationFrame(draw) }
    if (img.complete && img.naturalWidth > 0) start()
    else img.onload = start
}

onMounted(startAnimation)
onUnmounted(() => cancelAnimationFrame(rafId))
watch(() => props.state, () => {
    frameIdx = 0
    cancelAnimationFrame(rafId)
    rafId = requestAnimationFrame(draw)
})
</script>