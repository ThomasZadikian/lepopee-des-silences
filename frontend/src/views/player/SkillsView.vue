<template>
  <div>

    <!-- Chargement -->
    <div v-if="loading" class="d-flex justify-center align-center" style="min-height: 60vh;">
      <v-progress-circular indeterminate color="primary" size="40" width="2" />
    </div>

    <template v-else>

      <!-- ── HERO ───────────────────────────────────────────────────── -->
      <div
  style="
    margin: -32px -32px 40px -32px;
    position: relative; overflow: hidden;
    border-bottom: 1px solid var(--rpg-border);
  "
>
  <div style="display: grid; grid-template-columns: 1fr 1fr 1fr 1fr; min-height: 160px;">
    <div style="padding: 40px 32px; border-right: 1px solid var(--rpg-border);">
      <div class="editorial-label mb-3">Mes données · Talents</div>
      <div style="font-family:var(--font-serif);font-size:clamp(2rem,3vw,3rem);font-weight:900;line-height:1;letter-spacing:-0.03em;margin-bottom:12px;">
        Arbre
        <span style="font-style:italic;color:var(--rpg-ink-muted);display:block;">de talents.</span>
      </div>
    </div>
    <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
      <div class="editorial-label mb-3">Débloquées</div>
      <div style="font-family:var(--font-serif);font-size:2.5rem;font-weight:700;line-height:1;">
        {{ skills.length }}
      </div>
    </div>
  </div>
</div>

      <!-- Aucune compétence -->
      <div v-if="skills.length === 0" class="text-center py-16">
        <div class="editorial-label mb-4">Aucune compétence</div>
        <div style="font-family:var(--font-serif);font-size:1.5rem;font-weight:700;">
          Aucune compétence débloquée
        </div>
        <div class="mt-2" style="color:var(--rpg-ink-muted);font-size:13px;">
          Progressez dans le jeu pour débloquer des compétences.
        </div>
      </div>

      <template v-else>

        <!-- ── SECTION PAR TYPE ───────────────────────────────────────── -->
        <div
          v-for="section in sections"
          :key="section.type"
          v-show="section.items.length > 0"
          style="margin-bottom: 48px;"
        >
          <!-- Header section -->
          <div
            style="
              border-top: 1px solid rgba(0,0,0,0.08);
              padding-top: 20px; margin-bottom: 20px;
              display: flex; align-items: baseline;
              justify-content: space-between;
            "
          >
            <div>
              <div style="font-family:var(--font-serif);font-size:1.6rem;font-weight:700;line-height:1;margin-bottom:4px;">
                {{ section.label }}
                <span style="font-style:italic;color:var(--rpg-ink-muted);font-size:1.2rem;">
                  {{ section.sublabel }}
                </span>
              </div>
              <div class="editorial-label" style="color:var(--rpg-ink-muted);">
                {{ section.description }}
              </div>
            </div>
            <div style="text-align:right;">
              <div class="editorial-label">Débloquées</div>
              <div style="font-family:var(--font-serif);font-size:1.4rem;font-weight:700;">
                {{ section.items.length }}
              </div>
            </div>
          </div>

          <!-- Grille de compétences -->
          <v-row no-gutters style="gap: 0;">
            <v-col
              v-for="entry in section.items"
              :key="entry.id"
              cols="6" sm="4" md="3" lg="2"
              style="border: 1px solid rgba(0,0,0,0.08); margin-right: -1px; margin-bottom: -1px;"
            >
              <div style="padding: 16px;">
                <!-- Point indicateur -->
                <div style="
                  width: 8px; height: 8px; border-radius: 50%;
                  margin-bottom: 10px;
                " :style="`background: ${section.dotColor};`" />

                <!-- Nom -->
                <div style="font-family:var(--font-serif);font-size:0.95rem;font-weight:700;margin-bottom:4px;line-height:1.2;">
                  {{ entry.skill.name }}
                </div>

                <!-- Élément -->
                <div v-if="entry.skill.elementType && entry.skill.elementType !== 'neutral'"
                  class="editorial-label"
                  style="margin-bottom:8px;"
                  :style="`color:${elementColor(entry.skill.elementType)};`"
                >
                  {{ entry.skill.elementType }}
                </div>

                <!-- Stats -->
                <div style="margin-top: 8px; border-top: 1px solid rgba(0,0,0,0.06); padding-top: 8px;">
                  <div style="font-size:11px;color:var(--rpg-ink-muted);">
                    MP <span style="font-weight:600;color:var(--rpg-ink);">{{ entry.skill.mpCost }}</span>
                    <span v-if="entry.skill.baseDamage" style="margin-left:8px;">
                      DMG <span style="font-weight:600;color:#C0392B;">{{ entry.skill.baseDamage }}</span>
                    </span>
                    <span v-if="entry.skill.healAmount" style="margin-left:8px;">
                      Soin <span style="font-weight:600;color:#1E8449;">{{ entry.skill.healAmount }}</span>
                    </span>
                  </div>
                </div>

                <!-- Date déblocage -->
                <div style="font-size:10px;color:var(--rpg-ink-muted);margin-top:6px;">
                  Obtenue le : {{ new Date(entry.unlockedAt).toLocaleDateString('fr-FR') }}
                </div>
              </div>
            </v-col>
          </v-row>
        </div>
      </template>
    </template>
  </div>
</template>

<script setup lang="ts">
import { playerSkillsApi } from '@/api/skills'
import type { PlayerSkill } from '@/interfaces/playerSkill'
import { computed, onMounted, ref } from 'vue'

const skills  = ref<PlayerSkill[]>([])
const loading = ref(true)

const damageSkills = computed(() => skills.value.filter(s => s.skill.effectType === 'damage'))
const healSkills   = computed(() => skills.value.filter(s => s.skill.effectType === 'heal'))
const buffSkills   = computed(() => skills.value.filter(s => s.skill.effectType === 'buff' || s.skill.effectType === 'debuff'))

const sections = computed(() => [
  {
    type:        'damage',
    label:       'Attaque',
    sublabel:    ' & destruction',
    description: 'Compétences offensives · dégâts directs',
    dotColor:    '#C0392B',
    items:       damageSkills.value,
  },
  {
    type:        'heal',
    label:       'Soin',
    sublabel:    ' & restauration',
    description: 'Compétences défensives · restauration de vie',
    dotColor:    '#1E8449',
    items:       healSkills.value,
  },
  {
    type:        'buff',
    label:       'Buff',
    sublabel:    ' & altérations',
    description: 'Compétences de support · modifications de stats',
    dotColor:    '#D68910',
    items:       buffSkills.value,
  },
])

onMounted(async () => {
  try {
    const res = await playerSkillsApi.getMe()
    skills.value = res.data.items ?? []
  } finally {
    loading.value = false
  }
})

function elementColor(element: string): string {
  const map: Record<string, string> = {
    fire:      '#C0392B',
    lightning: '#D68910',
    ice:       '#2D4A8A',
    neutral:   'var(--rpg-ink-muted)',
  }
  return map[element] ?? 'var(--rpg-ink-muted)'
}
</script>