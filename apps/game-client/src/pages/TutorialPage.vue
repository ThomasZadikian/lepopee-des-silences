<script setup lang="ts">
import { RouterLink, useRouter } from 'vue-router';

import LivingWalls from '../shared/components/LivingWalls.vue';

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
    color: 'var(--mint-dim)',
    paragraphs: [
      "Chaque round annonce son ordre d'initiative. La Vitesse décide qui agit en premier ; quand tous les combattants encore debout ont joué, un nouveau round commence.",
      "À son tour, un combattant dispose d'un déplacement et d'une action indépendants. Il peut les effectuer dans l'ordre de son choix, ou renoncer à l'un sans perdre l'autre.",
      "Les obstacles, le relief, la portée et la ligne de vue déterminent les cases valides. Les zones d'effet sont montrées avant validation : cible unique, croix, losange ou carte entière.",
      "Les statuts exprimés en tours diminuent au fil des rounds. Un effet de trois tours reste donc lisible et prévisible pour les deux camps.",
    ],
  },
  {
    title: 'La matrice de Markov — un Palais sans plan fixe',
    color: 'var(--mint-dim)',
    paragraphs: [
      "Le Palais ne suit aucun plan prédéfini : à chaque nouvelle salle, la suivante est tirée au sort selon des probabilités de transition depuis la salle courante — une chaîne de Markov. Certaines salles ont plus de chances de s'enchaîner que d'autres, selon leurs affinités thématiques.",
      "Une salle à « chaîne stricte » déjà visitée voit sa probabilité de réapparition chuter fortement à chaque nouveau passage — sans jamais tomber à zéro. Revenir sur ses pas reste possible, juste improbable.",
      "Tout ce tirage est déterministe : il dépend uniquement de la seed de votre run. Rejouer la même seed reproduit exactement le même enchaînement de salles.",
    ],
  },
  {
    title: 'Génération des salles — profondeur, risque, climats',
    color: 'var(--mint-dim)',
    paragraphs: [
      "La profondeur mesure votre progression dans le Palais ; le niveau de risque d'une salle influence le nombre et la puissance des ennemis qui s'y trouvent.",
      "Chaque salle peut porter un climat passager (Gris, Pluie, Canicule, Grêle) qui module temporairement la puissance des ennemis ou la garde de départ des combattants.",
      "L'état du Palais dans une salle (Neutre, Douloureux, Silencieux…) influence aussi le déroulement des combats qui s'y jouent.",
      "Les Lois du Palais, une fois actives, appliquent des effets qui persistent selon leur portée : le temps de la salle, du combat, ou de toute la run.",
    ],
  },
  {
    title: 'Réputation et blessures des habitants du Palais',
    color: 'var(--mint-dim)',
    paragraphs: [
      "Chaque choix fait à un PNJ ajuste un score de relation propre à cette rencontre. Ce score, en franchissant certains seuils, fait basculer l'état de ses blessures : Latent, Tendu, puis Rompu.",
      "Certaines blessures se referment si l'on répare le lien (réversibles) ; d'autres restent ouvertes pour le reste de la traversée une fois rompues (irréversibles).",
      "Les dons majeurs — objets rares, sorts légendaires — ne sont offerts qu'une seule fois par joueur, et seulement au-delà d'un certain score de relation. La page Réputation récapitule où vous en êtes avec chacun.",
    ],
  },
  {
    title: 'Catégories de sorts — Physique et Magique',
    color: 'var(--mint-dim)',
    paragraphs: [
      "Chaque sort appartient à l'une de deux catégories : Physique ou Magique.",
      "Les sorts Magiques bénéficient du bonus de dégâts magiques (accordé par certains équipements ou sorts d'équipe) et sont amoindris par la réduction de dégâts magiques de leur cible. Les sorts Physiques ignorent totalement ces deux effets.",
    ],
  },
];
</script>

<template>
  <main class="tutorial-page" :class="{ 'tutorial-page--embedded': props.embedded }">
    <LivingWalls v-if="!props.embedded" />

    <div class="tutorial-page__content">
      <button v-if="!props.embedded" class="tutorial-page__back" @click="router.back()">← sommaire</button>

      <span class="tutorial-page__kicker">Système · comprendre le Palais</span>
      <h1 class="tutorial-page__title">Tutoriel &amp; explications</h1>
      <p class="tutorial-page__lede">
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
  min-height: 100dvh;
  background: var(--void);
  color: var(--ink);
  font-family: var(--font);
}

.tutorial-page--embedded { min-height: 0; }

.tutorial-page__content {
  position: relative;
  z-index: 2;
  max-width: 860px;
  margin: 0 auto;
  padding: 48px 40px 96px;
}

.tutorial-page--embedded .tutorial-page__content {
  padding: 0;
  max-width: none;
}

.tutorial-page__back {
  all: unset;
  cursor: pointer;
  display: block;
  margin-bottom: 24px;
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: 0.08em;
  color: var(--ink-4);
  transition: color .3s;
}
.tutorial-page__back:hover { color: var(--mint-dim); }

.tutorial-page__kicker {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.tutorial-page__title {
  margin: 12px 0 0;
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  font-size: 38px;
  color: var(--ink);
}

.tutorial-page__lede {
  max-width: 58ch;
  margin: 16px 0 0;
  color: var(--ink-3);
  font-size: 14px;
  line-height: 1.6;
}

.tutorial-section {
  margin-top: 50px;
}

.tutorial-section__title {
  font-family: var(--font-mono);
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
