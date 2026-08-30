<script setup lang="ts">
import { useRunStore } from '../stores/runStore';
import { statDescriptions } from '../../party/constants/statDescriptions';
import type { RunItemDto, RunModifierDto, RunPartyMemberDto } from '../types/runTypes';
import StatTooltip from '../../../shared/components/StatTooltip.vue';

defineProps<{
  allies: RunPartyMemberDto[] | null;
  modifiers: RunModifierDto[] | null;
  items: RunItemDto[] | null;
  caliceInfiniEnabled?: boolean;
  canUseCaliceInfini?: boolean;
}>();

defineEmits<{ close: [] }>();

const runStore = useRunStore();

function vitalityPct(m: RunPartyMemberDto): number {
  if (!m.maxVitality) return 0;
  return Math.max(0, Math.min(100, (m.currentVitality / m.maxVitality) * 100));
}

function vitalityColor(pct: number): string {
  if (pct <= 25) return 'var(--danger-dim)';
  if (pct <= 50) return 'var(--mauve-dim)';
  return 'var(--mint-dim)';
}

const modifierTypeLabels: Record<string, string> = {
  AddStartingGuard:         'Garde initiale',
  ModifyDifficultyMultiplier: 'Difficulté ×',
  ModifyRewardPowerMultiplier: 'Puissance récompense ×',
  ModifyAttackPower:        'Attaque',
  ModifyDefense:            'Défense',
  ModifySpeed:              'Vitesse',
  ModifyInitiative:         'Initiative',
};

function modifierLabel(mod: RunModifierDto): string {
  const label = modifierTypeLabels[mod.type]
    ?? mod.type.replace(/([A-Z])/g, ' $1').trim();
  const sign = mod.value >= 0 ? '+' : '';
  return `${label} ${sign}${mod.value}`;
}

function durationLabel(d: string): string {
  const map: Record<string, string> = {
    UntilRunEnds: 'run entière',
    Permanent:    'permanent',
    UntilRoomEnd: 'fin de salle',
    UntilCombatEnd: 'fin de combat',
  };
  return map[d] ?? d;
}

function sourceLabel(s: string): string {
  const map: Record<string, string> = {
    PalaceLaw: 'Loi du Palais',
    Curse:     'Malédiction',
    Item:      'Objet',
    Event:     'Événement',
  };
  return map[s] ?? s;
}

function rarityTone(rarity: string): string {
  const r = rarity.toLowerCase();
  if (r.includes('epic') || r.includes('épic') || r.includes('relique')) return 'pd-chip--mint';
  if (r.includes('rare')) return 'pd-chip--mauve';
  return '';
}

const temporarySlots = [
  { key: 'Temporary1', label: 'Temp. I' },
  { key: 'Temporary2', label: 'Temp. II' },
  { key: 'Grimoire', label: 'Temp. III' },
] as const;

function temporarySkill(
  member: RunPartyMemberDto,
  slot: typeof temporarySlots[number]['key'],
) {
  return member.skills.find((skill) => skill.temporarySlot === slot);
}
</script>

<template>
  <aside class="party-drawer" aria-label="Équipe">
    <button class="party-drawer__close" @click="$emit('close')" aria-label="Fermer">✕</button>

    <div class="party-drawer__head-row">
      <span class="party-drawer__kicker">Équipe</span>
    </div>

    <div class="party-drawer__body">

      <!-- ── Alliés ── -->
      <section class="party-drawer__section">
        <h4 class="party-drawer__section-title">Alliés</h4>

        <template v-if="allies && allies.length">
          <article
            v-for="member in allies"
            :key="member.id"
            class="party-card"
            :class="{ 'party-card--defeated': member.isDefeated }"
          >
            <div class="party-card__top">
              <div class="party-card__name">
                <span class="party-card__displayname">{{ member.displayName }}</span>
                <span v-if="member.isDefeated" class="pd-chip pd-chip--danger">KO</span>
                <span v-else-if="!member.isActive" class="pd-chip">Allié</span>
              </div>

              <div class="party-card__stats">
                <span class="party-card__stat">
                  <span class="party-card__stat-k">VIT</span>
                  <span class="party-card__stat-v" :style="{ color: vitalityColor(vitalityPct(member)) }">
                    {{ member.currentVitality }} / {{ member.maxVitality }}
                  </span>
                </span>
                <span v-if="member.guard > 0" class="party-card__stat">
                  <span class="party-card__stat-k">GARDE</span>
                  <span class="party-card__stat-v" style="color: var(--mint-dim)">{{ member.guard }}</span>
                </span>
                <span class="party-card__stat">
                  <StatTooltip :text="statDescriptions.Mana" placement="bottom">
                    <span class="party-card__stat-k">Mana</span>
                  </StatTooltip>
                  <span class="party-card__stat-v">{{ member.mana }}</span>
                </span>
                <span v-if="member.charge > 0" class="party-card__stat">
                  <StatTooltip text="Jauge tactique limitée à 5, générée par les actions utiles pendant le combat." placement="bottom">
                    <span class="party-card__stat-k">CHARGE</span>
                  </StatTooltip>
                  <span class="party-card__stat-v">{{ member.charge }}</span>
                </span>
                <span class="party-card__stat">
                  <span class="party-card__stat-k">MVT</span>
                  <span class="party-card__stat-v">{{ member.movement ?? 4 }}</span>
                </span>
              </div>
            </div>

            <!-- Vitality bar -->
            <div class="party-card__bar-track">
              <div
                class="party-card__bar-fill"
                :style="{
                  width: vitalityPct(member) + '%',
                  background: vitalityColor(vitalityPct(member)),
                }"
              />
            </div>

            <!-- Skills -->
            <div v-if="member.skills?.length" class="party-card__skills">
              <span
                v-for="skill in member.skills"
                :key="skill.key"
                class="party-card__skill"
                :class="{ 'party-card__skill--temporary': skill.temporarySlot && skill.temporarySlot !== 'Permanent' }"
                :title="`${skill.displayName} · ${skill.skillType} · ${skill.targetingMode}`"
              >
                {{ skill.displayName }}
                <small v-if="skill.temporarySlot && skill.temporarySlot !== 'Permanent'">
                  {{ skill.temporarySlot === 'Grimoire' ? 'Temp. III' : skill.temporarySlot.replace('Temporary', 'Temp. ') }}
                </small>
              </span>
            </div>
            <div class="party-card__temporary-slots" aria-label="Compétences temporaires">
              <span
                v-for="slot in temporarySlots"
                :key="slot.key"
                class="party-card__temporary-slot"
                :class="{ 'party-card__temporary-slot--filled': temporarySkill(member, slot.key) }"
              >
                <small>{{ slot.label }}</small>
                {{ temporarySkill(member, slot.key)?.displayName ?? 'Libre' }}
              </span>
            </div>
          </article>
        </template>

        <p v-else class="party-drawer__empty">
          Données disponibles en combat uniquement.
        </p>
      </section>

      <!-- ── Calice infini ── -->
      <section v-if="caliceInfiniEnabled" class="party-drawer__section">
        <button
          type="button"
          class="party-drawer__manage-btn"
          :disabled="!canUseCaliceInfini"
          :title="canUseCaliceInfini ? 'Calice infini' : 'Calice infini — en recharge'"
          @click="runStore.useCaliceInfini()"
        >
          Utiliser le Calice infini
        </button>
      </section>

      <!-- ── Modificateurs actifs ── -->
      <section v-if="modifiers && modifiers.length" class="party-drawer__section">
        <h4 class="party-drawer__section-title">Modificateurs actifs</h4>
        <ul class="party-drawer__list">
          <li v-for="mod in modifiers" :key="mod.id" class="party-drawer__mod">
            <span class="party-drawer__mod-label">{{ modifierLabel(mod) }}</span>
            <span class="party-drawer__mod-meta">
              {{ durationLabel(mod.duration) }} · {{ sourceLabel(mod.sourceType) }}
            </span>
          </li>
        </ul>
      </section>

      <!-- ── Objets de run ── -->
      <section v-if="items && items.length" class="party-drawer__section">
        <h4 class="party-drawer__section-title">Objets de run</h4>
        <ul class="party-drawer__list">
          <li v-for="item in items" :key="item.id" class="party-drawer__item">
            <div class="party-drawer__item-top">
              <span :class="['pd-chip', rarityTone(item.rarity)]">{{ item.rarity }}</span>
              <span class="party-drawer__item-name">{{ item.displayName }}</span>
              <span v-if="item.quantity > 1" class="party-drawer__item-qty">×{{ item.quantity }}</span>
            </div>
            <p v-if="item.description" class="party-drawer__item-desc">{{ item.description }}</p>
          </li>
        </ul>
      </section>

      <p v-if="!allies?.length && !modifiers?.length && !items?.length" class="party-drawer__empty">
        Aucune donnée d'équipe disponible.
      </p>
    </div>
  </aside>
</template>

<style scoped>
.party-drawer {
  position: fixed;
  top: 0;
  left: 0;
  bottom: 0;
  /* Pas --z-drawer (20) : TacticalGridMap.vue peint sa propre barre d'onglets (Exploration
     tactique/Lois/Difficulté) à z-index 120 et ses popovers de nœud à 220, dans le même
     contexte d'empilement — ce tiroir, un panneau global fixe, passait dessous (visuellement
     coupé, et son bouton fermer parfois inatteignable si les onglets s'étendent par-dessus). */
  z-index: 300;
  width: 380px;
  display: flex;
  flex-direction: column;
  background: var(--panel);
  border-right: 1px solid var(--line);
  padding: 24px 22px;
  overflow-y: auto;
  animation: party-drawer-slide .35s cubic-bezier(0.5, 0, 0.5, 1);
}

@keyframes party-drawer-slide {
  from { transform: translateX(-100%); }
  to { transform: translateX(0); }
}

.party-drawer__close {
  all: unset;
  position: absolute;
  top: 16px;
  right: 16px;
  cursor: pointer;
  color: var(--ink-4);
  font-size: 12px;
  padding: 4px;
  transition: color .15s;
}

.party-drawer__close:hover { color: var(--mint-dim); }

.party-drawer__head-row { margin-bottom: 16px; }

.party-drawer__kicker {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: .14em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.party-drawer__body {
  flex: 1;
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
  font-family: var(--font-mono);
  font-size: 9.5px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
  padding-bottom: 6px;
  border-bottom: 1px solid var(--line-soft);
  margin: 0;
}

.party-drawer__empty {
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--ink-4);
  font-style: italic;
}

.party-drawer__manage-btn {
  width: 100%;
  padding: 9px 14px;
  border: 1px solid var(--mint-dim);
  background: transparent;
  color: var(--mint-dim);
  font-family: var(--font);
  font-size: 10.5px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  cursor: pointer;
  transition: opacity .15s;
}

.party-drawer__manage-btn:hover:not(:disabled) { opacity: .8; }

.party-drawer__manage-btn:disabled {
  color: var(--ink-5);
  border-color: var(--line);
  cursor: not-allowed;
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
  background: var(--panel-2);
  display: flex;
  flex-direction: column;
  gap: 6px;
  transition: opacity .2s;
}

.party-card--defeated { opacity: 0.5; }

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
  font-family: var(--font-display);
  font-style: italic;
  font-size: 14px;
  color: var(--ink);
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
  font-family: var(--font-mono);
  font-size: 8.5px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.party-card__stat-v {
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--ink-3);
}

.party-card__bar-track {
  height: 2px;
  background: var(--line-soft);
  overflow: hidden;
}

.party-card__bar-fill {
  height: 100%;
  transition: width .3s ease, background .3s ease;
}

.party-card__skills {
  display: flex;
  gap: 4px;
  flex-wrap: wrap;
}

.party-card__skill {
  font-family: var(--font-mono);
  font-size: 9px;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--ink-4);
  border: 1px solid var(--line-soft);
  padding: 1px 5px;
}

.party-card__skill small {
  margin-left: 4px;
  color: var(--mint-dim);
  font-size: 8px;
}

.party-card__skill--temporary {
  border-color: var(--mint-dim);
  color: var(--ink-3);
}

.party-card__temporary-slots {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 4px;
}

.party-card__temporary-slot {
  display: flex;
  flex-direction: column;
  min-width: 0;
  padding: 4px 5px;
  border: 1px dashed var(--line-soft);
  color: var(--ink-4);
  font-size: 9px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.party-card__temporary-slot small {
  color: var(--mint-dim);
  font-family: var(--font-mono);
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.party-card__temporary-slot--filled {
  border-style: solid;
  border-color: var(--mint-dim);
  color: var(--ink-3);
}

/* ── Chips ── */
.pd-chip {
  font-size: 9px;
  letter-spacing: .06em;
  text-transform: uppercase;
  padding: 2px 6px;
  border: 1px solid var(--ink-5);
  color: var(--ink-4);
  flex-shrink: 0;
}

.pd-chip--mint { border-color: var(--mint-dim); color: var(--mint-dim); }
.pd-chip--mauve { border-color: var(--mauve-dim); color: var(--mauve-dim); }
.pd-chip--danger { border-color: var(--danger-dim); color: var(--danger-dim); }

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

.party-drawer__mod-label { color: var(--ink-3); flex: 1; }

.party-drawer__mod-meta {
  font-family: var(--font-mono);
  font-size: 10px;
  color: var(--ink-4);
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
  color: var(--ink-3);
  flex: 1;
}

.party-drawer__item-qty {
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--ink-4);
}

.party-drawer__item-desc {
  font-size: 11.5px;
  color: var(--ink-4);
  margin: 0;
  padding-left: 4px;
}

@media (prefers-reduced-motion: reduce) {
  .party-drawer { animation: none; }
}
</style>
