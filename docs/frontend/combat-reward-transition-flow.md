# Combat Reward Transition Flow

## Objectif

Stabiliser la transition entre combat, récompense et progression de run.

## Flow victoire

Combat completed
→ affichage victoire
→ clic Continuer
→ nettoyage combat store
→ affichage RewardOffer
→ sélection reward
→ reprise run.

## Flow défaite

Combat failed
→ affichage défaite
→ run failed
→ bouton Quitter la run
→ nettoyage état combat.

## Priorités d'affichage

- Run failed.
- Combat actif ou outcome combat non confirmé.
- Reward pending.
- Map ou flow node normal.

## Non-objectifs

- Pas de nouvelle mécanique backend.
- Pas de nouveau système de reward.
- Pas d'animations avancées.
- Pas d'ATB.
- Pas d'équilibrage.
