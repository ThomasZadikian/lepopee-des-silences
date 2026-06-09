# Encounter Composition Rules

## Objectif

Les règles de composition déterminent quels ennemis apparaissent dans une rencontre.

## Pourquoi

Le nombre d'ennemis ne doit pas dépendre uniquement du RiskLevel.
La composition doit prendre en compte :

- EncounterType ;
- RoomType ;
- RoomIndex ;
- RiskLevel ;
- archétypes disponibles ;
- budget de difficulté.

## Budget

### Budget de base par RiskLevel

| RiskLevel | Budget |
|-----------|--------|
| 1         | 2      |
| 2         | 3      |
| 3         | 4      |
| 4         | 5      |
| 5         | 7      |

### Modificateurs

| Condition                     | Bonus |
|-------------------------------|-------|
| Elite                         | +2    |
| Rare                          | +1    |
| RoomIndex 3-5                 | +1    |
| RoomIndex 6+                  | +2    |

## Coût des archétypes

| Archétype   | Coût |
|-------------|------|
| Fragile     | 1    |
| Support     | 1    |
| Skirmisher  | 2    |
| Guard       | 2    |
| Disruptor   | 2    |
| Bruiser     | 3    |
| Elite       | 4    |
| Unknown     | 2    |

## Limites d'ennemis

| EncounterType | Min | Max |
|---------------|-----|-----|
| Combat        | 1   | 3   |
| Elite         | 1   | 2   |
| Rare          | 1   | 2   |
| RoomBoss      | 1   | 1   |

## Déterminisme

La composition doit être stable pour un même contexte.
Aucun Random non seedé ne doit être utilisé.

## Architecture

```
CombatEncounterDraftGenerator
  └─ récupère les EnemyDefinitions compatibles via ICatalogContentGateway
  └─ construit EncounterCompositionContext
  └─ appelle IEncounterCompositionPolicy.Compose(context)
  └─ utilise SelectedEnemies pour le draft final

EncounterCompositionPolicy (implémente IEncounterCompositionPolicy)
  └─ valide le contexte
  └─ calcule le budget (base RiskLevel + modificateurs)
  └─ filtre les ennemis par RiskLevel
  └─ sélectionne selon les règles de l'EncounterType
  └─ retourne EncounterCompositionResult
```

## Sélection par EncounterType

### Combat
- Trie les ennemis par coût d'archétype croissant, puis difficulté décroissante, puis clé
- Sélection greedy : pour chaque ennemi, si son coût tient dans le budget restant et qu'on n'a pas atteint la limite, on l'ajoute
- Fallback : si aucun ennemi ne tient dans le budget, prendre le moins coûteux

### Elite
- Préfère un ennemi avec tag "elite" ou archétype "Elite"
- Si trouvé, tente d'ajouter un second ennemi dans le budget restant
- Sinon, prend l'ennemi le plus difficile disponible

### Rare
- Préfère un ennemi avec archétype "Support" ou "Disruptor"
- Si trouvé, tente d'ajouter un second ennemi dans le budget restant
- Sinon, prend l'ennemi le plus difficile disponible

### RoomBoss
- Prend l'ennemi le plus difficile disponible
- Un seul ennemi pour l'instant

## Non-objectifs

Cette PR ne gère pas :

- les actions de combat ;
- les dégâts ;
- les tours ;
- l'IA ennemie ;
- les effets de statut ;
- le frontend.

## Future work

- pondération par seed ;
- influence des Lois du Palais ;
- influence des compagnons ;
- compositions spéciales par RoomType ;
- adds de boss ;
- rencontres narratives rares.
