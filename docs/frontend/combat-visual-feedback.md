# Combat Visual Feedback

## Objectif

Améliorer la lisibilité du combat par des micro-animations sobres.

## Feedbacks ajoutés

- Cible sélectionnée.
- Combattant actif.
- Dégâts.
- Guard.
- Défaite.
- Skill sélectionnée.
- Résolution en cours.
- Tour ennemi.

## Principes

- Le backend reste source de vérité.
- Le frontend n'invente pas de dégâts.
- Les animations reflètent les logs backend.
- Les animations restent courtes et sobres.
- L'identité visuelle reste proche du palais mental, de l'encre et du silence.

## Accessibilité

- `prefers-reduced-motion` désactive les animations continues ou secouantes.
- Pas de flash agressif.
- Animations courtes.
- Lisibilité conservée pendant la résolution.

## Non-objectifs

- Pas de VFX lourds.
- Pas de sound design.
- Pas d'ATB.
- Pas de timeline animée avancée.
- Pas de recalcul de combat côté frontend.
