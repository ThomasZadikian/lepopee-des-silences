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
function theme(name) { return THEMES[name] ?? THEMES.Threshold; }

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
    default: return null;
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

function obstacleSilhouette(kind, R) {
  const cx = TILE.W / 2;
  const base = centerY(TILE.MAX) + 4; // assis sur la face du dessus, hauteur max
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

function bakeObstacle(name, variant, grain) {
  const th = theme(name);
  const kind = wallKind(th, variant);
  const R = makeRng(hashSeed('wall:' + name + ':' + kind));
  // Même toile haute que les décors : une silhouette de mur monte bien au-dessus de la
  // tuile et serait tronquée à plat sur la toile de sol. À blitter avec le rect "prop".
  const cv = makeCanvas(SPRITE_W, PROP_SPRITE_H);
  const ctx = cv.getContext('2d');
  if (!ctx) return cv;
  ctx.translate(0, PROP_EXTRA_H);

  // Un mur ne doit jamais paraître plus bas que le sol voisin : toujours à l'élévation max.
  paintRisers(ctx, TILE.MAX, th, R, { grain });
  const c = corners(TILE.MAX);
  ctx.save();
  ctx.clip(diamondPath(TILE.MAX, 0.5));
  const g = ctx.createLinearGradient(c.left.x, c.top.y, c.right.x, c.bottom.y);
  g.addColorStop(0, rgba(shade(th.riser, 0.14), 1));
  g.addColorStop(1, rgba(th.riserDeep, 1));
  ctx.fillStyle = g; ctx.fill(diamondPath(TILE.MAX, -1));
  speckle(ctx, c.cx - c.hw, c.cy - c.hh, TILE.W, TILE.H, R, Math.round(320 * grain), th.glow, '#000000');
  ctx.restore();
  ctx.strokeStyle = rgba(th.glow, 0.18); ctx.lineWidth = 1.6;
  ctx.stroke(diamondPath(TILE.MAX));

  // Halo froid derrière la masse : détache la silhouette du fond, même en salle sombre.
  const sty = WALL_STYLE[kind] ?? { striation: 'masonry' };
  ctx.save();
  ctx.shadowColor = rgba(th.accent, 0.5);
  ctx.shadowBlur = 16;
  for (const poly of obstacleSilhouette(kind, R)) paintMass(ctx, poly, th, R, { rim: 0.3, ...sty });
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
const HL = {
  move: { col: '#a6c8ff', fill: 0.16, line: 0.7 },
  attack: { col: '#ff9b86', fill: 0.18, line: 0.75 },
  cursor: { col: '#ffd98a', fill: 0.10, line: 0.95 },
  path: { col: '#ffe9b8', fill: 0.26, line: 0.5 },
};
function bakeHighlight(variant, elev) {
  const spec = HL[variant] ?? HL.move;
  const cv = makeCanvas(SPRITE_W, SPRITE_H);
  const ctx = cv.getContext('2d');
  if (!ctx) return cv;
  const c = corners(elev);
  const p = diamondPath(elev, 3);
  const g = ctx.createLinearGradient(c.top.x, c.top.y, c.bottom.x, c.bottom.y);
  g.addColorStop(0, rgba(spec.col, spec.fill * 1.5));
  g.addColorStop(1, rgba(spec.col, spec.fill * 0.4));
  ctx.fillStyle = g;
  ctx.fill(p);
  ctx.save();
  ctx.shadowColor = rgba(spec.col, 0.8);
  ctx.shadowBlur = 10;
  ctx.strokeStyle = rgba(spec.col, spec.line);
  ctx.lineWidth = variant === 'cursor' ? 2.6 : 1.8;
  if (variant === 'cursor') ctx.setLineDash([9, 6]);
  ctx.stroke(p);
  ctx.restore();
  if (variant === 'cursor') {
    for (const k of ['top', 'right', 'bottom', 'left']) {
      ctx.fillStyle = rgba('#fff3d4', 0.9);
      ctx.beginPath(); ctx.arc(c[k].x, c[k].y, 2.4, 0, Math.PI * 2); ctx.fill();
    }
  }
  return cv;
}

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
export function spriteKeyToString(k) {
  switch (k.kind) {
    case 'floor':
      return [
        'f', k.theme, k.elevation, k.surfaceSeed ?? 0, k.hidden ?? 'n', k.danger ?? 'n',
        k.cliffLeft ? 'L' : '-', k.cliffRight ? 'R' : '-', k.resolved ? 'r' : '-', k.glow ? 'g' : '-',
      ].join(':');
    case 'obstacle': return `o:${k.theme}:${k.variant ?? 0}`;
    case 'prop': return `p:${k.theme}:${k.prop}`;
    case 'highlight': return `h:${k.variant}:${k.elevation}`;
    case 'party': return `y:${k.elevation}`;
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
      case 'obstacle': cv = bakeObstacle(key.theme, key.variant ?? 0, grain); break;
      case 'prop': cv = bakeProp(key.theme, key.prop, grain); break;
      case 'highlight': cv = bakeHighlight(key.variant, key.elevation ?? 0); break;
      case 'party': cv = bakeParty(key.elevation ?? 0); break;
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
  veil.addColorStop(0, rgba(mix(th.sky[0], '#000000', 0.35), 0.94));
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
