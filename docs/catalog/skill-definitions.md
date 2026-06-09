# Skill Definitions

Les Skill Definitions définissent les compétences utilisables par les ennemis et les alliés durant les combats.

## Structure

| Champ | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Identifiant unique |
| `Key` | `string` | Identifiant textuel (ex: `skill.basic.strike`) |
| `Name` | `string` | Nom localisé |
| `Description` | `string` | Description localisée |
| `Version` | `string` | Version sémantique |
| `Status` | `Draft \| Active \| Deprecated \| Disabled` | Statut du contenu |
| `SkillType` | `string` | Catégorie de la compétence (ex: `Damage`, `Defense`, `Buff`, `Debuff`) |
| `TargetingType` | `string` | Type de ciblage (ex: `Self`, `SingleEnemy`, `AllAllies`) |
| `EffectType` | `string` | Type d'effet (ex: `Damage`, `Buff`, `Debuff`) |
| `ManaCost` | `int` | Coût en mana |
| `ChargeCost` | `int` | Coût en charge |
| `BasePower` | `int` | Puissance de base (dégâts ou soins) |

## Endpoints

### `GET /api/v2/catalog/skill-definitions`

Liste toutes les définitions actives.

### `GET /api/v2/catalog/skill-definitions/{key}`

Récupère une définition par sa clé.

### `GET /api/v2/catalog/skill-definitions/type/{skillType}`

Liste les définitions actives filtrées par type (`Damage`, `Defense`, `Buff`, `Debuff`).

### `POST /api/v2/catalog/skill-definitions/batch/by-keys`

Liste les définitions dont les clés sont fournies dans le body :

```json
{
  "keys": ["skill.basic.strike", "skill.basic.guard"]
}
```

## Seeds

| Key | Name | Type | Ciblage | Mana | Charge |
|-----|------|------|---------|------|--------|
| `skill.basic.strike` | Frappe | Damage | SingleEnemy | 5 | 0 |
| `skill.basic.guard` | Garde | Defense | Self | 3 | 1 |
| `skill.basic.weaken` | Affaiblissement | Debuff | SingleEnemy | 4 | 0 |
| `skill.basic.disrupt` | Perturbation | Debuff | SingleEnemy | 6 | 1 |
| `skill.basic.focus` | Concentration | Buff | Self | 2 | 0 |

## Limites connues

- `SkillType`, `TargetingType` et `EffectType` sont des `string` (pas d'enums) pour garder une frontière souple avec le Game Engine.
- La méthode `ListByKeysAsync` ignore silencieusement les clés inconnues (aucune erreur retournée).
