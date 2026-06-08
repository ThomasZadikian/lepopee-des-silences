# alpha-0.1.10 — CurrentRoomIndex: position de Room dans la run infinie

**PR:** Backend Game Engine 0.1.10  
**Statut:** Introduit dans cette PR — incrément futur dans MoveToNextRoom (0.2.x)

---

## Problème

Le backend exposait plusieurs notions de "profondeur" qui prêtaient à confusion :

| Propriété | Source | Signification |
|---|---|---|
| `Room.Depth` | `Room` | Ordinal de la Room dans la séquence de run (technique, utilisé par `MoveToNextRoom`) |
| `Room.CurrentNodeDepth` | `Room` | Progression interne dans la RoomMap (couche de nodes actuelle) |
| `MapNode.Row` | `MapNode` | Position fixe d'un node dans la grille de la RoomMap |
| `Run.CurrentDepth` | Calculé depuis `Room.Depth` | Alias de `Room.Depth`, utilisé uniquement pour la validation de séquence |

Aucune de ces propriétés ne devait être utilisée pour afficher au joueur "dans quelle Salle il se trouve".

---

## Solution

### `Run.CurrentRoomIndex`

Propriété explicite ajoutée à l'agrégat `Run` :

```csharp
/// <summary>
/// Zero-based index of the current room in the infinite run sequence.
/// Threshold is always index 0. Incremented by MoveToNextRoom (future).
/// Display as CurrentRoomIndex + 1 for player-facing "Salle N" labels.
/// </summary>
public int CurrentRoomIndex { get; private set; }
```

**Règles :**
- Initialisée à `0` au démarrage de toute run (`Run.StartNew`).
- La Room de `CurrentRoomIndex = 0` est toujours **Threshold**.
- Ne change **jamais** pendant la progression interne d'une Room :
  - `ChooseNode` → aucun effet
  - `ResolveCurrentEvent` → aucun effet
  - `ProgressRun` (unlock next node layer) → aucun effet
  - Victoire contre RoomBoss → aucun effet dans cette PR
- Sera incrémentée dans une future PR par le flow **MoveToNextRoom / Interlude / EnterNextRoom**.

### `RunDto.CurrentRoomIndex` + `RunDto.CurrentRoomNumber`

Exposés dans `RunDto` via `RunDto.FromDomain(run)` :

```csharp
// Zéro-based — usage technique
int CurrentRoomIndex      // 0, 1, 2 …

// One-based — affichage joueur
int CurrentRoomNumber     // = CurrentRoomIndex + 1 → "Salle 1", "Salle 2" …
```

Tous les endpoints de run retournent ces champs automatiquement :
- `POST /api/v2/runs` (StartRun)
- `GET /api/v2/runs/{id}` (GetRunById)
- `POST /api/v2/runs/{id}/nodes/{nodeId}/choose` (ChooseNode)
- `POST /api/v2/runs/{id}/current-event/resolve` (ResolveCurrentEvent)
- `POST /api/v2/runs/{id}/progress` (ProgressRun)
- `POST /api/v2/runs/{id}/rewards/{offerId}/select` (SelectReward)

---

## Guide terminologique

| Terme | Portée | À utiliser pour |
|---|---|---|
| `CurrentRoomIndex` | Run | Position de la Room dans la run infinie (0-based) |
| `CurrentRoomNumber` | RunDto | Affichage joueur one-based ("Salle 1") |
| `Room.CurrentNodeDepth` | Room | Couche de nodes active dans la RoomMap courante |
| `MapNode.Row` | MapNode | Position fixe d'un node dans la grille (immuable après génération) |
| `Room.Depth` | Room | Ordinal interne utilisé par `MoveToNextRoom` pour la validation de séquence |
| `Run.CurrentDepth` | Run | Alias calculé de `Room.Depth` — ne pas utiliser pour l'affichage joueur |

**Règle simple :** pour savoir dans quelle Salle est le joueur → `CurrentRoomIndex`. Pour savoir où il en est dans la Salle → `CurrentNodeDepth`. Pour identifier un node dans la grille → `Row`.

---

## Ce qui n'est PAS implémenté dans cette PR

- Incrément de `CurrentRoomIndex` (réservé à MoveToNextRoom)
- Génération de la Room suivante
- Transition RoomCleared / Interlude
- Multi-room jouable
- Modification de la RoomMap ou de son template `[2, 3, 4, 3, 4, 3, 2, 1]`
- Frontend

---

## Contrat futur : MoveToNextRoom

Quand `MoveToNextRoom` sera implémenté (0.2.x), il devra :

```csharp
public void MoveToNextRoom(Room nextRoom)
{
    // … validations existantes …
    _rooms.Add(nextRoom);
    CurrentRoomId = nextRoom.Id;
    CurrentRoomIndex++;   // ← seul endroit où CurrentRoomIndex est incrémenté
    Status = RunStatus.Active;
}
```

`CurrentRoomIndex` ne doit jamais être dérivé de `Room.Depth` ou de `CurrentNodeDepth`.
