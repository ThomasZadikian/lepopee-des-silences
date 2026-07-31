import {
  isoUnit,
  projectToScreen,
  type ProjectionParams,
} from '../../palace-map/composables/useTerrainDrawPlan';
import { hashSeed } from '../../palace-map/composables/usePalaceTerrain';
import {
  TERRAIN_SPRITE_CONSTANTS,
  obstacleVariantCount,
  type FloorTint,
  type RoomTheme,
  type SpriteKey,
} from '../../palace-map/composables/useTerrainSprites';

/**
 * Le plan de dessin d'un champ de bataille tactique.
 *
 * Fonction pure, sans canvas ni DOM — c'est ce qui la rend testable sans navigateur, comme son
 * homologue d'exploration (`buildDrawPlan`). Elle réutilise la même projection isométrique et
 * la même forge de tuiles : le terrain de combat EST la salle d'exploration, vidée de ses
 * nœuds. Ce qu'elle n'emprunte pas, ce sont les préoccupations d'exploration — nœuds, brouillard,
 * caches à fouiller — qui n'ont plus cours une fois le combat engagé.
 */

export type BattleCell = { x: number; y: number };

export type BattleDrawPlanEntry = {
  cellKey: string;
  x: number;
  y: number;
  spriteKey: SpriteKey;
  screenX: number;
  screenY: number;
  elevation: number;
  /** Ordre du peintre : le fond et le bas d'abord. */
  sortKey: number;
};

export type BuildBattlePlanInput = {
  canvasWidth: number;
  canvasHeight: number;
  gridWidth: number;
  gridHeight: number;
  elevation: number[];
  walkable: boolean[];
  floor?: boolean[];
  theme: RoomTheme;
  ambientTint: FloorTint;
  reachableCells?: Set<string>;
  targetableCells?: Set<string>;
  blockedCells?: Set<string>;
  aoeCells?: Set<string>;
  threatCells?: Set<string>;
  heightCells?: Set<string>;
  occupiedCells?: Set<string>;
  hoveredCell?: BattleCell | null;
  pathCells?: Set<string>; // Ajout pour TS2339/TS2353
};

export const battleCellKey = (x: number, y: number): string => `${x},${y}`;

/**
 * Variation de brosse du sol, pour qu'un terrain uni ne se répète pas visiblement. Même
 * formule que l'exploration : deux rendus de la même salle doivent se ressembler.
 */
const surfaceSeedFor = (x: number, y: number): number => ((x * 7) + (y * 13)) % 5;

const elevationAt = (input: BuildBattlePlanInput, x: number, y: number): number =>
  input.elevation[(y * input.gridWidth) + x] ?? 0;

const isWalkable = (input: BuildBattlePlanInput, x: number, y: number): boolean => {
  if (x < 0 || y < 0 || x >= input.gridWidth || y >= input.gridHeight) return false;

  return input.walkable[(y * input.gridWidth) + x] ?? false;
};

/** Faute de masque, on retombe sur « praticable = dans la salle » : aucun obstacle peint. */
const isFloor = (input: BuildBattlePlanInput, x: number, y: number): boolean => {
  if (x < 0 || y < 0 || x >= input.gridWidth || y >= input.gridHeight) return false;

  return input.floor
    ? input.floor[(y * input.gridWidth) + x] ?? false
    : isWalkable(input, x, y);
};

/**
 * Profondeur du peintre. L'élévation entre dans la clé pour qu'une case haute et proche ne se
 * fasse pas recouvrir par une case basse et lointaine.
 */
const sortKeyFor = (x: number, y: number, elevation: number): number =>
  ((x + y) * 4) + elevation;

export function buildBattlePlan(input: BuildBattlePlanInput): BattleDrawPlanEntry[] {
  const projection: ProjectionParams = {
    canvasWidth: input.canvasWidth,
    canvasHeight: input.canvasHeight,
    gridWidth: input.gridWidth,
    gridHeight: input.gridHeight,
  };

  const entries: BattleDrawPlanEntry[] = [];

  for (let y = 0; y < input.gridHeight; y += 1) {
    for (let x = 0; x < input.gridWidth; x += 1) {
      // Hors de la salle : rien du tout. Ni sol, ni obstacle — le vide se peint en ne peignant
      // pas, et c'est ce qui donne au champ de bataille ses vrais bords.
      if (!isFloor(input, x, y)) continue;

      const cellKey = battleCellKey(x, y);
      const elevation = elevationAt(input, x, y);
      const { screenX, screenY } = projectToScreen(x, y, projection);
      const walkable = isWalkable(input, x, y);
      const sortKey = sortKeyFor(x, y, elevation);

      if (!walkable) {
        // Comme en exploration : le sprite d'obstacle est un remplacement complet du
        // sol (il peint déjà son propre socle), pas une silhouette posée par-dessus —
        // peindre un sol séparé dessous fait apparaître une géométrie de falaise qui
        // ne correspond à rien une fois l'obstacle dessiné.
        const wallVariants = obstacleVariantCount(input.theme);
        entries.push({
          cellKey,
          x,
          y,
          spriteKey: {
            kind: 'obstacle',
            theme: input.theme,
            variant: wallVariants > 0 ? hashSeed(cellKey) % wallVariants : 0,
            elevation,
          },
          screenX,
          screenY,
          elevation,
          sortKey: sortKeyFor(x, y, TERRAIN_SPRITE_CONSTANTS.MAX_ELEVATION),
        });
        continue;
      }

      entries.push({
        cellKey,
        x,
        y,
        spriteKey: {
          kind: 'floor',
          tint: input.ambientTint,
          theme: input.theme,
          elevation,
          surfaceSeed: surfaceSeedFor(x, y),
          // Une falaise se dessine là où la case avant-gauche ou avant-droite sort de la SALLE,
          // pas là où elle est simplement encombrée : un éboulis voisin ne creuse pas un
          // précipice. C'est ce qui donne au champ de bataille de vrais bords.
          cliffLeft: !isFloor(input, x, y + 1),
          cliffRight: !isFloor(input, x + 1, y),
        },
        screenX,
        screenY,
        elevation,
        sortKey,
      });

      // Les surbrillances se posent juste au-dessus de leur tuile, jamais au-dessus de la
      // suivante — d'où l'incrément fractionnaire.
      if (input.reachableCells?.has(cellKey)) {
        entries.push({
          cellKey: `${cellKey}:move`,
          x,
          y,
          spriteKey: { kind: 'highlight', variant: 'move', elevation },
          screenX,
          screenY,
          elevation,
          sortKey: sortKey + 0.1,
        });
      }

      if (input.threatCells?.has(cellKey)) {
        entries.push({
          cellKey: `${cellKey}:threat`,
          x,
          y,
          spriteKey: { kind: 'highlight', variant: 'threat', elevation },
          screenX,
          screenY,
          elevation,
          sortKey: sortKey + 0.12,
        });
      }

      // La zone d'effet passe par-dessus la zone de déplacement : quand une compétence est
      // armée, c'est elle que le joueur doit lire.
      if (input.targetableCells?.has(cellKey)) {
        entries.push({
          cellKey: `${cellKey}:attack`,
          x,
          y,
          spriteKey: { kind: 'highlight', variant: 'attack', elevation },
          screenX,
          screenY,
          elevation,
          sortKey: sortKey + 0.2,
        });
      }

      if (input.blockedCells?.has(cellKey)) {
        entries.push({
          cellKey: `${cellKey}:blocked`,
          x,
          y,
          spriteKey: { kind: 'highlight', variant: 'blocked', elevation },
          screenX,
          screenY,
          elevation,
          sortKey: sortKey + 0.21,
        });
      }

      if (input.pathCells?.has(cellKey)) {
        entries.push({
          cellKey: `${cellKey}:path`,
          x,
          y,
          spriteKey: { kind: 'highlight', variant: 'path', elevation },
          screenX,
          screenY,
          elevation,
          sortKey: sortKey + 0.24,
        });
      }

      if (input.aoeCells?.has(cellKey)) {
        entries.push({
          cellKey: `${cellKey}:aoe`,
          x,
          y,
          spriteKey: { kind: 'highlight', variant: 'aoe', elevation },
          screenX,
          screenY,
          elevation,
          sortKey: sortKey + 0.26,
        });
      }

      if (input.heightCells?.has(cellKey)) {
        entries.push({
          cellKey: `${cellKey}:height`,
          x,
          y,
          spriteKey: { kind: 'highlight', variant: 'height', elevation },
          screenX,
          screenY,
          elevation,
          sortKey: sortKey + 0.28,
        });
      }

      if (input.occupiedCells?.has(cellKey) && !input.aoeCells?.has(cellKey)) {
        entries.push({
          cellKey: `${cellKey}:occupied`,
          x,
          y,
          spriteKey: { kind: 'highlight', variant: 'occupied', elevation },
          screenX,
          screenY,
          elevation,
          sortKey: sortKey + 0.29,
        });
      }

      if (input.hoveredCell && input.hoveredCell.x === x && input.hoveredCell.y === y) {
        entries.push({
          cellKey: `${cellKey}:cursor`,
          x,
          y,
          spriteKey: { kind: 'highlight', variant: 'cursor', elevation },
          screenX,
          screenY,
          elevation,
          sortKey: sortKey + 0.3,
        });
      }
    }
  }

  return entries.sort((a, b) => a.sortKey - b.sortKey);
}

/**
 * Les cases atteignables par un combattant, à budget donné.
 *
 * A* (A-Star) sur le sous-graphe praticable, coût d'un pas `1 + |Δélévation|` — la même règle que
 * `TacticalMovement.StepCost` côté serveur. Cette copie est <b>un aperçu</b>, pas une autorité :
 * le serveur retranche seul, et son refus reste le dernier mot. Elle existe pour que le joueur
 * voie où il peut aller avant de cliquer, pas pour valider quoi que ce soit.
 */
export function reachableCellsFrom(
  input: Pick<BuildBattlePlanInput, 'gridWidth' | 'gridHeight' | 'elevation' | 'walkable'>,
  origin: BattleCell,
  budget: number,
  occupied: ReadonlySet<string>,
): Set<string> {
  return reachableCellsWithPathsFrom(input, origin, budget, occupied).cells;
}

// Structure pour la frontière A* (priorité = f = g + h)
type AStarFrontierNode = {
  cell: BattleCell;
  g: number; // Coût réel depuis l'origine
  h: number; // Heuristique vers le but (0 si pas de but fixe)
  f: number; // g + h
  previous: BattleCell;
};

/**
 * Version A* de reachableCellsWithPathsFrom pour une exploration optimisée.
 * Utilise une heuristique de Manhattan (h = 0 si pas de but fixe, car on explore tout dans le budget).
 */
export function reachableCellsWithPathsFrom(
  input: Pick<BuildBattlePlanInput, 'gridWidth' | 'gridHeight' | 'elevation' | 'walkable'>,
  origin: BattleCell,
  budget: number,
  occupied: ReadonlySet<string>,
): { cells: Set<string>; previous: Map<string, BattleCell> } {
  const reached = new Map<string, number>([[battleCellKey(origin.x, origin.y), 0]]);
  const previous = new Map<string, BattleCell>();
  const frontier: AStarFrontierNode[] = [{
    cell: origin,
    g: 0,
    h: 0, // Pas de but fixe, donc h = 0
    f: 0,
    previous: origin,
  }];

  const cellElevation = (x: number, y: number): number =>
    input.elevation[(y * input.gridWidth) + x] ?? 0;

  const walkable = (x: number, y: number): boolean => {
    if (x < 0 || y < 0 || x >= input.gridWidth || y >= input.gridHeight) return false;
    return input.walkable[(y * input.gridWidth) + x] ?? false;
  };

  while (frontier.length > 0) {
    // Trier par f (priorité A*) : les cases avec le plus petit f sont traitées en premier
    frontier.sort((a, b) => a.f - b.f);
    const current = frontier.shift()!;

    // Si on dépasse le budget, on saute (optimisation)
    if (current.g > budget) continue;

    const neighbours: BattleCell[] = [
      { x: current.cell.x + 1, y: current.cell.y },
      { x: current.cell.x - 1, y: current.cell.y },
      { x: current.cell.x, y: current.cell.y + 1 },
      { x: current.cell.x, y: current.cell.y - 1 },
    ];

    for (const next of neighbours) {
      if (!walkable(next.x, next.y)) continue;

      const key = battleCellKey(next.x, next.y);
      if (occupied.has(key)) continue;

      const climb = Math.abs(
        cellElevation(next.x, next.y) - cellElevation(current.cell.x, current.cell.y),
      );
      const g = current.g + 1 + climb;

      if (g > budget) continue;

      const best = reached.get(key);
      if (best !== undefined && best <= g) continue;

      reached.set(key, g);
      previous.set(key, current.previous);
      frontier.push({
        cell: next,
        g,
        h: 0, // Pas de but fixe
        f: g, // f = g + h = g
        previous: current.cell,
      });
    }
  }

  reached.delete(battleCellKey(origin.x, origin.y));
  return { cells: new Set(reached.keys()), previous };
}

/**
 * Vérifie si une ligne de vue (Line of Sight) existe entre deux cases,
 * en tenant compte des obstacles et des différences d'élévation.
 *
 * @param input - Données du champ de bataille (grille, élévations, etc.).
 * @param from - Case de départ.
 * @param to - Case d'arrivée.
 * @param maxClimb - Différence d'élévation maximale autorisée entre deux cases adjacentes (défaut: 1).
 * @returns true si la ligne de vue est dégagée, false sinon.
 */
export function hasLos(
  input: Pick<BuildBattlePlanInput, 'gridWidth' | 'gridHeight' | 'elevation' | 'walkable' | 'floor'>,
  from: BattleCell,
  to: BattleCell,
  maxClimb: number = 1,
): boolean {
  if (manhattan(from, to) <= 1) return true;

  const isFloor = (x: number, y: number): boolean => {
    if (x < 0 || y < 0 || x >= input.gridWidth || y >= input.gridHeight) return false;
    return input.floor
      ? input.floor[(y * input.gridWidth) + x] ?? false
      : input.walkable[(y * input.gridWidth) + x] ?? false;
  };

  const cellElevation = (x: number, y: number): number =>
    input.elevation[(y * input.gridWidth) + x] ?? 0;

  const dx = Math.abs(to.x - from.x);
  const dy = Math.abs(to.y - from.y);
  const sx = from.x < to.x ? 1 : -1;
  const sy = from.y < to.y ? 1 : -1;
  let err = dx - dy;
  let x = from.x;
  let y = from.y;

  let prevElev = cellElevation(from.x, from.y);

  while (x !== to.x || y !== to.y) {
    const e2 = 2 * err;
    if (e2 > -dy) { err -= dy; x += sx; }
    if (e2 < dx) { err += dx; y += sy; }

    if (x === to.x && y === to.y) break;

    // Vérifier que la case est bien dans la salle
    if (!isFloor(x, y)) return false;

    const currentElev = cellElevation(x, y);
    // Bloquer si la différence d'élévation avec la case précédente est trop grande
    if (Math.abs(currentElev - prevElev) > maxClimb) return false;

    prevElev = currentElev;
  }

  return true;
}

/** Distance de Manhattan — la métrique du déplacement orthogonal. */
export const manhattan = (a: BattleCell, b: BattleCell): number =>
  Math.abs(a.x - b.x) + Math.abs(a.y - b.y);

/**
 * Vérifie si une ligne de vue (Line of Sight) existe entre deux cases,
 * en utilisant l'algorithme de Bresenham pour tracer une ligne droite.
 *
 * @deprecated Utiliser `hasLos` à la place (plus complet).
 */
export function hasDirectLos(
  input: Pick<BuildBattlePlanInput, 'gridWidth' | 'gridHeight' | 'elevation' | 'walkable' | 'floor'>,
  from: BattleCell,
  to: BattleCell,
): boolean {
  return hasLos(input, from, to);
}

export { isoUnit, projectToScreen };
