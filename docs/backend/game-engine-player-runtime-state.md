# Game Engine Player Runtime State

## Objectif

Persister l'état runtime du joueur pendant une run.

## Données persistées

- maxVitality
- currentVitality
- guard
- mana
- charge
- skills

## Tables

- `run_player_states` : état vitalité/guard/mana/charge
- `run_player_skills` : skills du joueur

## Flow

1. Run démarre → PlayerRuntimeState initialisé avec skills par défaut
2. Combat créé → CombatFactory utilise PlayerRuntimeState pour créer le combattant joueur
3. Actions modifient le combat → PV/guard/mana/charge du combattant changent
4. Fin de combat → PlayerRuntimeState synchronisé depuis le combattant joueur
5. Reward heal → PlayerRuntimeState.Heal() appliqué
6. Prochain combat → CombatFactory relit PlayerRuntimeState

## Non-objectifs

- pas d'inventaire
- pas d'équipement
- pas de monnaie
- pas de Rest Node
- pas de Law Node
- pas de compagnon
- pas d'ATB
- pas d'Event Sourcing

## Docker local

Voir `local-postgres-setup.md` pour démarrer PostgreSQL.
