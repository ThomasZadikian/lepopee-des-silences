<!-- SkillsView.vue -->
<template>
  <v-container fluid>
    <div class="text-h4 font-weight-bold mb-6">Compétences</div>

    <div v-if="loading" class="d-flex justify-center pa-12">
      <v-progress-circular indeterminate color="primary" size="48" />
    </div>

    <v-alert v-else-if="skills.length === 0" type="info">
      Aucune compétence débloquée.
    </v-alert>

    <v-row v-else>
      <v-col v-for="entry in skills" :key="entry.id" cols="12" sm="6" md="4">
        <v-card height="100%">
          <v-card-item>
            <template #prepend>
              <v-avatar
                :color="effectColor(entry.skill.effectType)"
                variant="tonal"
                size="40"
              >
                <v-icon :icon="effectIcon(entry.skill.effectType)" />
              </v-avatar>
            </template>
            <v-card-title>{{ entry.skill.name }}</v-card-title>
            <v-card-subtitle>
              <v-chip
                size="x-small"
                :color="effectColor(entry.skill.effectType)"
                variant="flat"
              >
                {{ entry.skill.effectType }}
              </v-chip>
              <v-chip
                v-if="entry.skill.elementType"
                size="x-small"
                :color="elementColor(entry.skill.elementType)"
                variant="flat"
                class="ml-1"
              >
                {{ entry.skill.elementType }}
              </v-chip>
            </v-card-subtitle>
          </v-card-item>

          <v-card-text>
            <div class="text-caption text-medium-emphasis mb-3">
              {{ entry.skill.description }}
            </div>

            <v-divider class="mb-2" />

            <div class="d-flex justify-space-between text-caption">
              <span>
                <v-icon size="12" icon="mdi-lightning-bolt" color="info" />
                Coût MP : <strong>{{ entry.skill.mpCost }}</strong>
              </span>
              <span v-if="entry.skill.baseDamage">
                <v-icon size="12" icon="mdi-sword" color="error" />
                Dégâts : <strong>{{ entry.skill.baseDamage }}</strong>
              </span>
              <span v-if="entry.skill.healAmount">
                <v-icon size="12" icon="mdi-heart" color="success" />
                Soin : <strong>{{ entry.skill.healAmount }}</strong>
              </span>
            </div>

            <div class="text-caption text-medium-emphasis mt-2">
              Débloqué le
              {{ new Date(entry.unlockedAt).toLocaleDateString("fr-FR") }}
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { playerSkillsApi } from "@/api/skills";
import type { PlayerSkill } from "@/interfaces/playerSkill";
import { onMounted, ref } from "vue";

const skills = ref<PlayerSkill[]>([]);
const loading = ref(true);

onMounted(async () => {
  try {
    const res = await playerSkillsApi.getMe();
    skills.value = res.data.items ?? [];
  } finally {
    loading.value = false;
  }
});

function effectColor(type: string): string {
  const map: Record<string, string> = {
    damage: "error",
    heal: "success",
    buff: "warning",
    debuff: "secondary",
  };
  return map[type] ?? "grey";
}

function effectIcon(type: string): string {
  const map: Record<string, string> = {
    damage: "mdi-sword-cross",
    heal: "mdi-heart-plus",
    buff: "mdi-arrow-up-bold",
    debuff: "mdi-arrow-down-bold",
  };
  return map[type] ?? "mdi-help";
}

function elementColor(element: string): string {
  const map: Record<string, string> = {
    fire: "deep-orange",
    lightning: "yellow-darken-2",
    ice: "light-blue",
    neutral: "grey",
  };
  return map[element] ?? "grey";
}
</script>
