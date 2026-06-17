<script setup lang="ts">
import type { ActivePalaceLawDto } from '../runs/types/runTypes';
defineProps<{ laws?: ActivePalaceLawDto[] }>()

function domainTone(domain: string): 'blood' | 'frost' | 'gold' | '' {
  const d = domain?.toLowerCase() ?? ''
  if (d.includes('combat') || d.includes('confron')) return 'blood'
  if (d.includes('mem') || d.includes('récit') || d.includes('recit') || d.includes('narr')) return 'frost'
  if (d.includes('loi') || d.includes('édit') || d.includes('edit')) return 'gold'
  return ''
}
</script>

<template>
  <div class="plp-root">
    <!-- Header -->
    <header class="plp-head">
      <div class="es-row" style="gap: 8px; align-items: center;">
        <!-- Shield SVG — gold, loi sigil -->
        <svg
          width="14"
          height="14"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
          aria-hidden="true"
          style="color: var(--gold); flex: 0 0 auto;"
        >
          <path d="M12 3L4 7v5c0 5 3.5 9.74 8 11 4.5-1.26 8-6 8-11V7l-8-4z"/>
          <line x1="12" y1="9" x2="12" y2="15"/>
          <line x1="9" y1="12" x2="15" y2="12"/>
        </svg>
        <span class="es-kicker" style="color: oklch(.65 .09 84 / .7);">Registre du Palais</span>
      </div>
      <h2 style="font-family: var(--display); font-size: 24px; color: var(--gold); margin: 6px 0 0;">
        Lois en vigueur
      </h2>
      <p
        v-if="laws?.length"
        class="es-body"
        style="font-size: 13px; color: var(--ink-3); margin: 4px 0 0;"
      >
        {{ laws.length }} loi{{ laws.length > 1 ? 's' : '' }} régissent ta descente.
      </p>
      <p v-else class="es-body" style="font-size: 13px; color: var(--ink-3); margin: 4px 0 0;">
        Aucune loi active.
      </p>
    </header>

    <!-- Divider -->
    <div class="plp-divider" />

    <!-- Empty state -->
    <div v-if="!laws?.length" class="plp-empty">
      <svg
        width="38"
        height="38"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="1"
        stroke-linecap="round"
        stroke-linejoin="round"
        aria-hidden="true"
        style="color: var(--ink-4);"
      >
        <path d="M12 3L4 7v5c0 5 3.5 9.74 8 11 4.5-1.26 8-6 8-11V7l-8-4z"/>
      </svg>
      <span class="es-label" style="color: var(--ink-4);">Le Palais n'a encore édicté aucune loi.</span>
    </div>

    <!-- Law list (flat, no accordion) -->
    <div v-else class="plp-list">
      <div
        v-for="law in laws"
        :key="law.key"
        class="plp-law"
      >
        <!-- Left: shield icon colored by domain tone -->
        <svg
          width="14"
          height="14"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
          aria-hidden="true"
          :style="{
            color: domainTone(law.domain ?? '') === 'gold'  ? 'var(--gold)'
                 : domainTone(law.domain ?? '') === 'blood' ? 'var(--blood)'
                 : domainTone(law.domain ?? '') === 'frost' ? 'var(--frost)'
                 : 'var(--gold-dim, oklch(.65 .09 84 / .6))',
            flex: '0 0 auto',
            marginTop: '3px',
          }"
        >
          <path d="M12 3L4 7v5c0 5 3.5 9.74 8 11 4.5-1.26 8-6 8-11V7l-8-4z"/>
        </svg>

        <!-- Right: body -->
        <div class="plp-law__body">
          <!-- Name + version row -->
          <div style="display: flex; justify-content: space-between; align-items: baseline; gap: 8px; margin-bottom: 5px;">
            <span style="font-family: var(--display); font-size: 16px; font-weight: 600; color: var(--gold);">
              {{ law.displayName }}
            </span>
            <span class="es-mono" style="font-size: 10.5px; color: var(--ink-4); flex: 0 0 auto;">
              {{ law.version }}
            </span>
          </div>

          <!-- Domain chip -->
          <div v-if="law.domain" style="margin-bottom: 6px;">
            <span
              class="es-chip"
              :class="{
                'es-chip--gold':  domainTone(law.domain) === 'gold',
                'es-chip--frost': domainTone(law.domain) === 'frost',
                'es-chip--blood': domainTone(law.domain) === 'blood',
              }"
            >{{ law.domain }}</span>
          </div>

          <!-- Description -->
          <p class="es-body" style="font-size: 12.5px; line-height: 1.5; color: var(--ink-3); margin: 0;">
            {{ law.description }}
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.plp-root {
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: hidden;
  border: 1px solid var(--line);
  border-radius: 6px;
  background: linear-gradient(180deg, oklch(.28 .034 268 / .55), oklch(.22 .025 268 / .45));
}

.plp-head {
  padding: 20px 22px 14px;
  flex: 0 0 auto;
}

.plp-divider {
  flex: 0 0 auto;
  height: 1px;
  background: linear-gradient(90deg, transparent, var(--line), transparent);
  margin: 0 22px 6px;
}

.plp-list {
  flex: 1;
  overflow-y: auto;
  padding: 6px 14px 14px;
  display: flex;
  flex-direction: column;
}

.plp-law {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 14px 0;
  border-bottom: 1px solid oklch(.32 .022 268 / .5);
}

.plp-law:last-child {
  border-bottom: none;
}

.plp-law__body {
  flex: 1;
  min-width: 0;
}

.plp-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  gap: 4px;
}
</style>
