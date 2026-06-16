<script setup lang="ts">
import { ref } from 'vue';
import type { ActivePalaceLawDto } from '../runs/types/runTypes';

defineProps<{ laws?: ActivePalaceLawDto[] }>();

const openIndex = ref<number | null>(null);

function toggle(i: number) {
  openIndex.value = openIndex.value === i ? null : i;
}

function domainTone(domain: string): string {
  const d = domain?.toLowerCase() ?? '';
  if (d.includes('combat') || d.includes('confron')) return 'blood';
  if (d.includes('mem') || d.includes('récit') || d.includes('recit') || d.includes('narr')) return 'frost';
  if (d.includes('loi') || d.includes('édit') || d.includes('edit')) return 'gold';
  return '';
}
</script>

<template>
  <div class="plp-root es-panel">
    <!-- En-tête stèle -->
    <header class="plp-head">
      <span class="es-kicker plp-kicker">
        <!-- Loi sigil inline -->
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="M12 3L4 7v5c0 5 3.5 9.74 8 11 4.5-1.26 8-6 8-11V7l-8-4z"/>
          <line x1="12" y1="9" x2="12" y2="15"/>
          <line x1="9" y1="12" x2="15" y2="12"/>
        </svg>
        Registre du Palais
      </span>
      <h2 class="es-h3 plp-title">Lois en vigueur</h2>
      <p class="es-body plp-subtitle" v-if="laws && laws.length">
        {{ laws.length }} loi{{ laws.length > 1 ? 's' : '' }} régissent ta descente.
      </p>
      <p class="es-body plp-subtitle" v-else>Aucune loi active pour l'instant.</p>
    </header>

    <div class="es-divider plp-divider" />

    <!-- Corps : accordéon de lois -->
    <div class="plp-list" v-if="laws && laws.length">
      <article
        v-for="(law, i) in laws"
        :key="law.key"
        class="plp-art"
        :class="{ 'plp-art--open': openIndex === i }"
      >
        <!-- Ligne cliquable -->
        <button class="plp-art__hd" @click="toggle(i)" :aria-expanded="openIndex === i">
          <!-- Chevron -->
          <svg class="plp-chevron" :class="{ 'plp-chevron--open': openIndex === i }"
            width="14" height="14" viewBox="0 0 14 14" fill="none"
            stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <path d="M3 5l4 4 4-4"/>
          </svg>

          <div class="plp-art__meta">
            <div class="plp-art__top">
              <span class="es-mono plp-art__idx">{{ String(i + 1).padStart(2, '0') }}</span>
              <span class="plp-art__name">{{ law.displayName }}</span>
              <span
                v-if="law.domain"
                class="es-chip"
                :class="{
                  'es-chip--gold':  domainTone(law.domain) === 'gold',
                  'es-chip--frost': domainTone(law.domain) === 'frost',
                  'es-chip--blood': domainTone(law.domain) === 'blood',
                }"
              >{{ law.domain }}</span>
            </div>
          </div>

          <!-- Version badge -->
          <span class="es-mono plp-art__ver">{{ law.version }}</span>
        </button>

        <!-- Corps expansible -->
        <div class="plp-art__body" :class="{ 'plp-art__body--open': openIndex === i }">
          <p class="plp-art__desc es-body">{{ law.description }}</p>
        </div>
      </article>
    </div>

    <!-- Vide -->
    <div v-else class="plp-empty">
      <svg width="38" height="38" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" style="color:var(--ink-4); margin-bottom:10px;">
        <path d="M12 3L4 7v5c0 5 3.5 9.74 8 11 4.5-1.26 8-6 8-11V7l-8-4z"/>
      </svg>
      <span class="es-label" style="color:var(--ink-4)">Le Palais n'a encore édicté aucune loi.</span>
    </div>
  </div>
</template>

<style scoped>
/* ── Racine ── */
.plp-root {
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: hidden;
  border: 1px solid var(--line);
  border-radius: 6px;
  background: linear-gradient(180deg, oklch(.28 .034 268 / .55), oklch(.22 .025 268 / .45));
}

/* ── En-tête ── */
.plp-head {
  padding: 24px 26px 18px;
  flex: 0 0 auto;
}

.plp-kicker {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  color: var(--gold-dim, oklch(.65 .09 84 / .7));
  margin-bottom: 8px;
}

.plp-title {
  font-size: 26px;
  line-height: 1.08;
  margin: 0 0 6px;
}

.plp-subtitle {
  font-size: 13px;
  color: var(--ink-3);
  margin: 0;
}

.plp-divider {
  flex: 0 0 auto;
  margin: 0 26px 6px;
}

/* ── Liste ── */
.plp-list {
  flex: 1;
  overflow-y: auto;
  padding: 6px 18px 18px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

/* ── Article ── */
.plp-art {
  border: 1px solid var(--line);
  border-radius: 5px;
  background: linear-gradient(180deg, oklch(.30 .034 268 / .40), oklch(.24 .028 268 / .32));
  overflow: hidden;
  transition: border-color .18s;
}

.plp-art:hover,
.plp-art--open {
  border-color: var(--line-strong);
}

.plp-art--open {
  border-color: oklch(.55 .08 84 / .45);
}

/* En-tête cliquable de l'article */
.plp-art__hd {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 13px 14px;
  background: none;
  border: none;
  cursor: pointer;
  text-align: left;
  color: inherit;
}

.plp-art__hd:focus-visible {
  outline: 2px solid var(--frost);
  outline-offset: -2px;
  border-radius: 4px;
}

/* Chevron rotatif */
.plp-chevron {
  flex: 0 0 auto;
  color: var(--ink-4);
  transition: transform .22s cubic-bezier(.4, 0, .2, 1), color .18s;
}

.plp-chevron--open {
  transform: rotate(-180deg);
  color: var(--gold);
}

/* Méta (titre + domaine) */
.plp-art__meta {
  flex: 1;
  min-width: 0;
}

.plp-art__top {
  display: flex;
  align-items: baseline;
  gap: 9px;
  flex-wrap: wrap;
}

.plp-art__idx {
  font-size: 10.5px;
  color: var(--ink-4);
  flex: 0 0 auto;
}

.plp-art__name {
  font-family: var(--font-display, var(--display));
  font-size: 16.5px;
  font-weight: 600;
  color: var(--gold);
  line-height: 1.2;
}

.plp-art__ver {
  flex: 0 0 auto;
  font-size: 10px;
  color: var(--ink-4);
  margin-left: auto;
}

/* Corps dépliable */
.plp-art__body {
  max-height: 0;
  overflow: hidden;
  opacity: 0;
  transition: max-height .28s cubic-bezier(.4, 0, .2, 1), opacity .22s;
}

.plp-art__body--open {
  max-height: 300px;
  opacity: 1;
}

.plp-art__desc {
  font-size: 13px;
  line-height: 1.6;
  color: var(--ink-3);
  border-top: 1px dashed var(--line);
  padding: 11px 14px 14px;
  margin: 0;
}

/* ── État vide ── */
.plp-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  gap: 4px;
}
</style>
