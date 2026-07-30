<script setup lang="ts">
import { RouterLink, useRouter } from 'vue-router';

import PalaceAtmosphere from '../shared/components/PalaceAtmosphere.vue';
import RuleOrnament from '../shared/components/RuleOrnament.vue';

const router = useRouter();
const props = defineProps<{ embedded?: boolean }>();

type TutorialSection = {
  title: string;
  color: string;
  paragraphs: string[];
};

const sections: TutorialSection[] = [
  {
    title: 'Le tour de jeu — le combat tactique',
    color: 'var(--gold)',
    paragraphs: [
      "Chaque round annonce son ordre d'initiative. La Vitesse décide qui agit en premier ; quand tous les combattants encore debout ont joué, un nouveau round commence.",
      "À son tour, un combattant dispose d'un déplacement et d'une action indépendants. Il peut les effectuer dans l'ordre de son choix, ou renoncer à l'un sans perdre l'autre.",
      "Les obstacles, le relief, la portée et la ligne de vue déterminent les cases valides. Les zones d'effet sont montrées avant validation : cible unique, croix, losange ou carte entière.",
      "Les statuts exprimés en tours diminuent au fil des rounds. Un effet de trois tours reste donc lisible et prévisible pour les deux camps.",
    ],
  },
  {
    title: 'La matrice de Markov — un Palais sans plan fixe',
    color: 'var(--frost)',
    paragraphs: [
      "Le Palais ne suit aucun plan prédéfini : à chaque nouvelle salle, la suivante est tirée au sort selon des probabilités de transition depuis la salle courante — une chaîne de Markov. Certaines salles ont plus de chances de s'enchaîner que d'autres, selon leurs affinités thématiques.",
      "Une salle à « chaîne stricte » déjà visitée voit sa probabilité de réapparition chuter fortement à chaque nouveau passage — sans jamais tomber à zéro. Revenir sur ses pas reste possible, juste improbable.",
      "Tout ce tirage est déterministe : il dépend uniquement de la seed de votre run. Rejouer la même seed reproduit exactement le même enchaînement de salles.",
    ],
  },
  {
    title: 'Génération des salles — profondeur, risque, climats',
    color: 'var(--sap)',
    paragraphs: [
      "La profondeur mesure votre progression dans le Palais ; le niveau de risque d'une salle influence le nombre et la puissance des ennemis qui s'y trouvent.",
      "Chaque salle peut porter un climat passager (Gris, Pluie, Canicule, Grêle) qui module temporairement la puissance des ennemis ou la garde de départ des combattants.",
      "L'état du Palais dans une salle (Neutre, Douloureux, Silencieux…) influence aussi le déroulement des combats qui s'y jouent.",
      "Les Lois du Palais, une fois actives, appliquent des effets qui persistent selon leur portée : le temps de la salle, du combat, ou de toute la run.",
    ],
  },
  {
    title: 'Réputation et blessures des habitants du Palais',
    color: 'oklch(0.66 0.19 38)',
    paragraphs: [
      "Chaque choix fait à un PNJ ajuste un score de relation propre à cette rencontre. Ce score, en franchissant certains seuils, fait basculer l'état de ses blessures : Latent, Tendu, puis Rompu.",
      "Certaines blessures se referment si l'on répare le lien (réversibles) ; d'autres restent ouvertes pour le reste de la traversée une fois rompues (irréversibles).",
      "Les dons majeurs — objets rares, sorts légendaires — ne sont offerts qu'une seule fois par joueur, et seulement au-delà d'un certain score de relation. La page Réputation récapitule où vous en êtes avec chacun.",
    ],
  },
  {
    title: 'Catégories de sorts — Physique et Magique',
    color: 'oklch(0.78 0.13 78)',
    paragraphs: [
      "Chaque sort appartient à l'une de deux catégories : Physique ou Magique.",
      "Les sorts Magiques bénéficient du bonus de dégâts magiques (accordé par certains équipements ou sorts d'équipe) et sont amoindris par la réduction de dégâts magiques de leur cible. Les sorts Physiques ignorent totalement ces deux effets.",
    ],
  },
];
</script>

<template>
  <main class="tutorial-page" :class="{ 'tutorial-page--embedded': props.embedded }" data-mood="palais">
    <PalaceAtmosphere v-if="!props.embedded" />

    <div class="tutorial-page__content">
      <button v-if="!props.embedded" class="tutorial-page__back" @click="router.back()">← Retour</button>

      <span class="es-kicker">Système · comprendre le Palais</span>
      <h1 class="es-h1" style="font-size: clamp(30px, 4.4vw, 52px); margin-top: 12px">
        Tutoriel &amp; explications
      </h1>
      <RuleOrnament style="width: 150px; margin: 16px 0" />
      <p class="es-lede es-dim" style="max-width: 58ch">
        Ce que le Palais ne vous dit jamais directement — le fonctionnement, sous la surface, de ses
        systèmes les plus obscurs. Pour le détail des altérations de combat (poison, silence, étourdissement…),
        consultez la page <RouterLink to="/statuts">Statuts</RouterLink>.
      </p>

      <section v-for="section in sections" :key="section.title" class="tutorial-section">
        <div class="tutorial-section__title" :style="{ color: section.color }">{{ section.title }}</div>
        <p v-for="(paragraph, index) in section.paragraphs" :key="index" class="tutorial-section__paragraph">
          {{ paragraph }}
        </p>
      </section>
    </div>
  </main>
</template>

<style scoped>
.tutorial-page {
  position: relative;
  height: 100dvh;
  overflow-y: auto;
  overflow-x: hidden;
  background:
    radial-gradient(70% 52% at 20% 12%, var(--wash-frost), transparent 60%),
    radial-gradient(64% 56% at 86% 80%, var(--wash-blood), transparent 58%),
    radial-gradient(58% 50% at 60% 26%, var(--wash-sap), transparent 60%),
    radial-gradient(56% 50% at 12% 92%, var(--wash-gold), transparent 60%),
    radial-gradient(150% 130% at 50% -10%, var(--bg) 0%, var(--bg-2) 48%, var(--void) 100%);
  color: var(--ink);
  font-family: var(--font);
}

.tutorial-page--embedded {
  height: auto;
  min-height: 100%;
  overflow: visible;
}

.tutorial-page__content {
  position: relative;
  z-index: 5;
  max-width: 860px;
  margin: 0 auto;
  padding: 64px 4vw 90px;
}

.tutorial-page--embedded .tutorial-page__content {
  padding: 40px 4vw 48px;
}

.tutorial-page__back {
  all: unset;
  cursor: pointer;
  display: block;
  margin-bottom: 24px;
  font-family: var(--font-caps);
  font-size: 11px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
  transition: color 0.2s;
}
.tutorial-page__back:hover { color: var(--gold); }

.tutorial-section {
  margin-top: 50px;
}

.tutorial-section__title {
  font-family: var(--font-caps);
  font-size: 10px;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  border-bottom: 1px solid var(--line-soft);
  padding-bottom: 11px;
  margin-bottom: 18px;
}

.tutorial-section__paragraph {
  font-family: var(--font);
  font-size: 14px;
  line-height: 1.65;
  color: var(--ink-3);
  margin: 0 0 14px;
}

.tutorial-section__paragraph:last-child {
  margin-bottom: 0;
}
</style>
