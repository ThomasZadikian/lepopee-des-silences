<script setup lang="ts">
import type { ActivePalaceLawDto } from '../runs/types/runTypes';
defineProps<{ laws?: ActivePalaceLawDto[] | null }>()
const emit = defineEmits<{ close: [] }>()

function domainTone(domain: string): 'blood' | 'frost' | 'gold' | '' {
  const d = domain?.toLowerCase() ?? ''
  if (d.includes('combat') || d.includes('confron')) return 'blood'
  if (d.includes('mem') || d.includes('récit') || d.includes('recit') || d.includes('narr')) return 'frost'
  if (d.includes('loi') || d.includes('édit') || d.includes('edit')) return 'gold'
  return ''
}
</script>

<template>
  <div
    class="lp-root"
    role="dialog"
    aria-modal="true"
    aria-label="Lois du Palais"
    tabindex="-1"
    @keydown.escape="emit('close')"
  >
    <!-- Header -->
    <header class="lp-head">
      <div class="lp-head__left">
        <!-- Shield SVG 16x16 gold -->
        <svg
          width="16"
          height="16"
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
        <div>
          <span class="es-kicker" style="color: oklch(.65 .09 84 / .7); display: block; margin-bottom: 3px;">
            Registre du Palais
          </span>
          <h3 style="font-size: 22px; font-family: var(--display); color: var(--ink); margin: 0;">
            Lois du Palais
          </h3>
        </div>
      </div>
      <!-- Close button -->
      <button class="lp-close" @click="emit('close')" aria-label="Fermer">
        <svg
          width="16"
          height="16"
          viewBox="0 0 16 16"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          aria-hidden="true"
        >
          <line x1="3" y1="3" x2="13" y2="13"/>
          <line x1="13" y1="3" x2="3" y2="13"/>
        </svg>
      </button>
    </header>

    <!-- Divider -->
    <div class="lp-divider" />

    <!-- Law list -->
    <div v-if="laws && laws.length" class="lp-list">
      <div
        v-for="law in laws"
        :key="law.key"
        class="lp-law"
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
            marginTop: '2px',
          }"
        >
          <path d="M12 3L4 7v5c0 5 3.5 9.74 8 11 4.5-1.26 8-6 8-11V7l-8-4z"/>
        </svg>

        <!-- Right: body -->
        <div class="lp-law__body">
          <!-- Name + version row -->
          <div style="display: flex; justify-content: space-between; align-items: baseline; gap: 8px; margin-bottom: 6px;">
            <span style="font-family: var(--display); font-size: 15.5px; font-weight: 600; color: var(--gold);">
              {{ law.displayName }}
            </span>
            <span class="es-mono" style="font-size: 10px; color: var(--ink-4); flex: 0 0 auto;">
              {{ law.version }}
            </span>
          </div>

          <!-- Domain chip row -->
          <div v-if="law.domain" style="margin-bottom: 7px;">
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
          <p class="es-body" style="font-size: 12.5px; color: var(--ink-3); margin: 0;">
            {{ law.description }}
          </p>
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div v-else class="lp-empty">
      <svg
        width="32"
        height="32"
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
      <span class="es-label" style="color: var(--ink-4);">Aucune loi active.</span>
    </div>

    <!-- Footer -->
    <footer class="lp-foot">
      <button
        class="es-btn es-btn--ghost"
        style="font-size: 12px; padding: 8px 18px;"
        @click="emit('close')"
      >
        Fermer
      </button>
    </footer>
  </div>
</template>

<style scoped>
.lp-root {
  position: absolute;
  top: 0;
  right: 0;
  height: 100%;
  width: 400px;
  display: flex;
  flex-direction: column;
  z-index: 40;
  background: oklch(.20 .028 268 / .82);
  backdrop-filter: blur(18px) saturate(1.4);
  -webkit-backdrop-filter: blur(18px) saturate(1.4);
  border-left: 1px solid var(--frost, oklch(.70 .07 232));
  outline: none;
}

/* Frost pseudo-corners */
.lp-root::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 24px;
  height: 24px;
  border-top: 1px solid var(--frost, oklch(.70 .07 232));
  border-left: 1px solid var(--frost, oklch(.70 .07 232));
  opacity: .5;
  pointer-events: none;
}

.lp-root::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 0;
  width: 24px;
  height: 24px;
  border-bottom: 1px solid var(--frost, oklch(.70 .07 232));
  border-left: 1px solid var(--frost, oklch(.70 .07 232));
  opacity: .5;
  pointer-events: none;
}

.lp-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  padding: 22px 22px 14px;
  flex: 0 0 auto;
}

.lp-head__left {
  display: flex;
  align-items: flex-start;
  gap: 10px;
}

.lp-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  border-radius: 4px;
  border: 1px solid var(--line);
  background: oklch(.26 .02 268 / .6);
  color: var(--ink-3);
  cursor: pointer;
  flex: 0 0 auto;
  transition: border-color .15s, color .15s, background .15s;
}

.lp-close:hover {
  border-color: var(--line-strong);
  color: var(--ink);
  background: oklch(.30 .024 268 / .7);
}

.lp-close:focus-visible {
  outline: 2px solid var(--frost);
  outline-offset: 2px;
}

.lp-divider {
  flex: 0 0 auto;
  height: 1px;
  background: var(--line);
  margin: 0 22px;
}

.lp-list {
  flex: 1;
  overflow-y: auto;
  padding: 10px 22px 0;
  display: flex;
  flex-direction: column;
}

.lp-law {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 14px 0;
  border-bottom: 1px solid var(--line-soft, oklch(.32 .022 268 / .5));
}

.lp-law:last-child {
  border-bottom: none;
}

.lp-law__body {
  flex: 1;
  min-width: 0;
}

.lp-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  gap: 4px;
}

.lp-foot {
  flex: 0 0 auto;
  padding: 14px 22px 18px;
  border-top: 1px solid var(--line-soft, oklch(.32 .022 268 / .5));
  display: flex;
  justify-content: flex-end;
}
</style>
