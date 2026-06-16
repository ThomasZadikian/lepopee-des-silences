<script setup lang="ts">

import { computed } from 'vue';

const props = withDefaults(defineProps<{
  seed?:      string
  roomName?:  string
  depth?:     number
  maxDepth?:  number
  activeLaws?: number
  score?:     string
  phase?:     string
  status?:    string
}>(), {
  seed:       'SIL-7F3A-29D',
  roomName:   'Galerie des Aveux',
  depth:      4,
  maxDepth:   10,
  activeLaws: 3,
  score:      '12 480',
  phase:      'EXPLORATION',
  status:     'ACTIVE',
})

const depthLabel  = computed(() =>
  `${String(props.depth).padStart(2, '0')} / ${String(props.maxDepth).padStart(2, '0')}`)
const lawsLabel   = computed(() => String(props.activeLaws).padStart(2, '0'))
const statusColor = computed(() =>
  props.status === 'ÉCHEC' ? 'var(--blood)' : 'var(--frost)')
</script>

<template>
  <header class="es-runbar" style="position: relative; z-index: 8">
    <!-- Palais -->
    <div class="es-seg" style="padding-left: 30px">
      <span class="es-seg__k">Palais</span>
      <span class="es-seg__v" style="letter-spacing: 0.04em; font-weight: 600">
        L'ÉPOPÉE DES SILENCES
      </span>
    </div>

    <!-- Salle -->
    <div class="es-seg">
      <span class="es-seg__k">Salle</span>
      <span class="es-seg__v">{{ roomName }}</span>
    </div>

    <!-- Seed -->
    <div class="es-seg">
      <span class="es-seg__k">Seed</span>
      <span class="es-seg__v es-gold">{{ seed }}</span>
    </div>

    <!-- Profondeur -->
    <div class="es-seg">
      <span class="es-seg__k">Profondeur</span>
      <span class="es-seg__v">{{ depthLabel }}</span>
    </div>

    <!-- Lois -->
    <div class="es-seg">
      <span class="es-seg__k">Lois actives</span>
      <span class="es-seg__v">{{ lawsLabel }}</span>
    </div>

    <!-- spacer -->
    <div class="es-seg es-seg--grow" />

    <!-- Score -->
    <div class="es-seg" style="align-items: flex-end">
      <span class="es-seg__k">Score projeté</span>
      <span class="es-seg__v">{{ score }}</span>
    </div>

    <!-- Phase -->
    <div class="es-seg" style="align-items: flex-end">
      <span class="es-seg__k">Phase</span>
      <span class="es-seg__v es-gold">● {{ phase }}</span>
    </div>

    <!-- État -->
    <div class="es-seg" style="padding-right: 30px; border-right: none; align-items: flex-end">
      <span class="es-seg__k">État</span>
      <span class="es-seg__v" :style="{ color: statusColor }">● {{ status }}</span>
    </div>

    <!-- Slot pour actions supplémentaires (boutons Sauvegarder, Lois, etc.) -->
    <slot />
  </header>
</template>
