<script setup lang="ts">
import type { CombatantRuntimeDto } from '../../combat/types/combatContracts';
import type { ActiveCurseDto, ActivePalaceLawDto, RunItemDto, RunModifierDto } from '../types/runTypes';

defineProps<{
  allies: CombatantRuntimeDto[] | null;
  modifiers: RunModifierDto[] | null;
  laws: ActivePalaceLawDto[] | null;
  curses: ActiveCurseDto[] | null;
  items: RunItemDto[] | null;
}>();

defineEmits<{ close: [] }>();

function vitalityPct(c: CombatantRuntimeDto): number {
  if (!c.maxVitality) return 0;
  return Math.max(0, Math.min(100, (c.currentVitality / c.maxVitality) * 100));
}

function vitalityColor(pct: number): string {
  if (pct <= 25) return 'var(--blood)';
  if (pct <= 50) return 'var(--gold)';
  return 'var(--frost)';
}

function modifierLabel(mod: RunModifierDto): string {
  const label = mod.type.replace(/([A-Z])/g, ' $1').trim();
  const sign = mod.value >= 0 ? '+' : '';
  return `${label} ${sign}${mod.value}`
}

function rarityTone(rarity: string): string {
  const r = rarity.toLowerCase();
  if (r.includes('epic') || r.includes('épic') || r.includes('relique')) return 'es-chip--gold';
  if (r.includes('rare')) return 'es-chip--frost';
  return '';
}
</script>

<template>
  <aside class="party-drawer">
    <!-- Header -->
    <header class="party-drawer__header">
      <div class="party-drawer__title">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/>
          <path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>
        </svg>
        <span class="es-label">Équipe</span>
      </div>
      <button class="party-drawer__close" @click="$emit('close')" aria-label="Fermer">
        <svg width="14" height="14" viewBox="0 0 14 14" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round">
          <path d="M3 3l8 8M11 3l-8 8"/>
        </svg>
      </button>
    </header>

    <div class="party-drawer__body">

      <!-- ── Alliés ── -->
      <section class="party-drawer__section">
        <h4 class="party-drawer__section-title">Alliés</h4>

        <template v-if="allies && allies.length">
          <article
            v-for="ally in allies"
            :key="ally.id"
            class="party-card"
            :class="{ 'party-card--defeated': ally.status === 'Defeated' }"
          >
            <div class="party-card__top">
              <div class="party-card__name">
                <span class="party-card__displayname">{{ ally.displayName }}</span>
                <span
                  v-if="ally.status === 'Defeated'"
                  class="es-chip es-chip--blood"
                  style="font-size: 9px; padding: 1px 6px;"
                >KO</span>
                <span v-else class="es-chip" style="font-size: 9px; padding: 1px 6px;">{{ ally.archetype }}</span>
              </div>

              <div class="party-card__stats">
                <span class="party-card__stat">
                  <span class="party-card__stat-k">VIT</span>
                  <span class="party-card__stat-v" :style="{ color: vitalityColor(vitalityPct(ally)) }">
                    {{ ally.currentVitality }} / {{ ally.maxVitality }}
                  </span>
                </span>
                <span v-if="ally.guard > 0" class="party-card__stat">
                  <span class="party-card__stat-k">GARDE</span>
                  <span class="party-card__stat-v" style="color: var(--frost)">{{ ally.guard }}</span>
                </span>
                <span v-if="ally.mana > 0" class="party-card__stat">
                  <span class="party-card__stat-k">MANA</span>
                  <span class="party-card__stat-v">{{ ally.mana }}</span>
                </span>
                <span v-if="ally.charge > 0" class="party-card__stat">
                  <span class="party-card__stat-k">CHARGE</span>
                  <span class="party-card__stat-v">{{ ally.charge }}</span>
                </span>
              </div>
            </div>

            <!-- Vitality bar -->
            <div class="party-card__bar-track">
              <div
                class="party-card__bar-fill"
                :style="{
                  width: vitalityPct(ally) + '%',
                  background: vitalityColor(vitalityPct(ally)),
                }"
              />
            </div>

            <!-- Skills -->
            <div v-if="ally.skills?.length" class="party-card__skills">
              <span
                v-for="skill in ally.skills"
                :key="skill.key"
                class="party-card__skill"
                :title="`${skill.displayName} · ${skill.skillType} · ${skill.targetingType}`"
              >
                {{ skill.displayName }}
              </span>
            </div>
          </article>
        </template>

        <p v-else class="party-drawer__empty">
          Données disponibles en combat uniquement.
        </p>
      </section>

      <!-- ── Modificateurs actifs ── -->
      <section v-if="modifiers && modifiers.length" class="party-drawer__section">
        <h4 class="party-drawer__section-title">Modificateurs actifs</h4>
        <ul class="party-drawer__list">
          <li v-for="mod in modifiers" :key="mod.id" class="party-drawer__mod">
            <span class="party-drawer__mod-label">{{ modifierLabel(mod) }}</span>
            <span class="es-label" style="color: var(--ink-4);">
              {{ mod.duration }} · {{ mod.sourceType }}
            </span>
          </li>
        </ul>
      </section>

      <!-- ── Malédictions actives ── -->
      <section v-if="curses && curses.length" class="party-drawer__section">
        <h4 class="party-drawer__section-title">Malédictions</h4>
        <ul class="party-drawer__list">
          <li v-for="curse in curses" :key="curse.id" class="party-drawer__curse">
            <div class="party-drawer__curse-name">
              <span class="es-chip es-chip--blood" style="font-size: 9px; padding: 1px 6px;">
                {{ curse.severity ?? 'Curse' }}
              </span>
              <span>{{ curse.displayName ?? curse.curseDefinitionKey }}</span>
            </div>
            <p v-if="curse.description" class="es-body party-drawer__curse-desc">
              {{ curse.description }}
            </p>
          </li>
        </ul>
      </section>

      <!-- ── Lois actives (résumé) ── -->
      <section v-if="laws && laws.length" class="party-drawer__section">
        <h4 class="party-drawer__section-title">Lois du Palais</h4>
        <ul class="party-drawer__list">
          <li v-for="law in laws" :key="law.key" class="party-drawer__law">
            <span class="es-chip es-chip--gold" style="font-size: 9px; padding: 1px 6px; flex-shrink: 0;">{{ law.domain }}</span>
            <span class="party-drawer__law-name">{{ law.displayName }}</span>
          </li>
        </ul>
      </section>

      <!-- ── Objets de run ── -->
      <section v-if="items && items.length" class="party-drawer__section">
        <h4 class="party-drawer__section-title">Objets de run</h4>
        <ul class="party-drawer__list">
          <li v-for="item in items" :key="item.id" class="party-drawer__item">
            <div class="party-drawer__item-top">
              <span :class="['es-chip', rarityTone(item.rarity)]" style="font-size: 9px; padding: 1px 6px; flex-shrink: 0;">
                {{ item.rarity }}
              </span>
              <span class="party-drawer__item-name">{{ item.displayName }}</span>
              <span v-if="item.quantity > 1" class="party-drawer__item-qty">×{{ item.quantity }}</span>
            </div>
            <p v-if="item.description" class="es-body party-drawer__item-desc">{{ item.description }}</p>
          </li>
        </ul>
      </section>

      <p v-if="!allies && !modifiers?.length && !laws?.length && !items?.length" class="party-drawer__empty">
        Aucune donnée d'équipe disponible.
      </p>
    </div>
  </aside>
</template>

<style scoped>
.party-drawer {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  width: 360px;
  display: flex;
  flex-direction: column;
  background: var(--panel, oklch(0.20 0.025 270));
  border-left: 1px solid var(--line-soft);
  z-index: var(--z-panel);
  overflow: hidden;
}

.party-drawer__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid var(--line-soft);
  flex-shrink: 0;
}

.party-drawer__title {
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--ink-3);
}

.party-drawer__close {
  background: none;
  border: none;
  cursor: pointer;
  color: var(--ink-4);
  display: flex;
  align-items: center;
  padding: 4px;
  border-radius: 3px;
  transition: color .15s;
}
.party-drawer__close:hover { color: var(--ink); }

.party-drawer__body {
  flex: 1;
  overflow-y: auto;
  padding: 16px 20px;
  display: flex;
  flex-direction: column;
  gap: 20px;
}

/* ── Sections ── */
.party-drawer__section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.party-drawer__section-title {
  font-family: var(--caps, var(--font));
  font-size: 9.5px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
  padding-bottom: 6px;
  border-bottom: 1px solid var(--line-soft);
}

.party-drawer__empty {
  font-size: 12px;
  color: var(--ink-4);
  font-style: italic;
}

.party-drawer__list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

/* ── Party card (ally) ── */
.party-card {
  padding: 10px 12px;
  border: 1px solid var(--line-soft);
  border-radius: 4px;
  background: oklch(0.24 0.015 283 / 0.4);
  display: flex;
  flex-direction: column;
  gap: 6px;
  transition: opacity .2s;
}

.party-card--defeated {
  opacity: 0.5;
}

.party-card__top {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.party-card__name {
  display: flex;
  align-items: center;
  gap: 6px;
}

.party-card__displayname {
  font-size: 13px;
  font-weight: 600;
  color: var(--ink-2);
  flex: 1;
}

.party-card__stats {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.party-card__stat {
  display: flex;
  gap: 4px;
  align-items: baseline;
}

.party-card__stat-k {
  font-family: var(--caps, var(--font));
  font-size: 8.5px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.party-card__stat-v {
  font-family: var(--mono, monospace);
  font-size: 11px;
  color: var(--ink-2);
}

.party-card__bar-track {
  height: 2px;
  background: oklch(0.30 0.02 270);
  border-radius: 1px;
  overflow: hidden;
}

.party-card__bar-fill {
  height: 100%;
  border-radius: 1px;
  transition: width .3s ease, background .3s ease;
}

.party-card__skills {
  display: flex;
  gap: 4px;
  flex-wrap: wrap;
}

.party-card__skill {
  font-family: var(--caps, var(--font));
  font-size: 9px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--ink-4);
  border: 1px solid var(--line-soft);
  border-radius: 2px;
  padding: 1px 5px;
}

/* ── Modifiers ── */
.party-drawer__mod {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: 8px;
  padding: 4px 0;
  border-bottom: 1px solid var(--line-soft);
  font-size: 12px;
}

.party-drawer__mod-label {
  color: var(--ink-2);
  flex: 1;
}

/* ── Curses ── */
.party-drawer__curse {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.party-drawer__curse-name {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12.5px;
  color: var(--ink-2);
}

.party-drawer__curse-desc {
  font-size: 11.5px;
  color: var(--ink-4);
  margin: 0;
  padding-left: 4px;
}

/* ── Laws ── */
.party-drawer__law {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: var(--ink-3);
}

.party-drawer__law-name {
  color: var(--ink-2);
}

/* ── Items ── */
.party-drawer__item {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.party-drawer__item-top {
  display: flex;
  align-items: center;
  gap: 6px;
}

.party-drawer__item-name {
  font-size: 12.5px;
  color: var(--ink-2);
  flex: 1;
}

.party-drawer__item-qty {
  font-family: var(--mono, monospace);
  font-size: 11px;
  color: var(--ink-4);
}

.party-drawer__item-desc {
  font-size: 11.5px;
  color: var(--ink-4);
  margin: 0;
  padding-left: 4px;
}
</style>
