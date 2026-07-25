/**
 * What a move would actually cost, computed client-side.
 *
 * This deliberately reverses an earlier decision. The rule until now was "no client-side
 * pathfinding, let the server refuse" — sound while the board only ever needed to dispatch a
 * move. It stopped being sound once the board had to ANSWER two questions before the click:
 * which cells are affordable, and which way would the party walk. Neither can be answered
 * without the route, and a server round-trip per hovered cell is not an option.
 *
 * So this is a second implementation of a rule that already exists in the domain, and that is a
 * real cost: the two can drift. It is contained deliberately —
 *
 *   - The server stays the authority. Nothing here gates the request; `movePartyTo` still
 *     dispatches optimistically and a `DomainException` still surfaces in `error`. If this
 *     module is wrong, the move is refused exactly as before — the UI misleads, it never
 *     corrupts.
 *   - Every rule below mirrors a named counterpart in `RoomGrid.cs` / `Room.cs`, cited inline,
 *     so a change over there has one place to look over here.
 *
 * Mirrored rules: `RoomGrid.FindPath` (Dijkstra, orthogonal, edge weight `1 + max(0, climb)`,
 * transit blockers reachable but not crossable) and `RoomGrid.MoveTo` (a walk truncates at the
 * first node that fires on contact, and is charged only for the steps it actually covered).
 */

export type Cell = { x: number; y: number };

export type MovementRangeInput = {
  gridWidth: number;
  gridHeight: number;
  /** Flat row-major, index = y * gridWidth + x. */
  elevation: number[];
  /** Bounds + holes in the room's shape + obstacles, in one test (mirrors `RoomGrid.IsWalkable`). */
  isWalkable: (x: number, y: number) => boolean;
  party: Cell;
  /** Cells a path may END on but never cross — mirrors `Room.CurrentTransitBlockers`. */
  transitBlockers: Set<string>;
  /** Cells that resolve the moment they are stepped on — mirrors `MapNode.TriggersOnContact`. */
  contactTriggers: Set<string>;
};

export type Route = {
  /** Steps after the party's own cell, in walking order, already truncated at a contact node. */
  path: Cell[];
  /** Budget actually spent — re-priced per step, as a truncated walk is charged for its steps. */
  cost: number;
  /** True when a node fires partway and the party stops short of the cell that was clicked. */
  truncated: boolean;
};

export type MovementRange = {
  /** Full cost to reach a cell, ignoring truncation. Absent when no path exists at all. */
  costTo: (x: number, y: number) => number | null;
  /** The walk that clicking a cell would actually produce, or null when unreachable. */
  routeTo: (x: number, y: number) => Route | null;
  /** Keys of every cell reachable within `budget`, party's own cell excluded. */
  within: (budget: number) => Set<string>;
};

const ORTHOGONAL: readonly Cell[] = [
  { x: 0, y: -1 }, { x: 1, y: 0 }, { x: 0, y: 1 }, { x: -1, y: 0 },
];

export function cellKey(x: number, y: number): string {
  return `${x},${y}`;
}

export function buildMovementRange(input: MovementRangeInput): MovementRange {
  const { gridWidth: w, gridHeight: h } = input;
  const size = w * h;
  const indexOf = (x: number, y: number) => (y * w) + x;
  const elevationAt = (x: number, y: number) => input.elevation[indexOf(x, y)] ?? 0;

  const distance = new Array<number>(size).fill(Number.POSITIVE_INFINITY);
  const previous = new Array<number>(size).fill(-1);
  const visited = new Array<boolean>(size).fill(false);

  const startInBounds = input.party.x >= 0 && input.party.x < w
    && input.party.y >= 0 && input.party.y < h;

  if (size > 0 && startInBounds) {
    distance[indexOf(input.party.x, input.party.y)] = 0;

    // Linear scan for the next node rather than a heap: the board is ~80 cells, and matching
    // the domain's own straightforward implementation is worth more here than the constant.
    for (let iteration = 0; iteration < size; iteration++) {
      let current = -1;
      let best = Number.POSITIVE_INFINITY;

      for (let i = 0; i < size; i++) {
        if (!visited[i] && distance[i] < best) {
          current = i;
          best = distance[i];
        }
      }

      if (current === -1) break;
      visited[current] = true;

      const cx = current % w;
      const cy = Math.floor(current / w);

      // Settled its own distance, so it stays a valid destination — but we never expand past it.
      if (input.transitBlockers.has(cellKey(cx, cy))) continue;

      for (const step of ORTHOGONAL) {
        const nx = cx + step.x;
        const ny = cy + step.y;
        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
        if (!input.isWalkable(nx, ny)) continue;

        const neighbor = indexOf(nx, ny);
        if (visited[neighbor]) continue;

        // Climbing costs; descending is free.
        const candidate = distance[current] + 1 + Math.max(0, elevationAt(nx, ny) - elevationAt(cx, cy));
        if (candidate < distance[neighbor]) {
          distance[neighbor] = candidate;
          previous[neighbor] = current;
        }
      }
    }
  }

  function inBounds(x: number, y: number): boolean {
    return x >= 0 && x < w && y >= 0 && y < h;
  }

  function costTo(x: number, y: number): number | null {
    if (!inBounds(x, y)) return null;
    const d = distance[indexOf(x, y)];
    return Number.isFinite(d) ? d : null;
  }

  function routeTo(x: number, y: number): Route | null {
    if (costTo(x, y) === null) return null;

    const full: Cell[] = [];
    for (let node = indexOf(x, y); node !== -1; node = previous[node]) {
      full.unshift({ x: node % w, y: Math.floor(node / w) });
    }
    full.shift(); // the party's own cell is where the walk starts, not a step of it

    const path: Cell[] = [];
    let spent = 0;
    let previousCell: Cell = input.party;
    let truncated = false;

    for (const step of full) {
      spent += 1 + Math.max(0, elevationAt(step.x, step.y) - elevationAt(previousCell.x, previousCell.y));
      previousCell = step;
      path.push(step);

      if (input.contactTriggers.has(cellKey(step.x, step.y))) {
        truncated = step.x !== x || step.y !== y;
        break;
      }
    }

    return { path, cost: spent, truncated };
  }

  function within(budget: number): Set<string> {
    const reachable = new Set<string>();
    for (let y = 0; y < h; y++) {
      for (let x = 0; x < w; x++) {
        if (x === input.party.x && y === input.party.y) continue;
        const cost = costTo(x, y);
        if (cost !== null && cost <= budget) reachable.add(cellKey(x, y));
      }
    }
    return reachable;
  }

  return { costTo, routeTo, within };
}
