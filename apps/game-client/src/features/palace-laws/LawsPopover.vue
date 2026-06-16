<script setup lang="ts">
import type { ActivePalaceLawDto } from '../runs/types/runTypes';

defineProps<{ laws?: ActivePalaceLawDto[] | null }>();
const emit = defineEmits<{ close: [] }>();

function domainTone(domain: string): string {
  const d = domain?.toLowerCase() ?? '';
  if (d.includes('combat') || d.includes('confron')) return 'blood';
  if (d.includes('mem') || d.includes('récit') || d.includes('recit') || d.includes('narr')) return 'frost';
  if (d.includes('loi') || d.includes('édit') || d.includes('edit')) return 'gold';
  return '';
}
</script>

<template>
  <div
    class="lp-root"
    role="dialog"
    aria-modal="true"
    aria-label="Lois du Palais"
    @keydown.escape="emit('close')"
    tabindex="-1"
  >
    <!-- En-tête -->
    <header class="lp-head">
      <div class="lp-head__left">
        <!-- Loi sigil inline -->
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
          stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"
          aria-hidden="true" style="color:var(--gold); flex:0 0 auto;">
          <path d="M12 3L4 7v5c0 5 3.5 9.74 8 11 4.5-1.26 8-6 8-11V7l-8-4z"/>
          <line x1="12" y1="9" x2="12" y2="15"/>
          <line x1="9" y1="12" x2="15" y2="12"/>
        </svg>
        <div>
          <span class="es-kicker lp-kicker">Registre du Palais</span>
          <h3 class="es-h3 lp-title">Lois du Palais</h3>
        </div>
      </div>
      <button class="lp-close" @click="emit('close')" aria-label="Fermer">
        <!-- ✕ inline SVG -->
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none"
          stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true">
          <line x1="3" y1="3" x2="13" y2="13"/>
          <line x1="13" y1="3" x2="3" y2="13"/>
        </svg>
      </button>
    </header>

    <div class="es-divider lp-divider" />

    <!-- Liste de lois -->
    <div class="lp-list" v-if="laws && laws.length">
      <div
        v-for="(law, i) in laws"
        :key="law.key"
        class="lp-law"
        :class="{ 'lp-law--last': i === (laws?.length ?? 0) - 1 }"
      >
        <!-- Icône loi -->
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
          stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"
          aria-hidden="true" style="color:var(--gold-dim, oklch(.65 .09 84 / .6)); flex:0 0 auto; margin-top:2px;">
          <path d="M12 3L4 7v5c0 5 3.5 9.74 8 11 4.5-1.26 8-6 8-11V7l-8-4z"/>
        </svg>

        <div class="lp-law__body">
          <!-- Nom + version -->
          <div class="lp-law__top">
            <span class="lp-law__name">{{ law.displayName }}</span>
            <span class="es-mono lp-law__ver">{{ law.version }}</span>
          </div>

          <!-- Domaine -->
          <div class="lp-law__chips" v-if="law.domain">
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
          <p class="lp-law__desc es-body" v-if="law.description">{{ law.description }}</p>
        </div>
      </div>
    </div>

    <!-- Vide -->
    <div class="lp-empty" v-else>
      <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1"
        stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" style="color:var(--ink-4); margin-bottom:8px;">
        <path d="M12 3L4 7v5c0 5 3.5 9.74 8 11 4.5-1.26 8-6 8-11V7l-8-4z"/>
      </svg>
      <span class="es-label" style="color:var(--ink-4)">Aucune loi active.</span>
    </div>

    <!-- Pied de panneau -->
    <footer class="lp-foot">
      <button class="es-btn es-btn--ghost lp-foot__btn" @click="emit('close')">
        Fermer
      </button>
    </footer>
  </div>
</template>

<style scoped>
/* ── Panneau latéral ── */
.lp-root {
  position: absolute;
  top: 0;
  right: 0;
  height: 100%;
  width: 380px;
  display: flex;
  flex-direction: column;
  z-index: 40;

  background: oklch(.20 .028 268 / .82);
  backdrop-filter: blur(18px) saturate(1.4);
  -webkit-backdrop-filter: blur(18px) saturate(1.4);

  border-left: 1px solid var(--line-strong, oklch(.38 .03 268 / .7));
  border-top: 1px solid oklch(.55 .08 84 / .25);

  /* Encadrement frost haut-gauche */
  box-shadow:
    -6px 0 60px -10px oklch(.2 .03 268 / .9),
    inset 1px 0 0 oklch(.6 .07 232 / .08);

  outline: none;
}

/* Coin frost haut-gauche */
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

/* Coin frost bas-gauche */
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

/* ── En-tête ── */
.lp-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  padding: 22px 20px 16px;
  flex: 0 0 auto;
}

.lp-head__left {
  display: flex;
  align-items: flex-start;
  gap: 11px;
}

.lp-kicker {
  display: block;
  color: var(--gold-dim, oklch(.65 .09 84 / .7));
  margin-bottom: 4px;
}

.lp-title {
  font-size: 22px;
  line-height: 1.1;
  margin: 0;
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
  margin: 0 20px 0;
}

/* ── Liste ── */
.lp-list {
  flex: 1;
  overflow-y: auto;
  padding: 10px 20px 0;
  display: flex;
  flex-direction: column;
}

/* ── Loi individuelle ── */
.lp-law {
  display: flex;
  align-items: flex-start;
  gap: 11px;
  padding: 14px 0;
  border-bottom: 1px solid var(--line-soft, oklch(.32 .022 268 / .5));
}

.lp-law--last {
  border-bottom: none;
}

.lp-law__body {
  flex: 1;
  min-width: 0;
}

.lp-law__top {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 6px;
}

.lp-law__name {
  font-family: var(--font-display, var(--display));
  font-size: 15.5px;
  font-weight: 600;
  color: var(--gold);
  line-height: 1.2;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.lp-law__ver {
  font-size: 10px;
  color: var(--ink-4);
  flex: 0 0 auto;
}

.lp-law__chips {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
  margin-bottom: 7px;
}

.lp-law__desc {
  font-size: 12.5px;
  line-height: 1.5;
  color: var(--ink-3);
  margin: 0;
}

/* ── État vide ── */
.lp-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  gap: 4px;
}

/* ── Pied ── */
.lp-foot {
  flex: 0 0 auto;
  padding: 14px 20px 18px;
  border-top: 1px solid var(--line-soft, oklch(.32 .022 268 / .5));
  display: flex;
  justify-content: flex-end;
}

.lp-foot__btn {
  font-size: 12px;
  padding: 8px 18px;
}
</style>
