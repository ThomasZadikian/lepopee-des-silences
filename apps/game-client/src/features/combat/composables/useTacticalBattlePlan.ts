import {
  isoUnit,
  projectToScreen,
  type ProjectionParams,
} from '../../palace-map/composables/useTerrainDrawPlan';
import type {
  FloorTint,
  RoomTheme,
  SpriteKey,
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
  /** À plat, row-major (`y * gridWidth + x`). */
  elevation: number[];
  /** Même indexation. Une case fausse porte un obstacle ou n'est pas dans la salle. */
  walkable: boolean[];
  /**
   * La case appartient-elle à la salle ? Sans cette distinction, tout ce qui borde la salle
   * reçoit un sprite d'obstacle et le plateau se retrouve noyé sous un décor inexistant.
   */
  floor?: boolean[];
  theme: RoomTheme;
  ambientTint: FloorTint;
  /** Cases où le combattant actif peut se rendre ce tour. Clés « x,y ». */
  reachableCells?: Set<string>;
  /** Cases couvertes par la compétence armée. Clés « x,y ». */
  targetableCells?: Set<string>;
  /** La case sous le pointeur. */
  hoveredCell?: BattleCell | null;
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
        // Une case de la salle qu'on ne peut pas fouler porte un obstacle. Elle se peint
        // toujours à la profondeur maximale pour ne jamais être traversée du regard.
        entries.push({
          cellKey,
          x,
          y,
          spriteKey: { kind: 'obstacle', theme: input.theme, elevation },
          screenX,
          screenY,
          elevation,
          sortKey: sortKeyFor(x, y, 3),
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
 * Dijkstra sur le sous-graphe praticable, coût d'un pas `1 + |Δélévation|` — la même règle que
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
  const reached = new Map<string, number>([[battleCellKey(origin.x, origin.y), 0]]);
  const frontier: Array<{ cell: BattleCell; spent: number }> = [{ cell: origin, spent: 0 }];

  const cellElevation = (x: number, y: number): number =>
    input.elevation[(y * input.gridWidth) + x] ?? 0;

  const walkable = (x: number, y: number): boolean => {
    if (x < 0 || y < 0 || x >= input.gridWidth || y >= input.gridHeight) return false;

    return input.walkable[(y * input.gridWidth) + x] ?? false;
  };

  while (frontier.length > 0) {
    // Tri par coût croissant : sans lui, un chemin cher trouvé tôt figerait une case que le
    // chemin bon marché aurait atteinte pour moins.
    frontier.sort((a, b) => a.spent - b.spent);
    const current = frontier.shift()!;

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
      const spent = current.spent + 1 + climb;

      if (spent > budget) continue;

      const best = reached.get(key);
      if (best !== undefined && best <= spent) continue;

      reached.set(key, spent);
      frontier.push({ cell: next, spent });
    }
  }

  reached.delete(battleCellKey(origin.x, origin.y));

  return new Set(reached.keys());
}

/** Distance de Manhattan — la métrique du déplacement orthogonal. */
export const manhattan = (a: BattleCell, b: BattleCell): number =>
  Math.abs(a.x - b.x) + Math.abs(a.y - b.y);

export { isoUnit, projectToScreen };
