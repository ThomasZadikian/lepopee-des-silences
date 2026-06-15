# 02 — Combat stat taxonomy

## Overview

This document defines the official combat statistics for L'épopée des silences. Each stat has a canonical name, type, range, and service ownership. This taxonomy is the single source of truth for all stat-related implementation.

## Primary stats (mandatory)

### max_vitality

```text
Nom technique:    max_vitality
Nom affichable:   Vitalité maximale
Type:             integer
Valeur minimale:  1
Valeur max recommandée: 9999
Services concernés: Catalog, Player, Game Engine
Description:      Points de vie maximum d'un combattant.
Utilisation actuelle: base stat on EnemyDefinition, PlayerCharacter, Combatant
Utilisation future: ATB defeat condition, scaling reference for heals
Exemple:          Le Porteur commence avec max_vitality = 100.
                  Un Fragment de Doute a max_vitality = 8 (base Catalog), scalé par DifficultyMultiplier.
```

**Ownership:**
- Catalog: base value on `EnemyDefinition.stat_block.max_vitality`
- Player: permanent value on `PlayerCharacterStatBlock.max_vitality`
- Game Engine: snapshotted to `run_combatant_stat_snapshots.max_vitality` (immutable during combat)

### current_vitality

```text
Nom technique:    current_vitality
Nom affichable:   Vitalité courante
Type:             integer
Valeur minimale:  0
Valeur max recommandée: 9999
Services concernés: Game Engine only
Description:      Points de vie actuels d'un combattant pendant le combat.
Utilisation actuelle: Combatant.CurrentVitality, PlayerRuntimeState.CurrentVitality
Utilisation future: ATB defeat trigger, healing target
Exemple:          Le Porteur prend 12 dégâts → current_vitality passe de 100 à 88.
```

**Ownership:**
- Catalog: does not own (no concept of "current" in a definition)
- Player: does not own (permanent stats only)
- Game Engine: runtime-only field, mutable during combat, reset at combat end

### attack_power

```text
Nom technique:    attack_power
Nom affichable:   Puissance d'attaque
Type:             integer
Valeur minimale:  0
Valeur max recommandée: 999
Services concernés: Catalog, Player, Game Engine
Description:      Force de base des attaques physiques.
Utilisation actuelle: Run.Attack (default 12), Combatant via CombatInstance.Attack
Utilisation future: input to DamageResolver, modified by RunModifiers
Exemple:          Le Porteur a attack_power = 12. Un Éclat de Garde n'affecte pas attack_power.
```

**Ownership:**
- Catalog: base value on `EnemyDefinition.stat_block.attack_power`
- Player: permanent value on `PlayerCharacterStatBlock.attack_power`
- Game Engine: snapshotted, modified by `RunModifier(ModifyAttackPower)`

### defense

```text
Nom technique:    defense
Nom affichable:   Défense
Type:             integer
Valeur minimale:  0
Valeur max recommandée: 999
Services concernés: Catalog, Player, Game Engine
Description:      Réduction des dégâts reçus. damage = max(1, attack_power - defense).
Utilisation actuelle: Run.Defense (default 6), CombatInstance.Defense
Utilisation future: modified by RunModifiers, skills (Garde)
Exemple:          Le Porteur a defense = 6. Un ennemi avec attack_power = 10 inflige 4 dégâts.
```

**Ownership:**
- Catalog: base value on `EnemyDefinition.stat_block.defense`
- Player: permanent value on `PlayerCharacterStatBlock.defense`
- Game Engine: snapshotted, modified by `RunModifier(ModifyDefense)`

### starting_guard

```text
Nom technique:    starting_guard
Nom affichable:   Garde de départ
Type:             integer
Valeur minimale:  0
Valeur max recommandée: 100
Services concernés: Catalog, Player, Game Engine
Description:      Garde passive restaurée au début de chaque round.
                  La garde absorbe les dégâts avant la vitalité.
Utilisation actuelle: Combatant.BaseGuard, RunModifier(StartingGuardBonus)
Utilisation future: additive from RunModifiers, capped by design constant
Exemple:          Le Porteur a starting_guard = 0. Après un Éclat de Garde (+8), starting_guard = 8.
```

**Ownership:**
- Catalog: base value on `EnemyDefinition.stat_block.starting_guard` (typically 0 for enemies)
- Player: permanent value on `PlayerCharacterStatBlock.starting_guard`
- Game Engine: computed as `base + SUM(active StartingGuardBonus modifiers)`, capped at `MaxStartingGuardBonus` (currently 30)

### current_guard

```text
Nom technique:    current_guard
Nom affichable:   Garde courante
Type:             integer
Valeur minimale:  0
Valeur max recommandée: 999
Services concernés: Game Engine only
Description:      Garde active pendant le combat. Peut dépasser starting_guard via des skills.
                  Reset au floor (base_guard) à chaque nouveau round.
Utilisation actuelle: Combatant.Guard, PlayerRuntimeState.Guard
Utilisation future: ATB guard action timing
Exemple:          Le Porteur utilise Garde (+5) → current_guard = 13 (base 8 + 5).
                  Au round suivant, current_guard revient à 8.
```

**Ownership:**
- Catalog: does not own
- Player: does not own
- Game Engine: runtime-only, mutable during combat, reset to `base_guard` each round

### speed

```text
Nom technique:    speed
Nom affichable:   Vitesse
Type:             integer
Valeur minimale:  1
Valeur max recommandée: 300
Services concernés: Catalog, Player, Game Engine
Description:      Détermine l'ordre d'action et, à terme, le taux de remplissage ATB.
Utilisation actuelle: Run.Speed (default 10), CombatInstance turn order (sorted by Speed DESC)
Utilisation future: atb_fill_rate = f(speed, modifiers)
Exemple:          Le Porteur a speed = 10. Un Loup d'Ombre a speed = 14 → agit en premier.
```

**Ownership:**
- Catalog: base value on `EnemyDefinition.stat_block.speed`
- Player: permanent value on `PlayerCharacterStat_block.speed`
- Game Engine: snapshotted, modified by `RunModifier(ModifySpeed)`

### initiative

```text
Nom technique:    initiative
Nom affichable:   Initiative
Type:             integer
Valeur minimale:  0
Valeur max recommandée: 300
Services concernés: Catalog, Player, Game Engine
Description:      Position initiale dans la timeline ATB. Détermine qui commence
                  en avantage au premier tour.
Utilisation actuelle: non utilisé (round-robin actuel)
Utilisation future: atb_initial_value = initiative
Exemple:          Le Porteur a initiative = 10. Un boss a initiative = 20 → le boss commence plus loin dans la timeline.
```

**Ownership:**
- Catalog: base value on `EnemyDefinition.stat_block.initiative`
- Player: permanent value on `PlayerCharacterStat_block.initiative`
- Game Engine: snapshotted, ATB-ready

### recovery

```text
Nom technique:    recovery
Nom affichable:   Récupération
Type:             integer
Valeur minimale:  0
Valeur max recommandée: 300
Services concernés: Catalog, Player, Game Engine
Description:      Temps de récupération après une action. Plus recovery est élevé,
                  plus le combattant est lent à reprendre son tour.
Utilisation actuelle: non utilisé
Utilisation future: action_recovery = skill.recovery_time + combatant.recovery + modifiers
Exemple:          Le Porteur a recovery = 5. Après Frappe (recovery_time = 10),
                  action_recovery = 15 ticks ATB.
```

**Ownership:**
- Catalog: base value on `EnemyDefinition.stat_block.recovery`
- Player: permanent value on `PlayerCharacterStat_block.recovery`
- Game Engine: snapshotted, modified by `RunModifier(ModifyRecovery)`

### focus

```text
Nom technique:    focus
Nom affichable:   Focus
Type:             integer
Valeur minimale:  0
Valeur max recommandée: 100
Services concernés: Catalog, Player, Game Engine
Description:      Ressource secondaire (si utilisée). Représente la concentration
                  ou l'énergie mentale du combattant.
Utilisation actuelle: non utilisé (mana et charge existent déjà)
Utilisation future: alternative ou complément à mana/charge
Exemple:          Le Porteur a focus = 0. Un skill futur pourrait consommer du focus.
```

**Ownership:**
- Catalog: base value on `EnemyDefinition.stat_block.focus`
- Player: permanent value on `PlayerCharacterStat_block.focus`
- Game Engine: snapshotted, mutable during combat

## Secondary stats (current, kept for compatibility)

### mana

```text
Nom technique:    mana
Nom affichable:   Mana
Type:             integer
Valeur minimale:  0
Valeur max recommandée: 999
Services concernés: Player, Game Engine
Description:      Ressource pour lancer des skills coûteux.
Utilisation actuelle: PlayerRuntimeState.Mana, Combatant.Mana, CombatantSkill.ManaCost
```

### charge

```text
Nom technique:    charge
Nom affichable:   Charge
Type:             integer
Valeur minimale:  0
Valeur max recommandée: 999
Services concernés: Player, Game Engine
Description:      Ressource secondaire pour les skills puissants.
Utilisation actuelle: PlayerRuntimeState.Charge, Combatant.Charge, CombatantSkill.ChargeCost
```

## ATB-ready stats (future, schema-only)

### action_cost

```text
Nom technique:    action_cost
Nom affichable:   Coût d'action
Type:             integer
Valeur minimale:  1
Valeur max recommandée: 100
Services concernés: Catalog (on SkillDefinition)
Description:      Nombre de ticks ATB consommés par l'utilisation du skill.
Utilisation actuelle: non utilisé
Utilisation future: déduit de la gauge ATB après utilisation
```

### cast_time

```text
Nom technique:    cast_time
Nom affichable:   Temps d'incantation
Type:             integer
Valeur minimale:  0
Valeur max recommandée: 100
Services concernés: Catalog (on SkillDefinition)
Description:      Nombre de ticks ATB avant que le skill ne se résolve.
                  0 = instantané.
Utilisation actuelle: non utilisé
Utilisation future: délai avant résolution, interruptible
```

### recovery_time

```text
Nom technique:    recovery_time
Nom affichable:   Temps de récupération
Type:             integer
Valeur minimale:  0
Valeur max recommandée: 200
Services concernés: Catalog (on SkillDefinition)
Description:      Nombre de ticks ATB ajoutés au recovery du combattant après utilisation.
Utilisation actuelle: non utilisé
Utilisation future: action_recovery = skill.recovery_time + combatant.recovery
```

## Scaling stats (design/balancing)

### difficulty_rating

```text
Nom technique:    difficulty_rating
Nom affichable:   Indice de difficulté
Type:             integer
Valeur minimale:  1
Valeur max recommandée: 20
Services concernés: Catalog (on EnemyDefinition)
Description:      Difficulté intrinsèque de l'ennemi, indépendante du contexte.
Utilisation actuelle: EnemyDefinition.BaseDifficulty
Utilisation future: input to encounter composition budget
Exemple:          Fragment de Doute = 1, Dernier Echo = 9, Him'Lit = 10
```

### difficulty_multiplier

```text
Nom technique:    difficulty_multiplier
Nom affichable:   Multiplicateur de difficulté
Type:             decimal(10,4)
Valeur minimale:  0.5
Valeur max recommandée: 3.0
Services concernés: Game Engine
Description:      Scala les stats des ennemis en fonction du risk level et des modifiers actifs.
Utilisation actuelle: CombatRiskProfile.DifficultyMultiplier = clamp(1.0 + riskDelta/100, 1.0, 1.75)
Utilisation future: appliqué aux stats de base de l'ennemi au moment de la création du combat
```

### reward_power_multiplier

```text
Nom technique:    reward_power_multiplier
Nom affichable:   Multiplicateur de puissance de récompense
Type:             decimal(10,4)
Valeur minimale:  0.5
Valeur max recommandée: 3.0
Services concernés: Game Engine
Description:      Scala les montants des récompenses en fonction du risque.
Utilisation actuelle: CombatRiskProfile.RewardPowerMultiplier (= DifficultyMultiplier)
Utilisation future: appliqué aux RewardChoice lors de la sélection
```

### threat_value

```text
Nom technique:    threat_value
Nom affichable:   Valeur de menace
Type:             integer
Valeur minimale:  0
Valeur max recommandée: 100
Services concernés: Catalog, Game Engine
Description:      Indicateur de priorité de ciblage pour l'IA ennemie.
Utilisation actuelle: non utilisé
Utilisation future: ciblage IA, focus fire
```

### encounter_weight

```text
Nom technique:    encounter_weight
Nom affichable:   Poids de rencontre
Type:             integer
Valeur minimale:  1
Valeur max recommandée: 100
Services concernés: Catalog (on EnemyDefinition)
Description:      Poids relatif de l'ennemi dans la sélection de composition de rencontre.
Utilisation actuelle: non formalisé (base_difficulty utilisé implicitement)
Utilisation future: input to Markov/adaptive encounter composition
```

## Stat usage by entity

| Stat | EnemyDefinition (Catalog) | PlayerCharacter (Player) | Combatant (Game Engine) | SkillDefinition (Catalog) |
|------|--------------------------|--------------------------|------------------------|--------------------------|
| `max_vitality` | stat_block column | stat_block column | snapshot + runtime | — |
| `current_vitality` | — | — | runtime only | — |
| `attack_power` | stat_block column | stat_block column | snapshot + modified | — |
| `defense` | stat_block column | stat_block column | snapshot + modified | — |
| `starting_guard` | stat_block column | stat_block column | computed from modifiers | — |
| `current_guard` | — | — | runtime only | — |
| `speed` | stat_block column | stat_block column | snapshot + modified | — |
| `initiative` | stat_block column | stat_block column | snapshot | — |
| `recovery` | stat_block column | stat_block column | snapshot + modified | — |
| `focus` | stat_block column | stat_block column | snapshot + runtime | — |
| `mana` | — | base_mana | snapshot + runtime | — |
| `charge` | — | base_charge | snapshot + runtime | — |
| `action_cost` | — | — | — | definition column |
| `cast_time` | — | — | — | definition column |
| `recovery_time` | — | — | — | definition column |
