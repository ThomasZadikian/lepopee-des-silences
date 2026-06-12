# Combat Difficulty Scaling

## Objectif

Appliquer le DifficultyMultiplier aux statistiques ennemies lors de la création d'un combat.

## Architecture

```
Catalog (stats de base)
    ↓
EncounterDraftGenerator (calcule DifficultyMultiplier)
    ↓
CombatEncounterDraft (contient DifficultyMultiplier)
    ↓
EnemyStatScaler (applique le scaling)
    ↓
CombatFactory (crée les Combatant ennemis)
```

## Comment ça fonctionne

### 1. Source du DifficultyMultiplier

Le `DifficultyMultiplier` est calculé par `CombatRiskProfileResolver` à partir du type d'événement et du niveau de risque :

```
riskDelta = max(0, actualRisk - baseRisk)
multiplier = clamp(1.0 + riskDelta / 100, 1.0, 1.75)
```

### 2. Application

Le multiplicateur est appliqué **une seule fois**, à la création du combat, dans `CombatFactory.CreateFromDraft`.

Flow :
1. `CombatEncounterDraftGenerator` récupère les définitions ennemies du Catalog
2. Le `DifficultyMultiplier` est calculé via `ICombatRiskProfileResolver`
3. Le multiplicateur est stocké dans `CombatEncounterDraft.DifficultyMultiplier`
4. `CombatFactory` passe le multiplicateur à `EnemyStatScaler.Scale`
5. Les stats ennemies sont calculées avant la création du `Combatant`

### 3. Stats impactées

| Stat | Formule | Arrondi |
|---|---|---|
| MaxVitality | `ceil(baseVitality * multiplier)` | Arrondi supérieur |
| Skill.BasePower | `ceil(basePower * multiplier)` | Arrondi supérieur |

Les stats du joueur ne sont **pas** impactées par ce multiplicateur.

### 4. Bornes de sécurité

| Borne | Valeur |
|---|---|
| MinMultiplier | 0.5 |
| MaxMultiplier | 3.0 |

Ces bornes empêchent :
- des ennemis avec 0 PV
- des ennemis avec des PV absurdes
- des dégâts négatifs ou overflow

## Exemples

| Enemy | BaseDifficulty | BaseVitality | Multiplier | FinalVitality |
|---|---|---|---|---|
| Fragment de Doute | 1 | 50 | 1.0 | 50 |
| Fragment de Doute | 1 | 50 | 1.25 | 63 |
| Fragment de Doute | 1 | 50 | 1.5 | 75 |
| Gardien de Porte | 5 | 90 | 1.0 | 90 |
| Gardien de Porte | 5 | 90 | 1.5 | 135 |
| Gardien de Porte | 5 | 90 | 0.75 | 68 |

## Non-objectifs

- Pas de difficulté dynamique
- Pas de scaling côté frontend
- Pas de modification des stats Catalog
- Pas de nouvelles mécaniques de combat
