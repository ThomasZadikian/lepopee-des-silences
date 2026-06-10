# Reward Selection Flow

## Objectif

Décrire le flow combat victory -> pending reward -> reward selection -> run continuation.

## Flow

Combat victory
-> RewardOffer créée
-> PendingRewardOfferId attaché à la Run
-> GET pending reward
-> POST select reward
-> application domaine
-> PendingRewardOfferId nettoyé
-> reprise de run.

## Invariants

- Une run ouverte peut avoir au plus une reward pending.
- Une reward pending doit être sélectionnée avant progression.
- Une option ne peut être sélectionnée qu'une fois.
- Le backend reste source de vérité.
- Aucune reward impossible à appliquer ne doit être proposée.

## MVP actuel

- Les RewardOffers post-combat exposent uniquement des choix `Heal`.
- `Run.ApplyReward` applique l'effet et borne les PV à `MaxHp`.
- Les types non supportés sont rejetés par le domaine.

## Non-objectifs

- Pas d'inventaire complet.
- Pas d'équipement.
- Pas de balancing final.
- Pas de rewards complexes.
- Pas de nouveaux effets combat.
