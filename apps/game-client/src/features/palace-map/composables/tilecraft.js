/**
 * TILECRAFT — moteur de tuiles isométriques "peintes" pour L'Épopée des Silences.
 *
 * Remplace un rendu en aplats par des tuiles texturées façon Final Fantasy Tactics :
 * dalles, strates de roche, grain, lumière rasante venue du haut-gauche, contremarches
 * lisibles, murs à silhouette imposante, bords de falaise qui plongent dans le noir.
 *
 * Aucune dépendance : Canvas2D pur. Chaque variante est peinte UNE fois dans un canvas
 * offscreen transparent, mise en cache par clé, puis blittée N fois par le renderer.
 * Format compatible avec l'existant : diamant 2:1, 4 niveaux d'élévation, ancre au sol
 * exprimée en ratio de la hauteur du sprite (donc indépendante de la taille à l'écran).
 */

// ── Géométrie de référence (espace sprite, pas espace écran) ─────────────────────────
export const TILE = { W: 128, H: 64, STEP: 20, MAX: 3, PAD: 46 };
export const SPRITE_W = TILE.W;
export const SPRITE_H = TILE.H + TILE.MAX * TILE.STEP + TILE.PAD; // 170
export const GROUND_ANCHOR_Y = TILE.MAX * TILE.STEP + TILE.H / 2; // 92
export const GROUND_ANCHOR_RATIO = GROUND_ANCHOR_Y / SPRITE_H;
/** Les décors verticaux (colonnes, troncs, colonnes de lumière) débordent vers le haut. */
export const PROP_EXTRA_H = 150;
export const PROP_SPRITE_H = SPRITE_H + PROP_EXTRA_H;
export const PROP_GROUND_ANCHOR_RATIO = (GROUND_ANCHOR_Y + PROP_EXTRA_H) / PROP_SPRITE_H;

// ── Aléatoire déterministe ───────────────────────────────────────────────────────────
export function hashSeed(str) {
  let h = 2166136261;
  for (let i = 0; i < str.length; i++) h = Math.imul(h ^ str.charCodeAt(i), 16777619);
  return h >>> 0;
}
function makeRng(seed) {
  let a = seed >>> 0;
  return function () {
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
const R2 = (r, a, b) => a + r() * (b - a);

// ── Couleur ──────────────────────────────────────────────────────────────────────────
function hx(c) {
  if (Array.isArray(c)) return c;
  const h = c.replace('#', '');
  return [parseInt(h.slice(0, 2), 16), parseInt(h.slice(2, 4), 16), parseInt(h.slice(4, 6), 16)];
}
function rgba(c, a) {
  const [r, g, b] = hx(c);
  return `rgba(${r | 0},${g | 0},${b | 0},${a})`;
}
function mix(a, b, t) {
  const A = hx(a), B = hx(b);
  return [A[0] + (B[0] - A[0]) * t, A[1] + (B[1] - A[1]) * t, A[2] + (B[2] - A[2]) * t];
}
/** amt > 0 éclaircit, amt < 0 assombrit. */
function shade(c, amt) {
  return amt >= 0 ? mix(c, '#ffffff', amt) : mix(c, '#000000', -amt);
}

// ── Les 7 thèmes de salle ────────────────────────────────────────────────────────────
// tint : le token d'accent déjà utilisé côté app. surface/wall/particle : vocabulaire
// de peinture. sky : dégradé de fond, du plus lointain au plus proche de la source.
export const THEMES = {
  Threshold: {
    label: 'Seuil', tint: 'frost', surface: 'plank', wall: 'monolith', walls: ['monolith', 'rubble', 'gate'], particle: 'mote',
    top: '#8b9dcf', topDeep: '#39406c', seam: '#22284a',
    riser: '#39406e', riserDeep: '#171a33',
    accent: '#b6c8ff', glow: '#dbe7ff',
    sky: ['#0d1024', '#1b2145', '#3a4585'],
    props: ['beam', 'arch'],
  },
  Memory: {
    label: 'Mémoire', tint: 'gold', surface: 'parchment', wall: 'shelf', walls: ['shelf', 'crates', 'brokenColumn'], particle: 'flake',
    top: '#a48e63', topDeep: '#463a26', seam: '#2c2418',
    riser: '#4a3d28', riserDeep: '#1d1710',
    accent: '#e8c877', glow: '#ffe6ac',
    sky: ['#150f09', '#2c2114', '#584127'],
    props: ['column', 'cairn'],
  },
  Forest: {
    label: 'Forêt', tint: 'sap', surface: 'moss', wall: 'trunk', walls: ['trunk', 'deadfall', 'boulder'], particle: 'spore',
    top: '#5d7f62', topDeep: '#223529', seam: '#16241b',
    riser: '#33452f', riserDeep: '#121c14',
    accent: '#93dcb4', glow: '#c9f5db',
    sky: ['#080f0b', '#12241a', '#2a4a33'],
    props: ['trunk', 'cairn'],
  },
  Rupture: {
    label: 'Rupture', tint: 'blood', surface: 'fracture', wall: 'shard', walls: ['shard', 'rubble', 'boulder'], particle: 'ember',
    top: '#7d504c', topDeep: '#341d20', seam: '#1d1013',
    riser: '#3d2224', riserDeep: '#170b0d',
    accent: '#f0937f', glow: '#ffb9a3',
    sky: ['#0f0709', '#2a1013', '#5c1f22'],
    props: ['spire', 'cairn'],
  },
  Silence: {
    label: 'Silence', tint: 'frost', surface: 'ripple', wall: 'obelisk', walls: ['obelisk', 'boulder', 'rubble'], particle: 'dust',
    top: '#8e9aac', topDeep: '#3e4753', seam: '#272d36',
    riser: '#3f4854', riserDeep: '#191d23',
    accent: '#d3e3ee', glow: '#eef6fb',
    sky: ['#0c0f12', '#1c2228', '#3a464f'],
    props: ['obeliskProp', 'beam'],
  },
  Antechamber: {
    label: 'Antichambre', tint: 'gold', surface: 'marble', wall: 'column', walls: ['column', 'brokenColumn', 'gate'], particle: 'gilt',
    top: '#998763', topDeep: '#413827', seam: '#282116',
    riser: '#463c29', riserDeep: '#1b160f',
    accent: '#dcb45c', glow: '#ffe2a0',
    sky: ['#120d08', '#291f12', '#544022'],
    props: ['column', 'arch'],
  },
  Final: {
    label: 'Confrontation', tint: 'blood', surface: 'pulse', wall: 'spire', walls: ['spire', 'shard', 'gate'], particle: 'emberDark',
    top: '#65373d', topDeep: '#27151b', seam: '#170c10',
    riser: '#2f181d', riserDeep: '#120709',
    accent: '#ff8874', glow: '#ffb09c',
    sky: ['#080406', '#25090e', '#600f18'],
    props: ['spire', 'obeliskProp'],
  },
};
export const THEME_NAMES = Object.keys(THEMES);

// ── Les 27 salles canon ──────────────────────────────────────────────────────────────
// Une salle = un thème de rendu de base + un jeu de surcharges. `base` garantit qu'une
// salle inconnue ou un champ non surchargé retombe sur un thème valide ; les surcharges
// portent l'identité (palette, vocabulaire de surface, murs, décors, particules, brume).
// La clé est le `catalogRoomKey` déjà présent dans RoomDto : aucun travail backend.
export const ROOMS = {
  // ── Niveau 0 ──
  'room.halldentree': {
    label: "Hall d'entrée", base: 'Antechamber', chain: null, step: 0,
    surface: 'carpet', rug: '#8e2f36', walls: ['column', 'gate', 'brokenColumn'], props: ['column', 'beam'],
    top: '#b9a377', topDeep: '#4c3f28', seam: '#2a2216', riser: '#54452c', riserDeep: '#1d1710',
    accent: '#e8c069', glow: '#fff1c4', particle: 'gilt',
    sky: ['#140d09', '#33210f', '#77531f'], fog: '#1b1410',
  },
  'room.palier': {
    label: 'Palier', base: 'Memory', chain: null, step: 1,
    surface: 'parchment', walls: ['shelf', 'crates', 'monolith'], props: ['column', 'cairn'],
    top: '#9c8a63', topDeep: '#3f3627', seam: '#272016',
    accent: '#f0d489', glow: '#fff4cd', particle: 'flake',
    sky: ['#100c0a', '#241d16', '#4d3d29'], fog: '#171310',
  },
  'room.couloirs': {
    label: 'Couloirs', base: 'Silence', chain: null, step: 1,
    surface: 'carpet', rug: '#5e2029', walls: ['gate', 'rubble', 'monolith'], props: ['obeliskProp', 'cairn'],
    top: '#6f707a', topDeep: '#2f3138', seam: '#1d1f24', riser: '#333540', riserDeep: '#14151a',
    accent: '#b9bfd0', glow: '#dfe5f0', particle: 'dust',
    sky: ['#0a0b0e', '#161a20', '#2f353f'], fog: '#12141a',
  },

  // ── Niveau 2 — culs-de-sac et portes ──
  'room.feelings': {
    label: 'Pièce des émotions', base: 'Threshold', chain: null, step: 2,
    surface: 'plank', walls: ['crates', 'shelf', 'brokenColumn'], props: ['beam', 'cairn'],
    top: '#9f86a8', topDeep: '#413353', seam: '#281f38', riser: '#43354f', riserDeep: '#1a1425',
    accent: '#d8aee0', glow: '#f4dcf6', particle: 'mote',
    sky: ['#0f0a15', '#221733', '#4a3168'], fog: '#170f1e',
  },
  'room.turtle': {
    label: 'Passage brisé, vers la tortue', base: 'Rupture', chain: null, step: 2,
    surface: 'fracture', walls: ['shard', 'rubble', 'boulder'], props: ['spire', 'cairn'],
    top: '#6b5a86', topDeep: '#2b2143', seam: '#1a1430', riser: '#302446', riserDeep: '#120d1f',
    accent: '#b78cff', glow: '#e2ccff', particle: 'ember',
    sky: ['#08060f', '#1c1030', '#472a7d'], fog: '#140d20',
  },
  'room.enfermement': {
    label: 'Pièce camisolée', base: 'Silence', chain: null, step: 2,
    surface: 'flagstone', walls: ['gate', 'monolith', 'rubble'], props: ['obeliskProp', 'cairn'],
    top: '#5f666a', topDeep: '#282d31', seam: '#191d20', riser: '#2c3236', riserDeep: '#111416',
    accent: '#9fb2b8', glow: '#cfdde1', particle: 'dust',
    sky: ['#070809', '#101416', '#242c30'], fog: '#0e1113',
  },
  'room.meditation': {
    label: 'Salle de méditation', base: 'Threshold', chain: null, step: 2,
    surface: 'marble', walls: ['column', 'brokenColumn', 'obelisk'], props: ['beam', 'arch'],
    top: '#b6c4dc', topDeep: '#4e5a75', seam: '#333c52', riser: '#54617d', riserDeep: '#232a3a',
    accent: '#d9e8ff', glow: '#f4faff', particle: 'mote',
    sky: ['#1a2338', '#3a4a70', '#8fa8d4'], fog: '#26314a',
  },
  'room.room08': {
    label: 'Chambre 08', base: 'Memory', chain: null, step: 2,
    surface: 'plank', walls: ['crates', 'shelf', 'brokenColumn'], props: ['cairn', 'beam'],
    top: '#a4825c', topDeep: '#463323', seam: '#2b1e14', riser: '#4d3826', riserDeep: '#1e140d',
    accent: '#f5b07a', glow: '#ffdcbc', particle: 'flake',
    sky: ['#120b08', '#2b1710', '#5e2f1c'], fog: '#1a1210',
  },
  'room.chambredelise': {
    label: "Chambre d'Elise", base: 'Antechamber', chain: null, step: 2,
    surface: 'marble', walls: ['column', 'gate', 'brokenColumn'], props: ['column', 'arch'],
    top: '#bfa48f', topDeep: '#4f3d3a', seam: '#302426', riser: '#553f3d', riserDeep: '#1f1719',
    accent: '#f3c3ac', glow: '#ffe6d8', particle: 'gilt',
    sky: ['#150e0f', '#33191d', '#7a3a3c'], fog: '#1d1416',
  },
  'room.jardin': {
    label: 'Le jardin', base: 'Forest', chain: 'soleil', step: 1,
    surface: 'moss', walls: ['trunk', 'deadfall', 'boulder'], props: ['trunk', 'arch'],
    top: '#79a06a', topDeep: '#31492f', seam: '#1f3020', riser: '#3d5636', riserDeep: '#182617',
    accent: '#c9f08f', glow: '#eaffd2', particle: 'petal',
    sky: ['#2a3f2c', '#4b6b45', '#a9c98a'], fog: '#3a5039',
  },
  'room.falaise': {
    label: 'La falaise', base: 'Silence', chain: 'enfers', step: 1,
    surface: 'flagstone', walls: ['boulder', 'rubble', 'monolith'], props: ['cairn', 'obeliskProp'],
    top: '#6d6577', topDeep: '#2e2a3a', seam: '#1d1a27', riser: '#332f42', riserDeep: '#13111c',
    accent: '#a891d6', glow: '#d8caf2', particle: 'dust',
    sky: ['#0b0812', '#1d1731', '#4b3577'], fog: '#151020',
  },
  'room.montagne': {
    label: 'La montagne', base: 'Silence', chain: 'montagne', step: 1,
    surface: 'flagstone', walls: ['boulder', 'rubble', 'obelisk'], props: ['cairn', 'obeliskProp'],
    top: '#8e9aa6', topDeep: '#3e454e', seam: '#282d34', riser: '#434a54', riserDeep: '#1a1d22',
    accent: '#dbe8f2', glow: '#f6fbff', particle: 'snow',
    sky: ['#1b2430', '#3d4f63', '#8fa6bd'], fog: '#2b3540',
  },
  'room.labyrinthe': {
    label: 'Labyrinthe', base: 'Memory', chain: null, step: 3,
    surface: 'flagstone', walls: ['monolith', 'gate', 'shelf'], props: ['column', 'cairn'],
    top: '#8a7548', topDeep: '#3b311e', seam: '#241d12', riser: '#413522', riserDeep: '#181209',
    accent: '#e3bf6a', glow: '#ffe9ae', particle: 'flake',
    sky: ['#0c0906', '#1e170d', '#3f2f18'], fog: '#13100b',
  },
  'room.hopital': {
    label: "L'hopital", base: 'Silence', chain: 'hopital', step: 1,
    surface: 'clinic', walls: ['gate', 'crates', 'monolith'], props: ['beam', 'obeliskProp'],
    top: '#c6cfc8', topDeep: '#5c6660', seam: '#3d4642', riser: '#646d67', riserDeep: '#262d2a',
    accent: '#e6f2ea', glow: '#ffffff', particle: 'dust',
    sky: ['#1c2422', '#39463f', '#7f938a'], fog: '#2b342f',
  },
  'room.faille': {
    label: 'La faille', base: 'Final', chain: null, step: 3,
    surface: 'pulse', walls: ['shard', 'spire', 'gate'], props: ['spire', 'beam'],
    top: '#5f4a86', topDeep: '#251c45', seam: '#150f2c', riser: '#2b2050', riserDeep: '#0f0a1f',
    accent: '#c69bff', glow: '#eddcff', particle: 'emberDark',
    sky: ['#050310', '#1a0b38', '#5a1fa8'], fog: '#120a22',
  },

  // ── Chaîne A — Les Enfers (dégradation : cendre → leurre → métal → sang) ──
  'room.enfer1': {
    label: 'Les enfers - La calamité', base: 'Rupture', chain: 'enfers', step: 2,
    surface: 'fracture', walls: ['rubble', 'boulder', 'shard'], props: ['cairn', 'spire'],
    top: '#7a7068', topDeep: '#332e2b', seam: '#201d1b', riser: '#3a3431', riserDeep: '#141211',
    accent: '#c8b8a4', glow: '#e8ddcd', particle: 'ash',
    sky: ['#0b0a09', '#1d1a17', '#413931'], fog: '#141210',
  },
  'room.enfer2': {
    label: 'Les enfers - la plaine', base: 'Forest', chain: 'enfers', step: 3,
    surface: 'moss', walls: ['deadfall', 'trunk', 'boulder'], props: ['trunk', 'cairn'],
    top: '#7d8449', topDeep: '#343722', seam: '#212314', riser: '#3b3e24', riserDeep: '#15170d',
    accent: '#d5d17a', glow: '#f2eeb8', particle: 'spore',
    sky: ['#0d0e08', '#232512', '#4d5223'], fog: '#171a0e',
  },
  'room.enfer3': {
    label: 'Les enfers - la forge', base: 'Rupture', chain: 'enfers', step: 4,
    surface: 'flagstone', walls: ['monolith', 'gate', 'rubble'], props: ['spire', 'obeliskProp'],
    top: '#6d5049', topDeep: '#2e211e', seam: '#1d1412', riser: '#372420', riserDeep: '#140c0a',
    accent: '#ff8a3c', glow: '#ffcf96', particle: 'ember',
    sky: ['#100604', '#33110a', '#8a2f10'], fog: '#1a0d09',
  },
  'room.enfer4': {
    label: 'Les enfers - Le chateau', base: 'Final', chain: 'enfers', step: 5,
    surface: 'flagstone', walls: ['gate', 'column', 'brokenColumn'], props: ['column', 'spire'],
    top: '#6b4046', topDeep: '#2c181d', seam: '#1b0e12', riser: '#331b21', riserDeep: '#12080b',
    accent: '#ff7a72', glow: '#ffc0b4', particle: 'emberDark',
    sky: ['#070305', '#26080e', '#6b1018'], fog: '#160a0d',
  },

  // ── Chaîne B — Le Soleil ──
  'room.soleil': {
    label: 'Le soleil', base: 'Final', chain: 'soleil', step: 2,
    surface: 'pulse', walls: ['spire', 'shard', 'gate'], props: ['beam', 'arch'],
    top: '#c4914b', topDeep: '#5b3b18', seam: '#3a250f', riser: '#63400f', riserDeep: '#251706',
    accent: '#ffd166', glow: '#fff6d0', particle: 'plasma',
    sky: ['#2a1203', '#6e2f05', '#ffb020'], fog: '#40200a',
  },
  'room.chateau': {
    // Même bâtiment que room.enfer4, déplacé au centre du soleil : mêmes murs, même
    // vocabulaire de sol, palette réchauffée. Le rappel doit se voir, la confusion non.
    label: 'Le chateau', base: 'Antechamber', chain: 'soleil', step: 3,
    surface: 'flagstone', walls: ['gate', 'column', 'brokenColumn'], props: ['column', 'arch'],
    top: '#c1a06a', topDeep: '#544128', seam: '#352819', riser: '#5b4527', riserDeep: '#211910',
    accent: '#ffd98a', glow: '#fff4d2', particle: 'gilt',
    sky: ['#1e1206', '#4a2c0c', '#b8761f'], fog: '#2c1d0c',
  },
  'room.cellule': {
    label: 'Le chateau - La cellule', base: 'Memory', chain: 'soleil', step: 4,
    surface: 'plank', walls: ['crates', 'shelf', 'brokenColumn'], props: ['cairn', 'beam'],
    top: '#a89275', topDeep: '#463b2c', seam: '#2b241a', riser: '#4c4030', riserDeep: '#1c1712',
    accent: '#a8d8e0', glow: '#e6f8fb', particle: 'flake',
    sky: ['#12100c', '#2a2318', '#5d4a2c'], fog: '#1a1610',
  },

  // ── Chaîne C — L'Hôpital ──
  'room.cellulehopital': {
    label: "L'hopital - la cellule", base: 'Silence', chain: 'hopital', step: 2,
    surface: 'clinic', walls: ['gate', 'rubble', 'monolith'], props: ['obeliskProp', 'beam'],
    top: '#a9a4b0', topDeep: '#474252', seam: '#2e2a38', riser: '#4d4759', riserDeep: '#1b1823',
    accent: '#b9a6de', glow: '#e4d9f6', particle: 'dust',
    sky: ['#0d0b12', '#1f1a2b', '#453a5e'], fog: '#171423',
  },

  // ── Chaîne D — La Montagne (extérieur → temple → tombeau → tunnel → merveille) ──
  'room.templempontagne': {
    label: 'La montagne - Le temple', base: 'Antechamber', chain: 'montagne', step: 2,
    surface: 'flagstone', walls: ['column', 'monolith', 'gate'], props: ['column', 'arch'],
    top: '#a08b62', topDeep: '#453a26', seam: '#2b2317', riser: '#4a3e28', riserDeep: '#1b160f',
    accent: '#7fd8b0', glow: '#d8f6e8', particle: 'gilt',
    sky: ['#100e0a', '#2b2515', '#5f5327'], fog: '#1a170f',
  },
  'room.chambrefunéraire': {
    label: 'La montagne - la chambre funéraire', base: 'Memory', chain: 'montagne', step: 3,
    surface: 'flagstone', walls: ['monolith', 'crates', 'rubble'], props: ['obeliskProp', 'cairn'],
    top: '#6f6350', topDeep: '#2f2a21', seam: '#1e1a14', riser: '#352f25', riserDeep: '#13100c',
    accent: '#e0a85c', glow: '#ffd79a', particle: 'ash',
    sky: ['#080706', '#171310', '#33291d'], fog: '#100e0b',
  },
  'room.sousterrainmontagne': {
    label: 'La montagne - Les sous-terrains', base: 'Silence', chain: 'montagne', step: 4,
    surface: 'flagstone', walls: ['boulder', 'rubble', 'monolith'], props: ['cairn', 'beam'],
    top: '#5b5348', topDeep: '#27231d', seam: '#191612', riser: '#2d2822', riserDeep: '#100e0c',
    accent: '#9ec6c0', glow: '#d6ecea', particle: 'dust',
    sky: ['#050505', '#0f1211', '#212a28'], fog: '#0b0d0d',
  },
  'room.cavernedecrystal': {
    label: 'La montagne - La caverne de crystal', base: 'Threshold', chain: 'montagne', step: 5,
    surface: 'crystal', walls: ['shard', 'boulder', 'monolith'], props: ['spire', 'beam'],
    top: '#7f97c4', topDeep: '#333f66', seam: '#20284a', riser: '#36406b', riserDeep: '#141930',
    accent: '#8ce8ff', glow: '#dff8ff', particle: 'mote',
    sky: ['#050813', '#141f45', '#3b64a8'], fog: '#101733',
  },
};
export const ROOM_KEYS = Object.keys(ROOMS);

const ROOM_CACHE = new Map();
/** Compose une salle : thème de base + surcharges. Mémoïsé — appelé à chaque sprite. */
function composeRoom(key) {
  const hit = ROOM_CACHE.get(key);
  if (hit) return hit;
  const spec = ROOMS[key];
  const base = THEMES[spec.base] ?? THEMES.Threshold;
  const th = { ...base, ...spec };
  th.wall = th.walls ? th.walls[0] : base.wall;
  th.roomKey = key;
  ROOM_CACHE.set(key, th);
  return th;
}

/** Accepte indifféremment un nom de thème de rendu ou une clé de salle (`room.*`). */
function theme(name) {
  if (name && ROOMS[name]) return composeRoom(name);
  return THEMES[name] ?? THEMES.Threshold;
}

/** Décors de salle disponibles pour un thème ou une salle. */
export function themeProps(name) { return theme(name).props ?? []; }
/** Libellé lisible d'un thème ou d'une salle. */
export function themeLabel(name) { return theme(name).label; }
/** Couleur du voile de brouillard de guerre, déclinée par salle. */
export function fogColor(name) {
  const th = theme(name);
  return th.fog ?? th.sky[0];
}

// ── Géométrie du diamant ─────────────────────────────────────────────────────────────
function centerY(elev) { return (TILE.MAX - elev) * TILE.STEP + TILE.H / 2; }
function corners(elev, inset = 0) {
  const cx = TILE.W / 2, cy = centerY(elev);
  const hw = TILE.W / 2 - inset, hh = TILE.H / 2 - inset;
  return {
    top: { x: cx, y: cy - hh }, right: { x: cx + hw, y: cy },
    bottom: { x: cx, y: cy + hh }, left: { x: cx - hw, y: cy },
    cx, cy, hw, hh,
  };
}
function diamondPath(elev, inset = 0, yOff = 0) {
  const c = corners(elev, inset);
  const p = new Path2D();
  p.moveTo(c.top.x, c.top.y + yOff);
  p.lineTo(c.right.x, c.right.y + yOff);
  p.lineTo(c.bottom.x, c.bottom.y + yOff);
  p.lineTo(c.left.x, c.left.y + yOff);
  p.closePath();
  return p;
}
/** Coordonnées (u,v) ∈ [0,1]² du plan de la tuile → pixels : permet de poser un damier
 * de dalles réellement en perspective iso plutôt qu'un motif plaqué. */
function isoPt(u, v, c) {
  return { x: c.cx + (u - v) * c.hw, y: c.cy + (u + v - 1) * c.hh };
}

function makeCanvas(w, h) {
  const cv = (typeof document !== 'undefined')
    ? document.createElement('canvas')
    : { width: 0, height: 0, getContext: () => null };
  cv.width = w; cv.height = h;
  return cv;
}

// ── Grain ────────────────────────────────────────────────────────────────────────────
function speckle(ctx, x0, y0, w, h, R, count, light, dark) {
  for (let i = 0; i < count; i++) {
    const x = x0 + R() * w, y = y0 + R() * h;
    const up = R() > 0.5;
    ctx.fillStyle = rgba(up ? light : dark, R2(R, 0.03, 0.13));
    ctx.fillRect(x, y, R() > 0.85 ? 2 : 1, 1);
  }
}

// ── Face supérieure ──────────────────────────────────────────────────────────────────
function paintTop(ctx, elev, th, R, o = {}) {
  const c = corners(elev);
  const grain = o.grain ?? 1;
  const lightBias = elev * 0.035;
  const top = shade(th.top, lightBias);
  const deep = shade(th.topDeep, lightBias * 0.5);

  ctx.save();
  ctx.clip(diamondPath(elev, 0.5));

  const g = ctx.createLinearGradient(c.left.x, c.top.y, c.right.x, c.bottom.y);
  g.addColorStop(0, rgba(shade(top, 0.12), 1));
  g.addColorStop(0.55, rgba(top, 1));
  g.addColorStop(1, rgba(deep, 1));
  ctx.fillStyle = g;
  ctx.fill(diamondPath(elev, -1));

  // Empâtement : taches larges de teintes voisines — la base du rendu "peint à la main".
  for (let i = 0; i < 54; i++) {
    const p = isoPt(R(), R(), c);
    const rad = R2(R, 5, 24);
    ctx.save();
    ctx.translate(p.x, p.y);
    ctx.rotate(R2(R, -0.5, 0.5));
    ctx.fillStyle = rgba(mix(top, R() > 0.5 ? deep : shade(top, 0.22), R()), R2(R, 0.05, 0.15));
    ctx.beginPath();
    ctx.ellipse(0, 0, rad, rad * 0.45, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
  }

  paintSurface(ctx, th, R, c, top, deep, o);

  if (o.danger && o.danger !== 'none') paintDanger(ctx, th, R, c, o.danger);
  if (o.hidden === 'hint') paintHiddenHint(ctx, th, R, c);
  if (o.hidden === 'revealed') paintHiddenOpen(ctx, th, R, c);

  speckle(ctx, c.cx - c.hw, c.cy - c.hh, TILE.W, TILE.H, R, Math.round(430 * grain), th.glow, '#000000');

  // Lumière rasante venue du haut-gauche : arêtes nord éclairées, arêtes sud à l'ombre.
  const edge = (a, b, col, alpha, lw) => {
    ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(b.x, b.y);
    ctx.strokeStyle = rgba(col, alpha); ctx.lineWidth = lw; ctx.stroke();
  };
  edge(c.top, c.left, th.glow, 0.30, 2);
  edge(c.top, c.right, th.glow, 0.14, 1.5);
  edge(c.left, c.bottom, '#000000', 0.26, 2);
  edge(c.right, c.bottom, '#000000', 0.20, 1.5);

  // Occlusion douce vers le coin proche du spectateur.
  const ao = ctx.createRadialGradient(c.bottom.x, c.bottom.y, 2, c.bottom.x, c.bottom.y, TILE.H * 1.1);
  ao.addColorStop(0, rgba('#000000', 0.26));
  ao.addColorStop(1, rgba('#000000', 0));
  ctx.fillStyle = ao;
  ctx.fill(diamondPath(elev));

  ctx.restore();
}

// ── Vocabulaires de surface ──────────────────────────────────────────────────────────
function paintSurface(ctx, th, R, c, top, deep, o) {
  switch (th.surface) {
    case 'flagstone': return flagstones(ctx, th, R, c, top, deep, 3);
    case 'plank': return planks(ctx, th, R, c, top, deep);
    case 'marble': return marble(ctx, th, R, c, top, deep);
    case 'parchment': return parchment(ctx, th, R, c, top, deep);
    case 'moss': return moss(ctx, th, R, c, top, deep);
    case 'fracture': return fracture(ctx, th, R, c, top, deep, 1);
    case 'pulse': return fracture(ctx, th, R, c, top, deep, 1.6);
    case 'ripple': return ripples(ctx, th, R, c, top, deep);
    case 'carpet': return carpet(ctx, th, R, c, top, deep);
    case 'clinic': return clinic(ctx, th, R, c, top, deep);
    case 'crystal': return crystalFloor(ctx, th, R, c, top, deep);
    default: return null;
  }
}

/** Tapis posé sur dalle : marge de pierre, bordure tissée, poil, usure. Hall & Couloirs. */
function carpet(ctx, th, R, c, top, deep) {
  flagstones(ctx, th, R, c, top, deep, 2);
  const rug = th.rug ?? mix(th.accent, '#5a1a20', 0.6);
  // Marge fine : les tapis de deux cases voisines se rejoignent presque, l'ensemble lit
  // comme une seule pièce d'étoffe et non comme un damier de carrés rouges.
  const m = 0.05;
  const quad = (a, b) => {
    const p = new Path2D();
    const pts = [isoPt(a, a, c), isoPt(b, a, c), isoPt(b, b, c), isoPt(a, b, c)];
    p.moveTo(pts[0].x, pts[0].y);
    for (let i = 1; i < 4; i++) p.lineTo(pts[i].x, pts[i].y);
    p.closePath();
    return p;
  };
  const outer = quad(m, 1 - m);
  ctx.save();
  const g = ctx.createLinearGradient(c.cx, c.cy - c.hh, c.cx, c.cy + c.hh);
  g.addColorStop(0, rgba(shade(rug, 0.16), 1));
  g.addColorStop(1, rgba(shade(rug, -0.3), 1));
  ctx.fillStyle = g;
  ctx.fill(outer);
  ctx.clip(outer);
  // Poil du tapis : grain fin, dense, orienté — c'est ce qui évite l'effet "aplat rouge".
  for (let i = 0; i < 140; i++) {
    const p = isoPt(R(), R(), c);
    ctx.fillStyle = rgba(R() > 0.5 ? shade(rug, 0.22) : shade(rug, -0.28), R2(R, 0.06, 0.24));
    ctx.fillRect(p.x, p.y, R2(R, 1, 2.6), 1);
  }
  // Usure : deux traînées plus claires, comme un passage souvent emprunté.
  for (let i = 0; i < 3; i++) {
    const p = isoPt(R2(R, 0.2, 0.8), R2(R, 0.2, 0.8), c);
    const wg = ctx.createRadialGradient(p.x, p.y, 1, p.x, p.y, R2(R, 14, 26));
    wg.addColorStop(0, rgba(shade(rug, 0.3), 0.16));
    wg.addColorStop(1, rgba(shade(rug, 0.3), 0));
    ctx.fillStyle = wg;
    ctx.fillRect(p.x - 30, p.y - 20, 60, 40);
  }
  ctx.restore();
  ctx.strokeStyle = rgba(shade(rug, 0.34), 0.5); ctx.lineWidth = 1.4;
  ctx.stroke(quad(m + 0.06, 1 - m - 0.06));
  ctx.strokeStyle = rgba('#000000', 0.4); ctx.lineWidth = 1;
  ctx.stroke(outer);
}

/** Carrelage d'hôpital : grandes dalles claires, joints froids, reflet sec, traçages au sol. */
function clinic(ctx, th, R, c, top, deep) {
  flagstones(ctx, th, R, c, top, deep, 2);
  ctx.save();
  ctx.clip(diamondPath(TILE.MAX, 1));
  const sg = ctx.createLinearGradient(c.left.x, c.top.y, c.right.x, c.bottom.y);
  sg.addColorStop(0, rgba('#ffffff', 0.16));
  sg.addColorStop(0.5, rgba('#ffffff', 0.03));
  sg.addColorStop(1, rgba(th.topDeep, 0.14));
  ctx.fillStyle = sg;
  ctx.fill(diamondPath(TILE.MAX, -1));
  // Quelques auréoles : le lieu est propre, pas neuf.
  for (let i = 0; i < 3; i++) {
    const p = isoPt(R(), R(), c);
    const st = ctx.createRadialGradient(p.x, p.y, 1, p.x, p.y, R2(R, 8, 18));
    st.addColorStop(0, rgba(mix(th.topDeep, '#6b7a5e', 0.5), R2(R, 0.06, 0.14)));
    st.addColorStop(1, rgba(th.topDeep, 0));
    ctx.fillStyle = st;
    ctx.fillRect(p.x - 20, p.y - 20, 40, 40);
  }
  ctx.restore();
}

/** Sol de la caverne : facettes de crystal affleurantes, arêtes lumineuses. */
function crystalFloor(ctx, th, R, c, top, deep) {
  flagstones(ctx, th, R, c, top, deep, 2);
  for (let i = 0; i < 7; i++) {
    const a = isoPt(R(), R(), c);
    const p = new Path2D();
    p.moveTo(a.x, a.y);
    const n = 3 + ((R() * 2) | 0);
    for (let k = 0; k < n; k++) {
      p.lineTo(a.x + R2(R, -13, 13), a.y + R2(R, -7, 7));
    }
    p.closePath();
    ctx.fillStyle = rgba(mix(top, th.glow, R2(R, 0.15, 0.55)), R2(R, 0.10, 0.3));
    ctx.fill(p);
    ctx.strokeStyle = rgba(th.glow, R2(R, 0.12, 0.4)); ctx.lineWidth = R2(R, 0.6, 1.3);
    ctx.stroke(p);
  }
}

function flagstones(ctx, th, R, c, top, deep, n) {
  const m = 0.045;
  for (let i = 0; i < n; i++) {
    for (let j = 0; j < n; j++) {
      const j0 = () => R2(R, -0.018, 0.018);
      const q = [
        isoPt(i / n + m + j0(), j / n + m + j0(), c),
        isoPt((i + 1) / n - m + j0(), j / n + m + j0(), c),
        isoPt((i + 1) / n - m + j0(), (j + 1) / n - m + j0(), c),
        isoPt(i / n + m + j0(), (j + 1) / n - m + j0(), c),
      ];
      ctx.beginPath();
      ctx.moveTo(q[0].x, q[0].y);
      for (let k = 1; k < 4; k++) ctx.lineTo(q[k].x, q[k].y);
      ctx.closePath();
      const t = R();
      ctx.fillStyle = rgba(mix(top, t > 0.5 ? shade(top, 0.16) : deep, R2(R, 0.15, 0.6)), 0.62);
      ctx.fill();
      // Joint sombre + chanfrein éclairé sur l'arête nord de chaque dalle.
      ctx.strokeStyle = rgba(th.seam, 0.55); ctx.lineWidth = 1.6; ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(q[0].x, q[0].y + 1); ctx.lineTo(q[1].x, q[1].y + 1);
      ctx.strokeStyle = rgba(th.glow, 0.13); ctx.lineWidth = 1.2; ctx.stroke();
    }
  }
}

/** Plancher de bois : lattes iso dans le sens de la profondeur, fibre, joints, clous. */
function planks(ctx, th, R, c, top, deep) {
  const n = 5;
  const wood = mix(top, '#6d5a44', 0.5);
  for (let i = 0; i < n; i++) {
    const v0 = i / n, v1 = (i + 1) / n;
    const q = [isoPt(0, v0, c), isoPt(1, v0, c), isoPt(1, v1 - 0.008, c), isoPt(0, v1 - 0.008, c)];
    const board = new Path2D();
    board.moveTo(q[0].x, q[0].y);
    for (let k = 1; k < 4; k++) board.lineTo(q[k].x, q[k].y);
    board.closePath();
    ctx.save();
    ctx.clip(board);
    ctx.fillStyle = rgba(mix(wood, R() > 0.5 ? shade(wood, 0.16) : deep, R2(R, 0.1, 0.65)), 0.72);
    ctx.fill(board);
    // Fibre : sillons parallèles à la latte, légèrement ondulés.
    for (let f = 0; f < 5; f++) {
      const vv = v0 + (f + 0.5) / (n * 5) * n * 0.2 + R2(R, -0.008, 0.008);
      const a = isoPt(0, vv, c), b = isoPt(1, vv, c);
      ctx.beginPath();
      ctx.moveTo(a.x, a.y);
      ctx.quadraticCurveTo((a.x + b.x) / 2, (a.y + b.y) / 2 + R2(R, -1.6, 1.6), b.x, b.y);
      const dark = R() > 0.45;
      ctx.strokeStyle = rgba(dark ? '#000000' : th.glow, dark ? R2(R, 0.06, 0.18) : R2(R, 0.04, 0.1));
      ctx.lineWidth = R2(R, 0.6, 1.4);
      ctx.stroke();
    }
    if (R() > 0.55) {
      const kp = isoPt(R2(R, 0.15, 0.85), (v0 + v1) / 2, c);
      ctx.beginPath();
      ctx.ellipse(kp.x, kp.y, R2(R, 2, 3.6), R2(R, 1.2, 2), R2(R, -0.4, 0.4), 0, Math.PI * 2);
      ctx.fillStyle = rgba('#000000', 0.34);
      ctx.fill();
    }
    ctx.restore();
    // Joint entre lattes : ombre + arête éclairée au nord.
    ctx.beginPath();
    ctx.moveTo(q[3].x, q[3].y); ctx.lineTo(q[2].x, q[2].y);
    ctx.strokeStyle = rgba(th.seam, 0.6); ctx.lineWidth = 1.8; ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(q[0].x, q[0].y + 1); ctx.lineTo(q[1].x, q[1].y + 1);
    ctx.strokeStyle = rgba(th.glow, 0.14); ctx.lineWidth = 1.1; ctx.stroke();
    // Clous forgés aux extrémités de la latte.
    for (const u of [0.1, 0.9]) {
      const np = isoPt(u, (v0 + v1) / 2, c);
      ctx.beginPath();
      ctx.arc(np.x, np.y, 1.3, 0, Math.PI * 2);
      ctx.fillStyle = rgba('#000000', 0.45); ctx.fill();
      ctx.beginPath();
      ctx.arc(np.x - 0.5, np.y - 0.5, 0.7, 0, Math.PI * 2);
      ctx.fillStyle = rgba(th.glow, 0.3); ctx.fill();
    }
  }
}

function marble(ctx, th, R, c, top, deep) {
  flagstones(ctx, th, R, c, top, deep, 2);
  for (let i = 0; i < 7; i++) {
    const a = isoPt(R(), 0, c), b = isoPt(R(), 1, c);
    ctx.beginPath();
    ctx.moveTo(a.x, a.y);
    ctx.bezierCurveTo(
      a.x + R2(R, -18, 18), a.y + 14,
      b.x + R2(R, -18, 18), b.y - 14,
      b.x, b.y,
    );
    ctx.strokeStyle = rgba(i % 3 === 0 ? th.accent : th.glow, R2(R, 0.06, 0.16));
    ctx.lineWidth = R2(R, 0.6, 1.6);
    ctx.stroke();
  }
}

function parchment(ctx, th, R, c, top, deep) {
  // Lattes iso, comme un plancher de bibliothèque, plus des feuillets épars.
  for (let i = 0; i < 6; i++) {
    const v0 = i / 6, v1 = (i + 1) / 6 - 0.012;
    const q = [isoPt(0, v0, c), isoPt(1, v0, c), isoPt(1, v1, c), isoPt(0, v1, c)];
    ctx.beginPath();
    ctx.moveTo(q[0].x, q[0].y);
    for (let k = 1; k < 4; k++) ctx.lineTo(q[k].x, q[k].y);
    ctx.closePath();
    ctx.fillStyle = rgba(mix(top, deep, R2(R, 0.1, 0.55)), 0.5);
    ctx.fill();
    ctx.strokeStyle = rgba(th.seam, 0.4); ctx.lineWidth = 1.1; ctx.stroke();
  }
  for (let i = 0; i < 9; i++) {
    const p = isoPt(R(), R(), c);
    ctx.save();
    ctx.translate(p.x, p.y);
    ctx.rotate(R2(R, -0.6, 0.6));
    ctx.fillStyle = rgba(th.glow, R2(R, 0.10, 0.26));
    ctx.fillRect(-4, -1.6, R2(R, 5, 11), R2(R, 2, 3.4));
    ctx.restore();
  }
}

function moss(ctx, th, R, c, top, deep) {
  for (let i = 0; i < 22; i++) {
    const p = isoPt(R(), R(), c);
    ctx.fillStyle = rgba(mix(deep, th.accent, R2(R, 0.1, 0.5)), R2(R, 0.10, 0.28));
    ctx.beginPath();
    ctx.ellipse(p.x, p.y, R2(R, 4, 13), R2(R, 2, 6), R2(R, -0.4, 0.4), 0, Math.PI * 2);
    ctx.fill();
  }
  // Touffes : petits coups de brosse verticaux, pointe plus claire.
  for (let i = 0; i < 34; i++) {
    const p = isoPt(R(), R(), c);
    const h = R2(R, 3.5, 8), lean = R2(R, -2.5, 2.5);
    ctx.beginPath();
    ctx.moveTo(p.x, p.y);
    ctx.quadraticCurveTo(p.x + lean * 0.5, p.y - h * 0.6, p.x + lean, p.y - h);
    ctx.strokeStyle = rgba(mix(th.accent, top, R2(R, 0.2, 0.7)), R2(R, 0.18, 0.45));
    ctx.lineWidth = R2(R, 0.7, 1.4);
    ctx.stroke();
  }
}

function fracture(ctx, th, R, c, top, deep, intensity) {
  flagstones(ctx, th, R, c, top, deep, 2);
  const cracks = 3;
  for (let i = 0; i < cracks; i++) {
    let p = isoPt(R() > 0.5 ? R() : 0, R(), c);
    const pts = [p];
    for (let s = 0; s < 4; s++) {
      p = { x: p.x + R2(R, 6, 22) * (R() > 0.25 ? 1 : -1), y: p.y + R2(R, -7, 7) };
      pts.push(p);
    }
    const draw = (col, alpha, lw) => {
      ctx.beginPath();
      ctx.moveTo(pts[0].x, pts[0].y);
      for (let k = 1; k < pts.length; k++) ctx.lineTo(pts[k].x, pts[k].y);
      ctx.strokeStyle = rgba(col, alpha); ctx.lineWidth = lw;
      ctx.lineJoin = 'miter';
      ctx.stroke();
    };
    draw('#000000', 0.55, 3.2);
    ctx.save();
    ctx.shadowColor = rgba(th.accent, 0.9);
    ctx.shadowBlur = 7 * intensity;
    draw(th.accent, 0.30 * intensity, 1.1);
    ctx.restore();
  }
}

function ripples(ctx, th, R, c, top, deep) {
  const ox = R2(R, -12, 12), oy = R2(R, -5, 5);
  for (let i = 5; i >= 1; i--) {
    const r = i / 5;
    ctx.beginPath();
    ctx.ellipse(c.cx + ox, c.cy + oy, c.hw * r * 0.92, c.hh * r * 0.92, 0, 0, Math.PI * 2);
    ctx.strokeStyle = rgba(i % 2 ? th.glow : th.seam, i % 2 ? 0.13 : 0.22);
    ctx.lineWidth = i % 2 ? 1.4 : 2.2;
    ctx.stroke();
  }
  ctx.beginPath();
  ctx.ellipse(c.cx + ox, c.cy + oy, c.hw * 0.3, c.hh * 0.3, 0, 0, Math.PI * 2);
  ctx.fillStyle = rgba(th.glow, 0.08);
  ctx.fill();
}

// ── Indices de gameplay peints dans le sol ───────────────────────────────────────────
function paintDanger(ctx, th, R, c, kind) {
  if (kind === 'tracks') {
    // Griffures + esquilles d'os : lisible avant contact, sans casser la texture.
    for (let i = 0; i < 3; i++) {
      const p = isoPt(R2(R, 0.2, 0.8), R2(R, 0.2, 0.8), c);
      for (let k = 0; k < 3; k++) {
        ctx.beginPath();
        ctx.moveTo(p.x + k * 4, p.y);
        ctx.quadraticCurveTo(p.x + k * 4 + 3, p.y + 4, p.x + k * 4 + 2, p.y + 9);
        ctx.strokeStyle = rgba('#000000', 0.42); ctx.lineWidth = 1.8; ctx.stroke();
      }
    }
    for (let i = 0; i < 3; i++) {
      const p = isoPt(R2(R, 0.15, 0.85), R2(R, 0.15, 0.85), c);
      ctx.save();
      ctx.translate(p.x, p.y); ctx.rotate(R2(R, -1, 1));
      ctx.fillStyle = rgba('#e8e2d0', 0.5);
      ctx.fillRect(-5, -1, 10, 2);
      ctx.beginPath(); ctx.arc(-5, 0, 2, 0, Math.PI * 2); ctx.arc(5, 0, 2, 0, Math.PI * 2);
      ctx.fill();
      ctx.restore();
    }
  }
  if (kind === 'glow') {
    const g = ctx.createRadialGradient(c.cx, c.cy, 1, c.cx, c.cy, c.hw * 0.8);
    g.addColorStop(0, rgba(th.accent, 0.42));
    g.addColorStop(0.5, rgba(th.accent, 0.14));
    g.addColorStop(1, rgba(th.accent, 0));
    ctx.fillStyle = g;
    ctx.fill(diamondPath(0, 0, c.cy - centerY(0)));
    for (let i = 0; i < 5; i++) {
      const p = isoPt(R(), R(), c);
      ctx.fillStyle = rgba(th.glow, R2(R, 0.2, 0.5));
      ctx.beginPath(); ctx.arc(p.x, p.y, R2(R, 0.8, 1.8), 0, Math.PI * 2); ctx.fill();
    }
  }
  if (kind === 'blight') {
    for (let i = 0; i < 7; i++) {
      const p = isoPt(R(), R(), c);
      ctx.fillStyle = rgba('#000000', R2(R, 0.14, 0.34));
      ctx.beginPath();
      ctx.ellipse(p.x, p.y, R2(R, 4, 15), R2(R, 2, 7), R2(R, -0.5, 0.5), 0, Math.PI * 2);
      ctx.fill();
    }
    for (let i = 0; i < 10; i++) {
      const p = isoPt(R(), R(), c);
      ctx.beginPath();
      ctx.moveTo(p.x, p.y);
      ctx.lineTo(p.x + R2(R, -5, 5), p.y - R2(R, 2, 6));
      ctx.strokeStyle = rgba('#6b5b4a', R2(R, 0.25, 0.5));
      ctx.lineWidth = 1; ctx.stroke();
    }
  }
}

/** État 2 : un renfoncement suspect. Presque rien — une dalle qui sonne creux. */
function paintHiddenHint(ctx, th, R, c) {
  const q = [isoPt(0.3, 0.3, c), isoPt(0.7, 0.3, c), isoPt(0.7, 0.7, c), isoPt(0.3, 0.7, c)];
  ctx.beginPath();
  ctx.moveTo(q[0].x, q[0].y);
  for (let k = 1; k < 4; k++) ctx.lineTo(q[k].x, q[k].y);
  ctx.closePath();
  ctx.fillStyle = rgba('#000000', 0.16);
  ctx.fill();
  ctx.strokeStyle = rgba(th.seam, 0.7); ctx.lineWidth = 1.6; ctx.stroke();
  ctx.beginPath();
  ctx.moveTo(q[3].x, q[3].y); ctx.lineTo(q[0].x, q[0].y); ctx.lineTo(q[1].x, q[1].y);
  ctx.strokeStyle = rgba(th.glow, 0.16); ctx.lineWidth = 1.2; ctx.stroke();
}

/** État 3 : la dalle est descellée, l'alcôve est ouverte, quelque chose luit dedans. */
function paintHiddenOpen(ctx, th, R, c) {
  const q = [isoPt(0.24, 0.24, c), isoPt(0.76, 0.24, c), isoPt(0.76, 0.76, c), isoPt(0.24, 0.76, c)];
  const path = new Path2D();
  path.moveTo(q[0].x, q[0].y);
  for (let k = 1; k < 4; k++) path.lineTo(q[k].x, q[k].y);
  path.closePath();
  ctx.fillStyle = rgba('#05040a', 0.92);
  ctx.fill(path);
  ctx.save();
  ctx.clip(path);
  const g = ctx.createRadialGradient(c.cx, c.cy + 3, 1, c.cx, c.cy + 3, 22);
  g.addColorStop(0, rgba(th.glow, 0.85));
  g.addColorStop(0.4, rgba(th.accent, 0.35));
  g.addColorStop(1, rgba(th.accent, 0));
  ctx.fillStyle = g;
  ctx.fillRect(c.cx - 30, c.cy - 20, 60, 40);
  for (let i = 0; i < 6; i++) {
    const p = { x: c.cx + R2(R, -14, 14), y: c.cy + R2(R, -7, 8) };
    ctx.fillStyle = rgba('#fff6dd', R2(R, 0.4, 0.9));
    ctx.beginPath(); ctx.arc(p.x, p.y, R2(R, 0.7, 1.7), 0, Math.PI * 2); ctx.fill();
  }
  ctx.restore();
  // Dalle basculée sur le côté + lèvre éclairée de l'ouverture.
  ctx.beginPath();
  ctx.moveTo(q[3].x, q[3].y); ctx.lineTo(q[0].x, q[0].y); ctx.lineTo(q[1].x, q[1].y);
  ctx.strokeStyle = rgba(th.glow, 0.4); ctx.lineWidth = 2; ctx.stroke();
  ctx.save();
  ctx.translate(q[2].x + 4, q[2].y + 1);
  ctx.rotate(0.22);
  ctx.fillStyle = rgba(shade(th.top, 0.1), 0.95);
  ctx.fillRect(-13, -5, 26, 6);
  ctx.fillStyle = rgba('#000000', 0.4);
  ctx.fillRect(-13, 1, 26, 3);
  ctx.restore();
}

// ── Contremarches (faces latérales SW + SE) ──────────────────────────────────────────
function paintFace(ctx, p0, p1, drop, th, R, lit, o = {}) {
  if (drop <= 0.5) return;
  const base = shade(th.riser, lit * 0.16);
  const path = new Path2D();
  path.moveTo(p0.x, p0.y);
  path.lineTo(p1.x, p1.y);
  path.lineTo(p1.x, p1.y + drop);
  if (o.jagged) {
    // Tranche rocheuse brisée : la bordure de salle ne se coupe pas au carré.
    const steps = 5;
    for (let i = steps; i >= 0; i--) {
      const t = i / steps;
      const x = p1.x + (p0.x - p1.x) * (1 - t);
      const y = p1.y + (p0.y - p1.y) * (1 - t) + drop - R2(R, 0, 26) - 4;
      path.lineTo(x, y);
    }
  } else {
    path.lineTo(p0.x, p0.y + drop);
  }
  path.closePath();

  ctx.save();
  ctx.clip(path);
  const g = ctx.createLinearGradient(0, Math.min(p0.y, p1.y), 0, Math.max(p0.y, p1.y) + drop);
  g.addColorStop(0, rgba(shade(base, 0.16), 1));
  g.addColorStop(0.35, rgba(base, 1));
  g.addColorStop(1, rgba(th.riserDeep, 1));
  ctx.fillStyle = g;
  ctx.fill(path);

  // Strates de roche parallèles à l'arête du dessus + joints verticaux jitterés.
  const lerp = (t, dy) => ({ x: p0.x + (p1.x - p0.x) * t, y: p0.y + (p1.y - p0.y) * t + dy });
  for (let dy = 4; dy < drop; dy += R2(R, 5, 9)) {
    const a = lerp(0, dy + R2(R, -1.5, 1.5)), b = lerp(1, dy + R2(R, -1.5, 1.5));
    ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(b.x, b.y);
    ctx.strokeStyle = rgba('#000000', R2(R, 0.12, 0.3)); ctx.lineWidth = R2(R, 1, 2.2);
    ctx.stroke();
    ctx.beginPath(); ctx.moveTo(a.x, a.y - 1.4); ctx.lineTo(b.x, b.y - 1.4);
    ctx.strokeStyle = rgba(th.glow, 0.07 + lit * 0.06); ctx.lineWidth = 1;
    ctx.stroke();
  }
  for (let i = 0; i < 4; i++) {
    const t = R2(R, 0.08, 0.92);
    const a = lerp(t, R2(R, 2, drop * 0.4)), b = lerp(t + R2(R, -0.04, 0.04), drop);
    ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(b.x, b.y);
    ctx.strokeStyle = rgba(th.seam, R2(R, 0.25, 0.5)); ctx.lineWidth = R2(R, 1, 2);
    ctx.stroke();
  }
  // Nez de marche : la ligne qui fait lire l'élévation d'un coup d'œil.
  ctx.beginPath(); ctx.moveTo(p0.x, p0.y + 1); ctx.lineTo(p1.x, p1.y + 1);
  ctx.strokeStyle = rgba(th.glow, 0.22 + lit * 0.22); ctx.lineWidth = 2;
  ctx.stroke();

  speckle(ctx, Math.min(p0.x, p1.x), Math.min(p0.y, p1.y), Math.abs(p1.x - p0.x) + 2, drop + 34, R,
    Math.round(drop * 3.4 * (o.grain ?? 1)), th.glow, '#000000');

  if (o.jagged) {
    // Plongée dans le noir : le vide n'est pas un aplat, c'est une absence de lumière.
    const fade = ctx.createLinearGradient(0, Math.max(p0.y, p1.y) + drop * 0.25, 0, Math.max(p0.y, p1.y) + drop);
    fade.addColorStop(0, rgba('#000000', 0));
    fade.addColorStop(0.7, rgba('#000000', 0.72));
    fade.addColorStop(1, rgba('#04030a', 0.98));
    ctx.fillStyle = fade;
    ctx.fill(path);
  }
  ctx.restore();
}

function paintRisers(ctx, elev, th, R, o = {}) {
  const c = corners(elev);
  const drop = elev * TILE.STEP;
  const cliffDrop = (cl) => cl ? Math.max(drop, SPRITE_H - c.bottom.y - 2) : drop;
  paintFace(ctx, c.left, c.bottom, cliffDrop(o.cliffLeft), th, R,
    1, { jagged: !!o.cliffLeft, grain: o.grain });
  paintFace(ctx, c.bottom, c.right, cliffDrop(o.cliffRight), th, R,
    0.15, { jagged: !!o.cliffRight, grain: o.grain });
}

// ── Masses (murs, obstacles, décors) ─────────────────────────────────────────────────
// `striation` choisit la trame interne : 'masonry' (assises horizontales, pour la pierre
// taillée), 'fibre' (fibre verticale du bois, avec nœuds), 'splinter' (esquilles obliques
// d'un éclat fracturé). Une trame horizontale sur une forme haute et étroite lit comme un
// empilement de planches — d'où l'option.
function paintMass(ctx, poly, th, R, o = {}) {
  const path = new Path2D();
  path.moveTo(poly[0].x, poly[0].y);
  for (let i = 1; i < poly.length; i++) path.lineTo(poly[i].x, poly[i].y);
  path.closePath();
  const ys = poly.map((p) => p.y), xs = poly.map((p) => p.x);
  const y0 = Math.min(...ys), y1 = Math.max(...ys), x0 = Math.min(...xs), x1 = Math.max(...xs);
  const w = x1 - x0, h = y1 - y0;
  const striation = o.striation ?? 'masonry';
  const base = o.base ?? th.riser;
  const deep = o.deep ?? th.riserDeep;

  ctx.save();
  ctx.clip(path);
  // Volume cylindrique pour la fibre (clair au tiers gauche, sombre aux deux bords),
  // plan éclairé en diagonale pour la pierre et les éclats.
  let g;
  if (striation === 'fibre') {
    g = ctx.createLinearGradient(x0, 0, x1, 0);
    g.addColorStop(0, rgba(shade(deep, 0.04), 1));
    g.addColorStop(0.3, rgba(shade(base, 0.2), 1));
    g.addColorStop(0.62, rgba(base, 1));
    g.addColorStop(1, rgba(deep, 1));
  } else {
    g = ctx.createLinearGradient(x0, y0, x1, y1);
    g.addColorStop(0, rgba(shade(base, 0.22), 1));
    g.addColorStop(0.5, rgba(base, 1));
    g.addColorStop(1, rgba(deep, 1));
  }
  ctx.fillStyle = g;
  ctx.fillRect(x0 - 2, y0 - 2, w + 4, h + 4);

  if (striation === 'fibre') {
    // Fibre verticale : sillons qui suivent la hauteur, légèrement sinueux.
    const lines = Math.max(6, Math.round(w / 2.6));
    for (let i = 0; i < lines; i++) {
      const x = x0 + (i / lines) * w + R2(R, -1.5, 1.5);
      const dark = R() > 0.42;
      ctx.beginPath();
      ctx.moveTo(x, y0 - 2);
      let cy = y0;
      let cxp = x;
      while (cy < y1) {
        const ny = cy + R2(R, 18, 46);
        const nx = cxp + R2(R, -2.6, 2.6);
        ctx.quadraticCurveTo(cxp + R2(R, -2, 2), (cy + ny) / 2, nx, ny);
        cy = ny; cxp = nx;
      }
      ctx.strokeStyle = rgba(dark ? '#000000' : th.glow, dark ? R2(R, 0.10, 0.3) : R2(R, 0.04, 0.13));
      ctx.lineWidth = R2(R, 0.7, 2.1);
      ctx.stroke();
    }
    // Nœuds du bois : deux ou trois yeux sombres cerclés d'un liseré clair.
    const knots = 2 + Math.floor(R() * 2);
    for (let i = 0; i < knots; i++) {
      const kx = x0 + R2(R, 0.22, 0.78) * w, ky = y0 + R2(R, 0.12, 0.88) * h;
      const kr = R2(R, 2.6, 5.4);
      ctx.beginPath();
      ctx.ellipse(kx, ky, kr, kr * 1.5, R2(R, -0.2, 0.2), 0, Math.PI * 2);
      ctx.fillStyle = rgba('#000000', 0.42);
      ctx.fill();
      ctx.strokeStyle = rgba(th.glow, 0.14);
      ctx.lineWidth = 1;
      ctx.stroke();
    }
  } else if (striation === 'splinter') {
    // Esquilles : facettes obliques nées du sommet, plus une arête vive éclairée.
    const cxm = (x0 + x1) / 2;
    for (let i = 0; i < 9; i++) {
      const ax = cxm + R2(R, -w * 0.5, w * 0.5);
      ctx.beginPath();
      ctx.moveTo(ax, y0 - 2);
      ctx.lineTo(ax + R2(R, -w * 0.55, w * 0.55), y1 + 2);
      const lit = R() > 0.55;
      ctx.strokeStyle = rgba(lit ? th.glow : '#000000', lit ? R2(R, 0.05, 0.16) : R2(R, 0.14, 0.34));
      ctx.lineWidth = R2(R, 1, 3.4);
      ctx.stroke();
    }
    for (let i = 0; i < 3; i++) {
      const fy = y0 + R2(R, 0.15, 0.8) * h;
      ctx.beginPath();
      ctx.moveTo(x0 - 2, fy);
      ctx.lineTo(cxm + R2(R, -6, 6), fy + R2(R, -10, 10));
      ctx.lineTo(x1 + 2, fy + R2(R, -14, 14));
      ctx.strokeStyle = rgba('#000000', R2(R, 0.12, 0.28));
      ctx.lineWidth = R2(R, 1, 2.2);
      ctx.stroke();
    }
  } else {
    for (let dy = y0; dy < y1; dy += R2(R, 6, 13)) {
      ctx.beginPath();
      ctx.moveTo(x0 - 2, dy + R2(R, -2, 2));
      ctx.lineTo(x1 + 2, dy + R2(R, -2, 2));
      ctx.strokeStyle = rgba('#000000', R2(R, 0.1, 0.26));
      ctx.lineWidth = R2(R, 1, 2.4);
      ctx.stroke();
    }
  }
  speckle(ctx, x0, y0, w, h, R, Math.round(w * h * 0.035), th.glow, '#000000');
  ctx.restore();

  ctx.strokeStyle = rgba(th.glow, o.rim ?? 0.24);
  ctx.lineWidth = 1.6;
  ctx.stroke(path);
}

function obstacleSilhouette(kind, R, elev = TILE.MAX) {
  const cx = TILE.W / 2;
  // Assis sur la face du dessus de SA case, pas sur celle d'un socle imaginaire à TILE.MAX.
  const base = centerY(Math.max(0, Math.min(TILE.MAX, elev))) + 4;
  const P = (x, y) => ({ x, y });
  switch (kind) {
    case 'monolith':
      return [
        [P(cx - 34, base), P(cx - 26, base - 62), P(cx - 6, base - 74), P(cx + 2, base)],
        [P(cx + 1, base), P(cx + 8, base - 48), P(cx + 30, base - 38), P(cx + 36, base)],
      ];
    case 'shard':
      return [
        [P(cx - 32, base), P(cx - 12, base - 78), P(cx + 4, base - 40), P(cx + 6, base)],
        [P(cx + 4, base), P(cx + 20, base - 56), P(cx + 38, base - 20), P(cx + 38, base)],
      ];
    case 'column':
      return [
        [P(cx - 30, base), P(cx - 30, base - 8), P(cx + 30, base - 8), P(cx + 30, base)],
        [P(cx - 20, base - 6), P(cx - 15, base - 70), P(cx + 15, base - 70), P(cx + 20, base - 6)],
        [P(cx - 26, base - 68), P(cx - 26, base - 82), P(cx + 26, base - 82), P(cx + 26, base - 68)],
      ];
    case 'obelisk':
      return [[P(cx - 24, base), P(cx - 13, base - 84), P(cx + 13, base - 84), P(cx + 24, base)]];
    case 'trunk':
      // Silhouette d'arbre : fût effilé + deux moûts de branches, pas un pilier droit.
      return [
        [P(cx - 26, base), P(cx - 21, base - 34), P(cx - 15, base - 88),
          P(cx + 6, base - 92), P(cx + 13, base - 40), P(cx + 26, base)],
        [P(cx - 14, base - 62), P(cx - 34, base - 88), P(cx - 30, base - 76), P(cx - 12, base - 54)],
        [P(cx + 8, base - 70), P(cx + 32, base - 92), P(cx + 30, base - 80), P(cx + 10, base - 62)],
      ];
    case 'shelf':
      return [
        [P(cx - 34, base), P(cx - 34, base - 26), P(cx + 34, base - 26), P(cx + 34, base)],
        [P(cx - 28, base - 28), P(cx - 28, base - 54), P(cx + 30, base - 54), P(cx + 30, base - 28)],
        [P(cx - 22, base - 56), P(cx - 22, base - 76), P(cx + 24, base - 76), P(cx + 24, base - 56)],
      ];
    case 'rubble':
      // Éboulis : dalles effondrées en tas, aucune arête verticale — lit comme un passage bouché.
      return [
        [P(cx - 38, base), P(cx - 30, base - 26), P(cx - 6, base - 34), P(cx + 8, base - 20), P(cx + 14, base)],
        [P(cx + 6, base), P(cx + 14, base - 30), P(cx + 32, base - 40), P(cx + 40, base - 12), P(cx + 38, base)],
        [P(cx - 16, base - 32), P(cx - 6, base - 58), P(cx + 14, base - 52), P(cx + 16, base - 28)],
      ];
    case 'boulder':
      // Rocher massif, silhouette bombée.
      return [
        [P(cx - 36, base), P(cx - 38, base - 26), P(cx - 24, base - 52), P(cx - 2, base - 60),
          P(cx + 22, base - 50), P(cx + 36, base - 24), P(cx + 36, base)],
        [P(cx + 20, base), P(cx + 26, base - 22), P(cx + 40, base - 16), P(cx + 42, base)],
      ];
    case 'deadfall':
      // Tronc couché en travers + souche : barrage naturel de sous-bois.
      return [
        [P(cx - 42, base - 6), P(cx - 40, base - 30), P(cx + 40, base - 22), P(cx + 42, base + 2)],
        [P(cx - 26, base), P(cx - 22, base - 40), P(cx - 6, base - 46), P(cx - 2, base - 4)],
        [P(cx + 12, base), P(cx + 16, base - 34), P(cx + 30, base - 30), P(cx + 30, base)],
      ];
    case 'crates':
      // Caisses et piles de volumes : encombrement de réserve d'archives.
      return [
        [P(cx - 36, base), P(cx - 36, base - 34), P(cx - 2, base - 40), P(cx - 2, base - 4)],
        [P(cx - 2, base - 4), P(cx - 2, base - 46), P(cx + 30, base - 38), P(cx + 30, base)],
        [P(cx - 28, base - 36), P(cx - 28, base - 66), P(cx + 4, base - 72), P(cx + 4, base - 40)],
        [P(cx + 6, base - 42), P(cx + 6, base - 60), P(cx + 26, base - 54), P(cx + 26, base - 38)],
      ];
    case 'gate':
      // Grille scellée : deux jambages, un linteau, des barreaux — infranchissable et lisible.
      return [
        [P(cx - 38, base), P(cx - 36, base - 96), P(cx - 22, base - 98), P(cx - 24, base)],
        [P(cx + 24, base), P(cx + 22, base - 98), P(cx + 36, base - 96), P(cx + 38, base)],
        [P(cx - 38, base - 92), P(cx - 34, base - 116), P(cx + 34, base - 116), P(cx + 38, base - 92)],
        [P(cx - 20, base), P(cx - 18, base - 90), P(cx - 12, base - 90), P(cx - 14, base)],
        [P(cx - 7, base), P(cx - 6, base - 90), P(cx + 1, base - 90), P(cx, base)],
        [P(cx + 7, base), P(cx + 8, base - 90), P(cx + 15, base - 90), P(cx + 14, base)],
      ];
    case 'brokenColumn':
      // Fût brisé net + chapiteau tombé à côté : ruine cérémonielle.
      return [
        [P(cx - 30, base), P(cx - 26, base - 10), P(cx + 6, base - 10), P(cx + 8, base)],
        [P(cx - 22, base - 8), P(cx - 19, base - 56), P(cx - 2, base - 62), P(cx + 2, base - 8)],
        [P(cx + 6, base), P(cx + 8, base - 18), P(cx + 34, base - 22), P(cx + 36, base - 2)],
        [P(cx + 12, base - 22), P(cx + 14, base - 34), P(cx + 34, base - 36), P(cx + 34, base - 22)],
      ];
    case 'spire':
    default:
      return [
        [P(cx - 28, base), P(cx - 18, base - 50), P(cx - 4, base - 92), P(cx + 6, base - 46), P(cx + 10, base)],
        [P(cx + 8, base), P(cx + 22, base - 42), P(cx + 34, base - 14), P(cx + 34, base)],
      ];
  }
}

// Trame interne par type de masse. La couleur par défaut vient du thème ; on ne la force
// que quand la matière est franchement autre (bois, métal).
const WALL_STYLE = {
  trunk: { striation: 'fibre', base: '#4a3a2c', deep: '#150f0b' },
  deadfall: { striation: 'fibre', base: '#453527', deep: '#130e0a' },
  crates: { striation: 'fibre', base: '#5b4830', deep: '#1a130c' },
  gate: { striation: 'masonry', base: '#3c3f4a', deep: '#111318' },
  shard: { striation: 'splinter' },
  spire: { striation: 'splinter' },
  rubble: { striation: 'splinter' },
  boulder: { striation: 'splinter' },
  monolith: { striation: 'masonry' },
  obelisk: { striation: 'masonry' },
  column: { striation: 'masonry' },
  brokenColumn: { striation: 'masonry' },
  shelf: { striation: 'fibre', base: '#54402a', deep: '#181008' },
};

function wallKind(th, variant) {
  const list = th.walls ?? [th.wall];
  return list[((variant | 0) % list.length + list.length) % list.length];
}

function bakeObstacle(name, variant, grain, elevation = 0) {
  const th = theme(name);
  const kind = wallKind(th, variant);
  const elev = Math.max(0, Math.min(TILE.MAX, elevation));
  const R = makeRng(hashSeed('wall:' + name + ':' + kind));
  // Même toile haute que les décors : une silhouette de mur monte bien au-dessus de la
  // tuile et serait tronquée à plat sur la toile de sol. À blitter avec le rect "prop".
  const cv = makeCanvas(SPRITE_W, PROP_SPRITE_H);
  const ctx = cv.getContext('2d');
  if (!ctx) return cv;
  ctx.translate(0, PROP_EXTRA_H);

  // ⚠ DIVERGENCE ASSUMÉE vis-à-vis des premiers lots — à reporter à chaque rafraîchissement.
  // Le socle suit l'ÉLÉVATION RÉELLE de la case. Le cuire toujours à TILE.MAX donnait un
  // piédestal de trois paliers à tout obstacle posé au niveau 0 : tout lisait comme une tour,
  // et les silhouettes basses (éboulis, rocher, tronc couché) perdaient leur rôle de barrage.
  paintRisers(ctx, elev, th, R, { grain });
  const c = corners(elev);
  ctx.save();
  ctx.clip(diamondPath(elev, 0.5));
  const g = ctx.createLinearGradient(c.left.x, c.top.y, c.right.x, c.bottom.y);
  g.addColorStop(0, rgba(shade(th.riser, 0.14), 1));
  g.addColorStop(1, rgba(th.riserDeep, 1));
  ctx.fillStyle = g; ctx.fill(diamondPath(elev, -1));
  speckle(ctx, c.cx - c.hw, c.cy - c.hh, TILE.W, TILE.H, R, Math.round(320 * grain), th.glow, '#000000');
  ctx.restore();
  ctx.strokeStyle = rgba(th.glow, 0.18); ctx.lineWidth = 1.6;
  ctx.stroke(diamondPath(elev));

  // Halo froid derrière la masse : détache la silhouette du fond, même en salle sombre.
  const sty = WALL_STYLE[kind] ?? { striation: 'masonry' };
  ctx.save();
  ctx.shadowColor = rgba(th.accent, 0.5);
  ctx.shadowBlur = 16;
  for (const poly of obstacleSilhouette(kind, R, elev)) paintMass(ctx, poly, th, R, { rim: 0.3, ...sty });
  ctx.restore();
  return cv;
}

// ── Décors verticaux ─────────────────────────────────────────────────────────────────
function bakeProp(name, propKind, grain) {
  const th = theme(name);
  const R = makeRng(hashSeed('prop:' + name + ':' + propKind));
  const cv = makeCanvas(SPRITE_W, PROP_SPRITE_H);
  const ctx = cv.getContext('2d');
  if (!ctx) return cv;
  ctx.translate(0, PROP_EXTRA_H);
  const cx = TILE.W / 2, base = centerY(0) + 6;
  const P = (x, y) => ({ x, y });

  // Ombre portée au sol, elliptique : ancre le décor sur sa case.
  ctx.fillStyle = rgba('#000000', 0.4);
  ctx.beginPath(); ctx.ellipse(cx, base - 2, 30, 12, 0, 0, Math.PI * 2); ctx.fill();

  if (propKind === 'beam') {
    // Colonne de lumière verticale : le motif du Seuil.
    const g = ctx.createLinearGradient(0, base - PROP_EXTRA_H - 60, 0, base);
    g.addColorStop(0, rgba(th.glow, 0));
    g.addColorStop(0.45, rgba(th.glow, 0.20));
    g.addColorStop(1, rgba(th.accent, 0.42));
    ctx.fillStyle = g;
    ctx.beginPath();
    ctx.moveTo(cx - 12, base - PROP_EXTRA_H - 60);
    ctx.lineTo(cx + 12, base - PROP_EXTRA_H - 60);
    ctx.lineTo(cx + 30, base);
    ctx.lineTo(cx - 30, base);
    ctx.closePath(); ctx.fill();
    ctx.fillStyle = rgba(th.glow, 0.5);
    ctx.beginPath(); ctx.ellipse(cx, base - 2, 26, 10, 0, 0, Math.PI * 2); ctx.fill();
    for (let i = 0; i < 26; i++) {
      ctx.fillStyle = rgba(th.glow, R2(R, 0.2, 0.7));
      ctx.beginPath();
      ctx.arc(cx + R2(R, -26, 26), base - R2(R, 4, PROP_EXTRA_H + 40), R2(R, 0.6, 1.8), 0, Math.PI * 2);
      ctx.fill();
    }
    return cv;
  }
  if (propKind === 'arch') {
    ctx.save();
    ctx.shadowColor = rgba(th.accent, 0.35); ctx.shadowBlur = 14;
    paintMass(ctx, [P(cx - 46, base), P(cx - 42, base - 120), P(cx - 26, base - 120), P(cx - 30, base)], th, R, {});
    paintMass(ctx, [P(cx + 30, base), P(cx + 26, base - 120), P(cx + 42, base - 120), P(cx + 46, base)], th, R, {});
    paintMass(ctx, [P(cx - 46, base - 118), P(cx - 40, base - 148), P(cx + 40, base - 148), P(cx + 46, base - 118)], th, R, {});
    ctx.restore();
    return cv;
  }
  if (propKind === 'trunk') {
    // Tronc : fût qui s'affine vers le haut, écorce brune (pas la mousse du sol),
    // contreforts de racines, branches basses et masse de feuillage au-dessus.
    const bark = '#4a3a2c', barkDeep = '#150f0b';
    const canopy = () => {
      ctx.save();
      ctx.globalCompositeOperation = 'multiply';
      for (let i = 0; i < 16; i++) {
        const a = R() * Math.PI * 2, rr = R2(R, 6, 46);
        const px = cx + Math.cos(a) * rr * 1.25;
        const py = base - 188 + Math.sin(a) * rr * 0.5;
        ctx.fillStyle = rgba(mix(th.topDeep, '#000000', R2(R, 0.15, 0.5)), 0.72);
        ctx.beginPath();
        ctx.ellipse(px, py, R2(R, 12, 26), R2(R, 8, 15), R2(R, -0.4, 0.4), 0, Math.PI * 2);
        ctx.fill();
      }
      ctx.restore();
      for (let i = 0; i < 26; i++) {
        const a = R() * Math.PI * 2, rr = R2(R, 4, 44);
        const px = cx + Math.cos(a) * rr * 1.2;
        const py = base - 190 + Math.sin(a) * rr * 0.48;
        ctx.fillStyle = rgba(mix(th.top, th.accent, R2(R, 0.1, 0.75)), R2(R, 0.10, 0.34));
        ctx.beginPath();
        ctx.ellipse(px, py, R2(R, 5, 15), R2(R, 3, 8), R2(R, -0.5, 0.5), 0, Math.PI * 2);
        ctx.fill();
      }
    };
    canopy();
    // Branches, dessinées avant le fût pour qu'elles semblent en partir.
    for (const [sx, sy, ex, ey] of [[-14, 118, -40, 168], [12, 132, 40, 176], [-6, 150, -20, 190]]) {
      ctx.beginPath();
      ctx.moveTo(cx + sx, base - sy);
      ctx.quadraticCurveTo(cx + sx + (ex - sx) * 0.5, base - sy - 18, cx + ex, base - ey);
      ctx.strokeStyle = rgba(barkDeep, 0.85);
      ctx.lineWidth = R2(R, 4, 6.5);
      ctx.lineCap = 'round';
      ctx.stroke();
      ctx.strokeStyle = rgba(shade(bark, 0.18), 0.5);
      ctx.lineWidth = 1.6;
      ctx.stroke();
    }
    ctx.save();
    ctx.shadowColor = rgba('#000000', 0.55); ctx.shadowBlur = 12;
    paintMass(ctx, [P(cx - 27, base), P(cx - 21, base - 34), P(cx - 17, base - 96),
      P(cx - 12, base - 172), P(cx + 9, base - 176), P(cx + 15, base - 98),
      P(cx + 20, base - 36), P(cx + 27, base)], th, R,
    { striation: 'fibre', base: bark, deep: barkDeep, rim: 0.1 });
    ctx.restore();
    // Contreforts de racines : élargissent l'assise, ancrent le tronc sur sa case.
    for (const [dx0, dx1] of [[-27, -40], [27, 41], [-8, -18], [10, 20]]) {
      paintMass(ctx, [P(cx + dx0, base - 4), P(cx + dx0 * 0.9, base - 26),
        P(cx + dx1, base + 2), P(cx + dx1 * 0.7, base + 4)], th, R,
      { striation: 'fibre', base: barkDeep, deep: '#0d0907', rim: 0.06 });
    }
    // Mousse qui remonte du pied, côté ombre : relie le tronc à la palette Forêt.
    for (let i = 0; i < 22; i++) {
      const mx = cx + R2(R, -24, 24), my = base - R2(R, 2, 54);
      ctx.fillStyle = rgba(mix(th.top, th.accent, R2(R, 0.1, 0.6)), R2(R, 0.10, 0.32));
      ctx.beginPath();
      ctx.ellipse(mx, my, R2(R, 2, 7), R2(R, 1.5, 4), R2(R, -0.5, 0.5), 0, Math.PI * 2);
      ctx.fill();
    }
    return cv;
  }
  if (propKind === 'spire') {
    ctx.save();
    ctx.shadowColor = rgba(th.accent, 0.55); ctx.shadowBlur = 18;
    paintMass(ctx, [P(cx - 30, base), P(cx - 14, base - 80), P(cx + 2, base - 168),
      P(cx + 10, base - 74), P(cx + 30, base)], th, R, { striation: 'splinter', rim: 0.34 });
    // Éclat secondaire, plus court : casse la symétrie et épaissit la silhouette.
    paintMass(ctx, [P(cx + 8, base), P(cx + 22, base - 62), P(cx + 30, base - 96),
      P(cx + 32, base - 30), P(cx + 34, base)], th, R,
    { striation: 'splinter', base: shade(th.riser, -0.12), rim: 0.2 });
    ctx.restore();
    // Veines incandescentes le long des arêtes de fracture.
    for (let i = 0; i < 5; i++) {
      const y = base - R2(R, 20, 150);
      ctx.beginPath();
      ctx.moveTo(cx - 10 + R2(R, -8, 8), y);
      ctx.lineTo(cx + 6 + R2(R, -8, 8), y - R2(R, 8, 24));
      ctx.strokeStyle = rgba(th.glow, R2(R, 0.15, 0.45)); ctx.lineWidth = R2(R, 0.8, 2);
      ctx.stroke();
    }
    return cv;
  }
  if (propKind === 'obeliskProp') {
    ctx.save();
    ctx.shadowColor = rgba(th.accent, 0.3); ctx.shadowBlur = 14;
    paintMass(ctx, [P(cx - 22, base), P(cx - 11, base - 158), P(cx + 11, base - 158), P(cx + 22, base)], th, R, {});
    ctx.restore();
    return cv;
  }
  if (propKind === 'column') {
    ctx.save();
    ctx.shadowColor = rgba(th.accent, 0.3); ctx.shadowBlur = 12;
    paintMass(ctx, [P(cx - 32, base), P(cx - 32, base - 10), P(cx + 32, base - 10), P(cx + 32, base)], th, R, {});
    paintMass(ctx, [P(cx - 22, base - 8), P(cx - 17, base - 140), P(cx + 17, base - 140), P(cx + 22, base - 8)], th, R, {});
    paintMass(ctx, [P(cx - 28, base - 138), P(cx - 28, base - 156), P(cx + 28, base - 156), P(cx + 28, base - 138)], th, R, {});
    ctx.restore();
    for (let i = 0; i < 6; i++) {
      const x = cx - 16 + i * 6.4;
      ctx.beginPath(); ctx.moveTo(x, base - 14); ctx.lineTo(x + 1.2, base - 136);
      ctx.strokeStyle = rgba(i % 2 ? th.glow : '#000000', i % 2 ? 0.12 : 0.22);
      ctx.lineWidth = 1.4; ctx.stroke();
    }
    return cv;
  }
  if (propKind === 'npc') {
    // PNJ en attente : figure encapuchonnée, appuyée sur un bâton, légèrement de trois quarts.
    const cloth = mix(th.riser, '#2a2636', 0.5);
    const clothDeep = shade(cloth, -0.45);
    ctx.beginPath();
    ctx.moveTo(cx + 16, base - 4);
    ctx.lineTo(cx + 21, base - 74);
    ctx.strokeStyle = rgba('#3b2c1e', 0.95); ctx.lineWidth = 3; ctx.stroke();
    ctx.strokeStyle = rgba(th.glow, 0.18); ctx.lineWidth = 1; ctx.stroke();
    ctx.save();
    ctx.shadowColor = rgba('#000000', 0.6); ctx.shadowBlur = 10;
    // Robe : large à la base, resserrée aux épaules.
    const robe = new Path2D();
    robe.moveTo(cx - 19, base - 2);
    robe.quadraticCurveTo(cx - 14, base - 34, cx - 11, base - 52);
    robe.quadraticCurveTo(cx - 4, base - 62, cx + 7, base - 52);
    robe.quadraticCurveTo(cx + 12, base - 32, cx + 16, base - 2);
    robe.closePath();
    const rg = ctx.createLinearGradient(cx - 19, 0, cx + 16, 0);
    rg.addColorStop(0, rgba(shade(cloth, 0.12), 1));
    rg.addColorStop(0.7, rgba(cloth, 1));
    rg.addColorStop(1, rgba(clothDeep, 1));
    ctx.fillStyle = rg; ctx.fill(robe);
    ctx.restore();
    ctx.save();
    ctx.clip(robe);
    for (let i = 0; i < 7; i++) {
      const x = cx - 16 + i * 5;
      ctx.beginPath();
      ctx.moveTo(x, base - 4);
      ctx.quadraticCurveTo(x + R2(R, -3, 3), base - 30, x + R2(R, -2, 4), base - 52);
      ctx.strokeStyle = rgba(R() > 0.5 ? '#000000' : th.glow, R() > 0.5 ? 0.2 : 0.08);
      ctx.lineWidth = R2(R, 0.8, 1.8); ctx.stroke();
    }
    ctx.restore();
    // Capuche, visage dans l'ombre, une seule lueur d'œil — assez pour être vivant, jamais bavard.
    const hood = new Path2D();
    hood.moveTo(cx - 11, base - 50);
    hood.quadraticCurveTo(cx - 13, base - 72, cx - 1, base - 76);
    hood.quadraticCurveTo(cx + 10, base - 73, cx + 8, base - 50);
    hood.closePath();
    ctx.fillStyle = rgba(clothDeep, 1); ctx.fill(hood);
    ctx.strokeStyle = rgba(th.glow, 0.24); ctx.lineWidth = 1.3; ctx.stroke(hood);
    ctx.beginPath();
    ctx.ellipse(cx - 2, base - 62, 5.4, 6.4, 0, 0, Math.PI * 2);
    ctx.fillStyle = rgba('#05040a', 0.95); ctx.fill();
    ctx.beginPath();
    ctx.arc(cx - 1, base - 62, 1.2, 0, Math.PI * 2);
    ctx.fillStyle = rgba(th.glow, 0.75); ctx.fill();
    ctx.strokeStyle = rgba(th.accent, 0.2); ctx.lineWidth = 1;
    ctx.beginPath(); ctx.moveTo(cx - 11, base - 50); ctx.lineTo(cx + 8, base - 50); ctx.stroke();
    return cv;
  }
  if (propKind === 'campfire') {
    // Feu de camp : cercle de pierres, bûches croisées, braises. La flamme est animée
    // au runtime (drawFireFx) — rien de vivant n'est cuit dans le sprite.
    for (let i = 0; i < 9; i++) {
      const a = (i / 9) * Math.PI * 2 + 0.2;
      const sx = cx + Math.cos(a) * 26, sy = base - 2 + Math.sin(a) * 11;
      paintMass(ctx, [P(sx - 6, sy + 3), P(sx - 5, sy - 5), P(sx + 4, sy - 6), P(sx + 6, sy + 3)],
        th, R, { striation: 'splinter', base: '#514c52', deep: '#171519', rim: 0.12 });
    }
    ctx.save();
    ctx.beginPath();
    ctx.ellipse(cx, base - 2, 21, 9, 0, 0, Math.PI * 2);
    ctx.fillStyle = rgba('#0b0708', 0.95); ctx.fill();
    ctx.clip();
    for (let i = 0; i < 26; i++) {
      ctx.fillStyle = rgba(i % 3 ? '#ff8a3c' : '#ffd08a', R2(R, 0.25, 0.85));
      ctx.beginPath();
      ctx.arc(cx + R2(R, -18, 18), base - 2 + R2(R, -7, 7), R2(R, 0.7, 2), 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.restore();
    for (const [x0, y0, x1, y1] of [[-16, 2, 14, -14], [14, 2, -12, -15], [-4, 3, 6, -18]]) {
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.5); ctx.shadowBlur = 6;
      paintMass(ctx, [P(cx + x0 - 3, base - y0), P(cx + x0 + 3, base - y0 - 3),
        P(cx + x1 + 3, base + y1), P(cx + x1 - 3, base + y1 + 3)], th, R,
      { striation: 'fibre', base: '#3d2c1d', deep: '#120b06', rim: 0.14 });
      ctx.restore();
    }
    return cv;
  }
  if (propKind === 'star') {
    // Objet à ramasser : éclat en suspension au-dessus d'un socle de lumière.
    const g = ctx.createRadialGradient(cx, base - 34, 2, cx, base - 34, 40);
    g.addColorStop(0, rgba('#fff8e0', 0.55));
    g.addColorStop(0.45, rgba(th.accent, 0.18));
    g.addColorStop(1, rgba(th.accent, 0));
    ctx.fillStyle = g;
    ctx.fillRect(cx - 44, base - 78, 88, 88);
    ctx.fillStyle = rgba('#ffe9b8', 0.5);
    ctx.beginPath(); ctx.ellipse(cx, base - 2, 20, 8, 0, 0, Math.PI * 2); ctx.fill();
    const star = (r1, r2, alpha, col) => {
      ctx.beginPath();
      for (let i = 0; i < 8; i++) {
        const a = (i / 8) * Math.PI * 2 - Math.PI / 2;
        const r = i % 2 ? r2 : r1;
        const px = cx + Math.cos(a) * r, py = base - 34 + Math.sin(a) * r * (i % 2 ? 0.9 : 1.15);
        if (i === 0) ctx.moveTo(px, py); else ctx.lineTo(px, py);
      }
      ctx.closePath();
      ctx.fillStyle = rgba(col, alpha);
      ctx.fill();
    };
    ctx.save();
    ctx.shadowColor = rgba('#ffe9b8', 0.9); ctx.shadowBlur = 18;
    star(15, 4.5, 0.95, '#fff6dc');
    star(9, 2.6, 0.9, '#ffffff');
    ctx.restore();
    return cv;
  }
  if (propKind === 'monster' || propKind === 'elite') {
    // Menace génerique. Opposée point par point au PNJ : quadrupède et non bipède, basse et
    // allongée et non élancée, hérissée et non drapée, deux yeux et non un. Le corps prend
    // la couleur sombre de la salle ; les yeux gardent partout le même rouge — c'est le
    // signal de gameplay, il ne doit pas dépendre de l'ambiance.
    const big = propKind === 'elite';
    const s = big ? 1.16 : 1;
    const hide = mix(th.riserDeep, '#1a1216', 0.5);
    const hideLit = shade(hide, 0.3);
    const EYE = '#ff5a3c', EYE_DEEP = '#8e1a0e';
    const y = (v) => base - v * s;
    const x = (v) => cx + v * s;
    const M = (pts, o) => paintMass(ctx, pts.map(([a, b]) => P(x(a), y(b))), th, R, o);
    const skin = { striation: 'fibre', base: hide, deep: '#08060a', rim: 0.12 };
    const skinDark = { striation: 'fibre', base: shade(hide, -0.24), deep: '#050308', rim: 0.06 };

    // Ombre large et basse, décentrée vers l'arrière : la bête est ramassée sur ses pattes.
    ctx.fillStyle = rgba('#000000', 0.5);
    ctx.beginPath(); ctx.ellipse(x(-4), base - 1, 38 * s, 12 * s, 0, 0, Math.PI * 2); ctx.fill();

    // Pattes arrière, cuisse haute et jarret cassé — la poussée d'un prédateur.
    M([[-32, 2], [-28, 30], [-12, 32], [-16, 4]], skinDark);
    M([[-20, 4], [-17, 22], [-7, 22], [-10, 2]], skinDark);
    // Patte avant, tendue, portant le poids de l'encolure basse.
    M([[8, 2], [12, 30], [22, 30], [18, 2]], skinDark);
    for (const px0 of [-26, -13, 13]) {
      M([[px0 - 2, 6], [px0 + 11, 6], [px0 + 9, -1], [px0, -1]], { ...skinDark, rim: 0.1 });
      for (let k = 0; k < 3; k++) {
        ctx.beginPath();
        ctx.moveTo(x(px0 + k * 3.6), y(1));
        ctx.lineTo(x(px0 + 1.6 + k * 3.6), y(-3));
        ctx.lineTo(x(px0 + 3.2 + k * 3.6), y(1));
        ctx.closePath();
        ctx.fillStyle = rgba('#cbbfae', 0.72); ctx.fill();
      }
    }

    ctx.save();
    ctx.shadowColor = rgba('#000000', 0.6); ctx.shadowBlur = 12;
    // Tronc : croupe basse à gauche, garrot bossu au centre, ligne qui plonge vers l'avant.
    M([[-30, 4], [-40, 14], [-44, 22], [-52, 20], [-48, 26], [-38, 26], [-32, 20]], skinDark);
    M([[-36, 12], [-35, 24], [-31, 34], [-22, 42], [-10, 45], [2, 44], [12, 40],
      [20, 34], [24, 26], [22, 18], [18, 11], [4, 8], [-14, 8], [-28, 9]], skin);
    M([[-30, 28], [-27, 46], [-11, 48], [-8, 30]], { ...skin, base: hideLit, rim: 0.2 });
    // Encolure courte, presque horizontale : la tête est portée plus bas que le garrot.
    M([[16, 34], [20, 42], [36, 38], [33, 24]], skin);
    // Tête allongée, museau en avant, arcade sourcilière lourde.
    M([[28, 20], [31, 36], [50, 34], [56, 24], [48, 15], [33, 14]], { ...skin, base: hideLit, rim: 0.24 });
    ctx.restore();

    // Crête d'épines, inclinée vers l'arrière : la ligne qui dit « hostile » de loin.
    for (let i = 0; i < (big ? 7 : 5); i++) {
      const t0 = i / (big ? 6 : 4);
      const sx = x(-28 + t0 * 42);
      const sy = y(34 + Math.sin(t0 * Math.PI) * 12);
      const hgt = (big ? 20 : 15) * (0.55 + 0.45 * Math.sin(t0 * Math.PI)) * s;
      ctx.beginPath();
      ctx.moveTo(sx - 3.4 * s, sy);
      ctx.lineTo(sx - hgt * 0.42, sy - hgt);
      ctx.lineTo(sx + 3.4 * s, sy);
      ctx.closePath();
      ctx.fillStyle = rgba(shade(hide, -0.4), 0.95); ctx.fill();
      ctx.strokeStyle = rgba(EYE_DEEP, 0.3); ctx.lineWidth = 1; ctx.stroke();
    }
    if (big) {
      // Élite : deux cornes de bélier qui partent en arrière puis reviennent. Seule
      // différence de lecture avec la bête commune — même corps, même posture.
      for (const [off, up, al] of [[-4, 0, 0.55], [3, 4, 0.9]]) {
        const horn = new Path2D();
        horn.moveTo(x(38 + off), y(32 + up));
        horn.quadraticCurveTo(x(34 + off), y(50 + up), x(14 + off), y(58 + up));
        horn.quadraticCurveTo(x(28 + off), y(46 + up), x(31 + off), y(30 + up));
        horn.closePath();
        ctx.fillStyle = rgba('#cfc3ae', al); ctx.fill(horn);
        ctx.strokeStyle = rgba('#241a12', 0.4); ctx.lineWidth = 1.1; ctx.stroke(horn);
      }
    }
    // Gueule : dents claires sur noir. Jamais de bouche dessinée trait par trait.
    ctx.save();
    const maw = new Path2D();
    maw.moveTo(x(36), y(26)); maw.lineTo(x(55), y(25)); maw.lineTo(x(50), y(16)); maw.lineTo(x(36), y(17));
    maw.closePath();
    ctx.fillStyle = rgba('#0a0406', 0.95); ctx.fill(maw);
    ctx.clip(maw);
    for (let i = 0; i < 6; i++) {
      const tx = x(37 + i * 3);
      ctx.beginPath();
      ctx.moveTo(tx, y(25.5)); ctx.lineTo(tx + 1.4 * s, y(20)); ctx.lineTo(tx + 2.8 * s, y(25.5));
      ctx.closePath();
      ctx.fillStyle = rgba('#e8ded0', 0.62); ctx.fill();
    }
    ctx.restore();
    // Arcade : un simple trait sombre au-dessus des yeux, et le regard s'enfonce.
    ctx.beginPath();
    ctx.moveTo(x(31), y(32)); ctx.lineTo(x(49), y(31));
    ctx.strokeStyle = rgba('#050306', 0.7); ctx.lineWidth = 3 * s; ctx.stroke();
    // Yeux : paire rapprochée, halo chaud, sous l'arcade.
    ctx.save();
    ctx.shadowColor = rgba(EYE, 0.8); ctx.shadowBlur = 6;
    for (const dx0 of [35, 42]) {
      ctx.beginPath();
      ctx.ellipse(x(dx0), y(28.5), 2.4 * s, 1.7 * s, -0.2, 0, Math.PI * 2);
      ctx.fillStyle = rgba(EYE, 0.95); ctx.fill();
    }
    ctx.restore();
    return cv;
  }
  if (propKind === 'boss') {
    // Objectif de la salle : doit se voir de l'autre bout du plateau. Trois lectures
    // superpos\u00e9es \u2014 un sceau au sol, une masse deux fois plus haute qu'un PNJ, une couronne.
    const hide = mix(th.riserDeep, '#150f18', 0.5);
    const EYE = '#ff5a3c';
    ctx.fillStyle = rgba('#000000', 0.55);
    ctx.beginPath(); ctx.ellipse(cx, base - 1, 44, 16, 0, 0, Math.PI * 2); ctx.fill();

    // Sceau au sol : deux anneaux en perspective iso + rayons. Marque le territoire.
    ctx.save();
    ctx.globalCompositeOperation = 'lighter';
    for (const [rr, al, lw] of [[44, 0.5, 2.2], [31, 0.3, 1.4]]) {
      ctx.beginPath();
      ctx.ellipse(cx, base - 2, rr, rr * 0.42, 0, 0, Math.PI * 2);
      ctx.strokeStyle = rgba(th.accent, al); ctx.lineWidth = lw;
      ctx.shadowColor = rgba(th.accent, 0.8); ctx.shadowBlur = 12;
      ctx.stroke();
    }
    for (let i = 0; i < 8; i++) {
      const a = (i / 8) * Math.PI * 2;
      ctx.beginPath();
      ctx.moveTo(cx + Math.cos(a) * 31, base - 2 + Math.sin(a) * 13);
      ctx.lineTo(cx + Math.cos(a) * 44, base - 2 + Math.sin(a) * 18);
      ctx.strokeStyle = rgba(th.glow, 0.3); ctx.lineWidth = 1.4;
      ctx.stroke();
    }
    ctx.restore();

    // Manteau : base tr\u00e8s large qui se resserre aux \u00e9paules, tomb\u00e9 jusqu'au sol.
    ctx.save();
    ctx.shadowColor = rgba('#000000', 0.7); ctx.shadowBlur = 18;
    const cloak = new Path2D();
    cloak.moveTo(cx - 40, base - 2);
    cloak.quadraticCurveTo(cx - 34, base - 66, cx - 24, base - 118);
    cloak.quadraticCurveTo(cx - 6, base - 136, cx + 20, base - 118);
    cloak.quadraticCurveTo(cx + 32, base - 62, cx + 40, base - 2);
    cloak.closePath();
    const cg = ctx.createLinearGradient(cx - 40, 0, cx + 40, 0);
    cg.addColorStop(0, rgba(shade(hide, 0.2), 1));
    cg.addColorStop(0.55, rgba(hide, 1));
    cg.addColorStop(1, rgba('#080509', 1));
    ctx.fillStyle = cg; ctx.fill(cloak);
    ctx.restore();
    ctx.save();
    ctx.clip(cloak);
    for (let i = 0; i < 11; i++) {
      const x0 = cx - 34 + i * 6.6;
      ctx.beginPath();
      ctx.moveTo(x0, base - 4);
      ctx.quadraticCurveTo(x0 + R2(R, -5, 5), base - 70, x0 + R2(R, -3, 7), base - 120);
      ctx.strokeStyle = rgba(R() > 0.45 ? '#000000' : th.accent, R() > 0.45 ? 0.34 : 0.12);
      ctx.lineWidth = R2(R, 1, 2.6); ctx.stroke();
    }
    // Lueur qui monte du bas du manteau : la masse a quelque chose dedans.
    const ig = ctx.createLinearGradient(0, base, 0, base - 60);
    ig.addColorStop(0, rgba(th.accent, 0.28));
    ig.addColorStop(1, rgba(th.accent, 0));
    ctx.fillStyle = ig; ctx.fillRect(cx - 44, base - 60, 88, 60);
    ctx.restore();

    // \u00c9paules : deux \u00e9paulettes angulaires qui \u00e9largissent le haut de la silhouette.
    for (const dir of [-1, 1]) {
      paintMass(ctx, [P(cx + dir * 16, base - 100), P(cx + dir * 34, base - 116),
        P(cx + dir * 42, base - 98), P(cx + dir * 22, base - 88)], th, R,
      { striation: 'masonry', base: shade(hide, 0.12), deep: '#0a070c', rim: 0.24 });
    }
    // T\u00eate voil\u00e9e + couronne de cornes.
    const head = new Path2D();
    head.moveTo(cx - 15, base - 118);
    head.quadraticCurveTo(cx - 17, base - 152, cx - 1, base - 158);
    head.quadraticCurveTo(cx + 14, base - 152, cx + 12, base - 118);
    head.closePath();
    ctx.fillStyle = rgba('#0a0710', 0.98); ctx.fill(head);
    ctx.strokeStyle = rgba(th.accent, 0.3); ctx.lineWidth = 1.4; ctx.stroke(head);
    ctx.save();
    ctx.shadowColor = rgba(th.glow, 0.7); ctx.shadowBlur = 14;
    for (let i = 0; i < 5; i++) {
      const t0 = i / 4;
      const hx0 = cx - 22 + t0 * 44;
      const hgt = 34 + Math.sin(t0 * Math.PI) * 26;
      ctx.beginPath();
      ctx.moveTo(hx0 - 5, base - 146);
      ctx.quadraticCurveTo(hx0 + (t0 - 0.5) * 34, base - 146 - hgt * 0.62, hx0 + (t0 - 0.5) * 58, base - 142 - hgt);
      ctx.lineTo(hx0 + 5, base - 146);
      ctx.closePath();
      ctx.fillStyle = rgba(mix('#d8cdbb', th.accent, 0.35), 0.9); ctx.fill();
      ctx.strokeStyle = rgba('#1a1218', 0.55); ctx.lineWidth = 1.2; ctx.stroke();
    }
    ctx.restore();
    // Trois yeux : une paire, plus un troisi\u00e8me au front. Impossible \u00e0 confondre avec la b\u00eate.
    ctx.save();
    ctx.shadowColor = rgba(EYE, 0.95); ctx.shadowBlur = 16;
    for (const [ex, ey, er] of [[-7, 134, 3.4], [5, 134, 3.4], [-1, 146, 2.2]]) {
      ctx.beginPath();
      ctx.ellipse(cx + ex, base - ey, er, er * 0.72, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba(EYE, 0.95); ctx.fill();
    }
    ctx.restore();
    // \u00c9clats en suspension autour des \u00e9paules : le boss d\u00e9range l'espace autour de lui.
    for (let i = 0; i < 7; i++) {
      const a = R() * Math.PI * 2, rr = R2(R, 46, 74);
      const px = cx + Math.cos(a) * rr, py = base - 110 + Math.sin(a) * rr * 0.5;
      ctx.save();
      ctx.translate(px, py); ctx.rotate(R2(R, -0.8, 0.8));
      ctx.fillStyle = rgba(th.glow, R2(R, 0.2, 0.5));
      ctx.beginPath();
      ctx.moveTo(0, -R2(R, 5, 11)); ctx.lineTo(R2(R, 2, 4), 0); ctx.lineTo(0, R2(R, 4, 9)); ctx.lineTo(-R2(R, 2, 4), 0);
      ctx.closePath(); ctx.fill();
      ctx.restore();
    }
    return cv;
  }
  if (propKind === 'merchant') {
    // \u00c9tal : tr\u00e9teau, drap, auvent, ballots, lanterne. Aucune figure \u2014 c'est le commerce
    // qu'on doit reconna\u00eetre, pas un personnage (le PNJ garde sa silhouette).
    const wood = '#4a3626', woodDeep = '#150e08';
    for (const dx0 of [-34, 30]) {
      paintMass(ctx, [P(cx + dx0, base), P(cx + dx0 + 1, base - 96),
        P(cx + dx0 + 6, base - 96), P(cx + dx0 + 6, base)], th, R,
      { striation: 'fibre', base: wood, deep: woodDeep, rim: 0.1 });
    }
    // Auvent ray\u00e9, l\u00e9g\u00e8rement pentu, festonn\u00e9 en bord.
    const cloth = mix(th.accent, '#7a3b32', 0.55);
    const awn = new Path2D();
    awn.moveTo(cx - 40, base - 92); awn.lineTo(cx + 40, base - 100);
    awn.lineTo(cx + 44, base - 78); awn.lineTo(cx - 44, base - 70);
    awn.closePath();
    ctx.save();
    ctx.shadowColor = rgba('#000000', 0.5); ctx.shadowBlur = 10;
    ctx.fillStyle = rgba(cloth, 1); ctx.fill(awn);
    ctx.restore();
    ctx.save();
    ctx.clip(awn);
    for (let i = 0; i < 7; i++) {
      ctx.fillStyle = rgba(i % 2 ? shade(cloth, 0.22) : shade(cloth, -0.26), 0.85);
      ctx.fillRect(cx - 44 + i * 12.6, base - 104, 6.3, 40);
    }
    ctx.restore();
    for (let i = 0; i < 8; i++) {
      const fx = cx - 42 + i * 11;
      ctx.beginPath();
      ctx.arc(fx + 5, base - 71 - i * 1, 5.5, 0, Math.PI);
      ctx.fillStyle = rgba(shade(cloth, -0.18), 0.9); ctx.fill();
    }
    // Plateau + drap qui pend.
    paintMass(ctx, [P(cx - 42, base - 40), P(cx - 42, base - 47), P(cx + 42, base - 52), P(cx + 42, base - 45)],
      th, R, { striation: 'fibre', base: shade(wood, 0.16), deep: woodDeep, rim: 0.16 });
    const drape = new Path2D();
    drape.moveTo(cx - 40, base - 42); drape.lineTo(cx + 40, base - 47);
    drape.quadraticCurveTo(cx + 20, base - 8, cx - 2, base - 14);
    drape.quadraticCurveTo(cx - 24, base - 6, cx - 40, base - 16);
    drape.closePath();
    ctx.fillStyle = rgba(mix(th.riser, cloth, 0.35), 0.96); ctx.fill(drape);
    ctx.save();
    ctx.clip(drape);
    for (let i = 0; i < 9; i++) {
      const x0 = cx - 38 + i * 9;
      ctx.beginPath(); ctx.moveTo(x0, base - 44); ctx.lineTo(x0 + R2(R, -3, 3), base - 12);
      ctx.strokeStyle = rgba('#000000', R2(R, 0.1, 0.28)); ctx.lineWidth = R2(R, 1, 2.2); ctx.stroke();
    }
    ctx.restore();
    // Marchandise : ballots ficel\u00e9s, deux fioles, une pi\u00e8ce de tissu roul\u00e9e.
    for (const [bx, by, brx, bry] of [[-26, 52, 11, 8], [-6, 54, 9, 7], [24, 56, 12, 9]]) {
      ctx.beginPath();
      ctx.ellipse(cx + bx, base - by, brx, bry, R2(R, -0.2, 0.2), 0, Math.PI * 2);
      ctx.fillStyle = rgba(mix(th.riser, '#6b5537', 0.6), 1); ctx.fill();
      ctx.strokeStyle = rgba('#1a1208', 0.6); ctx.lineWidth = 1.2; ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(cx + bx - brx, base - by); ctx.lineTo(cx + bx + brx, base - by);
      ctx.strokeStyle = rgba('#d8c49a', 0.35); ctx.lineWidth = 1; ctx.stroke();
    }
    for (const vx of [6, 14]) {
      paintMass(ctx, [P(cx + vx - 3, base - 52), P(cx + vx - 2, base - 64),
        P(cx + vx + 2, base - 64), P(cx + vx + 3, base - 52)], th, R,
      { striation: 'masonry', base: mix(th.glow, '#3c5a52', 0.5), deep: '#0d1614', rim: 0.3 });
    }
    // Lanterne suspendue \u00e0 l'auvent : la seule source de lumi\u00e8re, elle dit \"ouvert\".
    ctx.beginPath();
    ctx.moveTo(cx + 34, base - 76); ctx.lineTo(cx + 34, base - 64);
    ctx.strokeStyle = rgba('#2a2018', 0.9); ctx.lineWidth = 1.4; ctx.stroke();
    ctx.save();
    ctx.shadowColor = rgba('#ffcf8a', 0.9); ctx.shadowBlur = 16;
    ctx.fillStyle = rgba('#ffdca4', 0.9);
    ctx.beginPath(); ctx.ellipse(cx + 34, base - 58, 5, 6.5, 0, 0, Math.PI * 2); ctx.fill();
    ctx.restore();
    return cv;
  }
  if (propKind === 'curse') {
    // Mal\u00e9diction : rien de vivant, rien de chaud. Une st\u00e8le fendue, un anneau bris\u00e9 en
    // suspension, des vrilles qui rampent. La palette ignore l'accent de la salle \u2014 le
    // violet-noir doit \u00eatre le m\u00eame partout, comme le rouge des yeux de b\u00eate.
    const ink = '#1d0b22', inkDeep = '#070309', wound = '#6e1550';
    ctx.fillStyle = rgba('#000000', 0.5);
    ctx.beginPath(); ctx.ellipse(cx, base - 1, 28, 11, 0, 0, Math.PI * 2); ctx.fill();
    for (let i = 0; i < 9; i++) {
      const a = R() * Math.PI * 2, rr = R2(R, 12, 32);
      ctx.beginPath();
      ctx.moveTo(cx, base - 2);
      ctx.quadraticCurveTo(cx + Math.cos(a) * rr * 0.6, base - 2 + Math.sin(a) * rr * 0.3 - 6,
        cx + Math.cos(a) * rr, base - 2 + Math.sin(a) * rr * 0.42);
      ctx.strokeStyle = rgba(inkDeep, R2(R, 0.4, 0.85)); ctx.lineWidth = R2(R, 1, 2.6); ctx.lineCap = 'round';
      ctx.stroke();
    }
    ctx.save();
    ctx.shadowColor = rgba(wound, 0.3); ctx.shadowBlur = 9;
    paintMass(ctx, [P(cx - 17, base), P(cx - 13, base - 62), P(cx - 3, base - 74),
      P(cx + 4, base - 60), P(cx + 16, base)], th, R,
    { striation: 'splinter', base: ink, deep: inkDeep, rim: 0.16 });
    ctx.restore();
    // Fente qui court sur la st\u00e8le, ourl\u00e9e de lumi\u00e8re mauve.
    ctx.beginPath();
    ctx.moveTo(cx - 4, base - 70);
    ctx.quadraticCurveTo(cx + 4, base - 44, cx - 6, base - 8);
    ctx.strokeStyle = rgba(mix(wound, '#ff86c8', 0.35), 0.5); ctx.lineWidth = 1.6;
    ctx.shadowColor = rgba(wound, 0.6); ctx.shadowBlur = 7;
    ctx.stroke();
    ctx.shadowBlur = 0;
    // Anneau bris\u00e9 en suspension au-dessus : trois arcs, jamais un cercle ferm\u00e9.
    for (const [a0, a1] of [[-0.2, 1.5], [1.9, 3.2], [3.7, 5.6]]) {
      ctx.beginPath();
      ctx.ellipse(cx, base - 96, 22, 9, 0, a0, a1);
      ctx.strokeStyle = rgba(mix(wound, '#c88ab0', 0.3), 0.4); ctx.lineWidth = 2.2;
      ctx.shadowColor = rgba(wound, 0.5); ctx.shadowBlur = 8;
      ctx.stroke();
    }
    ctx.shadowBlur = 0;
    for (let i = 0; i < 12; i++) {
      ctx.fillStyle = rgba(mix(wound, '#000000', R2(R, 0, 0.5)), R2(R, 0.2, 0.6));
      ctx.beginPath();
      ctx.arc(cx + R2(R, -26, 26), base - 96 + R2(R, -22, 22), R2(R, 0.7, 2), 0, Math.PI * 2);
      ctx.fill();
    }
    return cv;
  }
  // cairn : petit empilement d'éclats de pierre, discret, sert de repère.
  ctx.save();
  ctx.shadowColor = rgba('#000000', 0.5); ctx.shadowBlur = 8;
  const stone = shade(th.riser, -0.06);
  paintMass(ctx, [P(cx - 26, base), P(cx - 22, base - 14), P(cx - 4, base - 18),
    P(cx + 14, base - 12), P(cx + 18, base)], th, R, { striation: 'splinter', base: stone, rim: 0.14 });
  paintMass(ctx, [P(cx - 19, base - 16), P(cx - 13, base - 34), P(cx + 6, base - 36),
    P(cx + 11, base - 15)], th, R,
  { striation: 'splinter', base: shade(th.riser, 0.06), rim: 0.16 });
  paintMass(ctx, [P(cx - 9, base - 35), P(cx - 3, base - 52), P(cx + 8, base - 48),
    P(cx + 6, base - 33)], th, R, { striation: 'splinter', base: stone, rim: 0.2 });
  ctx.restore();
  // Joints d'ombre entre les pierres, bornés à la masse : c'est ça qui fait lire l'empilement.
  for (const [y, xa, xb] of [[base - 15, cx - 20, cx + 12], [base - 34, cx - 11, cx + 6]]) {
    ctx.beginPath();
    ctx.moveTo(xa, y + R2(R, -1, 1));
    ctx.lineTo(xb, y + R2(R, -1.5, 1.5));
    ctx.strokeStyle = rgba('#000000', 0.5); ctx.lineWidth = 2;
    ctx.stroke();
  }
  return cv;
}

// ── Surbrillances de gameplay (couche séparée) ───────────────────────────────────────
// Chaque variante doit se lire sans sa couleur : remplissage, hachure, chevron ou croix.
// Deux variantes qui ne diffèrent que par la teinte se confondent dès que la salle est chaude.
const HL = {
  move: { col: '#a6c8ff', fill: 0.16, line: 0.7 },
  attack: { col: '#ff9b86', fill: 0.14, line: 0.75 },
  cursor: { col: '#ffd98a', fill: 0.10, line: 0.95 },
  path: { col: '#ffe9b8', fill: 0.26, line: 0.5 },
  aoe: { col: '#ff6a4a', fill: 0.36, line: 0.95 },
  threat: { col: '#d24a5e', fill: 0, line: 0.5 },
  blocked: { col: '#7d7896', fill: 0.09, line: 0.55 },
  height: { col: '#7ef0d0', fill: 0.11, line: 0.8 },
  occupied: { col: '#9aa2c4', fill: 0.05, line: 0.4 },
};
function bakeHighlight(variant, elev) {
  const spec = HL[variant] ?? HL.move;
  const cv = makeCanvas(SPRITE_W, SPRITE_H);
  const ctx = cv.getContext('2d');
  if (!ctx) return cv;
  const c = corners(elev);
  const p = diamondPath(elev, 3);
  if (spec.fill > 0) {
    const g = ctx.createLinearGradient(c.top.x, c.top.y, c.bottom.x, c.bottom.y);
    g.addColorStop(0, rgba(spec.col, spec.fill * 1.5));
    g.addColorStop(1, rgba(spec.col, spec.fill * 0.4));
    ctx.fillStyle = g;
    ctx.fill(p);
  }
  // Menace : hachure et non aplat. Une case tenue par l'ennemi n'est pas une case offerte —
  // elle ne doit jamais avoir la même matière qu'une case de déplacement.
  if (variant === 'threat') {
    ctx.save();
    ctx.clip(p);
    for (let i = -TILE.W; i < TILE.W; i += 8) {
      ctx.beginPath();
      ctx.moveTo(c.cx + i, c.cy - TILE.H);
      ctx.lineTo(c.cx + i + TILE.H * 2, c.cy + TILE.H);
      ctx.strokeStyle = rgba(spec.col, 0.3);
      ctx.lineWidth = 1.5;
      ctx.stroke();
    }
    ctx.restore();
  }
  ctx.save();
  ctx.shadowColor = rgba(spec.col, variant === 'occupied' ? 0.25 : 0.8);
  ctx.shadowBlur = variant === 'occupied' ? 4 : 10;
  ctx.strokeStyle = rgba(spec.col, spec.line);
  ctx.lineWidth = (variant === 'cursor' || variant === 'aoe') ? 2.6 : 1.8;
  if (variant === 'cursor') ctx.setLineDash([9, 6]);
  if (variant === 'threat') ctx.setLineDash([5, 4]);
  ctx.stroke(p);
  ctx.restore();
  if (variant === 'cursor') {
    for (const k of ['top', 'right', 'bottom', 'left']) {
      ctx.fillStyle = rgba('#fff3d4', 0.9);
      ctx.beginPath(); ctx.arc(c[k].x, c[k].y, 2.4, 0, Math.PI * 2); ctx.fill();
    }
  }
  if (variant === 'aoe') {
    // Double contour : l'impact se distingue de la portée même en pleine lumière de forge.
    ctx.strokeStyle = rgba('#ffe0d0', 0.6);
    ctx.lineWidth = 1.2;
    ctx.stroke(diamondPath(elev, 15));
  }
  if (variant === 'blocked') {
    ctx.strokeStyle = rgba('#d5d0e8', 0.7);
    ctx.lineWidth = 2.4; ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(c.cx - 13, c.cy - 7); ctx.lineTo(c.cx + 13, c.cy + 7);
    ctx.moveTo(c.cx + 13, c.cy - 7); ctx.lineTo(c.cx - 13, c.cy + 7);
    ctx.stroke();
  }
  if (variant === 'height') {
    // Trois chevrons montants : le bonus de hauteur se lit comme une direction, pas une couleur.
    for (let i = 0; i < 3; i++) {
      const yy = c.cy + 7 - i * 7;
      ctx.beginPath();
      ctx.moveTo(c.cx - 10, yy); ctx.lineTo(c.cx, yy - 5.5); ctx.lineTo(c.cx + 10, yy);
      ctx.strokeStyle = rgba(spec.col, 0.8 - i * 0.2);
      ctx.lineWidth = 2; ctx.lineJoin = 'round';
      ctx.stroke();
    }
  }
  return cv;
}

/** Variantes de surbrillance disponibles, dans l'ordre de la grammaire de lecture. */
export const HIGHLIGHT_VARIANTS = Object.keys(HL);

function bakeParty(elev) {
  const cv = makeCanvas(SPRITE_W, SPRITE_H);
  const ctx = cv.getContext('2d');
  if (!ctx) return cv;
  const cx = TILE.W / 2, cy = centerY(elev);
  const r = TILE.W * 0.45 / 2;
  ctx.fillStyle = rgba('#000000', 0.45);
  ctx.beginPath(); ctx.ellipse(cx, cy + r * 0.5, r * 0.9, r * 0.34, 0, 0, Math.PI * 2); ctx.fill();
  ctx.save();
  ctx.strokeStyle = rgba('#dcb45c', 0.75); ctx.lineWidth = 1.5;
  ctx.beginPath(); ctx.ellipse(cx, cy + r * 0.5, r * 0.92, r * 0.36, 0, 0, Math.PI * 2); ctx.stroke();
  ctx.restore();
  ctx.save();
  ctx.shadowColor = rgba('#dcb45c', 0.9); ctx.shadowBlur = 16;
  const g = ctx.createRadialGradient(cx - r * 0.3, cy - r * 0.4, r * 0.1, cx, cy, r);
  g.addColorStop(0, '#fff3d0');
  g.addColorStop(0.6, '#e8c877');
  g.addColorStop(1, '#8f6d2c');
  ctx.fillStyle = g;
  ctx.beginPath(); ctx.arc(cx, cy - 2, r, 0, Math.PI * 2); ctx.fill();
  ctx.strokeStyle = rgba('#fff8e4', 0.7); ctx.lineWidth = 1.6; ctx.stroke();
  ctx.restore();
  return cv;
}

// ── Sol ──────────────────────────────────────────────────────────────────────────────
function bakeFloor(key, grain) {
  const th = theme(key.theme);
  const seed = `floor:${key.theme}:${key.elevation}:${key.surfaceSeed ?? 0}:${key.hidden ?? ''}:${key.danger ?? ''}:${key.cliffLeft ? 'L' : ''}${key.cliffRight ? 'R' : ''}`;
  const R = makeRng(hashSeed(seed));
  const cv = makeCanvas(SPRITE_W, SPRITE_H);
  const ctx = cv.getContext('2d');
  if (!ctx) return cv;

  paintRisers(ctx, key.elevation, th, R, {
    cliffLeft: key.cliffLeft, cliffRight: key.cliffRight, grain,
  });
  paintTop(ctx, key.elevation, th, R, {
    grain, hidden: key.hidden, danger: key.danger,
  });

  if (key.glow) {
    ctx.save();
    ctx.shadowColor = rgba(th.accent, 0.95);
    ctx.shadowBlur = 18;
    ctx.strokeStyle = rgba(th.accent, 0.85);
    ctx.lineWidth = 2.5;
    ctx.stroke(diamondPath(key.elevation, -2));
    ctx.restore();
  }
  if (key.resolved) {
    ctx.save();
    ctx.globalCompositeOperation = 'source-atop';
    ctx.fillStyle = rgba('#06060c', 0.42);
    ctx.fillRect(0, 0, SPRITE_W, SPRITE_H);
    ctx.restore();
  }
  return cv;
}

// ── Cache ────────────────────────────────────────────────────────────────────────────
export const ALLY_RIM = '#8fc3ff';
export const ENEMY_RIM = '#ff5a3c';
export const ALLY_ROLES = ['guard', 'bruiser', 'skirmisher', 'mystic'];

// Quatre archétypes alliés, opposés point par point les uns aux autres ET au PNJ encapuchonné :
// il faut pouvoir dire qui est qui à la taille d'une case, de l'autre bout du plateau.
//   guard       — pavois plein hauteur, casque à fente. Silhouette : rectangle.
//   bruiser     — épaules énormes, tête enfoncée, hache posée au sol. Silhouette : trapèze.
//   skirmisher  — penché en avant, cape fendue, deux lames. Silhouette : oblique étroite.
//   mystic      — fuseau élancé, tête NUE (c'est ce qui le sépare du PNJ), focus en lévitation.
// Le corps est reteinté par la salle ; le liseré reste le même bleu partout — c'est du signal
// de camp, pas de l'ambiance. Même règle que le rouge des yeux du bestiaire.
function bakeUnit(name, role, grain) {
  const th = theme(name);
  const R = makeRng(hashSeed('unit:' + name + ':' + role));
  const cv = makeCanvas(SPRITE_W, PROP_SPRITE_H);
  const ctx = cv.getContext('2d');
  if (!ctx) return cv;
  ctx.translate(0, PROP_EXTRA_H);
  const cx = TILE.W / 2, base = centerY(0) + 4;
  const P = (x, y) => ({ x, y });
  const cloth = mix(th.riser, '#2b3348', 0.62);
  const clothDeep = shade(cloth, -0.5);
  const steel = mix(th.riser, '#8d97ad', 0.55);
  const steelDeep = shade(steel, -0.45);
  const skin = mix(th.top, '#e8cdb4', 0.7);

  ctx.fillStyle = rgba('#000000', 0.42);
  ctx.beginPath(); ctx.ellipse(cx, base - 1, 22, 9, 0, 0, Math.PI * 2); ctx.fill();

  const rim = (path, a = 0.5) => {
    ctx.strokeStyle = rgba(ALLY_RIM, a); ctx.lineWidth = 1.3; ctx.stroke(path);
  };
  const legs = (spread, h) => {
    ctx.save();
    ctx.shadowColor = rgba('#000000', 0.55); ctx.shadowBlur = 7;
    paintMass(ctx, [P(cx - spread, base), P(cx - spread + 3, base - h), P(cx - 2, base - h), P(cx - 3, base)], th, R, { base: clothDeep, rim: 0.1 });
    paintMass(ctx, [P(cx + 3, base), P(cx + 2, base - h), P(cx + spread - 3, base - h), P(cx + spread, base)], th, R, { base: clothDeep, rim: 0.1 });
    ctx.restore();
  };

  if (role === 'guard') {
    legs(13, 26);
    ctx.save();
    ctx.shadowColor = rgba('#000000', 0.6); ctx.shadowBlur = 9;
    paintMass(ctx, [P(cx - 13, base - 24), P(cx - 15, base - 62), P(cx + 14, base - 62), P(cx + 12, base - 24)], th, R, { base: cloth, deep: clothDeep, rim: 0.14 });
    ctx.restore();
    // Casque : calotte + fente horizontale unique. Aucune capuche, aucun drapé.
    const helm = new Path2D();
    helm.moveTo(cx - 10, base - 60);
    helm.quadraticCurveTo(cx - 11, base - 80, cx, base - 82);
    helm.quadraticCurveTo(cx + 11, base - 80, cx + 10, base - 60);
    helm.closePath();
    ctx.fillStyle = rgba(steel, 1); ctx.fill(helm); rim(helm, 0.55);
    ctx.fillStyle = rgba('#05070e', 0.95);
    ctx.fillRect(cx - 8, base - 71, 16, 3.4);
    ctx.fillStyle = rgba(ALLY_RIM, 0.5);
    ctx.fillRect(cx - 8, base - 71, 16, 1.1);
    // Pavois : la pièce qui porte la silhouette. Devant tout le reste, plein hauteur.
    const sh = new Path2D();
    sh.moveTo(cx - 24, base - 74);
    sh.lineTo(cx + 4, base - 76);
    sh.lineTo(cx + 6, base - 16);
    sh.quadraticCurveTo(cx - 10, base - 2, cx - 25, base - 16);
    sh.closePath();
    ctx.save();
    ctx.shadowColor = rgba('#000000', 0.65); ctx.shadowBlur = 10;
    const sg = ctx.createLinearGradient(cx - 25, 0, cx + 6, 0);
    sg.addColorStop(0, rgba(shade(steel, 0.16), 1));
    sg.addColorStop(0.65, rgba(steel, 1));
    sg.addColorStop(1, rgba(steelDeep, 1));
    ctx.fillStyle = sg; ctx.fill(sh);
    ctx.restore();
    rim(sh, 0.6);
    ctx.save();
    ctx.clip(sh);
    for (let i = 0; i < 5; i++) {
      ctx.beginPath();
      ctx.moveTo(cx - 24 + i * 7, base - 76);
      ctx.lineTo(cx - 22 + i * 7, base - 8);
      ctx.strokeStyle = rgba('#000000', R2(R, 0.1, 0.24)); ctx.lineWidth = R2(R, 0.8, 1.8);
      ctx.stroke();
    }
    ctx.restore();
    ctx.beginPath(); ctx.ellipse(cx - 10, base - 45, 7, 9, 0, 0, Math.PI * 2);
    ctx.fillStyle = rgba(shade(steel, 0.24), 1); ctx.fill();
    ctx.strokeStyle = rgba(ALLY_RIM, 0.45); ctx.lineWidth = 1.2; ctx.stroke();
    return cv;
  }

  if (role === 'bruiser') {
    legs(16, 24);
    // Hache : haft en diagonale, fer posé au sol — élargit la silhouette vers le bas-droit.
    ctx.beginPath();
    ctx.moveTo(cx + 8, base - 62); ctx.lineTo(cx + 27, base - 2);
    ctx.strokeStyle = rgba('#3a2a1c', 0.95); ctx.lineWidth = 4; ctx.lineCap = 'round'; ctx.stroke();
    ctx.save();
    ctx.shadowColor = rgba('#000000', 0.5); ctx.shadowBlur = 6;
    const axe = new Path2D();
    axe.moveTo(cx + 10, base - 58);
    axe.quadraticCurveTo(cx + 30, base - 66, cx + 32, base - 40);
    axe.quadraticCurveTo(cx + 22, base - 44, cx + 13, base - 50);
    axe.closePath();
    ctx.fillStyle = rgba(steel, 1); ctx.fill(axe);
    ctx.restore();
    rim(axe, 0.5);
    ctx.save();
    ctx.shadowColor = rgba('#000000', 0.62); ctx.shadowBlur = 10;
    // Torse : très large aux épaules, resserré à la taille. L'inverse exact de la robe du PNJ.
    paintMass(ctx, [P(cx - 12, base - 22), P(cx - 25, base - 58), P(cx - 18, base - 68),
      P(cx + 18, base - 68), P(cx + 25, base - 58), P(cx + 12, base - 22)], th, R,
    { base: cloth, deep: clothDeep, rim: 0.16 });
    ctx.restore();
    for (const sx of [-1, 1]) {
      const pl = new Path2D();
      pl.moveTo(cx + sx * 15, base - 66);
      pl.quadraticCurveTo(cx + sx * 30, base - 64, cx + sx * 27, base - 50);
      pl.quadraticCurveTo(cx + sx * 19, base - 54, cx + sx * 14, base - 58);
      pl.closePath();
      ctx.fillStyle = rgba(steel, 1); ctx.fill(pl); rim(pl, 0.45);
    }
    // Tête enfoncée entre les épaules : elle dépasse à peine, c'est ce qui fait la masse.
    ctx.beginPath(); ctx.ellipse(cx, base - 74, 9, 8.5, 0, 0, Math.PI * 2);
    ctx.fillStyle = rgba(shade(skin, -0.25), 1); ctx.fill();
    ctx.strokeStyle = rgba(ALLY_RIM, 0.4); ctx.lineWidth = 1.1; ctx.stroke();
    ctx.fillStyle = rgba('#0a0710', 0.8);
    ctx.fillRect(cx - 7, base - 77, 14, 2.6);
    return cv;
  }

  if (role === 'skirmisher') {
    // Penché vers l'avant : tout le corps est en biais, aucune verticale.
    ctx.save();
    ctx.shadowColor = rgba('#000000', 0.5); ctx.shadowBlur = 7;
    paintMass(ctx, [P(cx - 12, base), P(cx - 7, base - 24), P(cx - 1, base - 24), P(cx - 4, base)], th, R, { base: clothDeep, rim: 0.1 });
    paintMass(ctx, [P(cx + 6, base), P(cx + 3, base - 24), P(cx + 10, base - 24), P(cx + 15, base)], th, R, { base: clothDeep, rim: 0.1 });
    ctx.restore();
    // Cape courte fendue, qui traîne derrière — donne le sens de la course.
    const cape = new Path2D();
    cape.moveTo(cx + 2, base - 58);
    cape.quadraticCurveTo(cx + 22, base - 46, cx + 26, base - 14);
    cape.lineTo(cx + 17, base - 20);
    cape.lineTo(cx + 14, base - 8);
    cape.lineTo(cx + 8, base - 26);
    cape.closePath();
    ctx.fillStyle = rgba(mix(cloth, th.accent, 0.28), 0.92); ctx.fill(cape); rim(cape, 0.35);
    ctx.save();
    ctx.shadowColor = rgba('#000000', 0.55); ctx.shadowBlur = 8;
    paintMass(ctx, [P(cx - 9, base - 22), P(cx - 14, base - 48), P(cx - 6, base - 60),
      P(cx + 7, base - 58), P(cx + 8, base - 44), P(cx + 6, base - 22)], th, R,
    { base: cloth, deep: clothDeep, rim: 0.15 });
    ctx.restore();
    // Deux lames courtes, tenues basses, en opposition — jamais un bâton.
    for (const [x0, y0, x1, y1] of [[-13, -40, -25, -18], [9, -44, 21, -30]]) {
      ctx.beginPath();
      ctx.moveTo(cx + x0, base + y0); ctx.lineTo(cx + x1, base + y1);
      ctx.strokeStyle = rgba(shade(steel, 0.2), 0.95); ctx.lineWidth = 2.6; ctx.lineCap = 'round';
      ctx.stroke();
      ctx.strokeStyle = rgba(ALLY_RIM, 0.45); ctx.lineWidth = 1; ctx.stroke();
    }
    // Tête baissée, masque bas sur le visage, une seule fente d'yeux.
    ctx.beginPath(); ctx.ellipse(cx - 3, base - 66, 7.5, 8, -0.2, 0, Math.PI * 2);
    ctx.fillStyle = rgba(clothDeep, 1); ctx.fill();
    ctx.strokeStyle = rgba(ALLY_RIM, 0.45); ctx.lineWidth = 1.1; ctx.stroke();
    ctx.fillStyle = rgba(ALLY_RIM, 0.6);
    ctx.fillRect(cx - 9, base - 68, 12, 1.8);
    return cv;
  }

  // mystic — fuseau, tête nue, focus détaché du corps.
  ctx.save();
  ctx.shadowColor = rgba('#000000', 0.55); ctx.shadowBlur = 9;
  const robe = new Path2D();
  robe.moveTo(cx - 17, base - 1);
  robe.quadraticCurveTo(cx - 12, base - 36, cx - 8, base - 58);
  robe.quadraticCurveTo(cx, base - 66, cx + 8, base - 58);
  robe.quadraticCurveTo(cx + 12, base - 36, cx + 17, base - 1);
  robe.closePath();
  const rg = ctx.createLinearGradient(cx - 17, 0, cx + 17, 0);
  rg.addColorStop(0, rgba(shade(cloth, 0.14), 1));
  rg.addColorStop(0.66, rgba(cloth, 1));
  rg.addColorStop(1, rgba(clothDeep, 1));
  ctx.fillStyle = rg; ctx.fill(robe);
  ctx.restore();
  rim(robe, 0.4);
  ctx.save();
  ctx.clip(robe);
  for (let i = 0; i < 6; i++) {
    const x = cx - 13 + i * 5;
    ctx.beginPath();
    ctx.moveTo(x, base - 2);
    ctx.quadraticCurveTo(x + R2(R, -3, 3), base - 32, x + R2(R, -2, 3), base - 56);
    ctx.strokeStyle = rgba(R() > 0.5 ? '#000000' : ALLY_RIM, R() > 0.5 ? 0.18 : 0.1);
    ctx.lineWidth = R2(R, 0.8, 1.7); ctx.stroke();
  }
  ctx.restore();
  // Épaulière asymétrique : casse la symétrie de la robe, éloigne encore du PNJ.
  const pau = new Path2D();
  pau.moveTo(cx - 8, base - 58);
  pau.quadraticCurveTo(cx - 22, base - 56, cx - 18, base - 44);
  pau.quadraticCurveTo(cx - 12, base - 50, cx - 7, base - 52);
  pau.closePath();
  ctx.fillStyle = rgba(steel, 0.95); ctx.fill(pau); rim(pau, 0.45);
  // Tête NUE — c'est le point de séparation d'avec le PNJ encapuchonné.
  ctx.beginPath(); ctx.ellipse(cx + 1, base - 68, 6.6, 7.6, 0, 0, Math.PI * 2);
  ctx.fillStyle = rgba(skin, 1); ctx.fill();
  ctx.strokeStyle = rgba(ALLY_RIM, 0.4); ctx.lineWidth = 1; ctx.stroke();
  ctx.beginPath();
  ctx.moveTo(cx - 6, base - 73);
  ctx.quadraticCurveTo(cx + 2, base - 79, cx + 8, base - 70);
  ctx.quadraticCurveTo(cx + 2, base - 74, cx - 6, base - 73);
  ctx.fillStyle = rgba(shade(cloth, -0.3), 1); ctx.fill();
  // Focus en lévitation au-dessus de la paume ouverte : le seul élément détaché du corps.
  const fx = cx + 19, fy = base - 62;
  const fg = ctx.createRadialGradient(fx, fy, 0.5, fx, fy, 13);
  fg.addColorStop(0, rgba('#ffffff', 0.9));
  fg.addColorStop(0.35, rgba(th.glow, 0.6));
  fg.addColorStop(1, rgba(th.glow, 0));
  ctx.fillStyle = fg;
  ctx.beginPath(); ctx.arc(fx, fy, 13, 0, Math.PI * 2); ctx.fill();
  ctx.fillStyle = rgba('#fff8e8', 0.95);
  ctx.beginPath(); ctx.arc(fx, fy, 2.6, 0, Math.PI * 2); ctx.fill();
  ctx.beginPath();
  ctx.moveTo(cx + 9, base - 50);
  ctx.quadraticCurveTo(cx + 18, base - 54, fx - 1, fy + 7);
  ctx.strokeStyle = rgba(cloth, 1); ctx.lineWidth = 4.5; ctx.lineCap = 'round'; ctx.stroke();
  ctx.strokeStyle = rgba(ALLY_RIM, 0.3); ctx.lineWidth = 1.2; ctx.stroke();
  return cv;
}

/** Trousse de peinture partagée : `bestiaire.js` peint ses combattants avec exactement les
 *  mêmes outils que les tuiles et les décors, sinon les jetons se détachent du plateau. */
export const PAINT = {
  rgba, mix, shade, paintMass, makeCanvas, centerY, corners, diamondPath,
  hashSeed, makeRng, R2, theme,
  TILE, SPRITE_W, SPRITE_H, PROP_SPRITE_H, PROP_EXTRA_H,
  GROUND_ANCHOR_RATIO, PROP_GROUND_ANCHOR_RATIO,
};

export function spriteKeyToString(k) {
  switch (k.kind) {
    case 'floor':
      return [
        'f', k.theme, k.elevation, k.surfaceSeed ?? 0, k.hidden ?? 'n', k.danger ?? 'n',
        k.cliffLeft ? 'L' : '-', k.cliffRight ? 'R' : '-', k.resolved ? 'r' : '-', k.glow ? 'g' : '-',
      ].join(':');
    case 'obstacle': return `o:${k.theme}:${k.variant ?? 0}:${k.elevation ?? 0}`;
    case 'prop': return `p:${k.theme}:${k.prop}`;
    case 'highlight': return `h:${k.variant}:${k.elevation}`;
    case 'party': return `y:${k.elevation}`;
    case 'unit': return `u:${k.theme}:${k.role}`;
    default: return 'unknown';
  }
}

export function createTileForge(options = {}) {
  const grain = options.grain ?? 1;
  const cache = new Map();
  function getSprite(key) {
    const id = spriteKeyToString(key);
    const hit = cache.get(id);
    if (hit) return hit;
    let cv;
    switch (key.kind) {
      case 'obstacle': cv = bakeObstacle(key.theme, key.variant ?? 0, grain, key.elevation ?? 0); break;
      case 'prop': cv = bakeProp(key.theme, key.prop, grain); break;
      case 'highlight': cv = bakeHighlight(key.variant, key.elevation ?? 0); break;
      case 'party': cv = bakeParty(key.elevation ?? 0); break;
      case 'unit': cv = bakeUnit(key.theme, key.role, grain); break;
      default: cv = bakeFloor(key, grain);
    }
    cache.set(id, cv);
    return cv;
  }
  return {
    getSprite,
    clear: () => cache.clear(),
    spriteAspectRatio: SPRITE_W / SPRITE_H,
    groundAnchorRatio: GROUND_ANCHOR_RATIO,
    /** Pour les kinds 'prop' ET 'obstacle' : toile haute, ancre au sol décalée. */
    propAspectRatio: SPRITE_W / PROP_SPRITE_H,
    propGroundAnchorRatio: PROP_GROUND_ANCHOR_RATIO,
  };
}

/** Nombre de variantes d'obstacle disponibles pour un thème. */
export function obstacleVariantCount(name) {
  const th = theme(name);
  return (th.walls ?? [th.wall]).length;
}

/** Tous les décors disponibles, dans l'ordre : décor de salle puis décor d'événement. */
export const PROP_KINDS = [
  'beam', 'arch', 'trunk', 'spire', 'obeliskProp', 'column', 'cairn',
  'npc', 'merchant', 'campfire', 'star', 'curse', 'monster', 'elite', 'boss',
];

/** Décor à poser sur un nœud, par type de nœud. `null` = pas de décor (case nue). */
export const NODE_PROP = {
  presence: 'npc',
  merchant: 'merchant',
  rest: 'campfire',
  item: 'star',
  memory: 'star',
  law: 'star',
  curse: 'curse',
  ambush: 'monster',
  elite: 'elite',
  rare: 'elite',
  boss: 'boss',
  finalConfrontation: 'boss',
};

/** Faces latérales à peindre en falaise : la case devant-gauche est (x+1,y), devant-droite (x,y+1). */
export function cliffSides(x, y, isFloor) {
  return { cliffLeft: !isFloor(x + 1, y), cliffRight: !isFloor(x, y + 1) };
}

// ── Fond de salle ────────────────────────────────────────────────────────────────────
export function drawBackdrop(ctx, w, h, name, t = 0, seedStr = 'room', o = {}) {
  const th = theme(name);
  const R = makeRng(hashSeed('bg:' + name + ':' + seedStr));
  const g = ctx.createLinearGradient(0, 0, 0, h);
  g.addColorStop(0, th.sky[0]);
  g.addColorStop(0.55, th.sky[1]);
  g.addColorStop(1, th.sky[0]);
  ctx.fillStyle = g;
  ctx.fillRect(0, 0, w, h);

  // Source de lumière du thème, haut-gauche, respirant lentement.
  const pulse = 0.78 + Math.sin(t * 0.0009) * 0.22;
  const lg = ctx.createRadialGradient(w * 0.32, h * 0.06, 10, w * 0.32, h * 0.06, h * 1.15);
  lg.addColorStop(0, rgba(th.sky[2], 0.85 * pulse));
  lg.addColorStop(0.45, rgba(th.sky[2], 0.24 * pulse));
  lg.addColorStop(1, rgba(th.sky[0], 0));
  ctx.fillStyle = lg;
  ctx.fillRect(0, 0, w, h);

  if (o.scenery !== false) drawScenery(ctx, w, h, th, R, t);

  // Bandes de brume horizontales : donne de la profondeur sans dessiner de décor figuratif.
  for (let i = 0; i < 5; i++) {
    const y = h * (0.24 + i * 0.14) + Math.sin(t * 0.0004 + i) * 6;
    const bg = ctx.createLinearGradient(0, y - 40, 0, y + 40);
    bg.addColorStop(0, rgba(th.sky[2], 0));
    bg.addColorStop(0.5, rgba(th.sky[2], 0.07 + R() * 0.05));
    bg.addColorStop(1, rgba(th.sky[2], 0));
    ctx.fillStyle = bg;
    ctx.fillRect(0, y - 40, w, 80);
  }
  const vg = ctx.createRadialGradient(w / 2, h * 0.5, Math.min(w, h) * 0.25, w / 2, h * 0.5, Math.max(w, h) * 0.75);
  vg.addColorStop(0, rgba('#000000', 0));
  vg.addColorStop(1, rgba('#000000', 0.72));
  ctx.fillStyle = vg;
  ctx.fillRect(0, 0, w, h);
}

/**
 * Décor de fond : silhouettes du motif de la salle, en deux plans de profondeur, très
 * désaturs et à peine plus clairs que le ciel — il doit se lire comme un lointain, jamais
 * entrer en concurrence avec les tuiles.
 */
function drawScenery(ctx, w, h, th, R, t) {
  const horizon = h * 0.52;
  const layer = (depth) => {
    // depth 0 = lointain (presque le ciel), 1 = plan intermédiaire.
    const col = mix(th.sky[1], th.sky[2], depth * 0.55 + 0.1);
    return rgba(col, 0.42 + depth * 0.3);
  };
  const breathe = 0.9 + 0.1 * Math.sin(t * 0.0006);

  const arches = (depth, count, hgt, span) => {
    ctx.fillStyle = layer(depth);
    for (let i = 0; i < count; i++) {
      const x = ((i + 0.5) / count) * w + R2(R, -18, 18);
      const bw = span * R2(R, 0.8, 1.2);
      const bh = hgt * R2(R, 0.82, 1.18);
      ctx.beginPath();
      ctx.moveTo(x - bw / 2, horizon);
      ctx.lineTo(x - bw / 2, horizon - bh * 0.55);
      ctx.quadraticCurveTo(x, horizon - bh * 1.25, x + bw / 2, horizon - bh * 0.55);
      ctx.lineTo(x + bw / 2, horizon);
      ctx.closePath();
      ctx.fill();
    }
  };
  const pillars = (depth, count, hgt, wdt) => {
    ctx.fillStyle = layer(depth);
    for (let i = 0; i < count; i++) {
      const x = ((i + 0.5) / count) * w + R2(R, -22, 22);
      const bh = hgt * R2(R, 0.75, 1.25);
      const bw = wdt * R2(R, 0.7, 1.3);
      ctx.fillRect(x - bw / 2, horizon - bh, bw, bh);
      ctx.beginPath();
      ctx.ellipse(x, horizon - bh, bw * 0.9, bw * 0.45, 0, 0, Math.PI * 2);
      ctx.fill();
    }
  };
  const treeLine = (depth, count, hgt) => {
    ctx.fillStyle = layer(depth);
    for (let i = 0; i < count; i++) {
      const x = ((i + 0.5) / count) * w + R2(R, -26, 26);
      const bh = hgt * R2(R, 0.6, 1.35);
      ctx.fillRect(x - 4 * (1 + depth), horizon - bh, 8 * (1 + depth), bh);
      for (let k = 0; k < 5; k++) {
        ctx.beginPath();
        ctx.ellipse(x + R2(R, -26, 26), horizon - bh - R2(R, -14, 34),
          R2(R, 16, 34), R2(R, 9, 18), R2(R, -0.3, 0.3), 0, Math.PI * 2);
        ctx.fill();
      }
    }
  };
  const shards = (depth, count, hgt) => {
    ctx.fillStyle = layer(depth);
    for (let i = 0; i < count; i++) {
      const x = ((i + 0.5) / count) * w + R2(R, -30, 30);
      const bh = hgt * R2(R, 0.5, 1.4);
      ctx.beginPath();
      ctx.moveTo(x - R2(R, 14, 34), horizon);
      ctx.lineTo(x + R2(R, -12, 12), horizon - bh);
      ctx.lineTo(x + R2(R, 12, 32), horizon);
      ctx.closePath();
      ctx.fill();
    }
  };

  ctx.save();
  ctx.globalAlpha = breathe;
  switch (th.surface) {
    case 'plank': // Seuil : enfilade de portails, de plus en plus proches.
      arches(0, 5, h * 0.3, w * 0.11);
      arches(0.7, 3, h * 0.42, w * 0.17);
      break;
    case 'parchment': // Mémoire : rayonnages en enfilade et feuillets en suspension.
      pillars(0, 9, h * 0.26, w * 0.05);
      pillars(0.65, 5, h * 0.36, w * 0.08);
      ctx.fillStyle = rgba(th.glow, 0.10);
      for (let i = 0; i < 16; i++) {
        const px = R() * w, py = R2(R, h * 0.1, horizon);
        ctx.save();
        ctx.translate(px + Math.sin(t * 0.0004 + i) * 10, py);
        ctx.rotate(R2(R, -0.7, 0.7));
        ctx.fillRect(-7, -4, 14, 9);
        ctx.restore();
      }
      break;
    case 'moss': // Forêt : deux rideaux d'arbres.
      treeLine(0, 7, h * 0.32);
      treeLine(0.8, 4, h * 0.46);
      break;
    case 'fracture':
    case 'pulse': { // Rupture / Final : arêtes brisées + fracture incandescente dans le ciel.
      shards(0, 6, h * 0.3);
      shards(0.75, 4, h * 0.46);
      const pulse = 0.35 + 0.65 * Math.abs(Math.sin(t * (th.surface === 'pulse' ? 0.0016 : 0.0008)));
      ctx.save();
      ctx.strokeStyle = rgba(th.accent, 0.16 + 0.22 * pulse);
      ctx.lineWidth = 2.4;
      ctx.shadowColor = rgba(th.accent, 0.7);
      ctx.shadowBlur = 18 * pulse;
      ctx.beginPath();
      let px = w * 0.12, py = h * 0.06;
      ctx.moveTo(px, py);
      for (let i = 0; i < 7; i++) {
        px += w * 0.12 * R2(R, 0.6, 1.3);
        py += h * 0.05 * (i % 2 ? 1 : -0.6);
        ctx.lineTo(px, py);
      }
      ctx.stroke();
      ctx.restore();
      break;
    }
    case 'ripple': { // Silence : eau stagnante, horizon net, ondes très lentes.
      ctx.fillStyle = rgba(mix(th.sky[1], '#000000', 0.4), 0.5);
      ctx.fillRect(0, horizon, w, h - horizon);
      ctx.strokeStyle = rgba(th.glow, 0.12);
      ctx.lineWidth = 1;
      ctx.beginPath(); ctx.moveTo(0, horizon); ctx.lineTo(w, horizon); ctx.stroke();
      for (let i = 0; i < 7; i++) {
        const rr = ((t * 0.004 + i * 90) % 640);
        ctx.beginPath();
        ctx.ellipse(w * 0.5, horizon + h * 0.16, rr, rr * 0.13, 0, 0, Math.PI * 2);
        ctx.strokeStyle = rgba(th.glow, 0.07 * (1 - rr / 640));
        ctx.lineWidth = 1.4;
        ctx.stroke();
      }
      pillars(0.4, 3, h * 0.2, w * 0.04);
      break;
    }
    case 'marble': // Antichambre : colonnade cérémonielle et arcs en enfilade.
      pillars(0, 11, h * 0.3, w * 0.035);
      arches(0.75, 4, h * 0.44, w * 0.15);
      break;
    case 'carpet': // Hall / Couloirs : enfilade de portes, colonnade serrée derrière.
      pillars(0, 9, h * 0.26, w * 0.04);
      arches(0.75, 4, h * 0.4, w * 0.13);
      break;
    case 'clinic': // Hôpital : travées régulières, alignement clinique, rien qui dépasse.
      pillars(0, 13, h * 0.22, w * 0.025);
      pillars(0.7, 6, h * 0.3, w * 0.05);
      break;
    case 'crystal': // Caverne : aiguilles de crystal en deux plans.
      shards(0, 8, h * 0.34);
      shards(0.8, 5, h * 0.5);
      break;
    default:
      pillars(0, 7, h * 0.28, w * 0.05);
  }
  ctx.restore();
}

const AMBIENT = {
  // Peu de particules, très lentes, dérive latérale large : ça flotte, ça ne pleut pas.
  mote: { n: 20, rise: -0.0034, size: [0.8, 2.2], alpha: [0.12, 0.42], drift: 22 },
  flake: { n: 15, rise: 0.0042, size: [1.0, 2.6], alpha: [0.10, 0.34], drift: 30 },
  spore: { n: 18, rise: -0.0022, size: [0.9, 2.3], alpha: [0.09, 0.3], drift: 26 },
  ember: { n: 17, rise: -0.0062, size: [0.9, 2.4], alpha: [0.2, 0.6], drift: 20 },
  emberDark: { n: 14, rise: -0.0050, size: [1.0, 2.8], alpha: [0.16, 0.52], drift: 16 },
  dust: { n: 12, rise: -0.0012, size: [0.7, 1.8], alpha: [0.07, 0.24], drift: 12 },
  gilt: { n: 16, rise: -0.0026, size: [0.8, 2.1], alpha: [0.11, 0.36], drift: 24 },
  ash: { n: 20, rise: 0.0030, size: [0.9, 2.4], alpha: [0.10, 0.30], drift: 34 },
  snow: { n: 22, rise: 0.0052, size: [1.0, 2.4], alpha: [0.16, 0.5], drift: 40 },
  petal: { n: 13, rise: 0.0026, size: [1.4, 3.2], alpha: [0.14, 0.4], drift: 46 },
  plasma: { n: 18, rise: -0.0072, size: [1.1, 3.0], alpha: [0.22, 0.62], drift: 18 },
};
/** Particules d'ambiance, sans état : tout est fonction de (index, temps). */
export function drawAmbient(ctx, w, h, name, t) {
  const th = theme(name);
  const spec = AMBIENT[th.particle] ?? AMBIENT.mote;
  const R = makeRng(hashSeed('amb:' + name));
  ctx.save();
  ctx.globalCompositeOperation = 'lighter';
  for (let i = 0; i < spec.n; i++) {
    const bx = R() * w, phase = R() * 1000, sp = R2(R, 0.6, 1.6);
    const size = R2(R, spec.size[0], spec.size[1]);
    const aBase = R2(R, spec.alpha[0], spec.alpha[1]);
    const prog = ((t * spec.rise * sp * 0.06) + phase) % 1;
    const y = ((prog + 1) % 1) * h;
    const x = bx + Math.sin(t * 0.0006 * sp + phase) * spec.drift;
    const a = aBase * (0.4 + 0.6 * Math.sin(((y / h) * Math.PI)));
    // Halo très doux plutôt qu'un point net : c'est ce qui donne la sensation de flottement.
    const halo = ctx.createRadialGradient(x, y, 0, x, y, size * 3.2);
    halo.addColorStop(0, rgba(th.glow, a));
    halo.addColorStop(0.4, rgba(th.glow, a * 0.35));
    halo.addColorStop(1, rgba(th.glow, 0));
    ctx.fillStyle = halo;
    ctx.beginPath();
    ctx.arc(x, y, size * 3.2, 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.restore();
}

/** Éclat de révélation d'un nœud caché — composité par-dessus la tuile, t ∈ [0,1]. */
export function drawRevealFx(ctx, dx, dy, dw, dh, t, name, elev = 0) {
  const th = theme(name);
  const cx = dx + dw / 2;
  const cy = dy + dh * anchorRatioAt(elev);
  const e = 1 - Math.pow(1 - Math.min(1, t), 3);
  ctx.save();
  ctx.globalCompositeOperation = 'lighter';
  const rw = dw * 0.5 * (0.2 + e * 1.5);
  ctx.strokeStyle = rgba(th.glow, 0.7 * (1 - e));
  ctx.lineWidth = 3 * (1 - e) + 0.5;
  ctx.beginPath();
  ctx.ellipse(cx, cy, rw, rw * 0.5, 0, 0, Math.PI * 2);
  ctx.stroke();
  const g = ctx.createRadialGradient(cx, cy, 1, cx, cy, dw * 0.5);
  g.addColorStop(0, rgba(th.glow, 0.5 * (1 - e)));
  g.addColorStop(1, rgba(th.glow, 0));
  ctx.fillStyle = g;
  ctx.fillRect(cx - dw / 2, cy - dh / 2, dw, dh);
  const R = makeRng(hashSeed('fx'));
  for (let i = 0; i < 14; i++) {
    const ang = R() * Math.PI * 2, d = e * dw * 0.55 * R2(R, 0.4, 1);
    ctx.fillStyle = rgba(th.glow, (1 - e) * R2(R, 0.4, 0.9));
    ctx.beginPath();
    ctx.arc(cx + Math.cos(ang) * d, cy + Math.sin(ang) * d * 0.5 - e * 10, R2(R, 0.8, 2), 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.restore();
}

/** Ancre au sol pour une tuile à l'élévation donnée : la face du dessus monte dans la toile
 * quand la tuile s'élève, donc tout overlay doit être remonté d'autant. */
export function anchorRatioAt(elev = 0) {
  return (GROUND_ANCHOR_Y - Math.max(0, Math.min(TILE.MAX, elev)) * TILE.STEP) / SPRITE_H;
}

/** Aura de danger : pulsation rouge sur la face du dessus de la case. `t` en ms. */
export function drawDangerAura(ctx, dx, dy, dw, dh, t, elev = 0) {
  const cx = dx + dw / 2;
  const cy = dy + dh * anchorRatioAt(elev);
  const rw = dw * 0.5, rh = rw * 0.5;
  // Deux fréquences légèrement désaccordées : le clignotement ne paraît pas mécanique.
  const p = 0.5 + 0.5 * Math.sin(t * 0.0042);
  const p2 = 0.5 + 0.5 * Math.sin(t * 0.0028 + 1.1);
  const pulse = 0.35 + 0.65 * (p * 0.7 + p2 * 0.3);
  ctx.save();
  ctx.globalCompositeOperation = 'lighter';
  const g = ctx.createRadialGradient(cx, cy, 1, cx, cy, rw * 1.15);
  g.addColorStop(0, rgba('#ff5544', 0.30 * pulse));
  g.addColorStop(0.45, rgba('#e03024', 0.18 * pulse));
  g.addColorStop(1, rgba('#c01e18', 0));
  ctx.fillStyle = g;
  ctx.beginPath();
  ctx.ellipse(cx, cy, rw * 1.15, rh * 1.15, 0, 0, Math.PI * 2);
  ctx.fill();
  // Liseré sur le losange lui-même : la case, pas une tâche ronde posée dessus.
  ctx.beginPath();
  ctx.moveTo(cx, cy - rh * 0.94);
  ctx.lineTo(cx + rw * 0.94, cy);
  ctx.lineTo(cx, cy + rh * 0.94);
  ctx.lineTo(cx - rw * 0.94, cy);
  ctx.closePath();
  ctx.strokeStyle = rgba('#ff6a54', 0.35 + 0.5 * pulse);
  ctx.lineWidth = 1.4 + pulse * 1.6;
  ctx.shadowColor = rgba('#ff3b28', 0.9);
  ctx.shadowBlur = 6 + pulse * 14;
  ctx.stroke();
  ctx.restore();
}

/** Flamme du feu de camp — à composer sur le sprite `campfire`, avec le rect "prop". */
export function drawFireFx(ctx, dx, dy, dw, dh, t, anchorRatio = PROP_GROUND_ANCHOR_RATIO) {
  const s = dw / SPRITE_W;
  const cx = dx + dw / 2;
  const cy = dy + dh * anchorRatio - 2 * s;
  ctx.save();
  ctx.globalCompositeOperation = 'lighter';
  const flick = 0.82 + 0.18 * Math.sin(t * 0.011) + 0.08 * Math.sin(t * 0.027);
  const glow = ctx.createRadialGradient(cx, cy - 10 * s, 2, cx, cy - 10 * s, 62 * s * flick);
  glow.addColorStop(0, rgba('#ffcf8a', 0.5));
  glow.addColorStop(0.4, rgba('#ff7a2a', 0.18));
  glow.addColorStop(1, rgba('#ff5a10', 0));
  ctx.fillStyle = glow;
  ctx.fillRect(cx - 70 * s, cy - 80 * s, 140 * s, 110 * s);
  for (let i = 0; i < 3; i++) {
    const ph = t * 0.006 + i * 2.1;
    const hgt = (30 + i * 9) * s * (0.8 + 0.2 * Math.sin(ph * 1.7));
    const wob = Math.sin(ph) * 4 * s;
    ctx.beginPath();
    ctx.moveTo(cx - (9 - i * 2) * s, cy);
    ctx.quadraticCurveTo(cx - 8 * s + wob, cy - hgt * 0.55, cx + wob * 0.6, cy - hgt);
    ctx.quadraticCurveTo(cx + 9 * s + wob, cy - hgt * 0.5, cx + (9 - i * 2) * s, cy);
    ctx.closePath();
    ctx.fillStyle = rgba(i === 0 ? '#ff6a18' : i === 1 ? '#ffa63c' : '#ffe6a8', 0.42 + i * 0.16);
    ctx.fill();
  }
  const R = makeRng(hashSeed('fire'));
  for (let i = 0; i < 9; i++) {
    const ph = (t * 0.0009 * R2(R, 0.6, 1.5) + R()) % 1;
    const ex = cx + Math.sin(t * 0.002 + i) * 10 * s + R2(R, -8, 8) * s;
    const ey = cy - ph * 62 * s;
    ctx.fillStyle = rgba(i % 2 ? '#ffb057' : '#ffe0a0', (1 - ph) * 0.75);
    ctx.beginPath();
    ctx.arc(ex, ey, R2(R, 0.7, 1.9) * s, 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.restore();
}

/** Scintillement de l'objet à ramasser — à composer sur le sprite `star`, rect "prop". */
export function drawStarFx(ctx, dx, dy, dw, dh, t, anchorRatio = PROP_GROUND_ANCHOR_RATIO) {
  const s = dw / SPRITE_W;
  const cx = dx + dw / 2;
  const cy = dy + dh * anchorRatio - (34 + Math.sin(t * 0.0016) * 4) * s;
  ctx.save();
  ctx.globalCompositeOperation = 'lighter';
  const beat = 0.55 + 0.45 * Math.sin(t * 0.0035);
  const g = ctx.createRadialGradient(cx, cy, 1, cx, cy, 34 * s);
  g.addColorStop(0, rgba('#fff6dc', 0.5 * beat));
  g.addColorStop(1, rgba('#ffd98a', 0));
  ctx.fillStyle = g;
  ctx.fillRect(cx - 36 * s, cy - 36 * s, 72 * s, 72 * s);
  const spike = (18 + beat * 12) * s;
  ctx.strokeStyle = rgba('#fff8e6', 0.5 + 0.4 * beat);
  ctx.lineWidth = 1.2;
  for (const [ax, ay] of [[1, 0], [0, 1]]) {
    ctx.beginPath();
    ctx.moveTo(cx - ax * spike, cy - ay * spike * 0.8);
    ctx.lineTo(cx + ax * spike, cy + ay * spike * 0.8);
    ctx.stroke();
  }
  const R = makeRng(hashSeed('star'));
  for (let i = 0; i < 6; i++) {
    const ph = (t * 0.0007 + R()) % 1;
    const a = R() * Math.PI * 2;
    ctx.fillStyle = rgba('#fff2cf', (1 - ph) * 0.8);
    ctx.beginPath();
    ctx.arc(cx + Math.cos(a) * ph * 26 * s, cy + Math.sin(a) * ph * 20 * s - ph * 8 * s, R2(R, 0.6, 1.6) * s, 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.restore();
}

/**
 * Brouillard de guerre : voile sombre sur tout le plateau, perçé autour des points donnés.
 * `centers` = positions écran des cases vues (typiquement l'allié et ses voisines), `radius`
 * = rayon du trou en pixels écran (≈ 2 cases). Le voile est peint dans un canvas tampon puis
 * perçé en `destination-out`, pour que les trous se fondent entre eux au lieu de se cumuler.
 */
let fogBuf = null;
export function drawFogOfWar(ctx, w, h, centers, radius, name, t = 0) {
  const th = theme(name);
  if (!fogBuf || fogBuf.width !== w || fogBuf.height !== h) fogBuf = makeCanvas(w, h);
  const f = fogBuf.getContext('2d');
  if (!f) return;
  f.clearRect(0, 0, w, h);
  const veil = f.createLinearGradient(0, 0, 0, h);
  veil.addColorStop(0, rgba(mix(th.fog ?? th.sky[0], '#000000', 0.35), 0.94));
  veil.addColorStop(1, rgba('#04040a', 0.97));
  f.fillStyle = veil;
  f.fillRect(0, 0, w, h);
  // Volutes lentes : le voile n'est pas un aplat, il bouge à peine.
  const R = makeRng(hashSeed('fog:' + name));
  f.globalCompositeOperation = 'lighter';
  for (let i = 0; i < 14; i++) {
    const bx = R() * w, by = R() * h, rr = R2(R, 60, 190);
    const px = bx + Math.sin(t * 0.00013 + i) * 26;
    const py = by + Math.cos(t * 0.00009 + i * 1.7) * 14;
    const cg = f.createRadialGradient(px, py, 1, px, py, rr);
    cg.addColorStop(0, rgba(th.sky[2], 0.05));
    cg.addColorStop(1, rgba(th.sky[2], 0));
    f.fillStyle = cg;
    f.fillRect(px - rr, py - rr, rr * 2, rr * 2);
  }
  f.globalCompositeOperation = 'destination-out';
  const breathe = 1 + Math.sin(t * 0.0011) * 0.02;
  for (const c of centers) {
    const r = (c.radius ?? radius) * breathe;
    const hole = f.createRadialGradient(c.x, c.y, r * 0.34, c.x, c.y, r);
    hole.addColorStop(0, 'rgba(0,0,0,1)');
    hole.addColorStop(0.62, 'rgba(0,0,0,0.92)');
    hole.addColorStop(1, 'rgba(0,0,0,0)');
    f.fillStyle = hole;
    f.beginPath();
    // Ellipse 2:1 : le champ de vision suit la grille iso, pas un cercle écran.
    f.ellipse(c.x, c.y, r, r * 0.62, 0, 0, Math.PI * 2);
    f.fill();
  }
  f.globalCompositeOperation = 'source-over';
  ctx.drawImage(fogBuf, 0, 0);
}

/** Rayon de vision par défaut, en pixels écran, pour n cases autour d'une unité. */
export function visionRadius(cells, isoUnitX) {
  return isoUnitX * (cells + 0.55);
}

// ── Combat tactique : chrome de grille (100 % runtime, zéro variante de cache) ────────

/** Étalonnage de combat : le décor perd sa saturation pour que les unités et les
 *  surbrillances dominent. À appeler APRÈS les tuiles et les décors, AVANT les
 *  surbrillances et les unités — c'est l'ordre qui fait la bascule, pas l'opacité. */
export function drawCombatGrade(ctx, w, h, amount = 1, tint = '#0b0d1a') {
  if (amount <= 0) return;
  ctx.save();
  ctx.globalCompositeOperation = 'saturation';
  ctx.globalAlpha = Math.min(1, amount) * 0.55;
  ctx.fillStyle = '#808080';
  ctx.fillRect(0, 0, w, h);
  ctx.globalCompositeOperation = 'source-over';
  ctx.globalAlpha = Math.min(1, amount) * 0.18;
  ctx.fillStyle = tint;
  ctx.fillRect(0, 0, w, h);
  ctx.restore();
}

/** Paliers de risque : ils se voient sur le plateau, pas seulement dans une table.
 *  `grade` reste discret aux paliers calmes (le cas courant) et ne monte franchement
 *  qu'à l'approche du Fatal — sinon tout combat, même le plus anodin, se lit comme
 *  désaturé à l'écran, ce qui casse l'alignement visuel avec l'exploration. */
export const RISK_TIERS = {
  calm: { label: 'Calme', accent: '#7fb4a8', enemies: 2, fog: 0.5, ambient: 0.6, grade: 0.3 },
  tense: { label: 'Tendu', accent: '#c9a24a', enemies: 3, fog: 0.75, ambient: 0.9, grade: 0.45 },
  grim: { label: 'Sombre', accent: '#d2703c', enemies: 4, fog: 1, ambient: 1.25, grade: 0.65 },
  fatal: { label: 'Fatal', accent: '#e0344a', enemies: 5, fog: 1.35, ambient: 1.7, grade: 0.85 },
};
export const RISK_KEYS = Object.keys(RISK_TIERS);
export function riskTier(key) { return RISK_TIERS[key] ?? RISK_TIERS.tense; }

/** Anneau au sol d'une unité : camp, PV, tour actif, état à terre. Posé sur le rect « sol ». */
export function drawUnitRing(ctx, dx, dy, dw, dh, o = {}, t = 0) {
  const side = o.side === 'enemy' ? ENEMY_RIM : ALLY_RIM;
  const cx = dx + dw / 2;
  const cy = dy + dh * anchorRatioAt(o.elevation ?? 0);
  const rw = dw * 0.30, rh = rw * 0.5;
  ctx.save();
  ctx.fillStyle = rgba('#000000', 0.35);
  ctx.beginPath(); ctx.ellipse(cx, cy + 1, rw * 1.02, rh * 1.02, 0, 0, Math.PI * 2); ctx.fill();
  if (o.active) {
    // Halo respirant : l'unité qui a la main se repère sans lire la colonne d'initiative.
    const pulse = 0.5 + 0.5 * Math.sin(t * 0.004);
    const g = ctx.createRadialGradient(cx, cy, rw * 0.2, cx, cy, rw * (1.7 + pulse * 0.4));
    g.addColorStop(0, rgba(side, 0.32));
    g.addColorStop(1, rgba(side, 0));
    ctx.fillStyle = g;
    ctx.beginPath(); ctx.ellipse(cx, cy, rw * 2.1, rh * 2.1, 0, 0, Math.PI * 2); ctx.fill();
    ctx.strokeStyle = rgba('#fff3d4', 0.5 + pulse * 0.4);
    ctx.lineWidth = 1.6;
    ctx.setLineDash([7, 5]);
    ctx.lineDashOffset = -t * 0.02;
    ctx.beginPath(); ctx.ellipse(cx, cy, rw * 1.32, rh * 1.32, 0, 0, Math.PI * 2); ctx.stroke();
    ctx.setLineDash([]);
  }
  ctx.strokeStyle = rgba('#05070e', 0.7); ctx.lineWidth = 3.4;
  ctx.beginPath(); ctx.ellipse(cx, cy, rw, rh, 0, 0, Math.PI * 2); ctx.stroke();
  const hp = Math.max(0, Math.min(1, o.hp ?? 1));
  ctx.strokeStyle = rgba(side, 0.22); ctx.lineWidth = 2.6;
  ctx.beginPath(); ctx.ellipse(cx, cy, rw, rh, 0, 0, Math.PI * 2); ctx.stroke();
  if (hp > 0) {
    // L'arc de PV court sur l'anneau : la santé du plateau se lit sans survol.
    const col = hp > 0.5 ? side : (hp > 0.25 ? '#e8b04a' : '#ff5a3c');
    ctx.strokeStyle = rgba(col, 0.95); ctx.lineWidth = 2.8; ctx.lineCap = 'round';
    ctx.shadowColor = rgba(col, 0.8); ctx.shadowBlur = 6;
    ctx.beginPath();
    ctx.ellipse(cx, cy, rw, rh, 0, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * hp);
    ctx.stroke();
    ctx.shadowBlur = 0;
  }
  if (o.downed) {
    ctx.strokeStyle = rgba('#8b8398', 0.8); ctx.lineWidth = 2.4; ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(cx - rw * 0.6, cy - rh * 0.6); ctx.lineTo(cx + rw * 0.6, cy + rh * 0.6);
    ctx.moveTo(cx + rw * 0.6, cy - rh * 0.6); ctx.lineTo(cx - rw * 0.6, cy + rh * 0.6);
    ctx.stroke();
  }
  ctx.restore();
}

/** Les deux pastilles d'économie d'action, flottant au-dessus de l'unité active.
 *  Éteinte = dépensée. C'est le seul endroit où l'état du tour est affiché. */
export function drawActionPips(ctx, cx, cy, moved, acted, t = 0) {
  const pips = [{ on: !moved, col: '#8fc3ff' }, { on: !acted, col: '#ffd98a' }];
  const gap = 13;
  const pulse = 0.5 + 0.5 * Math.sin(t * 0.005);
  ctx.save();
  pips.forEach((p, i) => {
    const x = cx + (i - 0.5) * gap;
    ctx.beginPath(); ctx.arc(x, cy, 5.2, 0, Math.PI * 2);
    ctx.fillStyle = rgba('#05070e', 0.7); ctx.fill();
    if (p.on) {
      ctx.shadowColor = rgba(p.col, 0.9); ctx.shadowBlur = 8 + pulse * 5;
      ctx.fillStyle = rgba(p.col, 0.95);
      ctx.beginPath(); ctx.arc(x, cy, 3.4, 0, Math.PI * 2); ctx.fill();
      ctx.shadowBlur = 0;
    }
    ctx.strokeStyle = rgba(p.on ? p.col : '#5c5872', p.on ? 0.9 : 0.5);
    ctx.lineWidth = 1.3;
    ctx.beginPath(); ctx.arc(x, cy, 5.2, 0, Math.PI * 2); ctx.stroke();
  });
  ctx.restore();
}

/** Un nœud d'exploration qui s'efface à l'entrée en combat : il se défait en particules
 *  qui montent. `p` de 0 (intact) à 1 (disparu). */
export function drawDissolveFx(ctx, dx, dy, dw, dh, p, color = '#dcb45c', elev = 0) {
  if (p <= 0 || p >= 1) return;
  const cx = dx + dw / 2;
  const cy = dy + dh * anchorRatioAt(elev);
  const s = dw / SPRITE_W;
  const R = makeRng(hashSeed('dissolve'));
  ctx.save();
  for (let i = 0; i < 22; i++) {
    const a = R() * Math.PI * 2;
    const rr = R2(R, 4, 26) * s;
    const lift = R2(R, 40, 130) * s * p;
    const life = Math.max(0, 1 - p * R2(R, 0.9, 1.6));
    ctx.fillStyle = rgba(color, life * 0.7);
    ctx.beginPath();
    ctx.arc(cx + Math.cos(a) * rr, cy - lift + Math.sin(a) * rr * 0.4, R2(R, 0.8, 2.2) * s * life, 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.restore();
}

/** Une unité qui se pose sur la grille : l'anneau se resserre, la poussière se lève.
 *  `p` de 0 (loin) à 1 (posée). */
export function drawDeployFx(ctx, dx, dy, dw, dh, p, color = '#8fc3ff', elev = 0) {
  if (p <= 0 || p >= 1) return;
  const cx = dx + dw / 2;
  const cy = dy + dh * anchorRatioAt(elev);
  const rw = dw * 0.30;
  const k = 1 - p;
  ctx.save();
  ctx.strokeStyle = rgba(color, 0.2 + 0.7 * p);
  ctx.lineWidth = 1.6 + k * 2;
  ctx.shadowColor = rgba(color, 0.8); ctx.shadowBlur = 12;
  const r = rw * (1 + k * 3.2);
  ctx.beginPath(); ctx.ellipse(cx, cy, r, r * 0.5, 0, 0, Math.PI * 2); ctx.stroke();
  ctx.shadowBlur = 0;
  if (p > 0.6) {
    const d = (p - 0.6) / 0.4;
    const R = makeRng(hashSeed('deploy'));
    for (let i = 0; i < 12; i++) {
      const a = R() * Math.PI * 2;
      const rr = rw * (0.6 + d * 1.8);
      ctx.fillStyle = rgba(color, (1 - d) * 0.5);
      ctx.beginPath();
      ctx.arc(cx + Math.cos(a) * rr, cy + Math.sin(a) * rr * 0.45 - d * 8, R2(R, 1, 2.4) * (1 - d), 0, Math.PI * 2);
      ctx.fill();
    }
  }
  ctx.restore();
}

/** Impact d'une compétence sur une case. `p` de 0 à 1. */
export function drawImpactFx(ctx, dx, dy, dw, dh, p, color = '#ff6a4a', elev = 0) {
  if (p <= 0 || p >= 1) return;
  const cx = dx + dw / 2;
  const cy = dy + dh * anchorRatioAt(elev);
  const s = dw / SPRITE_W;
  const fade = 1 - p;
  ctx.save();
  const g = ctx.createRadialGradient(cx, cy, 1, cx, cy, 34 * s * (0.4 + p));
  g.addColorStop(0, rgba('#ffffff', fade * 0.8));
  g.addColorStop(0.4, rgba(color, fade * 0.6));
  g.addColorStop(1, rgba(color, 0));
  ctx.fillStyle = g;
  ctx.beginPath(); ctx.ellipse(cx, cy, 40 * s * (0.4 + p), 20 * s * (0.4 + p), 0, 0, Math.PI * 2); ctx.fill();
  const R = makeRng(hashSeed('impact'));
  for (let i = 0; i < 10; i++) {
    const a = R() * Math.PI * 2;
    const rr = 40 * s * p * R2(R, 0.5, 1.2);
    ctx.strokeStyle = rgba(color, fade * 0.8);
    ctx.lineWidth = 1.8 * fade;
    ctx.beginPath();
    ctx.moveTo(cx + Math.cos(a) * rr * 0.5, cy + Math.sin(a) * rr * 0.25);
    ctx.lineTo(cx + Math.cos(a) * rr, cy + Math.sin(a) * rr * 0.5);
    ctx.stroke();
  }
  ctx.restore();
}

// ── Projection (identique à celle du client) ─────────────────────────────────────────
export const ISO_FIT = 0.82;
export const ISO_V_CENTER = 0.56;
export function isoUnit(p) {
  const isoUnitX = (p.canvasWidth / (p.gridWidth + p.gridHeight)) * ISO_FIT;
  return { isoUnitX, isoUnitY: isoUnitX / 2 };
}
export function projectToScreen(x, y, p) {
  const { isoUnitX, isoUnitY } = isoUnit(p);
  const maxSpan = p.gridWidth - 1 + (p.gridHeight - 1);
  return {
    screenX: (p.canvasWidth / 2) - ((x - y) * isoUnitX),
    screenY: (p.canvasHeight * ISO_V_CENTER) + (((x + y) - (maxSpan / 2)) * isoUnitY),
  };
}
