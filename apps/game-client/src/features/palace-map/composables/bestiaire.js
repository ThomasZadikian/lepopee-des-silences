// Bestiaire — jetons de combat peints, d'après « Brief de direction artistique — Combattants ».
//
// Trois règles tenues partout dans ce fichier :
//
//   1. LA SILHOUETTE PRIME. Chaque figure est d'abord une forme reconnaissable à la taille
//      d'une case : Guard large et bas, Bruiser haut et asymétrique, Skirmisher étroit et
//      penché, Support vertical avec un objet devant soi, Disruptor à contour instable,
//      Swarm jamais seul. Le détail vient après, et seulement s'il survit à la réduction.
//   2. RIEN N'EST FRANCHEMENT MONSTRUEUX. Tout est légèrement faux. On ne peint pas du gore,
//      on peint un protocole qui a mal tourné : une révérence qui ne se relève pas, une
//      hauteur de service trop constante, un salut qui ne redescend jamais.
//   3. TOUTE COULEUR VIVE EST UN ÉVÉNEMENT. Le fond du jeu est sombre, violacé, désaturé.
//      Une note saturée signale une menace, une magie ou une émotion — jamais un décor.
//
// Aucune ombre portée n'est peinte en dur : la scène la calcule. Ce qui est peint ici, c'est
// un contact au sol (un assombrissement de la case), ce qui n'est pas la même chose.

import { PAINT } from './tilecraft.js';

const {
  rgba, mix, shade, paintMass, makeCanvas, centerY,
  hashSeed, makeRng, R2, SPRITE_W, PROP_SPRITE_H, PROP_EXTRA_H,
} = PAINT;

// ── Tokens ─────────────────────────────────────────────────────────────────────────────
// Transcription hex des tokens oklch du jeu. Sombre, violacé, faiblement saturé.
export const TOKEN = {
  void: '#1d1b2e', bg: '#262338', panel: '#302c46',
  ink: '#efedf7', ink4: '#6f6a86',
  gold: '#e6c273', goldDim: '#c3a35c', goldDeep: '#a5843c',
  ember: '#e0793f', emberDeep: '#a3451c',
  frost: '#b3bdf2', frostDeep: '#6a6fb0',
  blood: '#e0605e', bloodDim: '#8e2b32',
  sap: '#86dcb4',
};

/** Les sept registres émotionnels. Glyphe et couleur sont la clé de lecture du joueur :
 *  ils ne changent jamais d'une salle à l'autre. */
export const REGISTRES = {
  effroi: { label: 'Effroi', glyph: '✶', col: '#c8394a' },
  deni: { label: 'Déni', glyph: '◇', col: '#d9a441' },
  melancolie: { label: 'Mélancolie', glyph: '❍', col: '#6f96c8' },
  rupture: { label: 'Rupture', glyph: '⟡', col: '#d1662c' },
  memoire: { label: 'Mémoire', glyph: '◈', col: '#e0b45f' },
  silence: { label: 'Silence', glyph: '○', col: '#c3bfcc' },
  folie: { label: 'Folie', glyph: '✳', col: '#cf3f92' },
};

/** Lecture visuelle attendue par rôle — c'est le contrat de silhouette. */
export const ROLES = {
  guard: 'large, bas, symétrique — occupe la case entière',
  bruiser: 'haut, épaules lourdes, asymétrie marquée',
  skirmisher: 'étroit, penché en avant, appui sur un pied',
  support: 'vertical, statique, un objet tenu devant soi',
  disruptor: 'contour instable, mal défini, flotte',
  swarm: 'petit, jamais seul — se dessine par groupes de 3+',
  drain: 'envahissant, déborde de sa case',
};

// ── Outils de peinture ─────────────────────────────────────────────────────────────────
const P = (x, y) => ({ x, y });

/** Contact au sol : pas une ombre portée, un assombrissement de la case sous la figure. */
function contact(ctx, cx, base, rx, ry, a = 0.42) {
  ctx.fillStyle = rgba('#000000', a);
  ctx.beginPath(); ctx.ellipse(cx, base - 1, rx, ry, 0, 0, Math.PI * 2); ctx.fill();
}

function fill(ctx, path, col, a = 1) { ctx.fillStyle = rgba(col, a); ctx.fill(path); }

function line(ctx, path, col, a = 1, w = 1.3) {
  ctx.strokeStyle = rgba(col, a); ctx.lineWidth = w; ctx.stroke(path);
}

function poly(pts, close = true) {
  const p = new Path2D();
  p.moveTo(pts[0].x, pts[0].y);
  for (let i = 1; i < pts.length; i++) p.lineTo(pts[i].x, pts[i].y);
  if (close) p.closePath();
  return p;
}

function seg(ctx, x0, y0, x1, y1, col, a, w, cap = 'round') {
  ctx.beginPath(); ctx.moveTo(x0, y0); ctx.lineTo(x1, y1);
  ctx.strokeStyle = rgba(col, a); ctx.lineWidth = w; ctx.lineCap = cap; ctx.stroke();
}

function glowDot(ctx, x, y, r, col, a = 0.8) {
  const g = ctx.createRadialGradient(x, y, 0.4, x, y, r * 3.2);
  g.addColorStop(0, rgba('#ffffff', a));
  g.addColorStop(0.3, rgba(col, a * 0.75));
  g.addColorStop(1, rgba(col, 0));
  ctx.fillStyle = g;
  ctx.beginPath(); ctx.arc(x, y, r * 3.2, 0, Math.PI * 2); ctx.fill();
  ctx.fillStyle = rgba(shade(col, 0.5), Math.min(1, a + 0.15));
  ctx.beginPath(); ctx.arc(x, y, r, 0, Math.PI * 2); ctx.fill();
}

/** Le drapé lourd : deux courbes qui tombent, plis creusés à l'intérieur. La forme de base
 *  du Palais — livrées, bures, robes cardinalices en sortent toutes. */
function drape(ctx, R, cx, base, o) {
  const { top, halfTop, halfBot, col, deep, plis = 7, glow = null, lean = 0 } = o;
  const p = new Path2D();
  p.moveTo(cx - halfBot, base);
  p.quadraticCurveTo(cx - halfTop - 2 + lean * 0.4, (base + top) / 2, cx - halfTop + lean, top);
  p.lineTo(cx + halfTop + lean, top);
  p.quadraticCurveTo(cx + halfTop + 2 + lean * 0.4, (base + top) / 2, cx + halfBot, base);
  p.closePath();
  ctx.save();
  ctx.shadowColor = rgba('#000000', 0.55); ctx.shadowBlur = 9;
  const g = ctx.createLinearGradient(cx - halfBot, 0, cx + halfBot, 0);
  g.addColorStop(0, rgba(shade(col, 0.12), 1));
  g.addColorStop(0.6, rgba(col, 1));
  g.addColorStop(1, rgba(deep ?? shade(col, -0.5), 1));
  ctx.fillStyle = g; ctx.fill(p);
  ctx.restore();
  ctx.save();
  ctx.clip(p);
  for (let i = 0; i < plis; i++) {
    const x = cx - halfBot + ((i + 0.5) / plis) * halfBot * 2;
    ctx.beginPath();
    ctx.moveTo(x, base + 2);
    ctx.quadraticCurveTo(x + R2(R, -4, 4), (base + top) / 2, x + R2(R, -3, 3) + lean, top - 2);
    ctx.strokeStyle = rgba(R() > 0.55 && glow ? glow : '#000000', R() > 0.55 && glow ? 0.12 : R2(R, 0.14, 0.3));
    ctx.lineWidth = R2(R, 0.8, 2.1);
    ctx.stroke();
  }
  ctx.restore();
  return p;
}

/** Capuche fermée sur du vide ou sur un visage usé — jamais sur un visage lisible. */
function hood(ctx, cx, y, w, h, col, deep, inner = '#05060c') {
  const p = new Path2D();
  p.moveTo(cx - w, y + h);
  p.quadraticCurveTo(cx - w - 1, y - h * 0.5, cx, y - h);
  p.quadraticCurveTo(cx + w + 1, y - h * 0.5, cx + w, y + h);
  p.closePath();
  fill(ctx, p, col);
  const inn = new Path2D();
  inn.ellipse(cx, y + h * 0.15, w * 0.62, h * 0.6, 0, 0, Math.PI * 2);
  fill(ctx, inn, inner, 0.95);
  line(ctx, p, deep, 0.5, 1.2);
  return p;
}

/** Os : un membre grêle, renflé aux extrémités. */
function bone(ctx, x0, y0, x1, y1, col, w = 3.4) {
  seg(ctx, x0, y0, x1, y1, col, 0.95, w);
  ctx.fillStyle = rgba(shade(col, 0.18), 1);
  ctx.beginPath(); ctx.arc(x0, y0, w * 0.75, 0, Math.PI * 2); ctx.fill();
  ctx.beginPath(); ctx.arc(x1, y1, w * 0.75, 0, Math.PI * 2); ctx.fill();
}

function skull(ctx, cx, cy, r, col, jaw = 0) {
  const p = new Path2D();
  p.ellipse(cx, cy, r, r * 1.08, 0, Math.PI, Math.PI * 2);
  p.lineTo(cx + r * 0.62, cy + r * 0.55);
  p.lineTo(cx - r * 0.62, cy + r * 0.55);
  p.closePath();
  fill(ctx, p, col);
  ctx.fillStyle = rgba('#04050a', 0.92);
  for (const s of [-1, 1]) {
    ctx.beginPath();
    ctx.ellipse(cx + s * r * 0.38, cy - r * 0.05, r * 0.26, r * 0.3, 0, 0, Math.PI * 2);
    ctx.fill();
  }
  if (jaw > 0) {
    const j = new Path2D();
    j.moveTo(cx - r * 0.6, cy + r * 0.5);
    j.quadraticCurveTo(cx, cy + r * 0.5 + jaw, cx + r * 0.6, cy + r * 0.5);
    j.quadraticCurveTo(cx, cy + r * 0.5 + jaw * 0.45, cx - r * 0.6, cy + r * 0.5);
    fill(ctx, j, col);
    ctx.fillStyle = rgba('#04050a', 0.95);
    ctx.beginPath();
    ctx.ellipse(cx, cy + r * 0.52 + jaw * 0.36, r * 0.5, jaw * 0.34, 0, 0, Math.PI * 2);
    ctx.fill();
  }
  return p;
}

/** Gravures illisibles mais manifestement volontaires : des traits courts, réguliers,
 *  qui suivent la forme. Aucun caractère reconnaissable. */
function gravures(ctx, R, x, y, w, h, col, n = 14) {
  ctx.save();
  for (let i = 0; i < n; i++) {
    const gx = x + R() * w, gy = y + R() * h;
    ctx.beginPath();
    ctx.moveTo(gx, gy);
    ctx.lineTo(gx + R2(R, -3.5, 3.5), gy + R2(R, -1.5, 1.5));
    ctx.strokeStyle = rgba(col, R2(R, 0.12, 0.4));
    ctx.lineWidth = 0.9;
    ctx.stroke();
  }
  ctx.restore();
}

/** Texte figuré : des mots trop brefs pour être lus. Jamais de vraies lettres. */
function faketext(ctx, R, x, y, w, lines, col, a = 0.5, lh = 5.5) {
  for (let i = 0; i < lines; i++) {
    let cxp = x;
    const yy = y + i * lh;
    while (cxp < x + w - 3) {
      const wd = R2(R, 3, 11);
      ctx.fillStyle = rgba(col, a * R2(R, 0.5, 1));
      ctx.fillRect(cxp, yy, Math.min(wd, x + w - cxp), 1.3);
      cxp += wd + R2(R, 1.6, 3.4);
    }
  }
}

// ── Le roster ──────────────────────────────────────────────────────────────────────────
// `paint(k)` reçoit : ctx, R (aléatoire déterministe), cx, base, th (thème de peinture
// synthétique pour paintMass), pal (palette de la figure), v (numéro de variante).

export const ROSTER = {

  // ═══ ALLIÉS ══════════════════════════════════════════════════════════════════════════
  // Les seules figures que le joueur regarde pendant des heures. Chaleureuses au milieu
  // d'un décor hostile : plus de contraste, des tissus qui bougent, des visages lisibles.
  // Là où le bestiaire est figé, les alliés respirent.

  porteur: {
    catalogKey: null,
    name: 'Le Porteur', side: 'ally', role: 'skirmisher', family: 'allies', rarity: 'joueur',
    quote: 'Il porte quelque chose qui n’est pas à lui et qu’il n’a pas encore rendu.',
    silhouette: 'Humaine, adulte, sans armure. Un voyageur, pas un guerrier. Besace en évidence.',
    pal: { body: '#4a4152', deep: '#241f2e', skin: '#d9b394', accent: TOKEN.ember, light: TOKEN.ink },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 20, 8);
      // Jambes de marche : appuis inégaux, il est toujours en train d'arriver.
      paintMass(ctx, [P(cx - 12, base), P(cx - 9, base - 30), P(cx - 2, base - 30), P(cx - 4, base)], th, R, { base: pal.deep, rim: 0.1 });
      paintMass(ctx, [P(cx + 3, base), P(cx + 2, base - 30), P(cx + 10, base - 30), P(cx + 13, base)], th, R, { base: pal.deep, rim: 0.1 });
      // Couches superposées et réparées : trois pans de longueurs différentes.
      for (const [w, top, col] of [[15, 30, shade(pal.body, -0.3)], [13, 42, pal.body], [10, 52, shade(pal.body, 0.1)]]) {
        const p = poly([P(cx - w, base - top + 14), P(cx - w + 2, base - top), P(cx + w - 2, base - top), P(cx + w, base - top + 14)]);
        fill(ctx, p, col); line(ctx, p, '#000000', 0.25, 1);
      }
      paintMass(ctx, [P(cx - 12, base - 30), P(cx - 13, base - 60), P(cx + 12, base - 60), P(cx + 11, base - 30)], th, R,
        { base: pal.body, deep: pal.deep, rim: 0.14 });
      // La besace : l'inventaire est un système central, elle doit se lire d'un coup d'œil.
      const sac = new Path2D();
      sac.moveTo(cx + 6, base - 44);
      sac.quadraticCurveTo(cx + 26, base - 42, cx + 25, base - 22);
      sac.quadraticCurveTo(cx + 15, base - 14, cx + 5, base - 22);
      sac.closePath();
      fill(ctx, sac, '#5c4632'); line(ctx, sac, '#22180f', 0.7, 1.4);
      seg(ctx, cx + 6, base - 42, cx + 24, base - 34, '#3a2a1c', 0.9, 2.4);
      seg(ctx, cx - 10, base - 58, cx + 20, base - 38, '#3a2a1c', 0.85, 3);
      // La note chaude unique : un accessoire qui n'est pas à lui.
      glowDot(ctx, cx + 17, base - 30, 2.4, pal.accent, 0.85);
      ctx.beginPath(); ctx.ellipse(cx - 1, base - 68, 7.4, 8.4, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.skin, 1); ctx.fill();
      seg(ctx, cx - 8, base - 74, cx + 7, base - 76, shade(pal.body, -0.2), 0.9, 5);
      ctx.fillStyle = rgba('#2a2030', 0.85);
      ctx.fillRect(cx - 5, base - 70, 10, 1.8);
    },
  },

  elise: {
    catalogKey: null,
    name: 'Elise', side: 'ally', role: 'support', family: 'allies', rarity: 'accompagnatrice',
    quote: 'Le silence et l’apathie d’Elise ne sont qu’une façade.',
    silhouette: 'La plus statique de l’équipe. Robe longue qui tombe droit, comme un rideau.',
    pal: { body: '#4c4a5e', deep: '#232232', skin: '#d8cdd6', accent: TOKEN.blood, light: '#c3bfcc' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 19, 7.5);
      // Un rideau, pas une jupe : les bords sont presque verticaux, l'ourlet est droit.
      drape(ctx, R, cx, base, { top: base - 62, halfTop: 11, halfBot: 17, col: pal.body, deep: pal.deep, plis: 9, glow: pal.light });
      // La zone chaude au niveau du cœur : visible seulement quand elle soigne — ici, en veille.
      glowDot(ctx, cx - 2, base - 46, 1.8, pal.accent, 0.3);
      const sh = poly([P(cx - 11, base - 62), P(cx - 8, base - 68), P(cx + 8, base - 68), P(cx + 11, base - 62)]);
      fill(ctx, sh, shade(pal.body, 0.08)); line(ctx, sh, pal.deep, 0.5, 1.1);
      // Elle ne regarde jamais tout à fait la caméra : la tête est décalée, jamais de face.
      ctx.beginPath(); ctx.ellipse(cx - 3, base - 76, 6.8, 8, -0.06, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.skin, 1); ctx.fill();
      const hair = new Path2D();
      hair.moveTo(cx - 10, base - 80);
      hair.quadraticCurveTo(cx - 2, base - 87, cx + 6, base - 79);
      hair.quadraticCurveTo(cx + 8, base - 62, cx + 4, base - 58);
      hair.quadraticCurveTo(cx + 3, base - 74, cx - 10, base - 80);
      fill(ctx, hair, shade(pal.deep, 0.06));
      // Bouche fermée en permanence : son pouvoir, c'est « Se taire ».
      ctx.fillStyle = rgba('#7c5f66', 0.8);
      ctx.fillRect(cx - 5, base - 73, 6, 1.2);
      for (const s of [-1, 1]) seg(ctx, cx - 3 + s * 3.6, base - 76.5, cx - 3 + s * 5.6, base - 76.5, '#3a3040', 0.7, 1.4);
    },
  },

  thomas: {
    catalogKey: null,
    name: 'Thomas', side: 'ally', role: 'guard', family: 'allies', rarity: 'projection',
    quote: 'Celle des projections qui ressemble le plus à l’Architecte.',
    silhouette: 'La plus large et la plus stable. Épaules carrées, pieds écartés. Il occupe la case.',
    pal: { body: '#8a7a5c', deep: '#3d3524', skin: '#d6b995', accent: TOKEN.gold, light: '#cdb98a' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 25, 9.5);
      // Position d'appui : pieds écartés, aucune verticale unique. Il est bâti, pas habillé.
      paintMass(ctx, [P(cx - 20, base), P(cx - 15, base - 32), P(cx - 4, base - 32), P(cx - 7, base)], th, R, { base: pal.deep, rim: 0.12 });
      paintMass(ctx, [P(cx + 7, base), P(cx + 4, base - 32), P(cx + 15, base - 32), P(cx + 20, base)], th, R, { base: pal.deep, rim: 0.12 });
      paintMass(ctx, [P(cx - 18, base - 28), P(cx - 22, base - 62), P(cx + 22, base - 62), P(cx + 18, base - 28)], th, R,
        { base: pal.body, deep: pal.deep, rim: 0.18 });
      // Épaules carrées : deux blocs, pas deux courbes. C'est ce qui fait la stabilité.
      for (const s of [-1, 1]) {
        const ep = poly([P(cx + s * 14, base - 60), P(cx + s * 27, base - 62), P(cx + s * 26, base - 48), P(cx + s * 15, base - 50)]);
        fill(ctx, ep, mix(pal.body, '#b9b2a4', 0.4)); line(ctx, ep, pal.deep, 0.6, 1.3);
      }
      // Avant-bras portant des lignes de plan d'architecte, comme des veines réglées.
      for (const s of [-1, 1]) {
        const br = poly([P(cx + s * 22, base - 50), P(cx + s * 27, base - 32), P(cx + s * 19, base - 30), P(cx + s * 15, base - 48)]);
        fill(ctx, br, pal.skin, 0.95);
        ctx.save(); ctx.clip(br);
        for (let i = 0; i < 5; i++) seg(ctx, cx + s * 14, base - 48 + i * 4, cx + s * 28, base - 44 + i * 4, pal.accent, 0.35, 0.9);
        ctx.restore();
        line(ctx, br, '#5c4a34', 0.5, 1);
      }
      // Le carnet de bord, toujours sur lui.
      const cn = poly([P(cx - 6, base - 40), P(cx + 8, base - 42), P(cx + 9, base - 28), P(cx - 5, base - 26)]);
      fill(ctx, cn, '#6b4f30'); line(ctx, cn, TOKEN.gold, 0.4, 1.1);
      ctx.fillStyle = rgba('#e8e2cf', 0.8); ctx.fillRect(cx - 4, base - 39, 11, 2);
      ctx.beginPath(); ctx.ellipse(cx, base - 70, 8.4, 9, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.skin, 1); ctx.fill();
      seg(ctx, cx - 8, base - 77, cx + 8, base - 76, '#4c3f2c', 0.9, 5.5);
      ctx.fillStyle = rgba('#2d2418', 0.8); ctx.fillRect(cx - 6, base - 71, 12, 1.8);
    },
  },

  mane: {
    catalogKey: null,
    name: 'Mané', side: 'ally', role: 'skirmisher', family: 'allies', rarity: 'projection',
    quote: 'Très émotive, très impulsive, d’une intelligence émotionnelle redoutable.',
    silhouette: 'La plus mobile. Étroite, en mouvement même à l’arrêt.',
    pal: { body: '#a8474f', deep: '#4a1a22', skin: '#e0b294', accent: TOKEN.blood, light: '#f0b8a8' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 17, 7);
      // Un seul appui franc : elle part avant d'avoir décidé.
      paintMass(ctx, [P(cx - 13, base), P(cx - 6, base - 30), P(cx, base - 30), P(cx - 3, base)], th, R, { base: pal.deep, rim: 0.1 });
      paintMass(ctx, [P(cx + 5, base - 4), P(cx + 2, base - 30), P(cx + 8, base - 30), P(cx + 14, base - 6)], th, R, { base: pal.deep, rim: 0.1 });
      // Cheveux et vêtements en retard d'un temps sur le corps : tout traîne vers l'arrière.
      const trail = new Path2D();
      trail.moveTo(cx + 4, base - 56);
      trail.quadraticCurveTo(cx + 26, base - 48, cx + 30, base - 20);
      trail.lineTo(cx + 20, base - 26);
      trail.quadraticCurveTo(cx + 18, base - 42, cx + 6, base - 44);
      trail.closePath();
      fill(ctx, trail, mix(pal.body, pal.accent, 0.3), 0.9);
      paintMass(ctx, [P(cx - 9, base - 28), P(cx - 12, base - 52), P(cx - 4, base - 60), P(cx + 8, base - 57), P(cx + 7, base - 30)], th, R,
        { base: pal.body, deep: pal.deep, rim: 0.16 });
      // Les mains toujours OUVERTES, jamais en poing, même quand elle frappe.
      for (const [hx0, hy0, hx1, hy1] of [[-11, -50, -22, -36], [8, -48, 20, -42]]) {
        seg(ctx, cx + hx0, base + hy0, cx + hx1, base + hy1, pal.skin, 0.95, 4);
        for (let i = -2; i <= 2; i++) {
          const a = Math.atan2(hy1 - hy0, hx1 - hx0) + i * 0.34;
          seg(ctx, cx + hx1, base + hy1, cx + hx1 + Math.cos(a) * 6, base + hy1 + Math.sin(a) * 6, pal.skin, 0.9, 1.7);
        }
      }
      ctx.beginPath(); ctx.ellipse(cx - 1, base - 66, 7, 8, 0.08, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.skin, 1); ctx.fill();
      const hr = new Path2D();
      hr.moveTo(cx - 9, base - 70);
      hr.quadraticCurveTo(cx, base - 79, cx + 8, base - 70);
      hr.quadraticCurveTo(cx + 22, base - 64, cx + 14, base - 52);
      hr.quadraticCurveTo(cx + 8, base - 64, cx - 9, base - 70);
      fill(ctx, hr, shade(pal.deep, 0.1));
      ctx.fillStyle = rgba('#8a3c40', 0.85); ctx.fillRect(cx - 4, base - 63, 6, 1.6);
    },
  },

  mina: {
    catalogKey: null,
    name: 'Mina', side: 'ally', role: 'support', family: 'allies', rarity: 'enfant',
    quote: 'Ses parents restent inconnus ; elle les cherche.',
    silhouette: 'Nettement plus petite. Immédiatement lisible comme « l’enfant » du groupe.',
    pal: { body: '#c8bcd4', deep: '#6e6480', skin: '#e8c9ac', accent: '#e0a8bc', light: '#f2ecf6' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 14, 6);
      // Nettement plus petite : hauteur totale réduite d'un tiers. La seule tache claire du champ.
      drape(ctx, R, cx, base, { top: base - 34, halfTop: 8, halfBot: 13, col: pal.body, deep: pal.deep, plis: 6, glow: pal.light });
      for (const s of [-1, 1]) seg(ctx, cx + s * 5, base - 4, cx + s * 5, base, shade(pal.skin, -0.2), 0.9, 3.4);
      const bust = poly([P(cx - 8, base - 32), P(cx - 7, base - 48), P(cx + 7, base - 48), P(cx + 8, base - 32)]);
      fill(ctx, bust, shade(pal.body, 0.06)); line(ctx, bust, pal.deep, 0.4, 1.1);
      // La peluche, tenue par un bras, qui TRAÎNE AU SOL quand elle marche.
      seg(ctx, cx - 7, base - 44, cx - 15, base - 22, pal.skin, 0.95, 3.4);
      const pl = new Path2D();
      pl.ellipse(cx - 18, base - 13, 7, 8.5, -0.25, 0, Math.PI * 2);
      fill(ctx, pl, '#a88a70'); line(ctx, pl, '#5c4638', 0.6, 1.2);
      for (const s of [-1, 1]) {
        ctx.beginPath(); ctx.ellipse(cx - 18 + s * 5.5, base - 20 + (s < 0 ? 1 : -1), 3, 3.4, 0, 0, Math.PI * 2);
        ctx.fillStyle = rgba('#a88a70', 1); ctx.fill();
      }
      seg(ctx, cx - 20, base - 6, cx - 14, base - 3, '#8c7460', 0.9, 4);
      ctx.beginPath(); ctx.ellipse(cx + 1, base - 55, 7.6, 8.4, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.skin, 1); ctx.fill();
      const hr = new Path2D();
      hr.moveTo(cx - 8, base - 58);
      hr.quadraticCurveTo(cx + 1, base - 66, cx + 9, base - 57);
      hr.quadraticCurveTo(cx + 10, base - 46, cx + 6, base - 44);
      hr.quadraticCurveTo(cx + 5, base - 56, cx - 8, base - 58);
      fill(ctx, hr, '#8a6a58');
      // Elle regarde ailleurs, vers les portes, vers les couloirs. Elle cherche quelqu'un.
      ctx.fillStyle = rgba('#3a3040', 0.8);
      ctx.fillRect(cx + 4, base - 56, 4, 1.5);
    },
  },

  john: {
    catalogKey: null,
    name: 'John', side: 'ally', role: 'skirmisher', family: 'allies', rarity: 'voleur',
    quote: 'En pillant d’anciennes ruines, il a fini par traverser la faille du Palais.',
    silhouette: 'Penchée en avant, mains près du corps, jamais de face. Toujours prêt à partir.',
    pal: { body: '#3a3444', deep: '#1a1824', skin: '#c9a685', accent: TOKEN.goldDim, light: '#5c5468' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 17, 7);
      // Le moins visible de l'équipe, ce qui est le propos : valeurs très basses, pas de contour clair.
      paintMass(ctx, [P(cx - 14, base), P(cx - 8, base - 28), P(cx - 1, base - 28), P(cx - 5, base)], th, R, { base: pal.deep, rim: 0.08 });
      paintMass(ctx, [P(cx + 4, base), P(cx + 1, base - 28), P(cx + 9, base - 28), P(cx + 13, base)], th, R, { base: pal.deep, rim: 0.08 });
      // Trois-quarts systématique : le torse est en biais, jamais parallèle au bord de la case.
      paintMass(ctx, [P(cx - 10, base - 26), P(cx - 15, base - 50), P(cx - 5, base - 58), P(cx + 9, base - 55), P(cx + 8, base - 27)], th, R,
        { base: pal.body, deep: pal.deep, rim: 0.12 });
      const cape = new Path2D();
      cape.moveTo(cx + 5, base - 56);
      cape.quadraticCurveTo(cx + 22, base - 44, cx + 22, base - 16);
      cape.lineTo(cx + 13, base - 24);
      cape.lineTo(cx + 10, base - 12);
      cape.quadraticCurveTo(cx + 9, base - 38, cx + 3, base - 44);
      cape.closePath();
      fill(ctx, cape, pal.deep, 0.95); line(ctx, cape, pal.light, 0.3, 1.1);
      // Le butin d'un autre monde : seule preuve qu'un dehors existe. Une note d'or, sale.
      for (const [ox, oy, r] of [[-13, -40, 2.6], [-15, -32, 1.9], [11, -46, 2.2]]) {
        ctx.beginPath(); ctx.arc(cx + ox, base + oy, r, 0, Math.PI * 2);
        ctx.fillStyle = rgba(pal.accent, 0.7); ctx.fill();
        ctx.strokeStyle = rgba('#000000', 0.5); ctx.lineWidth = 0.8; ctx.stroke();
      }
      // Mains près du corps.
      seg(ctx, cx - 9, base - 48, cx - 4, base - 36, pal.skin, 0.9, 3.4);
      seg(ctx, cx + 6, base - 46, cx + 3, base - 34, pal.skin, 0.9, 3.4);
      ctx.beginPath(); ctx.ellipse(cx - 4, base - 64, 7, 7.8, -0.14, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.skin, 1); ctx.fill();
      const cap = new Path2D();
      cap.moveTo(cx - 12, base - 67);
      cap.quadraticCurveTo(cx - 4, base - 76, cx + 5, base - 68);
      cap.lineTo(cx + 3, base - 65);
      cap.lineTo(cx - 12, base - 64);
      cap.closePath();
      fill(ctx, cap, shade(pal.body, -0.2));
      ctx.fillStyle = rgba('#2a2230', 0.9); ctx.fillRect(cx - 9, base - 65, 8, 2);
    },
  },

  // ═══ BOSS ════════════════════════════════════════════════════════════════════════════
  // Reconnaissable à la silhouette seule, en un quart de seconde. Chacun occupe
  // visuellement plusieurs cases, même si la mécanique le place sur une seule.

  // ═══ BOSS ════════════════════════════════════════════════════════════════════════════
  // PLACEHOLDER en attendant les fiches définitives. Un seul jeton, volontairement muet :
  // une ombre menaçante. Aucun détail à réfuter plus tard — que de la masse et une échelle
  // supérieure aux élites. Les deux points rouges sont le seul signal, comme partout ailleurs
  // dans le bestiaire : le rouge est du signal, jamais de l'ambiance.
  'boss-ombre': {
    catalogKey: null,
    name: 'Ombre menaçante', side: 'enemy', role: 'bruiser', family: 'boss', rarity: 'boss',
    registre: 'effroi', boss: true,
    quote: 'Quelque chose occupe la salle. On ne voit pas quoi.',
    silhouette: 'Une masse haute, sans détail, qui dépasse tout le reste. Placeholder assumé.',
    pal: { body: '#0e0d16', deep: '#04040a', accent: '#ff4038', light: '#3a3650' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 38, 13, 0.62);
      // Elle déborde de sa case : c'est la seule chose qui doit se lire à coup sûr.
      // Bords rongés, jamais nets — une découpe franche donnerait une créature, pas une ombre.
      const ms = new Path2D();
      ms.moveTo(cx - 34, base + 2);
      ms.quadraticCurveTo(cx - 44, base - 44, cx - 26, base - 92);
      ms.quadraticCurveTo(cx - 16, base - 126, cx - 2, base - 140);
      ms.quadraticCurveTo(cx + 16, base - 126, cx + 26, base - 92);
      ms.quadraticCurveTo(cx + 44, base - 44, cx + 32, base + 2);
      ms.closePath();
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.8); ctx.shadowBlur = 20;
      const g = ctx.createLinearGradient(cx, base - 140, cx, base + 2);
      g.addColorStop(0, rgba(pal.body, 0.9));
      g.addColorStop(0.45, rgba(pal.body, 1));
      g.addColorStop(1, rgba(pal.deep, 1));
      ctx.fillStyle = g; ctx.fill(ms);
      ctx.restore();
      // Le haut se dissipe : l'ombre n'a pas de sommet, elle s'éteint. Rien à identifier.
      ctx.save();
      ctx.clip(ms);
      const fade = ctx.createLinearGradient(cx, base - 148, cx, base - 96);
      fade.addColorStop(0, rgba('#000000', 0));
      fade.addColorStop(1, rgba(pal.body, 0));
      ctx.globalCompositeOperation = 'destination-out';
      const f2 = ctx.createLinearGradient(cx, base - 152, cx, base - 104);
      f2.addColorStop(0, rgba('#000000', 1));
      f2.addColorStop(1, rgba('#000000', 0));
      ctx.fillStyle = f2;
      ctx.fillRect(cx - 50, base - 152, 100, 50);
      ctx.globalCompositeOperation = 'source-over';
      ctx.fillStyle = fade;
      // Quelques lambeaux qui se détachent des bords, pour que la masse respire.
      for (let i = 0; i < 22; i++) {
        const yy = base - R2(R, 10, 120);
        const s = R() > 0.5 ? 1 : -1;
        const w = 26 + Math.sin((base - yy) * 0.03) * 12;
        ctx.beginPath();
        ctx.ellipse(cx + s * w, yy, R2(R, 3, 11), R2(R, 6, 20), R2(R, -0.4, 0.4), 0, Math.PI * 2);
        ctx.fillStyle = rgba(pal.deep, R2(R, 0.4, 0.9)); ctx.fill();
      }
      ctx.restore();
      // Un liseré très faible sur le flanc éclairé : sans lui, la masse devient un trou.
      ctx.save();
      ctx.clip(ms);
      ctx.strokeStyle = rgba(pal.light, 0.4); ctx.lineWidth = 2.4;
      ctx.stroke(ms);
      ctx.restore();
      // Deux points rouges, hauts, très rapprochés. Le seul endroit où l'œil se pose.
      for (const s of [-1, 1]) glowDot(ctx, cx + s * 6 - 2, base - 112, 2.2, pal.accent, 0.9);
    },
  },

  // ═══ 3.1 LES VEILLEURS DU SEUIL — registre Silence ○ ═════════════════════════════════
  // La livrée de maison, le service, la révérence. AUCUN N'A DE VISAGE.
  // Ils ne vous attaquent pas : ils vous corrigent.

  'veilleur-tapis': {
    catalogKey: 'canon.enemy.veilleur-tapis',
    name: 'Veilleur du Tapis', side: 'enemy', role: 'guard', family: 'veilleurs', rarity: 'common',
    registre: 'silence',
    quote: '« Vos pieds. Je vous prie. »',
    silhouette: 'Pliée à 90°, jamais redressée ; large et basse, elle barre le passage.',
    pal: { body: '#17151f', deep: '#0a0910', accent: '#6d1420', light: '#e8e4ef' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 27, 10);
      // Le tapis bordeaux le suit où qu'il aille, COUSU À SES CHEVILLES. La couture est explicite.
      const tp = new Path2D();
      tp.moveTo(cx - 34, base + 2);
      tp.quadraticCurveTo(cx - 10, base - 6, cx + 30, base - 2);
      tp.quadraticCurveTo(cx + 34, base + 5, cx + 24, base + 8);
      tp.quadraticCurveTo(cx - 6, base + 4, cx - 32, base + 8);
      tp.closePath();
      fill(ctx, tp, pal.accent, 0.92);
      ctx.save(); ctx.clip(tp);
      for (let i = 0; i < 10; i++) seg(ctx, cx - 34 + i * 7, base - 4, cx - 32 + i * 7, base + 9, shade(pal.accent, -0.4), 0.5, 1.2);
      ctx.restore();
      line(ctx, tp, TOKEN.goldDeep, 0.35, 1.1);
      // Pliée à 90° : le dos est HORIZONTAL, les jambes VERTICALES. C'est toute la silhouette.
      for (const s of [-1, 1]) {
        paintMass(ctx, [P(cx + s * 4 + 14, base), P(cx + s * 4 + 13, base - 30), P(cx + s * 4 + 21, base - 30), P(cx + s * 4 + 22, base)], th, R,
          { base: pal.deep, rim: 0.08 });
        // La couture cheville / tapis, point par point : il est attaché à son devoir.
        for (let i = 0; i < 3; i++) seg(ctx, cx + s * 4 + 15 + i * 2.4, base - 3, cx + s * 4 + 17 + i * 2.4, base + 3, pal.light, 0.55, 0.9);
      }
      // Le dos, franchement horizontal, du bassin (à droite) à la nuque (à gauche).
      const dos = new Path2D();
      dos.moveTo(cx + 30, base - 26);
      dos.quadraticCurveTo(cx + 34, base - 50, cx + 18, base - 54);
      dos.lineTo(cx - 22, base - 50);
      dos.quadraticCurveTo(cx - 32, base - 44, cx - 26, base - 34);
      dos.quadraticCurveTo(cx - 4, base - 30, cx + 26, base - 22);
      dos.closePath();
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.6); ctx.shadowBlur = 9;
      fill(ctx, dos, pal.body);
      ctx.restore();
      line(ctx, dos, shade(pal.body, 0.4), 0.3, 1.2);
      // Basques de livrée : deux pans qui pendent du bassin relevé, c'est ce qui dit « majordome ».
      for (const s of [0, 1]) {
        const bq = poly([P(cx + 20 + s * 7, base - 26), P(cx + 26 + s * 7, base - 26), P(cx + 23 + s * 7, base - 8), P(cx + 19 + s * 7, base - 10)]);
        fill(ctx, bq, shade(pal.body, -0.3)); line(ctx, bq, shade(pal.body, 0.3), 0.25, 1);
      }
      // Boutons alignés le long du dos : le protocole avant le corps.
      for (let i = 0; i < 4; i++) {
        ctx.beginPath(); ctx.arc(cx + 12 - i * 9, base - 46 + i * 0.6, 1.4, 0, Math.PI * 2);
        ctx.fillStyle = rgba(TOKEN.goldDim, 0.55); ctx.fill();
      }
      // La tête PEND vers le sol, sans visage : un ovale sombre sous la nuque, et rien dedans.
      const tt = new Path2D();
      tt.ellipse(cx - 30, base - 24, 8.5, 10, 0.15, 0, Math.PI * 2);
      fill(ctx, tt, shade(pal.body, -0.25));
      line(ctx, tt, shade(pal.body, 0.25), 0.3, 1.1);
      // Le col empesé, à la jonction nuque/tête : un mince liseré clair, jamais un visage.
      ctx.save();
      ctx.translate(cx - 27, base - 36); ctx.rotate(0.5);
      ctx.fillStyle = rgba(pal.light, 0.85);
      ctx.fillRect(-6, -1.6, 12, 3.2);
      ctx.restore();
      // Les mains gantées lissent le tapis, en boucle, même en combat. Seule valeur claire
      // du jeton, et elles sont AU SOL — c'est là que se lit la révérence forcée.
      seg(ctx, cx - 22, base - 40, cx - 34, base - 12, pal.body, 0.95, 5.5);
      seg(ctx, cx + 4, base - 40, cx - 2, base - 12, pal.body, 0.95, 5.5);
      for (const [gx, gy] of [[-36, -8], [-3, -8]]) {
        ctx.beginPath(); ctx.ellipse(cx + gx, base + gy, 5.4, 3.4, -0.15, 0, Math.PI * 2);
        ctx.fillStyle = rgba('#f0ecf4', 0.95); ctx.fill();
        ctx.strokeStyle = rgba('#9a94a8', 0.6); ctx.lineWidth = 0.8; ctx.stroke();
      }
    },
  },

  'porteur-plateau': {
    catalogKey: 'canon.enemy.porteur-plateau',
    name: 'Porteur de Plateau', side: 'enemy', role: 'support', family: 'veilleurs', rarity: 'common',
    registre: 'silence',
    quote: '« Thé ? Eau ? Attention ? »',
    silhouette: 'Buste seul, coupé net à la taille, flottant à hauteur constante.',
    pal: { body: '#1a1824', deep: '#0c0b12', accent: '#b8bcc8', light: '#e8e4ef' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      // Pas de contact au sol : il flotte à hauteur EXACTE de service. Juste une ombre lointaine.
      contact(ctx, cx, base, 15, 6, 0.3);
      const lift = 30;
      // COUPÉ NET À LA TAILLE : la section est franche, pas déchirée. Elle est nette et sombre.
      const bs = new Path2D();
      bs.moveTo(cx - 15, base - lift);
      bs.quadraticCurveTo(cx - 18, base - lift - 26, cx - 12, base - lift - 40);
      bs.lineTo(cx + 12, base - lift - 40);
      bs.quadraticCurveTo(cx + 18, base - lift - 26, cx + 15, base - lift);
      bs.closePath();
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.6); ctx.shadowBlur = 10;
      fill(ctx, bs, pal.body);
      ctx.restore();
      ctx.save(); ctx.clip(bs);
      for (let i = 0; i < 5; i++) seg(ctx, cx - 12 + i * 6, base - lift, cx - 10 + i * 6, base - lift - 40, shade(pal.body, 0.3), 0.16, 1.2);
      ctx.restore();
      // La section : une ellipse plus claire, comme une coupe d'anatomie propre. Rien ne pend.
      ctx.beginPath(); ctx.ellipse(cx, base - lift, 15, 4.6, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba(shade(pal.deep, 0.14), 1); ctx.fill();
      ctx.strokeStyle = rgba(pal.accent, 0.4); ctx.lineWidth = 1; ctx.stroke();
      for (let i = 0; i < 3; i++) {
        ctx.beginPath(); ctx.arc(cx, base - lift - 14 - i * 8, 1.5, 0, Math.PI * 2);
        ctx.fillStyle = rgba(TOKEN.goldDim, 0.55); ctx.fill();
      }
      const col = poly([P(cx - 9, base - lift - 40), P(cx - 6, base - lift - 46), P(cx + 6, base - lift - 46), P(cx + 9, base - lift - 40)]);
      fill(ctx, col, shade(pal.body, 0.2));
      ctx.fillStyle = rgba('#04050a', 0.95);
      ctx.beginPath(); ctx.ellipse(cx, base - lift - 44, 6, 4.4, 0, 0, Math.PI * 2); ctx.fill();
      // LE PLATEAU NE PENCHE JAMAIS : parfaitement horizontal, quoi qu'il arrive.
      const py = base - lift - 30;
      seg(ctx, cx + 10, base - lift - 34, cx + 22, py + 2, pal.body, 0.95, 4.5);
      ctx.beginPath(); ctx.ellipse(cx + 30, py, 20, 6.2, 0, 0, Math.PI * 2);
      const pg = ctx.createLinearGradient(cx + 10, py, cx + 50, py);
      pg.addColorStop(0, rgba(shade(pal.accent, 0.25), 1));
      pg.addColorStop(1, rgba(shade(pal.accent, -0.4), 1));
      ctx.fillStyle = pg; ctx.fill();
      ctx.strokeStyle = rgba(pal.light, 0.5); ctx.lineWidth = 1; ctx.stroke();
      // Trois tasses : la première FUME, la deuxième est VIDE, la troisième est RETOURNÉE.
      // Personne n'a jamais bu la troisième. Toujours lisibles dans cet ordre.
      const cup = (x, kind) => {
        if (kind === 'over') {
          ctx.beginPath(); ctx.ellipse(x, py - 2, 4.4, 2.6, 0, Math.PI, Math.PI * 2);
          ctx.fillStyle = rgba('#dcd8e4', 1); ctx.fill();
          ctx.strokeStyle = rgba('#8e88a0', 0.7); ctx.lineWidth = 0.8; ctx.stroke();
          return;
        }
        const c = poly([P(x - 4, py - 8), P(x + 4, py - 8), P(x + 3, py - 1), P(x - 3, py - 1)]);
        fill(ctx, c, '#e4e0ec'); line(ctx, c, '#8e88a0', 0.7, 0.8);
        if (kind === 'steam') {
          ctx.beginPath();
          ctx.moveTo(x, py - 9);
          ctx.quadraticCurveTo(x + 3, py - 15, x - 1, py - 21);
          ctx.strokeStyle = rgba(TOKEN.ink, 0.4); ctx.lineWidth = 1.4; ctx.stroke();
        }
      };
      cup(cx + 20, 'steam'); cup(cx + 30, 'empty'); cup(cx + 40, 'over');
    },
  },

  'echo-politesse': {
    catalogKey: 'canon.enemy.echo-politesse',
    name: 'Écho de Politesse', side: 'enemy', role: 'disruptor', family: 'veilleurs', rarity: 'common',
    registre: 'silence',
    quote: '« Après vous. Non — après vous. »',
    silhouette: 'Quasi absente : une distorsion, un pli. Le plus difficile à cadrer, et c’est le propos.',
    pal: { body: '#c3bfcc', deep: '#4a4658', accent: '#a8c0d8', light: '#e8e4ef' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 13, 5, 0.2);
      // Une brume EN FORME DE COURBETTE : la silhouette n'existe que par la déformation.
      // Elle est peinte en décalages chromatiques superposés, jamais en aplat.
      const courbette = (dx, dy, col, a, w) => {
        const p = new Path2D();
        p.moveTo(cx - 14 + dx, base - 4 + dy);
        p.quadraticCurveTo(cx - 18 + dx, base - 40 + dy, cx - 2 + dx, base - 52 + dy);
        p.quadraticCurveTo(cx + 16 + dx, base - 58 + dy, cx + 22 + dx, base - 40 + dy);
        ctx.strokeStyle = rgba(col, a); ctx.lineWidth = w; ctx.lineCap = 'round';
        ctx.stroke(p);
      };
      // Le décalage chromatique : trois passes légèrement désalignées, gris-violet.
      courbette(-2.5, 1, pal.accent, 0.22, 9);
      courbette(2.5, -1, pal.deep, 0.26, 9);
      courbette(0, 0, pal.body, 0.3, 8);
      // Le pli lui-même : un liseré clair, presque un reflet sur du vide.
      courbette(0, -2.5, pal.light, 0.35, 2);
      ctx.save();
      ctx.globalAlpha = 0.5;
      for (let i = 0; i < 26; i++) {
        const t = R();
        const px = cx - 14 + t * 38 + R2(R, -5, 5);
        const py = base - 6 - Math.sin(t * Math.PI) * 46 + R2(R, -6, 6);
        ctx.beginPath(); ctx.arc(px, py, R2(R, 0.6, 2), 0, Math.PI * 2);
        ctx.fillStyle = rgba(R() > 0.5 ? pal.light : pal.accent, R2(R, 0.08, 0.3)); ctx.fill();
      }
      ctx.restore();
      // Là où serait la tête, seulement un creux plus sombre : rien à regarder.
      const g = ctx.createRadialGradient(cx + 12, base - 48, 1, cx + 12, base - 48, 13);
      g.addColorStop(0, rgba(pal.deep, 0.3));
      g.addColorStop(1, rgba(pal.deep, 0));
      ctx.fillStyle = g;
      ctx.beginPath(); ctx.arc(cx + 12, base - 48, 13, 0, Math.PI * 2); ctx.fill();
    },
  },

  'sentinelle-seuil': {
    catalogKey: 'canon.enemy.sentinelle-seuil',
    name: 'Sentinelle du Seuil', side: 'enemy', role: 'bruiser', family: 'veilleurs', rarity: 'elite',
    registre: 'silence',
    quote: '« Le seuil a été souillé. Cela ne se pardonne pas. »',
    silhouette: 'Colonne. Strictement cylindrique, sans bras évidents, plus haute que tout.',
    pal: { body: '#c8c4cf', deep: '#6a6678', accent: TOKEN.frost, light: '#eae6f0' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 24, 9, 0.5);
      // Un pilier de marbre descendu de son socle. Strictement cylindrique : aucun membre.
      const p = poly([P(cx - 19, base), P(cx - 16, base - 108), P(cx + 16, base - 108), P(cx + 19, base)]);
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.6); ctx.shadowBlur = 12;
      const g = ctx.createLinearGradient(cx - 19, 0, cx + 19, 0);
      g.addColorStop(0, rgba(pal.deep, 1));
      g.addColorStop(0.32, rgba(pal.light, 1));
      g.addColorStop(0.7, rgba(pal.body, 1));
      g.addColorStop(1, rgba(shade(pal.deep, -0.2), 1));
      ctx.fillStyle = g; ctx.fill(p);
      ctx.restore();
      // Les veines bleu-violet : la Flamme froide DORT DEDANS. Elles pulsent sous la pierre.
      ctx.save(); ctx.clip(p);
      for (let i = 0; i < 7; i++) {
        const x0 = cx + R2(R, -15, 15);
        const pth = new Path2D();
        pth.moveTo(x0, base - 4);
        let yy = base - 4, xx = x0;
        while (yy > base - 106) {
          const ny = yy - R2(R, 14, 30), nx = xx + R2(R, -7, 7);
          pth.quadraticCurveTo(xx + R2(R, -4, 4), (yy + ny) / 2, nx, ny);
          yy = ny; xx = nx;
        }
        ctx.strokeStyle = rgba(pal.accent, R2(R, 0.22, 0.5));
        ctx.lineWidth = R2(R, 0.9, 2.4);
        ctx.shadowColor = rgba(pal.accent, 0.8); ctx.shadowBlur = 5;
        ctx.stroke(pth);
      }
      ctx.shadowBlur = 0;
      for (let i = 0; i < 5; i++) {
        const yy = base - R2(R, 10, 100);
        seg(ctx, cx - 20, yy, cx + 20, yy + R2(R, -3, 3), '#000000', R2(R, 0.08, 0.2), R2(R, 1, 2.4));
      }
      ctx.restore();
      // Chapiteau et base : la pierre garde la mémoire de son socle.
      for (const [yy, hh, ww] of [[base - 108, 8, 23], [base - 8, 8, 22]]) {
        const cap = poly([P(cx - ww, yy + hh), P(cx - ww + 3, yy), P(cx + ww - 3, yy), P(cx + ww, yy + hh)]);
        fill(ctx, cap, shade(pal.body, 0.1)); line(ctx, cap, pal.deep, 0.5, 1.2);
      }
      // Le sol s'essuie tout seul devant ses pas : une traînée INVERSÉE, plus propre que le reste.
      const tr = ctx.createLinearGradient(cx, base + 10, cx, base - 4);
      tr.addColorStop(0, rgba(pal.light, 0));
      tr.addColorStop(1, rgba(pal.light, 0.26));
      ctx.fillStyle = tr;
      ctx.beginPath(); ctx.ellipse(cx, base + 4, 30, 9, 0, 0, Math.PI * 2); ctx.fill();
      glowDot(ctx, cx - 6, base - 58, 2.2, pal.accent, 0.5);
    },
  },

  // ═══ 3.2 LES COPISTES — registre Mémoire ◈ ═══════════════════════════════════════════
  // Le papier, l'encre, l'acte d'écrire. Ils enregistrent le combat pendant qu'il a lieu.

  'copiste-aveugle': {
    catalogKey: 'canon.enemy.copiste-aveugle',
    name: 'Copiste Aveugle', side: 'enemy', role: 'disruptor', family: 'copistes', rarity: 'common',
    registre: 'memoire',
    quote: '« Je n’ai pas besoin de voir. Le texte se souvient pour moi. »',
    silhouette: 'Voûtée sur un parchemin flottant qui la prolonge à l’horizontale.',
    pal: { body: '#4a4234', deep: '#241f18', accent: TOKEN.blood, paper: '#ded0a8', light: TOKEN.gold },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 20, 8);
      // Le parchemin déroulé À MÊME L'AIR prolonge la silhouette à l'horizontale.
      const pc = new Path2D();
      pc.moveTo(cx + 2, base - 46);
      pc.quadraticCurveTo(cx + 30, base - 52, cx + 50, base - 42);
      pc.quadraticCurveTo(cx + 30, base - 34, cx + 2, base - 36);
      pc.closePath();
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.4); ctx.shadowBlur = 6;
      fill(ctx, pc, pal.paper, 0.94);
      ctx.restore();
      ctx.save(); ctx.clip(pc);
      // Il recopie tout ce qui se passe, en temps réel. Le texte se remplit en direct.
      faketext(ctx, R, cx + 6, base - 47, 40, 4, '#100c08', 0.55, 3.4);
      ctx.restore();
      line(ctx, pc, shade(pal.paper, -0.35), 0.5, 1);
      drape(ctx, R, cx, base, { top: base - 54, halfTop: 13, halfBot: 18, col: pal.body, deep: pal.deep, plis: 7, glow: pal.light });
      // Voûté : les épaules montent plus haut que la nuque, le dos fait une bosse.
      const dos = new Path2D();
      dos.moveTo(cx - 14, base - 50);
      dos.quadraticCurveTo(cx - 16, base - 74, cx + 2, base - 76);
      dos.quadraticCurveTo(cx + 16, base - 72, cx + 13, base - 50);
      dos.closePath();
      fill(ctx, dos, shade(pal.body, 0.06)); line(ctx, dos, pal.deep, 0.5, 1.2);
      // Les DOIGTS TERMINÉS PAR DES PLUMES, qui courent sur le parchemin.
      seg(ctx, cx + 4, base - 66, cx + 14, base - 48, pal.body, 0.95, 4.5);
      for (let i = -1; i <= 1; i++) {
        const a = -0.5 + i * 0.42;
        const fx = cx + 14 + Math.cos(a) * 12, fy = base - 48 + Math.sin(a) * 12;
        seg(ctx, cx + 14, base - 48, fx, fy, pal.paper, 0.85, 1.6);
        const pl = new Path2D();
        pl.moveTo(fx, fy);
        pl.quadraticCurveTo(fx + 5, fy + 2, fx + 9, fy + 7);
        pl.quadraticCurveTo(fx + 4, fy + 5, fx, fy + 3);
        pl.closePath();
        fill(ctx, pl, '#efe6d2', 0.9);
      }
      ctx.beginPath(); ctx.ellipse(cx + 2, base - 80, 8, 8.6, 0.1, 0, Math.PI * 2);
      ctx.fillStyle = rgba('#c9b696', 1); ctx.fill();
      // LES ORBITES SCELLÉES DE CIRE À CACHETER : deux disques rouges, en relief, avec sceau.
      for (const s of [-1, 1]) {
        const ex = cx + 2 + s * 3.6, ey = base - 81;
        ctx.beginPath(); ctx.arc(ex, ey, 3, 0, Math.PI * 2);
        ctx.fillStyle = rgba(pal.accent, 0.95); ctx.fill();
        ctx.strokeStyle = rgba(shade(pal.accent, -0.5), 0.9); ctx.lineWidth = 0.8; ctx.stroke();
        seg(ctx, ex - 1.4, ey - 1.4, ex + 1.4, ey + 1.4, shade(pal.accent, -0.5), 0.7, 0.7);
      }
    },
  },

  'encrier-vivant': {
    catalogKey: 'canon.enemy.encrier-vivant',
    name: 'Encrier Vivant', side: 'enemy', role: 'support', family: 'copistes', rarity: 'common',
    registre: 'memoire',
    quote: '« Il ne faut jamais, jamais manquer d’encre. »',
    silhouette: 'Humanoïde approximative, contour en verre, contenu liquide et mobile.',
    pal: { body: '#8a96a8', deep: '#3a4250', accent: '#02020a', light: '#dce4ee' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 20, 8);
      // Les flaques-mots laissées au sol : toujours les mêmes premières pièces du Palais.
      for (const [ox, oy, rr] of [[-22, 3, 9], [16, 5, 7], [-4, 7, 11]]) {
        ctx.beginPath(); ctx.ellipse(cx + ox, base + oy, rr, rr * 0.4, 0, 0, Math.PI * 2);
        ctx.fillStyle = rgba(pal.accent, 0.8); ctx.fill();
        faketext(ctx, R, cx + ox - rr * 0.6, base + oy - 1, rr * 1.2, 1, TOKEN.gold, 0.3, 3);
      }
      // Le corps de VERRE FÊLÉ : un contour clair, un intérieur presque vide de lumière.
      const bd = new Path2D();
      bd.moveTo(cx - 16, base - 2);
      bd.quadraticCurveTo(cx - 20, base - 34, cx - 10, base - 52);
      bd.quadraticCurveTo(cx, base - 60, cx + 10, base - 52);
      bd.quadraticCurveTo(cx + 20, base - 34, cx + 16, base - 2);
      bd.closePath();
      // L'ENCRE : le point le plus sombre du jeu. Elle remplit les deux tiers du volume.
      ctx.save();
      ctx.clip(bd);
      ctx.fillStyle = rgba(pal.accent, 1);
      ctx.beginPath();
      ctx.moveTo(cx - 22, base + 2);
      ctx.lineTo(cx - 22, base - 32);
      ctx.quadraticCurveTo(cx, base - 38, cx + 22, base - 30);
      ctx.lineTo(cx + 22, base + 2);
      ctx.closePath(); ctx.fill();
      // Reflet de surface : une seule ligne claire, c'est ce qui dit « liquide ».
      ctx.beginPath();
      ctx.moveTo(cx - 20, base - 32);
      ctx.quadraticCurveTo(cx, base - 38, cx + 20, base - 30);
      ctx.strokeStyle = rgba(pal.light, 0.4); ctx.lineWidth = 1.6; ctx.stroke();
      const g = ctx.createLinearGradient(cx - 20, 0, cx + 20, 0);
      g.addColorStop(0, rgba(pal.light, 0.3));
      g.addColorStop(0.3, rgba(pal.body, 0.14));
      g.addColorStop(1, rgba(pal.deep, 0.3));
      ctx.fillStyle = g; ctx.fillRect(cx - 24, base - 62, 48, 66);
      ctx.restore();
      line(ctx, bd, pal.light, 0.55, 1.6);
      // Les fêlures : franches, rayonnantes, avec un liseré clair. Le verre a déjà cédé.
      ctx.save(); ctx.clip(bd);
      for (let i = 0; i < 5; i++) {
        const sx = cx + R2(R, -14, 14), sy = base - R2(R, 8, 50);
        const pth = new Path2D();
        pth.moveTo(sx, sy);
        for (let j = 0; j < 3; j++) pth.lineTo(sx + R2(R, -11, 11), sy + R2(R, -13, 13));
        ctx.strokeStyle = rgba(pal.light, R2(R, 0.3, 0.6)); ctx.lineWidth = R2(R, 0.7, 1.3);
        ctx.stroke(pth);
      }
      ctx.restore();
      // La tête : un bulbe de verre, presque vide. Deux points d'encre en suspension.
      ctx.beginPath(); ctx.ellipse(cx, base - 60, 9, 8, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.body, 0.28); ctx.fill();
      ctx.strokeStyle = rgba(pal.light, 0.5); ctx.lineWidth = 1.3; ctx.stroke();
      for (const s of [-1, 1]) {
        ctx.beginPath(); ctx.arc(cx + s * 3.2, base - 60, 1.9, 0, Math.PI * 2);
        ctx.fillStyle = rgba(pal.accent, 0.95); ctx.fill();
      }
    },
  },

  'page-inachevee': {
    catalogKey: 'canon.enemy.page-inachevee',
    name: 'Page Inachevée', side: 'enemy', role: 'disruptor', family: 'copistes', rarity: 'uncommon',
    registre: 'memoire',
    quote: '« La phrase s’arrête ici. Vous aussi. »',
    silhouette: 'Plan vertical, presque 2D. Vue de profil, elle disparaît presque.',
    pal: { body: '#f0ead8', deep: '#a89c78', accent: '#100c08', light: '#ffffff' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 11, 4, 0.25);
      // Une feuille immense, flottante, DÉCHIRÉE À MI-HAUTEUR. Le contraste le plus violent
      // de la famille : blanc papier plein sur fond sombre.
      const top = base - 96, mid = base - 48;
      const upper = new Path2D();
      upper.moveTo(cx - 22, top + 4);
      upper.lineTo(cx + 21, top);
      upper.lineTo(cx + 22, mid - 2);
      // La déchirure : une ligne brisée, irrégulière, franche. Jamais une courbe douce.
      let dx = cx + 22;
      for (let i = 0; i < 9; i++) {
        dx -= 5;
        upper.lineTo(dx, mid + (i % 2 ? 5 : -4) + R2(R, -2, 2));
      }
      upper.closePath();
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.5); ctx.shadowBlur = 9;
      const g = ctx.createLinearGradient(cx - 22, 0, cx + 22, 0);
      g.addColorStop(0, rgba(pal.deep, 1));
      g.addColorStop(0.28, rgba(pal.light, 1));
      g.addColorStop(1, rgba(pal.body, 1));
      ctx.fillStyle = g; ctx.fill(upper);
      ctx.restore();
      // LE TEXTE S'INTERROMPT EN PLEIN MOT : les lignes se remplissent, la dernière casse net.
      ctx.save(); ctx.clip(upper);
      faketext(ctx, R, cx - 17, top + 12, 34, 7, pal.accent, 0.62, 5);
      // La ligne coupée : un mot commencé, un blanc, rien.
      ctx.fillStyle = rgba(pal.accent, 0.62);
      ctx.fillRect(cx - 17, top + 47, 9, 1.4);
      ctx.restore();
      line(ctx, upper, shade(pal.deep, -0.2), 0.4, 1);
      // La moitié basse manque : à sa place, quelques fibres qui pendent.
      for (let i = 0; i < 7; i++) {
        const fx = cx - 20 + i * 6.4;
        ctx.beginPath();
        ctx.moveTo(fx, mid + R2(R, -3, 4));
        ctx.quadraticCurveTo(fx + R2(R, -3, 3), mid + 9, fx + R2(R, -5, 5), mid + R2(R, 12, 22));
        ctx.strokeStyle = rgba(pal.body, R2(R, 0.3, 0.6)); ctx.lineWidth = R2(R, 0.7, 1.5);
        ctx.stroke();
      }
      // Elle se retourne pour éviter d'être vue de profil : une arête vive sur un bord.
      seg(ctx, cx + 21, top, cx + 22, mid - 2, pal.light, 0.8, 1.6);
    },
  },

  relieur: {
    catalogKey: 'canon.enemy.relieur',
    name: 'Le Relieur', side: 'enemy', role: 'bruiser', family: 'copistes', rarity: 'rare',
    registre: 'memoire',
    quote: '« Rien ne se termine tant que je n’ai pas cousu la dernière page. »',
    silhouette: 'La plus massive de la famille. Épaules d’artisan, tablier lourd, bras-aiguilles.',
    pal: { body: '#6a4c30', deep: '#2e2014', accent: TOKEN.bloodDim, steel: '#b0aebc', light: '#a8814e' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 27, 10, 0.5);
      for (const s of [-1, 1]) {
        paintMass(ctx, [P(cx + s * 6, base), P(cx + s * 5, base - 26), P(cx + s * 18, base - 26), P(cx + s * 20, base)], th, R,
          { base: pal.deep, rim: 0.1 });
      }
      // Épaules d'artisan : la masse est en HAUT. Le trapèze est inversé par rapport au Guard.
      paintMass(ctx, [P(cx - 16, base - 22), P(cx - 26, base - 62), P(cx - 18, base - 74),
        P(cx + 18, base - 74), P(cx + 26, base - 62), P(cx + 16, base - 22)], th, R,
      { base: pal.body, deep: pal.deep, rim: 0.16 });
      // Le tablier de cuir, lourd, qui tombe droit et couvre le ventre.
      const tb = poly([P(cx - 15, base - 4), P(cx - 17, base - 52), P(cx + 17, base - 52), P(cx + 15, base - 4)]);
      const tg = ctx.createLinearGradient(cx - 17, 0, cx + 17, 0);
      tg.addColorStop(0, rgba(shade(pal.body, 0.14), 1));
      tg.addColorStop(0.6, rgba(pal.body, 1));
      tg.addColorStop(1, rgba(pal.deep, 1));
      ctx.fillStyle = tg; ctx.fill(tb);
      line(ctx, tb, pal.deep, 0.6, 1.4);
      for (const s of [-1, 1]) seg(ctx, cx + s * 14, base - 52, cx + s * 8, base - 70, pal.light, 0.7, 2);
      // LES BRAS SE TERMINENT EN AIGUILLES COURBES ENFILÉES DE NERF.
      for (const [s, ay] of [[-1, -60], [1, -56]]) {
        const bx = cx + s * 22, by = base + ay;
        seg(ctx, cx + s * 17, base - 66, bx, by, shade(pal.body, -0.1), 0.95, 7);
        seg(ctx, bx, by, bx + s * 12, by + 16, shade(pal.body, -0.2), 0.95, 5.5);
        const nx = bx + s * 12, ny = by + 16;
        const ai = new Path2D();
        ai.moveTo(nx, ny);
        ai.quadraticCurveTo(nx + s * 18, ny + 8, nx + s * 16, ny + 24);
        ctx.strokeStyle = rgba(pal.steel, 0.95); ctx.lineWidth = 2.6; ctx.lineCap = 'round';
        ctx.stroke(ai);
        ctx.strokeStyle = rgba(pal.light, 0.5); ctx.lineWidth = 1; ctx.stroke(ai);
        // Le fil, rouge sombre, qui pend de l'aiguille : il reliera deux instants.
        const fil = new Path2D();
        fil.moveTo(nx + s * 16, ny + 24);
        fil.quadraticCurveTo(nx + s * 22, ny + 34, nx + s * 10, ny + 40);
        ctx.strokeStyle = rgba(pal.accent, 0.85); ctx.lineWidth = 1.4; ctx.stroke(fil);
      }
      ctx.beginPath(); ctx.ellipse(cx, base - 82, 9.4, 9, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba('#c9a683', 1); ctx.fill();
      seg(ctx, cx - 9, base - 88, cx + 9, base - 87, pal.deep, 0.9, 5);
      ctx.fillStyle = rgba('#2a1c10', 0.85); ctx.fillRect(cx - 7, base - 83, 14, 1.9);
      // Une bobine de fil à la ceinture : son métier avant sa personne.
      ctx.beginPath(); ctx.arc(cx - 12, base - 30, 4.4, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.accent, 0.9); ctx.fill();
      ctx.strokeStyle = rgba(pal.deep, 0.8); ctx.lineWidth = 1; ctx.stroke();
    },
  },

  // ═══ 3.3 LES SQUELETTES DE SOUVENIRS — registre Mémoire ◈ ════════════════════════════
  // L'os gris cendre, la gravure illisible, l'objet incongru. Des souvenirs que personne
  // n'a jamais racontés. Aucun blanc pur : gris cendre mat.

  'squelette-souvenir': {
    catalogKey: 'canon.enemy.squelette-souvenir',
    name: 'Squelette de Souvenir', side: 'enemy', role: 'skirmisher', family: 'squelettes', rarity: 'common',
    registre: 'memoire', variants: ['tasse', 'jouet', 'clef', 'chaussure', 'lettre'],
    quote: '« ... » (il n’a jamais été raconté ; il n’a pas de voix)',
    silhouette: 'Squelette humain classique, mais TOUJOURS tenant un objet du quotidien.',
    pal: { body: '#9a9690', deep: '#4e4a46', accent: TOKEN.gold, light: '#b8b4ae' },
    paint(k) {
      const { ctx, R, cx, base, pal, v } = k;
      contact(ctx, cx, base, 17, 7);
      const b = pal.body;
      // Jambes : appui décalé, il est en marche. Skirmisher, donc étroit et penché.
      bone(ctx, cx - 5, base - 30, cx - 9, base - 14, b); bone(ctx, cx - 9, base - 14, cx - 11, base, b);
      bone(ctx, cx + 4, base - 30, cx + 7, base - 15, b); bone(ctx, cx + 7, base - 15, cx + 10, base - 1, b);
      // Bassin et colonne, légèrement inclinés vers l'avant.
      const bs = poly([P(cx - 8, base - 30), P(cx + 8, base - 31), P(cx + 6, base - 38), P(cx - 7, base - 37)]);
      fill(ctx, bs, b); line(ctx, bs, pal.deep, 0.6, 1);
      bone(ctx, cx - 3, base - 38, cx - 1, base - 56, b, 4);
      // Cage thoracique : côtes lisibles une à une, c'est la lecture du jeton.
      for (let i = 0; i < 5; i++) {
        const yy = base - 40 - i * 4;
        const w = 11 - Math.abs(i - 2) * 1.6;
        ctx.beginPath();
        ctx.moveTo(cx - 2 - w, yy);
        ctx.quadraticCurveTo(cx - 2, yy - 4, cx - 2 + w, yy);
        ctx.strokeStyle = rgba(b, 0.95); ctx.lineWidth = 2.2; ctx.stroke();
      }
      bone(ctx, cx - 10, base - 56, cx - 20, base - 40, b, 3); bone(ctx, cx - 20, base - 40, cx - 22, base - 26, b, 2.8);
      skull(ctx, cx - 1, base - 62, 8.4, pal.light, 0);
      const sk = new Path2D(); sk.ellipse(cx - 1, base - 62, 8.4, 9, 0, 0, Math.PI * 2);
      // Les gravures : illisibles, mais manifestement volontaires. Elles captent une lueur d'or.
      ctx.save(); ctx.clip(sk);
      gravures(ctx, R, cx - 9, base - 70, 18, 16, pal.accent, 18);
      ctx.restore();
      gravures(ctx, R, cx - 14, base - 52, 24, 16, pal.accent, 16);
      // L'OBJET INCONGRU : le seul indice de ce qu'il fut. Il ne le lâche jamais.
      const ox = cx + 12, oy = base - 24;
      bone(ctx, cx + 9, base - 55, ox, oy - 5, b, 3);
      const kind = (this.variants || [])[v % 5] ?? 'tasse';
      if (kind === 'tasse') {
        const c = poly([P(ox - 5, oy - 8), P(ox + 5, oy - 8), P(ox + 4, oy), P(ox - 4, oy)]);
        fill(ctx, c, '#d8d2c4'); line(ctx, c, pal.deep, 0.6, 0.9);
        ctx.beginPath(); ctx.arc(ox + 7, oy - 4, 3.2, -1.2, 1.2);
        ctx.strokeStyle = rgba('#d8d2c4', 0.9); ctx.lineWidth = 1.4; ctx.stroke();
      } else if (kind === 'jouet') {
        ctx.beginPath(); ctx.arc(ox, oy - 5, 5.4, 0, Math.PI * 2);
        ctx.fillStyle = rgba('#b06a72', 0.9); ctx.fill();
        for (const s of [-1, 1]) {
          ctx.beginPath(); ctx.arc(ox + s * 4.4, oy - 10, 2.6, 0, Math.PI * 2);
          ctx.fillStyle = rgba('#b06a72', 0.9); ctx.fill();
        }
      } else if (kind === 'clef') {
        seg(ctx, ox, oy - 12, ox, oy + 1, TOKEN.goldDeep, 0.95, 2.2);
        ctx.beginPath(); ctx.arc(ox, oy - 14, 3.4, 0, Math.PI * 2);
        ctx.strokeStyle = rgba(TOKEN.goldDeep, 0.95); ctx.lineWidth = 1.8; ctx.stroke();
        seg(ctx, ox, oy - 2, ox + 5, oy - 2, TOKEN.goldDeep, 0.95, 1.8);
      } else if (kind === 'chaussure') {
        const c = new Path2D();
        c.moveTo(ox - 7, oy); c.lineTo(ox - 6, oy - 7); c.lineTo(ox, oy - 8);
        c.quadraticCurveTo(ox + 8, oy - 6, ox + 8, oy); c.closePath();
        fill(ctx, c, '#5c4636'); line(ctx, c, '#2a1e16', 0.7, 0.9);
      } else {
        const c = poly([P(ox - 7, oy - 6), P(ox + 7, oy - 7), P(ox + 7, oy + 1), P(ox - 7, oy + 2)]);
        fill(ctx, c, '#e0d8c0'); line(ctx, c, pal.deep, 0.6, 0.9);
        seg(ctx, ox - 7, oy - 6, ox, oy - 1, pal.deep, 0.5, 0.9);
        seg(ctx, ox + 7, oy - 7, ox, oy - 1, pal.deep, 0.5, 0.9);
      }
    },
  },

  'porteur-cendre': {
    catalogKey: 'canon.enemy.porteur-cendre',
    name: 'Porteur de Cendre', side: 'enemy', role: 'support', family: 'squelettes', rarity: 'uncommon',
    registre: 'memoire',
    quote: '« Je me souviens d’eux. C’est mon fardeau, et ma monnaie. »',
    silhouette: 'Courbée sous une charge qui la dépasse. La hotte est plus grande que le porteur.',
    pal: { body: '#5a5450', deep: '#28242a', accent: TOKEN.ember, ash: '#8e8a86', light: '#a8a29c' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 22, 8.5);
      // Démarche lourde : les jambes sont pliées, jamais tendues.
      for (const s of [-1, 1]) {
        paintMass(ctx, [P(cx + s * 5 - 3, base), P(cx + s * 4 - 3, base - 22), P(cx + s * 13 - 3, base - 22), P(cx + s * 14 - 3, base)], th, R,
          { base: pal.deep, rim: 0.08 });
      }
      // Courbé sous la charge : le corps fait un angle, la tête est plus basse que les épaules.
      const cp = new Path2D();
      cp.moveTo(cx - 14, base - 18);
      cp.quadraticCurveTo(cx - 22, base - 44, cx - 12, base - 58);
      cp.quadraticCurveTo(cx + 6, base - 64, cx + 12, base - 48);
      cp.quadraticCurveTo(cx + 14, base - 30, cx + 12, base - 18);
      cp.closePath();
      fill(ctx, cp, pal.body); line(ctx, cp, pal.deep, 0.5, 1.2);
      // LA HOTTE EST PLUS GRANDE QUE LE PORTEUR : elle le dépasse en hauteur et en largeur.
      const ht = new Path2D();
      ht.moveTo(cx - 4, base - 46);
      ht.lineTo(cx - 14, base - 96);
      ht.lineTo(cx + 30, base - 92);
      ht.lineTo(cx + 26, base - 40);
      ht.closePath();
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.6); ctx.shadowBlur = 10;
      fill(ctx, ht, shade(pal.body, -0.25));
      ctx.restore();
      ctx.save(); ctx.clip(ht);
      // Vannerie : trame croisée lâche, on voit à travers.
      for (let i = 0; i < 9; i++) seg(ctx, cx - 16, base - 94 + i * 6.4, cx + 32, base - 90 + i * 6.4, pal.light, 0.16, 1.2);
      for (let i = 0; i < 7; i++) seg(ctx, cx - 12 + i * 7, base - 96, cx - 4 + i * 7, base - 40, pal.light, 0.14, 1.2);
      // Cendre et ossements qui débordent, et les braises qui se rallument une à une.
      ctx.fillStyle = rgba(pal.ash, 0.85);
      ctx.beginPath();
      ctx.moveTo(cx - 14, base - 84);
      ctx.quadraticCurveTo(cx + 8, base - 96, cx + 30, base - 82);
      ctx.lineTo(cx + 28, base - 44); ctx.lineTo(cx - 10, base - 48);
      ctx.closePath(); ctx.fill();
      for (let i = 0; i < 5; i++) {
        bone(ctx, cx + R2(R, -8, 22), base - R2(R, 82, 94), cx + R2(R, -8, 24), base - R2(R, 76, 90), pal.light, 2.4);
      }
      ctx.restore();
      line(ctx, ht, pal.deep, 0.6, 1.4);
      for (let i = 0; i < 6; i++) {
        glowDot(ctx, cx + R2(R, -8, 26), base - R2(R, 48, 86), R2(R, 1, 2.2), pal.accent, R2(R, 0.35, 0.8));
      }
      // Sangles de portage, qui mordent l'épaule.
      for (const s of [-1, 1]) seg(ctx, cx - 8 + s * 4, base - 56, cx + 4 + s * 4, base - 44, pal.deep, 0.85, 3);
      // La silhouette encapuchonnée, tête basse : il se penche pour ramasser ce qui tombe.
      hood(ctx, cx - 14, base - 58, 9, 10, pal.body, pal.deep);
      seg(ctx, cx - 16, base - 44, cx - 24, base - 26, pal.body, 0.95, 4.5);
      ctx.beginPath(); ctx.ellipse(cx - 26, base - 22, 4.4, 3.2, 0.3, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.light, 0.9); ctx.fill();
    },
  },

  'choeur-muet': {
    catalogKey: 'canon.enemy.choeur-muet',
    name: 'Chœur Muet', side: 'enemy', role: 'disruptor', family: 'squelettes', rarity: 'rare',
    registre: 'memoire',
    quote: '« Ils chantent. Vous ne l’entendrez jamais. C’est ça, le supplice. »',
    silhouette: 'Triple. Trois crânes en éventail sur un buste unique — la plus identifiable.',
    pal: { body: '#9a9690', deep: '#4a4642', accent: '#c3bfcc', light: '#b8b4ae' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 24, 9, 0.45);
      // La vibration de l'air : aucun son ne sort, mais le silence pèse. Anneaux qui convergent.
      ctx.save();
      for (let i = 0; i < 4; i++) {
        ctx.beginPath();
        ctx.ellipse(cx, base - 52, 26 + i * 9, 30 + i * 10, 0, 0, Math.PI * 2);
        ctx.strokeStyle = rgba(pal.accent, 0.13 - i * 0.025); ctx.lineWidth = 1.4;
        ctx.stroke();
      }
      ctx.restore();
      // TROIS CAGES THORACIQUES FUSIONNÉES en un seul buste. La fusion doit être visible.
      const bs = new Path2D();
      bs.moveTo(cx - 24, base - 6);
      bs.quadraticCurveTo(cx - 28, base - 40, cx - 16, base - 58);
      bs.lineTo(cx + 16, base - 58);
      bs.quadraticCurveTo(cx + 28, base - 40, cx + 24, base - 6);
      bs.closePath();
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.55); ctx.shadowBlur = 10;
      fill(ctx, bs, pal.deep);
      ctx.restore();
      ctx.save(); ctx.clip(bs);
      for (const ox of [-14, 0, 14]) {
        for (let i = 0; i < 6; i++) {
          const yy = base - 14 - i * 7;
          const w = 10 - Math.abs(i - 2) * 1.2;
          ctx.beginPath();
          ctx.moveTo(cx + ox - w, yy);
          ctx.quadraticCurveTo(cx + ox, yy - 5, cx + ox + w, yy);
          ctx.strokeStyle = rgba(pal.body, 0.9); ctx.lineWidth = 2.4; ctx.stroke();
        }
        seg(ctx, cx + ox, base - 10, cx + ox, base - 56, pal.light, 0.6, 2.2);
      }
      ctx.restore();
      line(ctx, bs, pal.deep, 0.7, 1.4);
      // TROIS CRÂNES EN ÉVENTAIL, MÂCHOIRES GRANDES OUVERTES, JAMAIS FERMÉES.
      // L'intérieur des mâchoires est en noir absolu : c'est là que se tient le silence.
      const heads = [[-19, -66, -0.34, 8], [0, -74, 0, 9], [19, -66, 0.34, 8]];
      for (const [ox, oy, rot, r] of heads) {
        ctx.save();
        ctx.translate(cx + ox, base + oy);
        ctx.rotate(rot);
        seg(ctx, 0, r * 1.4, -ox * 0.35, 14, pal.light, 0.85, 3);
        skull(ctx, 0, 0, r, pal.light, r * 1.15);
        ctx.restore();
      }
    },
  },

  // ═══ 3.6 LES BLOUSES BLANCHES — registre Déni ◇ ══════════════════════════════════════
  // Le blanc amidonné, la propreté excessive, le vocabulaire de soin retourné en menace.
  // PERSONNE N'A D'YEUX VISIBLES.

  'infirmiere-deni': {
    catalogKey: 'canon.enemy.infirmiere-deni',
    name: 'Infirmière du Déni', side: 'enemy', role: 'disruptor', family: 'blouses', rarity: 'uncommon',
    registre: 'deni',
    quote: '« Vous n’avez pas mal. Regardez le dossier. »',
    silhouette: 'Verticale et nette, élargie par le chariot qu’elle pousse.',
    pal: { body: '#f0eef4', deep: '#a8a4b4', accent: TOKEN.goldDim, chrome: '#c8c8d2', light: '#ffffff' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 22, 8, 0.38);
      // Le chariot élargit la silhouette : c'est lui qui la rend reconnaissable, pas la blouse.
      const cw = 26, cy0 = base - 34;
      const ch = poly([P(cx + 10, cy0), P(cx + 10 + cw, cy0 - 3), P(cx + 10 + cw, cy0 + 4), P(cx + 10, cy0 + 6)]);
      fill(ctx, ch, pal.chrome); line(ctx, ch, pal.deep, 0.6, 1.1);
      for (const ox of [14, 32]) {
        seg(ctx, cx + ox, cy0 + 5, cx + ox + 1, base - 5, pal.deep, 0.85, 1.8);
        ctx.beginPath(); ctx.arc(cx + ox + 1, base - 3, 2.4, 0, Math.PI * 2);
        ctx.fillStyle = rgba(pal.deep, 0.95); ctx.fill();
      }
      // Les fioles : TOUTES IDENTIQUES, même étiquette illisible. C'est la répétition qui inquiète.
      for (let i = 0; i < 4; i++) {
        const fx = cx + 14 + i * 6.4;
        const f = poly([P(fx - 2.2, cy0 - 3), P(fx + 2.2, cy0 - 3.4), P(fx + 2, cy0 - 13), P(fx - 2, cy0 - 12.6)]);
        fill(ctx, f, pal.chrome, 0.55); line(ctx, f, pal.light, 0.6, 0.8);
        ctx.fillStyle = rgba(pal.accent, 0.8);
        ctx.fillRect(fx - 2, cy0 - 9, 4, 2.4);
      }
      // La blouse : verticale, nette, amidonnée. Aucun pli mou — que des arêtes.
      const bl = poly([P(cx - 13, base), P(cx - 11, base - 58), P(cx + 11, base - 58), P(cx + 13, base)]);
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.45); ctx.shadowBlur = 8;
      const g = ctx.createLinearGradient(cx - 13, 0, cx + 13, 0);
      g.addColorStop(0, rgba(pal.deep, 1));
      g.addColorStop(0.34, rgba(pal.light, 1));
      g.addColorStop(1, rgba(pal.body, 1));
      ctx.fillStyle = g; ctx.fill(bl);
      ctx.restore();
      ctx.save(); ctx.clip(bl);
      for (let i = 0; i < 4; i++) seg(ctx, cx - 9 + i * 6, base - 2, cx - 8 + i * 6, base - 56, pal.deep, 0.3, 1);
      ctx.restore();
      line(ctx, bl, pal.deep, 0.5, 1.1);
      // Le liseré ambre du registre Déni : une seule ligne, à la taille.
      seg(ctx, cx - 12, base - 34, cx + 12, base - 34, pal.accent, 0.7, 2);
      seg(ctx, cx + 11, base - 46, cx + 22, base - 36, pal.light, 0.95, 4);
      // LA COIFFE DESCEND TROP BAS POUR QU'ON VOIE LES YEUX. Le visage s'arrête au nez.
      ctx.beginPath(); ctx.ellipse(cx, base - 66, 7.4, 8.4, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba('#e0c4ac', 1); ctx.fill();
      const cf = new Path2D();
      cf.moveTo(cx - 10, base - 64);
      cf.quadraticCurveTo(cx - 11, base - 80, cx, base - 80);
      cf.quadraticCurveTo(cx + 11, base - 80, cx + 10, base - 64);
      cf.closePath();
      fill(ctx, cf, pal.light); line(ctx, cf, pal.deep, 0.5, 1.1);
      ctx.fillStyle = rgba(pal.accent, 0.75);
      ctx.fillRect(cx - 3.4, base - 76, 6.8, 1.6);
      ctx.fillRect(cx - 1.4, base - 78, 2.8, 5.6);
      // La bouche : douce, fermée. Sa voix est celle de Margot, en plus douce, ce qui est pire.
      ctx.fillStyle = rgba('#a8747a', 0.8);
      ctx.fillRect(cx - 2.6, base - 61, 5.2, 1.2);
    },
  },

  'souvenir-alite': {
    catalogKey: 'canon.enemy.souvenir-alite',
    name: 'Souvenir Alité', side: 'enemy', role: 'skirmisher', family: 'blouses', rarity: 'common',
    registre: 'deni',
    quote: '« Il attend une visite. Vous ferez l’affaire. »',
    silhouette: 'Mobilier, pas créature. Un lit sur roulettes, drap tendu formant une bosse humaine.',
    pal: { body: '#e8e6ee', deep: '#a09cae', accent: TOKEN.sap, chrome: '#c4c4d0', light: '#ffffff' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 30, 10, 0.42);
      // Un lit, pas un corps : la lecture est celle d'un meuble. Cadre chromé, roulettes.
      for (const ox of [-22, -8, 12, 26]) {
        seg(ctx, cx + ox, base - 22, cx + ox + 1, base - 4, pal.chrome, 0.9, 2);
        ctx.beginPath(); ctx.arc(cx + ox + 1, base - 3, 2.6, 0, Math.PI * 2);
        ctx.fillStyle = rgba(pal.deep, 0.95); ctx.fill();
      }
      // La table de chevet, avec les fleurs fanées qui se refont une jeunesse.
      const tb = poly([P(cx + 26, base - 30), P(cx + 44, base - 32), P(cx + 44, base - 26), P(cx + 26, base - 24)]);
      fill(ctx, tb, pal.chrome); line(ctx, tb, pal.deep, 0.6, 1);
      seg(ctx, cx + 34, base - 26, cx + 35, base - 6, pal.deep, 0.8, 2);
      for (let i = -1; i <= 1; i++) {
        const fx = cx + 35 + i * 3.4;
        ctx.beginPath();
        ctx.moveTo(fx, base - 32);
        ctx.quadraticCurveTo(fx + i * 2, base - 40, fx + i * 4, base - 44);
        ctx.strokeStyle = rgba(pal.accent, 0.55); ctx.lineWidth = 1.2; ctx.stroke();
        ctx.beginPath(); ctx.arc(fx + i * 4, base - 45, 2.4, 0, Math.PI * 2);
        ctx.fillStyle = rgba(mix(pal.accent, '#b08890', 0.5), 0.7); ctx.fill();
      }
      // Le sommier et le drap TENDU : les angles sont tirés au carré, c'est un lit fait.
      const mt = poly([P(cx - 26, base - 22), P(cx + 28, base - 26), P(cx + 28, base - 34), P(cx - 26, base - 30)]);
      fill(ctx, mt, pal.body); line(ctx, mt, pal.deep, 0.5, 1.1);
      // LA BOSSE HUMAINE : quelque chose respire dessous. Personne n'est dessous.
      const dr = new Path2D();
      dr.moveTo(cx - 26, base - 30);
      dr.quadraticCurveTo(cx - 14, base - 32, cx - 8, base - 44);
      dr.quadraticCurveTo(cx + 2, base - 52, cx + 12, base - 42);
      dr.quadraticCurveTo(cx + 20, base - 32, cx + 28, base - 34);
      dr.lineTo(cx + 28, base - 26);
      dr.lineTo(cx - 26, base - 22);
      dr.closePath();
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.4); ctx.shadowBlur = 8;
      const g = ctx.createLinearGradient(cx, base - 52, cx, base - 22);
      g.addColorStop(0, rgba(pal.light, 1));
      g.addColorStop(1, rgba(pal.deep, 1));
      ctx.fillStyle = g; ctx.fill(dr);
      ctx.restore();
      ctx.save(); ctx.clip(dr);
      for (let i = 0; i < 6; i++) {
        const px = cx - 22 + i * 9;
        ctx.beginPath();
        ctx.moveTo(px, base - 22);
        ctx.quadraticCurveTo(px + R2(R, -3, 3), base - 34, px + R2(R, -4, 4), base - 46);
        ctx.strokeStyle = rgba(pal.deep, R2(R, 0.18, 0.4)); ctx.lineWidth = R2(R, 0.8, 1.8);
        ctx.stroke();
      }
      ctx.restore();
      line(ctx, dr, pal.deep, 0.45, 1.1);
      // L'oreiller, creusé : une tête l'a marqué. Elle n'y est plus.
      const or = poly([P(cx - 30, base - 36), P(cx - 12, base - 40), P(cx - 10, base - 30), P(cx - 28, base - 27)]);
      fill(ctx, or, pal.light); line(ctx, or, pal.deep, 0.5, 1);
      ctx.beginPath(); ctx.ellipse(cx - 20, base - 34, 6, 3, -0.1, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.deep, 0.32); ctx.fill();
    },
  },

  'regisseur-blanc': {
    catalogKey: 'canon.enemy.regisseur-blanc',
    name: 'Régisseur des Couloirs Blancs', side: 'enemy', role: 'support', family: 'blouses', rarity: 'rare',
    registre: 'deni',
    quote: '« Les visites sont terminées. Elles l’ont toujours été. »',
    silhouette: 'Le plus haut de la famille, dos parfaitement droit, allongé par le trousseau.',
    pal: { body: '#eae8f0', deep: '#9a96a8', accent: TOKEN.goldDeep, light: '#ffffff' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 20, 8, 0.4);
      // DOS PARFAITEMENT DROIT : une seule verticale, du sol au col. Le plus haut de la famille.
      const bl = poly([P(cx - 13, base), P(cx - 11, base - 96), P(cx + 11, base - 96), P(cx + 13, base)]);
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.5); ctx.shadowBlur = 9;
      const g = ctx.createLinearGradient(cx - 13, 0, cx + 13, 0);
      g.addColorStop(0, rgba(pal.deep, 1));
      g.addColorStop(0.32, rgba(pal.light, 1));
      g.addColorStop(1, rgba(pal.body, 1));
      ctx.fillStyle = g; ctx.fill(bl);
      ctx.restore();
      ctx.save(); ctx.clip(bl);
      for (let i = 0; i < 3; i++) seg(ctx, cx - 6 + i * 6, base - 4, cx - 6 + i * 6, base - 90, pal.deep, 0.28, 1.1);
      ctx.restore();
      line(ctx, bl, pal.deep, 0.5, 1.1);
      seg(ctx, cx, base - 90, cx, base - 40, pal.deep, 0.3, 1.4);
      // LE TROUSSEAU PEND JUSQU'AU SOL : des dizaines de clefs, toutes différentes, toutes inutiles.
      // C'est lui qui allonge la silhouette. Il touche le sol, ce n'est pas négociable.
      const anchor = { x: cx + 12, y: base - 52 };
      seg(ctx, anchor.x, anchor.y, anchor.x + 4, anchor.y + 8, pal.accent, 0.9, 2);
      for (let i = 0; i < 16; i++) {
        const t = i / 15;
        const kx = anchor.x + 4 + Math.sin(i * 1.7) * 5 + t * 3;
        const ky = anchor.y + 8 + t * 44;
        seg(ctx, kx, ky, kx + R2(R, -1.5, 1.5), ky + R2(R, 5, 9), pal.accent, R2(R, 0.5, 0.9), R2(R, 1.1, 2));
        // Chaque clef a un panneton différent : aucune n'ouvre la même porte absente.
        const n = 1 + Math.floor(R() * 3);
        for (let j = 0; j < n; j++) seg(ctx, kx, ky + 6 + j * 1.8, kx + R2(R, 2, 4.4), ky + 6 + j * 1.8, pal.accent, 0.8, 1.1);
        ctx.beginPath(); ctx.arc(kx, ky, R2(R, 1.4, 2.4), 0, Math.PI * 2);
        ctx.strokeStyle = rgba(pal.accent, 0.85); ctx.lineWidth = 1; ctx.stroke();
      }
      // Il vérifie des serrures qui ne sont pas là : la main est tendue vers un mur nu.
      seg(ctx, cx - 11, base - 66, cx - 26, base - 58, pal.light, 0.95, 4.5);
      ctx.beginPath(); ctx.ellipse(cx - 28, base - 57, 4, 3.2, -0.2, 0, Math.PI * 2);
      ctx.fillStyle = rgba('#e0c4ac', 0.95); ctx.fill();
      const cl = poly([P(cx - 9, base - 96), P(cx - 6, base - 102), P(cx + 6, base - 102), P(cx + 9, base - 96)]);
      fill(ctx, cl, pal.light); line(ctx, cl, pal.deep, 0.5, 1);
      ctx.beginPath(); ctx.ellipse(cx, base - 108, 7, 8, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba('#dcc0a8', 1); ctx.fill();
      // Pas d'yeux visibles : le front est trop bas, l'ombre les mange.
      ctx.fillStyle = rgba('#0a0810', 0.6);
      ctx.fillRect(cx - 7, base - 112, 14, 4.4);
      ctx.fillStyle = rgba('#a8747a', 0.7);
      ctx.fillRect(cx - 2.4, base - 104, 4.8, 1.2);
    },
  },

  // ═══ 3.11 PRÉDATEURS — l'ombre qui chasse ════════════════════════════════════════════
  // Noir d'ombre, sans texture, gueule plus claire. L'intelligence doit se lire : elles se
  // coordonnent, se placent, attendent.

  voraces: {
    catalogKey: 'canon.enemy.voraces',
    name: 'Voraces', side: 'enemy', role: 'bruiser', family: 'predateurs', rarity: 'elite',
    registre: 'effroi', variants: ['petite', 'moyenne', 'grande'],
    quote: 'Elles dévorent les énergies. Intelligentes, elles chassent en meute.',
    silhouette: 'Bipède allongée, prédatrice. Trois échelles du même modèle, de 1,40 m à 3 m.',
    pal: { body: '#14121c', deep: '#05050a', accent: '#8e8a9c', light: '#c8c4d2' },
    paint(k) {
      const { ctx, R, cx, base, pal, v } = k;
      // Trois échelles du même modèle : c'est ce qui justifie visuellement la meute.
      const sc = [0.72, 0.88, 1.1][v % 3] ?? 0.88;
      contact(ctx, cx, base, 22 * sc, 8 * sc, 0.5);
      ctx.save();
      ctx.translate(cx, base);
      ctx.scale(sc, sc);
      // Noir d'ombre SANS TEXTURE : aucune striation, aucun grain. Une découpe.
      const bd = new Path2D();
      bd.moveTo(-6, 0);
      bd.quadraticCurveTo(-14, -26, -8, -46);
      bd.quadraticCurveTo(4, -60, 18, -56);
      bd.quadraticCurveTo(26, -50, 22, -38);
      bd.quadraticCurveTo(12, -32, 6, -20);
      bd.quadraticCurveTo(4, -8, 8, 0);
      bd.closePath();
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.7); ctx.shadowBlur = 12;
      ctx.fillStyle = rgba(pal.body, 1); ctx.fill(bd);
      ctx.restore();
      // Pattes arrière repliées haut : la détente est armée en permanence.
      for (const s of [-1, 1]) {
        const lg = new Path2D();
        lg.moveTo(s * 3 - 2, -22);
        lg.quadraticCurveTo(s * 14 - 2, -30, s * 12 - 2, -14);
        lg.quadraticCurveTo(s * 10 - 2, -4, s * 16 - 2, 0);
        ctx.strokeStyle = rgba(pal.body, 1); ctx.lineWidth = 5; ctx.lineCap = 'round';
        ctx.stroke(lg);
      }
      // Bras longs, griffes vers le sol.
      const arm = new Path2D();
      arm.moveTo(12, -48); arm.quadraticCurveTo(30, -40, 26, -20);
      ctx.strokeStyle = rgba(pal.body, 1); ctx.lineWidth = 4.4; ctx.stroke(arm);
      for (let i = -1; i <= 1; i++) {
        seg(ctx, 26, -20, 28 + i * 4, -10 + Math.abs(i) * 2, pal.accent, 0.8, 1.6);
      }
      // La GUEULE PLUS CLAIRE : le seul endroit du corps qui capte la lumière.
      const hd = new Path2D();
      hd.moveTo(10, -58);
      hd.quadraticCurveTo(28, -62, 34, -52);
      hd.quadraticCurveTo(26, -46, 12, -48);
      hd.closePath();
      ctx.fillStyle = rgba(pal.body, 1); ctx.fill(hd);
      const gu = new Path2D();
      gu.moveTo(20, -54); gu.quadraticCurveTo(30, -56, 34, -52);
      gu.quadraticCurveTo(28, -48, 20, -50);
      gu.closePath();
      fill(ctx, gu, pal.light, 0.85);
      for (let i = 0; i < 5; i++) {
        seg(ctx, 22 + i * 2.6, -54, 22.6 + i * 2.6, -50, '#f0ecf4', 0.9, 1);
      }
      // L'intelligence se lit dans le regard : une fente, orientée, qui évalue.
      ctx.fillStyle = rgba(pal.light, 0.9);
      ctx.beginPath(); ctx.ellipse(18, -57, 2.6, 1.2, -0.2, 0, Math.PI * 2); ctx.fill();
      ctx.restore();
    },
  },

  lamiz: {
    catalogKey: 'canon.enemy.lamiz',
    name: 'Lamiz', side: 'enemy', role: 'swarm', family: 'predateurs', rarity: 'common',
    registre: 'effroi',
    quote: 'Là où l’une apparaît, les autres suivent.',
    silhouette: 'Petite, quadrupède, JAMAIS SEULE : conçue directement comme un groupe de 3 à 5.',
    pal: { body: '#191723', deep: '#08080e', accent: '#6a6a86', light: '#a8a4bc' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      // Jamais seule : le jeton EST un groupe. Quatre individus, décalés en profondeur.
      const pack = [[-20, -2, 0.78, 0], [6, -6, 0.9, 1], [22, 2, 0.72, 2], [-4, 4, 1, 3]];
      contact(ctx, cx, base, 30, 10, 0.45);
      for (const [ox, oy, sc, i] of pack) {
        ctx.save();
        ctx.translate(cx + ox, base + oy);
        ctx.scale(sc, sc);
        contact(ctx, 0, 1, 12, 4.4, 0.35);
        // Reflets huileux distincts par individu : c'est ce qui les sépare dans la masse.
        const oil = [pal.accent, '#5c5a78', '#7a6a86', '#4e5a72'][i % 4];
        const bd = new Path2D();
        bd.moveTo(-13, -6);
        bd.quadraticCurveTo(-10, -18, 2, -18);
        bd.quadraticCurveTo(14, -18, 16, -8);
        bd.quadraticCurveTo(14, -2, 0, -3);
        bd.quadraticCurveTo(-10, -2, -13, -6);
        bd.closePath();
        ctx.save();
        ctx.shadowColor = rgba('#000000', 0.6); ctx.shadowBlur = 7;
        ctx.fillStyle = rgba(pal.body, 1); ctx.fill(bd);
        ctx.restore();
        ctx.save(); ctx.clip(bd);
        const g = ctx.createLinearGradient(-13, -18, 16, -2);
        g.addColorStop(0, rgba(oil, 0.4));
        g.addColorStop(0.5, rgba(oil, 0.08));
        g.addColorStop(1, rgba(oil, 0.3));
        ctx.fillStyle = g; ctx.fillRect(-16, -20, 34, 20);
        ctx.restore();
        for (const lx of [-9, -2, 6, 12]) {
          seg(ctx, lx, -5, lx + R2(R, -2, 2), 1, pal.body, 1, 2.6);
        }
        // Museau bas, tendu vers l'avant : elles suivent une piste, pas une cible.
        const hd = new Path2D();
        hd.moveTo(12, -14); hd.quadraticCurveTo(24, -13, 25, -7);
        hd.quadraticCurveTo(18, -4, 13, -7); hd.closePath();
        ctx.fillStyle = rgba(pal.body, 1); ctx.fill(hd);
        ctx.fillStyle = rgba(pal.light, 0.85);
        ctx.beginPath(); ctx.ellipse(18, -11, 1.8, 0.9, -0.15, 0, Math.PI * 2); ctx.fill();
        seg(ctx, 22, -8, 25, -7, '#e8e4f0', 0.7, 0.9);
        ctx.restore();
      }
    },
  },

  uguiro: {
    catalogKey: 'canon.enemy.uguiro',
    name: 'Uguiro', side: 'enemy', role: 'bruiser', family: 'predateurs', rarity: 'elite',
    registre: 'effroi',
    quote: 'Un monstre des profondeurs du Palais. Lent à se révéler, terrible une fois éveillé.',
    silhouette: 'Masse au repos, indistincte. Au repos, on ne doit pas comprendre ce que c’est.',
    pal: { body: '#101420', deep: '#04060c', accent: '#3c5a6a', light: '#7a98a8' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 32, 11, 0.55);
      // ÉTAT DE REPOS : une masse. Aucune tête, aucun membre lisible. On ne comprend pas.
      const ms = new Path2D();
      ms.moveTo(cx - 32, base + 2);
      ms.quadraticCurveTo(cx - 36, base - 24, cx - 16, base - 38);
      ms.quadraticCurveTo(cx + 6, base - 48, cx + 24, base - 36);
      ms.quadraticCurveTo(cx + 36, base - 24, cx + 30, base + 2);
      ms.closePath();
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.72); ctx.shadowBlur = 14;
      const g = ctx.createRadialGradient(cx - 8, base - 34, 4, cx, base - 10, 44);
      g.addColorStop(0, rgba(shade(pal.body, 0.18), 1));
      g.addColorStop(1, rgba(pal.deep, 1));
      ctx.fillStyle = g; ctx.fill(ms);
      ctx.restore();
      // L'humidité des profondeurs : des luisances, pas des textures. Elles suivent la forme.
      ctx.save(); ctx.clip(ms);
      for (let i = 0; i < 9; i++) {
        const a = R() * Math.PI - Math.PI;
        const rr = R2(R, 8, 30);
        ctx.beginPath();
        ctx.ellipse(cx + Math.cos(a) * rr, base - 20 + Math.sin(a) * rr * 0.55, R2(R, 3, 9), R2(R, 1, 3), a, 0, Math.PI * 2);
        ctx.fillStyle = rgba(pal.light, R2(R, 0.05, 0.16)); ctx.fill();
      }
      // Des plis serrés : quelque chose est replié là-dedans, très compact.
      for (let i = 0; i < 6; i++) {
        const yy = base - 6 - i * 6;
        ctx.beginPath();
        ctx.moveTo(cx - 30, yy);
        ctx.quadraticCurveTo(cx, yy - R2(R, 4, 10), cx + 28, yy + R2(R, -3, 3));
        ctx.strokeStyle = rgba('#000000', R2(R, 0.3, 0.55)); ctx.lineWidth = R2(R, 1.4, 3);
        ctx.stroke();
      }
      ctx.restore();
      // Le seul indice qu'il y a un être : une paire de luisances, très basses, très écartées.
      // Le réveil est l'événement — au repos, il ne se passe rien d'autre.
      for (const s of [-1, 1]) glowDot(ctx, cx + s * 13, base - 30, 1.6, pal.accent, 0.4);
    },
  },

  'fossoyeur-pale': {
    catalogKey: 'canon.enemy.fossoyeur-pale',
    name: 'Le Fossoyeur pâle', side: 'enemy', role: 'skirmisher', family: 'predateurs', rarity: 'common',
    registre: 'effroi',
    quote: 'Il creuse avant même que tu sois tombé.',
    silhouette: 'Maigre, haute, penchée sur son ouvrage, avec un outil de creusement.',
    pal: { body: '#3a3640', deep: '#1a1820', accent: '#d8d0c0', skin: '#c8c0b0', light: '#8a8290' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 19, 7.5);
      // La tombe qu'il creuse, en cours, sur la case : un trou et un tas de terre.
      ctx.fillStyle = rgba('#0a0810', 0.85);
      ctx.beginPath(); ctx.ellipse(cx + 22, base + 2, 13, 5.4, 0.1, 0, Math.PI * 2); ctx.fill();
      ctx.fillStyle = rgba('#2e2620', 0.9);
      ctx.beginPath(); ctx.ellipse(cx + 36, base - 1, 9, 4.4, 0, Math.PI, Math.PI * 2); ctx.fill();
      // Maigre et haute : les membres sont longs, l'épaisseur minimale. Penchée sur l'ouvrage.
      for (const s of [-1, 1]) {
        paintMass(ctx, [P(cx + s * 4 - 4, base), P(cx + s * 3 - 4, base - 34), P(cx + s * 9 - 4, base - 34), P(cx + s * 10 - 4, base)], th, R,
          { base: pal.deep, rim: 0.08 });
      }
      const bd = new Path2D();
      bd.moveTo(cx - 11, base - 32);
      bd.quadraticCurveTo(cx - 16, base - 56, cx - 4, base - 70);
      bd.quadraticCurveTo(cx + 10, base - 74, cx + 12, base - 58);
      bd.quadraticCurveTo(cx + 11, base - 40, cx + 8, base - 32);
      bd.closePath();
      fill(ctx, bd, pal.body); line(ctx, bd, pal.light, 0.3, 1.2);
      // La pelle : longue, tenue à deux mains, déjà en terre. Il ne s'interrompt pas.
      seg(ctx, cx + 4, base - 62, cx + 30, base - 6, '#4e3c28', 0.95, 3.4);
      const pe = new Path2D();
      pe.moveTo(cx + 26, base - 14);
      pe.quadraticCurveTo(cx + 38, base - 10, cx + 34, base + 3);
      pe.quadraticCurveTo(cx + 26, base + 2, cx + 22, base - 6);
      pe.closePath();
      fill(ctx, pe, '#9a96a4'); line(ctx, pe, '#3a3640', 0.7, 1.1);
      seg(ctx, cx - 2, base - 56, cx + 12, base - 44, pal.skin, 0.9, 3.4);
      seg(ctx, cx + 6, base - 48, cx + 20, base - 28, pal.skin, 0.9, 3.4);
      // Pâleur cireuse : la tête est la valeur la plus claire, et elle est baissée.
      ctx.beginPath(); ctx.ellipse(cx - 2, base - 76, 7.4, 8.4, 0.2, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.skin, 1); ctx.fill();
      ctx.save();
      ctx.beginPath(); ctx.ellipse(cx - 2, base - 76, 7.4, 8.4, 0.2, 0, Math.PI * 2); ctx.clip();
      ctx.fillStyle = rgba(pal.deep, 0.55);
      ctx.fillRect(cx - 11, base - 78, 18, 5);
      ctx.restore();
      for (const s of [-1, 1]) {
        ctx.beginPath(); ctx.arc(cx - 2 + s * 3.2, base - 76, 1.3, 0, Math.PI * 2);
        ctx.fillStyle = rgba(pal.accent, 0.85); ctx.fill();
      }
    },
  },

  // ═══ 3.6 ÉCHOS D'ÉMOTIONS — la forme d'un sentiment qui a survécu à son porteur ══════
  // Presque pas de corps : un contour, une posture, et la couleur de son registre. Ce sont
  // les seules figures du bestiaire où la couleur EST l'anatomie.

  'echo-colere': {
    catalogKey: 'canon.enemy.echo-colere',
    name: 'Écho de Colère', side: 'enemy', role: 'bruiser', family: 'echos', rarity: 'uncommon',
    registre: 'rupture',
    quote: '« Il ne sait plus contre qui. Cela ne l’arrête pas. »',
    silhouette: 'Haute, épaules énormes, poings serrés bas. Le contour est déchiré, pas flou.',
    pal: { body: '#2a1e28', deep: '#100a10', accent: '#d1662c', light: '#e88a4a' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 22, 8.5, 0.5);
      // Le corps est un vide sombre : ce qui se lit, c'est la déchirure de son contour.
      const bd = new Path2D();
      bd.moveTo(cx - 12, base);
      bd.lineTo(cx - 22, base - 40);
      bd.lineTo(cx - 30, base - 52);
      bd.lineTo(cx - 8, base - 60);
      bd.lineTo(cx - 2, base - 72);
      bd.lineTo(cx + 8, base - 60);
      bd.lineTo(cx + 28, base - 54);
      bd.lineTo(cx + 20, base - 38);
      bd.lineTo(cx + 12, base);
      bd.closePath();
      ctx.save();
      ctx.shadowColor = rgba(pal.accent, 0.35); ctx.shadowBlur = 14;
      ctx.fillStyle = rgba(pal.body, 0.96); ctx.fill(bd);
      ctx.restore();
      // Les fractures internes : de la lumière chaude qui sort par les cassures.
      ctx.save(); ctx.clip(bd);
      for (let i = 0; i < 11; i++) {
        const x0 = cx + R2(R, -18, 18), y0 = base - 10 - R() * 50;
        const p = new Path2D();
        p.moveTo(x0, y0);
        p.lineTo(x0 + R2(R, -8, 8), y0 - R2(R, 6, 18));
        p.lineTo(x0 + R2(R, -12, 12), y0 - R2(R, 20, 34));
        line(ctx, p, R() > 0.4 ? pal.accent : pal.light, R2(R, 0.3, 0.8), R2(R, 1, 2.6));
      }
      ctx.restore();
      line(ctx, bd, pal.accent, 0.55, 1.4);
      // Les poings : serrés, très bas, plus clairs que tout le reste.
      for (const s of [-1, 1]) {
        ctx.beginPath(); ctx.arc(cx + s * 20, base - 24, 5.4, 0, Math.PI * 2);
        ctx.fillStyle = rgba(pal.body, 1); ctx.fill();
        ctx.strokeStyle = rgba(pal.light, 0.6); ctx.lineWidth = 1.3; ctx.stroke();
      }
      // Pas de visage : une fente horizontale, ouverte, à la place de la mâchoire.
      ctx.fillStyle = rgba(pal.light, 0.85);
      ctx.fillRect(cx - 7, base - 66, 14, 2.6);
      glowDot(ctx, cx, base - 65, 1.6, pal.accent, 0.5);
    },
  },


  // ═══ COPISTES · suite ════════════════════════════════════════════════════════════════

  relieur: {
    catalogKey: 'canon.enemy.relieur',
    name: 'Le Relieur', side: 'enemy', role: 'bruiser', family: 'copistes', rarity: 'rare',
    registre: 'memoire',
    quote: '« Rien ne se termine tant que je n’ai pas cousu la dernière page. »',
    silhouette: 'Massif, tablier de cuir. Les avant-bras se terminent en aiguilles courbes.',
    pal: { body: '#4a3a2e', deep: '#1a1410', accent: TOKEN.gold, light: '#c8a878', steel: '#b8bcc8' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 24, 9, 0.56);
      for (const s of [-1, 1]) {
        paintMass(ctx, [P(cx + s * 6 - 4, base), P(cx + s * 5 - 4, base - 26), P(cx + s * 14 - 4, base - 26), P(cx + s * 15 - 4, base)], th, R,
          { base: pal.deep, rim: 0.1 });
      }
      const bd = new Path2D();
      bd.moveTo(cx - 20, base - 22);
      bd.quadraticCurveTo(cx - 26, base - 58, cx - 6, base - 70);
      bd.quadraticCurveTo(cx + 18, base - 72, cx + 22, base - 52);
      bd.quadraticCurveTo(cx + 20, base - 30, cx + 16, base - 22);
      bd.closePath();
      fill(ctx, bd, pal.body); line(ctx, bd, pal.deep, 0.6, 1.3);
      // Le tablier de cuir : une surface pleine, plus claire, qui porte tous les outils.
      const ap = new Path2D();
      ap.moveTo(cx - 15, base - 58); ap.lineTo(cx + 15, base - 58);
      ap.lineTo(cx + 18, base - 12); ap.lineTo(cx - 18, base - 12);
      ap.closePath();
      fill(ctx, ap, shade(pal.body, 0.16)); line(ctx, ap, pal.light, 0.3, 1.2);
      for (let i = 0; i < 5; i++) {
        seg(ctx, cx - 12 + i * 6, base - 54, cx - 12 + i * 6, base - 44, pal.steel, 0.45, 1.4);
      }
      // LES AIGUILLES : les avant-bras eux-mêmes. Courbes, enfilées de nerf qui pend.
      for (const [s, ay] of [[-1, -46], [1, -52]]) {
        const ar = new Path2D();
        ar.moveTo(cx + s * 14, base - 60);
        ar.quadraticCurveTo(cx + s * 28, base + ay - 4, cx + s * 26, base + ay + 10);
        line(ctx, ar, pal.body, 1, 5.2);
        const nd = new Path2D();
        nd.moveTo(cx + s * 26, base + ay + 10);
        nd.quadraticCurveTo(cx + s * 36, base + ay + 14, cx + s * 34, base + ay + 26);
        line(ctx, nd, pal.steel, 0.95, 2.4);
        line(ctx, nd, '#ffffff', 0.35, 0.9);
        const th2 = new Path2D();
        th2.moveTo(cx + s * 34, base + ay + 26);
        th2.quadraticCurveTo(cx + s * 30, base + ay + 40, cx + s * 33, base + ay + 54);
        line(ctx, th2, '#c8b8a0', 0.5, 1.1);
      }
      // La couture entre deux instants : un fil tendu en travers du torse, points réguliers.
      for (let i = 0; i < 7; i++) {
        const sx = cx - 16 + i * 5.4;
        seg(ctx, sx, base - 40 + (i % 2) * 3, sx + 3.4, base - 37 + (i % 2) * 3, pal.accent, 0.55, 1.2);
      }
      // La tête : basse, penchée sur l'ouvrage. Une loupe d'artisan à la place d'un œil.
      ctx.beginPath(); ctx.ellipse(cx + 4, base - 76, 8, 8.4, 0.16, 0, Math.PI * 2);
      ctx.fillStyle = rgba(shade(pal.body, 0.12), 1); ctx.fill();
      ctx.beginPath(); ctx.arc(cx + 8, base - 77, 4, 0, Math.PI * 2);
      ctx.strokeStyle = rgba(pal.steel, 0.8); ctx.lineWidth = 1.4; ctx.stroke();
      ctx.fillStyle = rgba('#0a0810', 0.7); ctx.fill();
      glowDot(ctx, cx + 8, base - 77, 1.3, pal.accent, 0.6);
    },
  },

  // ═══ BRUME · suite ═══════════════════════════════════════════════════════════════════

  'ombres-tentaculaires': {
    catalogKey: 'canon.enemy.ombres-tentaculaires',
    name: 'Ombres tentaculaires', side: 'enemy', role: 'disruptor', family: 'brume', rarity: 'common',
    registre: 'silence',
    quote: '« Des rats grands comme des chiens, des serpents à pattes — ce ne sont que ses bras. »',
    silhouette: 'Un corps bas, presque absent. Quatre bras qui montent hors du cadre.',
    pal: { body: '#2a2c3e', deep: '#0e1018', accent: '#c3bfcc', light: '#8f8ca4' },
    paint(k) {
      const { ctx, R, cx, base, p, pal } = k;
      ctx.fillStyle = rgba(pal.body, 0.2);
      ctx.beginPath(); ctx.ellipse(cx, base - 1, 26, 9, 0, 0, Math.PI * 2); ctx.fill();
      // Les bras d'abord : ils s'étirent jusqu'aux toits, donc ils SORTENT du jeton par le haut.
      for (const [ox, sw, ln, w] of [[-18, -1, 96, 5.4], [-5, 1, 118, 4.2], [9, -1, 104, 4.8], [22, 1, 78, 3.4]]) {
        const t = new Path2D();
        t.moveTo(cx + ox, base - 8);
        t.quadraticCurveTo(cx + ox + sw * 22, base - ln * 0.45, cx + ox + sw * 6, base - ln * 0.78);
        t.quadraticCurveTo(cx + ox - sw * 14, base - ln, cx + ox + sw * 10, base - ln - 14);
        ctx.strokeStyle = rgba(pal.body, 0.9); ctx.lineWidth = w; ctx.lineCap = 'round';
        ctx.stroke(t);
        ctx.strokeStyle = rgba(pal.light, 0.22); ctx.lineWidth = w * 0.35; ctx.stroke(t);
      }
      // Le corps : à peine plus dense que la brume. Il n'y a rien à frapper de solide.
      for (let i = 0; i < 6; i++) {
        const g = ctx.createRadialGradient(cx + R2(R, -6, 6), base - 8 - i * 4, 1, cx, base - 10 - i * 4, 20 - i * 2);
        g.addColorStop(0, rgba(pal.body, 0.42 - i * 0.05));
        g.addColorStop(1, rgba(pal.body, 0));
        ctx.fillStyle = g;
        ctx.beginPath(); ctx.ellipse(cx, base - 8 - i * 4, 20 - i * 2, 9 - i, 0, 0, Math.PI * 2); ctx.fill();
      }
      // Le contour instable du Disruptor : trois répliques décalées du même profil bas.
      for (let i = 1; i <= 3; i++) {
        ctx.save(); ctx.globalAlpha = 0.12;
        ctx.strokeStyle = rgba(pal.accent, 1); ctx.lineWidth = 1.2;
        ctx.beginPath(); ctx.ellipse(cx + i * 3, base - 14, 18, 11, 0, 0, Math.PI * 2); ctx.stroke();
        ctx.restore();
      }
      for (const s of [-1, 1]) glowDot(ctx, cx + s * 5, base - 20, 1.1, pal.accent, 0.3);
    },
  },

  // ═══ LITUISME · suite ════════════════════════════════════════════════════════════════

  'oeil-du-visionnaire': {
    catalogKey: 'canon.enemy.oeil-du-visionnaire',
    name: 'L’Œil du Visionnaire animé', side: 'enemy', role: 'disruptor', family: 'lituisme', rarity: 'elite',
    registre: 'memoire',
    quote: '« Il vous voit avant que vous ne le voyiez. »',
    silhouette: 'Presque plat : un symbole qui rampe sur les pavés. Rien ne se dresse.',
    pal: { body: '#221c2c', deep: '#0a0810', accent: '#a86fd8', light: '#e8c94a' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      // Rampant : la figure est peinte DANS le plan du sol, pas debout. Le seul jeton du
      // bestiaire qui n'a pas de hauteur — c'est ce qui le rend impossible à confondre.
      ctx.save();
      ctx.translate(cx, base - 8); ctx.scale(1, 0.46);
      const halo = ctx.createRadialGradient(0, 0, 2, 0, 0, 38);
      halo.addColorStop(0, rgba(pal.accent, 0.3));
      halo.addColorStop(1, rgba(pal.accent, 0));
      ctx.fillStyle = halo;
      ctx.beginPath(); ctx.arc(0, 0, 38, 0, Math.PI * 2); ctx.fill();
      // L'amande : deux arcs, tracés à l'encre. Trop régulière pour avoir poussé là.
      const al = new Path2D();
      al.moveTo(-30, 0);
      al.quadraticCurveTo(0, -26, 30, 0);
      al.quadraticCurveTo(0, 26, -30, 0);
      al.closePath();
      ctx.fillStyle = rgba(pal.deep, 0.92); ctx.fill(al);
      ctx.strokeStyle = rgba(pal.light, 0.7); ctx.lineWidth = 2.2; ctx.stroke(al);
      const ir = ctx.createRadialGradient(0, 0, 1, 0, 0, 15);
      ir.addColorStop(0, rgba(pal.light, 0.95));
      ir.addColorStop(0.45, rgba(pal.accent, 0.9));
      ir.addColorStop(1, rgba('#2a1440', 1));
      ctx.fillStyle = ir;
      ctx.beginPath(); ctx.arc(0, 0, 15, 0, Math.PI * 2); ctx.fill();
      ctx.fillStyle = rgba('#05040a', 0.95);
      ctx.beginPath(); ctx.ellipse(0, 0, 4.4, 12, 0, 0, Math.PI * 2); ctx.fill();
      ctx.restore();
      // Les flammes qui le font ramper : elles bougent, l'œil suit. Basses, au ras du sol.
      for (let i = 0; i < 9; i++) {
        const a = R() * Math.PI * 2, rr = R2(R, 26, 42);
        const fx = cx + Math.cos(a) * rr, fy = base - 6 + Math.sin(a) * rr * 0.4;
        const fp = new Path2D();
        fp.moveTo(fx - 3, fy);
        fp.quadraticCurveTo(fx, fy - R2(R, 5, 12), fx + 3, fy);
        ctx.fillStyle = rgba(i % 3 ? pal.light : pal.accent, R2(R, 0.12, 0.3));
        ctx.fill(fp);
      }
      glowDot(ctx, cx, base - 8, 2.2, pal.accent, 0.4);
    },
  },

  // ═══ PSYCHÉ · suite ══════════════════════════════════════════════════════════════════

  'goule-anxiete': {
    catalogKey: 'canon.enemy.goule-anxiete',
    name: 'La Goule', side: 'enemy', role: 'drain', family: 'psyche', rarity: 'elite',
    registre: 'folie',
    quote: '« Elle envahit, recouvre, étouffe — jusqu’au “Tais-toi” d’Elise. »',
    silhouette: 'Déborde largement de sa case. Pas de bas : elle recouvre le sol alentour.',
    pal: { body: '#2c1e34', deep: '#0e0812', accent: '#cf3f92', light: '#8f74a8' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      // Le Drain déborde : la nappe couvre les cases voisines avant que le corps commence.
      for (let i = 0; i < 7; i++) {
        const rr = 52 - i * 5;
        ctx.fillStyle = rgba(pal.body, 0.1 + i * 0.035);
        ctx.beginPath();
        ctx.ellipse(cx + R2(R, -4, 4), base - 2 - i * 1.6, rr, rr * 0.34, 0, 0, Math.PI * 2);
        ctx.fill();
      }
      // Des doigts de nappe qui rampent vers l'extérieur : elle gagne du terrain.
      for (let i = 0; i < 10; i++) {
        const a = R() * Math.PI * 2;
        const fp = new Path2D();
        fp.moveTo(cx, base - 6);
        fp.quadraticCurveTo(cx + Math.cos(a) * 34, base - 6 + Math.sin(a) * 12, cx + Math.cos(a) * 56, base - 2 + Math.sin(a) * 19);
        line(ctx, fp, pal.body, R2(R, 0.2, 0.45), R2(R, 2, 5));
      }
      // Le corps : une masse molle qui monte sans structure. Aucun membre, aucune articulation.
      const bd = new Path2D();
      bd.moveTo(cx - 26, base - 6);
      bd.quadraticCurveTo(cx - 30, base - 44, cx - 8, base - 58);
      bd.quadraticCurveTo(cx + 16, base - 64, cx + 24, base - 42);
      bd.quadraticCurveTo(cx + 30, base - 16, cx + 26, base - 6);
      bd.closePath();
      ctx.save();
      ctx.shadowColor = rgba(pal.accent, 0.22); ctx.shadowBlur = 14;
      const g = ctx.createLinearGradient(cx, base - 64, cx, base);
      g.addColorStop(0, rgba(mix(pal.body, pal.light, 0.28), 0.96));
      g.addColorStop(1, rgba(pal.deep, 0.98));
      ctx.fillStyle = g; ctx.fill(bd);
      ctx.restore();
      // Ce qu'elle a déjà recouvert : des formes prises dans la masse, à demi visibles.
      ctx.save(); ctx.clip(bd);
      for (const [ox, oy, rr] of [[-10, -26, 6], [8, -40, 5], [2, -14, 7]]) {
        ctx.strokeStyle = rgba(pal.light, 0.26); ctx.lineWidth = 1.3;
        ctx.beginPath(); ctx.arc(cx + ox, base + oy, rr, 0, Math.PI * 2); ctx.stroke();
      }
      for (let i = 0; i < 24; i++) {
        const bx = cx + R2(R, -24, 24), by = base - R2(R, 8, 56);
        ctx.fillStyle = rgba(pal.accent, R2(R, 0.05, 0.18));
        ctx.beginPath(); ctx.arc(bx, by, R2(R, 1.4, 4), 0, Math.PI * 2); ctx.fill();
      }
      ctx.restore();
      // La bouche : une seule ouverture, très large, tout en haut. Elle étouffe, elle ne mord pas.
      const mo = new Path2D();
      mo.moveTo(cx - 14, base - 52);
      mo.quadraticCurveTo(cx, base - 40, cx + 13, base - 54);
      mo.quadraticCurveTo(cx, base - 48, cx - 14, base - 52);
      mo.closePath();
      fill(ctx, mo, '#05040a', 0.9);
      glowDot(ctx, cx - 2, base - 50, 1.6, pal.accent, 0.5);
    },
  },

  // ═══ ALCHIMIE · suite ════════════════════════════════════════════════════════════════

  homoncule: {
    catalogKey: 'canon.enemy.homoncule',
    name: 'L’Homoncule', side: 'enemy', role: 'bruiser', family: 'alchimie', rarity: 'elite',
    registre: 'deni',
    quote: '« Lent, presque doux — jusqu’à ce qu’il hurle. »',
    silhouette: 'Haut, épaules molles, tête inclinée. Une flamme froide contenue dans le torse.',
    pal: { body: '#4a4260', deep: '#161228', accent: '#9a7cf0', light: '#e0d8c0' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 22, 8.5, 0.5);
      for (const s of [-1, 1]) {
        const lg = new Path2D();
        lg.moveTo(cx + s * 7, base - 34);
        lg.quadraticCurveTo(cx + s * 13, base - 18, cx + s * 9, base - 2);
        line(ctx, lg, pal.body, 1, 7);
      }
      // Nacré et soufré : la chair est claire, satinée, jamais franchement humaine.
      const bd = new Path2D();
      bd.moveTo(cx - 16, base - 30);
      bd.quadraticCurveTo(cx - 22, base - 62, cx - 4, base - 74);
      bd.quadraticCurveTo(cx + 18, base - 76, cx + 20, base - 54);
      bd.quadraticCurveTo(cx + 17, base - 36, cx + 13, base - 30);
      bd.closePath();
      const g = ctx.createLinearGradient(cx - 20, base - 74, cx + 20, base - 30);
      g.addColorStop(0, rgba(mix(pal.body, pal.light, 0.4), 1));
      g.addColorStop(0.5, rgba(pal.body, 1));
      g.addColorStop(1, rgba(pal.deep, 1));
      ctx.fillStyle = g; ctx.fill(bd);
      line(ctx, bd, pal.light, 0.2, 1.2);
      // La flamme froide bleu-violet : DANS le torse, jamais autour. C'est l'organe.
      const fl = ctx.createRadialGradient(cx, base - 50, 1, cx, base - 50, 18);
      fl.addColorStop(0, rgba('#e8e0ff', 0.7));
      fl.addColorStop(0.4, rgba(pal.accent, 0.5));
      fl.addColorStop(1, rgba(pal.accent, 0));
      ctx.save(); ctx.clip(bd);
      ctx.fillStyle = fl;
      ctx.beginPath(); ctx.arc(cx, base - 50, 18, 0, Math.PI * 2); ctx.fill();
      for (let i = 0; i < 5; i++) {
        const fp = new Path2D();
        const fx = cx + R2(R, -8, 8);
        fp.moveTo(fx, base - 40);
        fp.quadraticCurveTo(fx + R2(R, -3, 3), base - 52, fx + R2(R, -2, 2), base - 62);
        line(ctx, fp, '#c8b4ff', R2(R, 0.2, 0.5), R2(R, 1.4, 3));
      }
      ctx.restore();
      // Bras lourds, pendants : la douceur est dans le fait qu'ils ne sont jamais levés.
      for (const [s, ey] of [[-1, -34], [1, -30]]) {
        const ar = new Path2D();
        ar.moveTo(cx + s * 15, base - 62);
        ar.quadraticCurveTo(cx + s * 26, base - 48, cx + s * 22, base + ey);
        line(ctx, ar, pal.body, 1, 5.4);
      }
      // La tête : inclinée, sans traits, une seule couture soufrée là où serait la bouche.
      ctx.beginPath(); ctx.ellipse(cx + 3, base - 82, 9, 9.6, 0.2, 0, Math.PI * 2);
      ctx.fillStyle = rgba(mix(pal.body, pal.light, 0.32), 1); ctx.fill();
      seg(ctx, cx - 3, base - 79, cx + 9, base - 81, '#c8a83c', 0.6, 1.6);
      for (const s of [-1, 1]) glowDot(ctx, cx + 3 + s * 3.4, base - 85, 1, pal.accent, 0.45);
    },
  },

  'enfant-argile': {
    catalogKey: 'canon.enemy.enfant-argile',
    name: 'L’Enfant d’argile', side: 'enemy', role: 'support', family: 'alchimie', rarity: 'common',
    registre: 'deni',
    quote: '« Un essai raté, abandonné avant l’achèvement. Il soigne encore, par réflexe. »',
    silhouette: 'Petit, vertical, une moitié inachevée : le bras droit s’arrête au coude.',
    pal: { body: '#7a6450', deep: '#2a2018', accent: '#86dcb4', light: '#a89078' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 13, 5.5, 0.42);
      for (const s of [-1, 1]) seg(ctx, cx + s * 3, base - 24, cx + s * 6, base - 1, pal.body, 1, 5);
      // L'argile : mate, striée par les doigts qui l'ont montée. Aucun poli.
      const bd = new Path2D();
      bd.moveTo(cx - 10, base - 22);
      bd.quadraticCurveTo(cx - 13, base - 42, cx - 2, base - 50);
      bd.quadraticCurveTo(cx + 11, base - 51, cx + 12, base - 38);
      bd.quadraticCurveTo(cx + 10, base - 26, cx + 8, base - 22);
      bd.closePath();
      fill(ctx, bd, pal.body); line(ctx, bd, pal.deep, 0.5, 1.2);
      ctx.save(); ctx.clip(bd);
      for (let i = 0; i < 12; i++) {
        const yy = base - 22 - i * 2.4;
        ctx.beginPath();
        ctx.moveTo(cx - 12, yy); ctx.quadraticCurveTo(cx, yy - R2(R, 1, 3), cx + 12, yy);
        ctx.strokeStyle = rgba(R() > 0.5 ? pal.light : pal.deep, R2(R, 0.1, 0.26));
        ctx.lineWidth = R2(R, 1, 2.2); ctx.stroke();
      }
      ctx.restore();
      // Le bras gauche entier, tendu, portant le soin. Le droit S'ARRÊTE : rien après le coude.
      seg(ctx, cx - 8, base - 44, cx - 20, base - 34, pal.body, 1, 3.8);
      const bowl = new Path2D();
      bowl.moveTo(cx - 27, base - 34); bowl.quadraticCurveTo(cx - 20, base - 26, cx - 13, base - 34);
      bowl.quadraticCurveTo(cx - 20, base - 38, cx - 27, base - 34);
      bowl.closePath();
      fill(ctx, bowl, shade(pal.body, -0.2)); line(ctx, bowl, pal.light, 0.4, 1);
      const gl = ctx.createRadialGradient(cx - 20, base - 34, 1, cx - 20, base - 34, 12);
      gl.addColorStop(0, rgba(pal.accent, 0.6));
      gl.addColorStop(1, rgba(pal.accent, 0));
      ctx.fillStyle = gl;
      ctx.beginPath(); ctx.arc(cx - 20, base - 34, 12, 0, Math.PI * 2); ctx.fill();
      const stub = new Path2D();
      stub.moveTo(cx + 9, base - 44); stub.lineTo(cx + 17, base - 38);
      line(ctx, stub, pal.body, 1, 3.8);
      ctx.fillStyle = rgba(pal.deep, 0.85);
      ctx.beginPath(); ctx.ellipse(cx + 18, base - 37, 2.6, 2.2, 0.4, 0, Math.PI * 2); ctx.fill();
      // La tête : ébauchée. Deux creux de pouce pour les yeux, pas de bouche du tout.
      ctx.beginPath(); ctx.ellipse(cx + 1, base - 57, 7.4, 7.8, 0.1, 0, Math.PI * 2);
      ctx.fillStyle = rgba(shade(pal.body, 0.08), 1); ctx.fill();
      for (const s of [-1, 1]) {
        ctx.fillStyle = rgba(pal.deep, 0.7);
        ctx.beginPath(); ctx.ellipse(cx + 1 + s * 3, base - 58, 2, 2.6, s * 0.3, 0, Math.PI * 2); ctx.fill();
      }
      glowDot(ctx, cx - 20, base - 34, 1.8, pal.accent, 0.5);
    },
  },

  // ═══ CHIMÈRES DES PLAINES ════════════════════════════════════════════════════════════
  // L'animal recomposé de mémoire. Une seule articulation fausse par figure — c'est elle
  // qui fait le malaise, jamais le nombre de têtes.

  'chimere-affamee': {
    catalogKey: 'canon.enemy.chimere-affamee',
    name: 'Chimère Affamée', side: 'enemy', role: 'skirmisher', family: 'chimeres', rarity: 'common',
    registre: 'effroi',
    quote: '« Elle ne rugit pas. Elle compte vos battements de cœur. »',
    silhouette: 'Basse, allongée, immobile. La mâchoire est trop longue pour le crâne.',
    pal: { body: '#5a4a3a', deep: '#1e1812', accent: '#c8394a', light: '#8f7a5e' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 28, 9, 0.46);
      // Tapie : le corps est presque horizontal, à peine plus haut que les herbes.
      const bd = new Path2D();
      bd.moveTo(cx - 26, base - 8);
      bd.quadraticCurveTo(cx - 24, base - 30, cx - 2, base - 32);
      bd.quadraticCurveTo(cx + 22, base - 33, cx + 26, base - 18);
      bd.quadraticCurveTo(cx + 20, base - 6, cx - 26, base - 8);
      bd.closePath();
      fill(ctx, bd, pal.body); line(ctx, bd, pal.deep, 0.6, 1.2);
      ctx.save(); ctx.clip(bd);
      for (let i = 0; i < 34; i++) {
        const fx = cx - 26 + R() * 52, fy = base - 32 + R() * 26;
        seg(ctx, fx, fy, fx + R2(R, 1, 4), fy + R2(R, 1, 4), R() > 0.55 ? pal.light : pal.deep, R2(R, 0.1, 0.26), 1);
      }
      ctx.restore();
      // LES PATTES EN TROP : quatre, repliées sous le ventre, jamais posées. C'est le faux.
      for (let i = 0; i < 4; i++) {
        const px = cx - 14 + i * 9;
        const lp = new Path2D();
        lp.moveTo(px, base - 10);
        lp.quadraticCurveTo(px + 5, base - 4, px + 1, base - 1);
        line(ctx, lp, pal.deep, 0.85, 2.4);
        seg(ctx, px + 2, base - 16, px + 7, base - 11, pal.light, 0.4, 1.8);
      }
      // La mâchoire de brochet : longue, fine, tenue à l'horizontale. Elle prime sur le crâne.
      seg(ctx, cx - 22, base - 26, cx - 34, base - 22, pal.body, 1, 7);
      const jw = new Path2D();
      jw.moveTo(cx - 32, base - 24);
      jw.lineTo(cx - 56, base - 20);
      jw.lineTo(cx - 55, base - 15);
      jw.lineTo(cx - 31, base - 17);
      jw.closePath();
      fill(ctx, jw, shade(pal.body, 0.08)); line(ctx, jw, pal.deep, 0.5, 1);
      for (let i = 0; i < 9; i++) {
        const tx = cx - 54 + i * 2.6;
        seg(ctx, tx, base - 20, tx, base - 16, '#e0d8c8', 0.6, 1);
      }
      glowDot(ctx, cx - 34, base - 26, 1.2, pal.accent, 0.55);
    },
  },

  'berger-ordres': {
    catalogKey: 'canon.enemy.berger-ordres',
    name: 'Berger d’Ordres', side: 'enemy', role: 'support', family: 'chimeres', rarity: 'uncommon',
    registre: 'effroi',
    quote: '« Le troupeau ne demande qu’une chose. Je la lui accorde. »',
    silhouette: 'Très vertical, immobile, une règle d’architecte démesurée tenue comme houlette.',
    pal: { body: '#3e3a44', deep: '#161418', accent: '#d9a441', light: '#9a9488' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 16, 7);
      const p = drape(ctx, R, cx, base, { top: base - 58, halfTop: 10, halfBot: 17, col: pal.body, deep: pal.deep, plis: 8 });
      // La houlette : une règle graduée, plus haute que lui. L'objet du Support, tenu droit.
      const rl = new Path2D();
      rl.moveTo(cx + 15, base - 2); rl.lineTo(cx + 13, base - 104);
      rl.lineTo(cx + 18, base - 104); rl.lineTo(cx + 20, base - 2);
      rl.closePath();
      fill(ctx, rl, '#c8b898'); line(ctx, rl, pal.deep, 0.5, 1);
      for (let i = 0; i < 20; i++) {
        const yy = base - 8 - i * 4.8;
        seg(ctx, cx + 13.5, yy, cx + (i % 5 === 0 ? 19.5 : 17), yy, pal.deep, 0.55, 0.9);
      }
      // Le crochet du haut : la seule courbe de la figure, et elle est tracée au compas.
      const hk = new Path2D();
      hk.moveTo(cx + 15, base - 104);
      hk.quadraticCurveTo(cx + 2, base - 112, cx + 4, base - 98);
      line(ctx, hk, '#c8b898', 0.95, 3.4);
      seg(ctx, cx + 9, base - 62, cx + 15, base - 60, pal.light, 0.8, 3.2);
      // Le visage effacé : pas une capuche, une face lisse, plus claire que la bure.
      ctx.beginPath(); ctx.ellipse(cx - 1, base - 66, 8, 9, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba(mix(pal.body, pal.light, 0.4), 1); ctx.fill();
      ctx.save();
      ctx.beginPath(); ctx.ellipse(cx - 1, base - 66, 8, 9, 0, 0, Math.PI * 2); ctx.clip();
      const sm = ctx.createLinearGradient(cx - 9, base - 74, cx + 7, base - 58);
      sm.addColorStop(0, rgba('#ffffff', 0.14));
      sm.addColorStop(1, rgba(pal.deep, 0.4));
      ctx.fillStyle = sm; ctx.fillRect(cx - 10, base - 76, 20, 20);
      ctx.restore();
      // Les gestes : trois traits d'or courts, en l'air, qui sont un plan et non un sort.
      for (let i = 0; i < 3; i++) {
        seg(ctx, cx - 26 - i * 2, base - 46 + i * 8, cx - 12, base - 44 + i * 8, pal.accent, 0.3 - i * 0.06, 1.2);
      }
      seg(ctx, cx - 10, base - 58, cx - 24, base - 46, pal.body, 1, 3.4);
    },
  },

  'agneau-inverse': {
    catalogKey: 'canon.enemy.agneau-inverse',
    name: 'Agneau Inversé', side: 'enemy', role: 'disruptor', family: 'chimeres', rarity: 'uncommon',
    registre: 'effroi',
    quote: '« Il broutait. Vous avez cligné des yeux. Il vous regarde. »',
    silhouette: 'Petit, rond, paisible. La seule figure du bestiaire qu’on prend pour un décor.',
    pal: { body: '#d8d2c4', deep: '#1c1a20', accent: '#c8394a', light: '#f0ece0' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 20, 7.5, 0.4);
      for (const ox of [-10, -3, 6, 12]) seg(ctx, cx + ox, base - 14, cx + ox + R2(R, -1, 1), base - 1, '#3e3a34', 1, 2.6);
      // La toison : ronde, régulière, presque décorative. C'est le piège de la figure.
      const bd = new Path2D();
      bd.ellipse(cx, base - 26, 19, 14, 0, 0, Math.PI * 2);
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.4); ctx.shadowBlur = 8;
      ctx.fillStyle = rgba(pal.body, 1); ctx.fill(bd);
      ctx.restore();
      // LA LAINE POUSSE VERS L'INTÉRIEUR : les boucles creusent au lieu de dépasser.
      ctx.save(); ctx.clip(bd);
      for (let i = 0; i < 30; i++) {
        const wx = cx + R2(R, -18, 18), wy = base - 26 + R2(R, -13, 13);
        ctx.strokeStyle = rgba(pal.deep, R2(R, 0.16, 0.42)); ctx.lineWidth = R2(R, 0.8, 1.8);
        ctx.beginPath(); ctx.arc(wx, wy, R2(R, 2, 4.6), 0, Math.PI * 1.6); ctx.stroke();
      }
      // Ce qui remplit le corps n'est pas de la chair : un vide comprimé, au centre.
      const hollow = ctx.createRadialGradient(cx, base - 26, 1, cx, base - 26, 13);
      hollow.addColorStop(0, rgba('#05060c', 0.55));
      hollow.addColorStop(1, rgba('#05060c', 0));
      ctx.fillStyle = hollow;
      ctx.beginPath(); ctx.arc(cx, base - 26, 13, 0, Math.PI * 2); ctx.fill();
      ctx.restore();
      // Le contour instable du Disruptor : deux répliques très pâles, à peine décalées.
      for (let i = 1; i <= 2; i++) {
        ctx.save(); ctx.translate(i * 2.2, -i * 1);
        line(ctx, bd, pal.light, 0.14, 1.2);
        ctx.restore();
      }
      // La tête, basse, en train de brouter — mais l'œil est déjà remonté vers vous.
      ctx.beginPath(); ctx.ellipse(cx + 17, base - 16, 7.4, 6, 0.35, 0, Math.PI * 2);
      ctx.fillStyle = rgba(shade(pal.body, -0.1), 1); ctx.fill();
      ctx.fillStyle = rgba('#05060c', 0.9);
      ctx.beginPath(); ctx.ellipse(cx + 20, base - 19, 2, 1.5, 0.3, 0, Math.PI * 2); ctx.fill();
      glowDot(ctx, cx + 20, base - 19, 1, pal.accent, 0.5);
    },
  },

  // ═══ CRÉATIONS DU FORGERON ═══════════════════════════════════════════════════════════
  // Métal battu, rivets, une braise interne. Aucun visage : la chaleur sort par les joints,
  // jamais par des yeux.

  'creation-instable': {
    catalogKey: 'canon.enemy.creation-instable',
    name: 'Création Instable', side: 'enemy', role: 'bruiser', family: 'forgeron', rarity: 'common',
    registre: 'rupture',
    quote: '« Elle se tient debout. Presque. C’est le presque qui fait mal. »',
    silhouette: 'Humanoïde de guingois : une jambe plus courte, tout le corps penché pour compenser.',
    pal: { body: '#4a4650', deep: '#1a1820', accent: TOKEN.ember, light: '#8e8a98' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 21, 8.5, 0.52);
      // La jambe courte est à gauche : le corps entier s'incline pour compenser, en permanence.
      paintMass(ctx, [P(cx - 16, base), P(cx - 15, base - 16), P(cx - 6, base - 16), P(cx - 5, base)], th, R, { base: pal.deep, rim: 0.1 });
      paintMass(ctx, [P(cx + 4, base), P(cx + 5, base - 30), P(cx + 14, base - 30), P(cx + 15, base)], th, R, { base: pal.deep, rim: 0.1 });
      ctx.save(); ctx.translate(cx, base); ctx.rotate(-0.11); ctx.translate(-cx, -base);
      // Les plaques mal jointes : chacune peinte à part, aucune ne s'aligne sur sa voisine.
      const plates = [
        [[-18, -26], [-20, -44], [-2, -48], [0, -28]],
        [[-1, -30], [-3, -50], [16, -52], [18, -34]],
        [[-16, -46], [-14, -62], [4, -66], [6, -50]],
        [[3, -52], [5, -68], [20, -66], [18, -50]],
      ];
      plates.forEach((pl, i) => {
        const pp = poly(pl.map(([x, y]) => P(cx + x, base + y)));
        const g = ctx.createLinearGradient(cx - 20, base - 68, cx + 20, base - 26);
        g.addColorStop(0, rgba(mix(pal.body, pal.light, 0.2 + (i % 2) * 0.16), 1));
        g.addColorStop(1, rgba(pal.deep, 1));
        ctx.fillStyle = g; ctx.fill(pp);
        line(ctx, pp, pal.light, 0.28, 1.1);
        for (let j = 0; j < 3; j++) {
          ctx.fillStyle = rgba(pal.light, 0.45);
          ctx.beginPath();
          ctx.arc(cx + pl[j][0] + 3, base + pl[j][1] + 4, 1.2, 0, Math.PI * 2); ctx.fill();
        }
      });
      // LE FOYER QUI S'OUVRE PAR INTERMITTENCE : une fente verticale, franche, au torse.
      const slit = new Path2D();
      slit.moveTo(cx - 3, base - 58); slit.lineTo(cx + 4, base - 58);
      slit.lineTo(cx + 3, base - 34); slit.lineTo(cx - 2, base - 34);
      slit.closePath();
      const fg = ctx.createLinearGradient(cx, base - 58, cx, base - 34);
      fg.addColorStop(0, rgba('#ffd08a', 0.9));
      fg.addColorStop(1, rgba(pal.accent, 0.35));
      ctx.fillStyle = fg; ctx.fill(slit);
      for (const s of [-1, 1]) {
        const ar = new Path2D();
        ar.moveTo(cx + s * 16, base - 60);
        ar.quadraticCurveTo(cx + s * 28, base - 46, cx + s * 22, base - 28);
        line(ctx, ar, pal.body, 1, 4.4);
      }
      // Pas de tête : un moignon de col rivé, et de la chaleur qui s'en échappe.
      paintMass(ctx, [P(cx - 6, base - 66), P(cx - 4, base - 76), P(cx + 8, base - 76), P(cx + 9, base - 64)], th, R, { base: pal.body, rim: 0.2 });
      for (let i = 0; i < 5; i++) glowDot(ctx, cx + R2(R, -6, 8), base - 78 - R() * 10, R2(R, 0.7, 1.4), pal.accent, 0.3);
      ctx.restore();
    },
  },

  'marteau-vivant': {
    catalogKey: 'canon.enemy.marteau-vivant',
    name: 'Marteau Vivant', side: 'enemy', role: 'bruiser', family: 'forgeron', rarity: 'uncommon',
    registre: 'rupture',
    quote: '« Les marteaux qui hurlent. C’est de lui qu’on parle. »',
    silhouette: 'Très haut, très étroit : une masse de forge dressée sur un manche vertébral.',
    pal: { body: '#464250', deep: '#18161e', accent: TOKEN.ember, light: '#9a96a6' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 15, 6.5, 0.54);
      // Le manche tordu en colonne vertébrale : segmenté, jamais droit. C'est le corps.
      let px = cx, py = base - 2;
      for (let i = 0; i < 11; i++) {
        const nx = cx + Math.sin(i * 0.7) * 5, ny = base - 6 - i * 7;
        seg(ctx, px, py, nx, ny, pal.body, 1, 6.4 - i * 0.15);
        ctx.fillStyle = rgba(pal.light, 0.32);
        ctx.beginPath(); ctx.ellipse(nx, ny, 4.4, 2, Math.sin(i * 0.7) * 0.3, 0, Math.PI * 2); ctx.fill();
        px = nx; py = ny;
      }
      // La masse : lourde, tout en haut, franchement plus large que le manche.
      const hd = poly([P(px - 22, py - 6), P(px - 20, py - 26), P(px + 22, py - 28), P(px + 24, py - 8)]);
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.6); ctx.shadowBlur = 12;
      const g = ctx.createLinearGradient(px - 22, 0, px + 24, 0);
      g.addColorStop(0, rgba(shade(pal.body, 0.18), 1));
      g.addColorStop(0.6, rgba(pal.body, 1));
      g.addColorStop(1, rgba(pal.deep, 1));
      ctx.fillStyle = g; ctx.fill(hd);
      ctx.restore();
      line(ctx, hd, pal.light, 0.3, 1.3);
      for (let i = 0; i < 6; i++) {
        ctx.beginPath();
        ctx.ellipse(px - 16 + R() * 36, py - 26, R2(R, 2, 4.5), 1.3, 0, 0, Math.PI * 2);
        ctx.fillStyle = rgba('#000000', R2(R, 0.2, 0.4)); ctx.fill();
      }
      const jg = ctx.createLinearGradient(px - 20, 0, px + 22, 0);
      jg.addColorStop(0, rgba(pal.accent, 0.7));
      jg.addColorStop(1, rgba(pal.accent, 0.08));
      ctx.fillStyle = jg; ctx.fillRect(px - 20, py - 18, 42, 2.2);
      // Le hurlement : pas une bouche, une onde. Trois arcs au sol, au rythme de la frappe.
      for (let i = 0; i < 3; i++) {
        ctx.strokeStyle = rgba(pal.accent, 0.2 - i * 0.05); ctx.lineWidth = 1.6;
        ctx.beginPath(); ctx.ellipse(cx, base - 3, 18 + i * 11, 6 + i * 4, 0, 0, Math.PI * 2); ctx.stroke();
      }
      for (let i = 0; i < 5; i++) glowDot(ctx, cx + R2(R, -14, 14), base - 6 - R() * 12, R2(R, 0.8, 1.6), pal.accent, 0.28);
    },
  },

  'sentinelle-fonte': {
    catalogKey: 'canon.enemy.sentinelle-fonte',
    name: 'Sentinelle de Fonte', side: 'enemy', role: 'support', family: 'forgeron', rarity: 'uncommon',
    registre: 'rupture',
    quote: '« Plomb, or, mercure, soufre, sel. Elle récite. C’est tout ce qu’on lui a laissé. »',
    silhouette: 'Assise en tailleur, large et basse. Elle ne se lève jamais, et ça se voit.',
    pal: { body: '#4e4a4e', deep: '#1c1a1c', accent: TOKEN.ember, light: '#8e8a8e' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 27, 9.5, 0.5);
      // Assise : la base est un triangle stable, plus large que tout le reste de la figure.
      const lg = new Path2D();
      lg.moveTo(cx - 27, base - 2);
      lg.quadraticCurveTo(cx - 20, base - 20, cx, base - 22);
      lg.quadraticCurveTo(cx + 20, base - 20, cx + 27, base - 2);
      lg.closePath();
      fill(ctx, lg, pal.body); line(ctx, lg, pal.deep, 0.6, 1.3);
      ctx.save(); ctx.clip(lg);
      for (let i = 0; i < 40; i++) {
        const gx = cx + R2(R, -27, 27), gy = base - 22 + R() * 22;
        ctx.fillStyle = rgba(R() > 0.5 ? pal.light : pal.deep, R2(R, 0.08, 0.22));
        ctx.beginPath(); ctx.arc(gx, gy, R2(R, 0.8, 2.2), 0, Math.PI * 2); ctx.fill();
      }
      ctx.restore();
      const bd = new Path2D();
      bd.moveTo(cx - 15, base - 20);
      bd.quadraticCurveTo(cx - 18, base - 46, cx - 4, base - 54);
      bd.quadraticCurveTo(cx + 14, base - 55, cx + 16, base - 40);
      bd.quadraticCurveTo(cx + 14, base - 26, cx + 12, base - 20);
      bd.closePath();
      fill(ctx, bd, shade(pal.body, 0.08)); line(ctx, bd, pal.deep, 0.5, 1.2);
      // LES MAINS SUR LES GENOUX, ROUGIES : le seul endroit chaud, et il est en bas.
      for (const s of [-1, 1]) {
        seg(ctx, cx + s * 12, base - 46, cx + s * 20, base - 20, pal.body, 1, 4.2);
        const hg = ctx.createRadialGradient(cx + s * 20, base - 18, 1, cx + s * 20, base - 18, 11);
        hg.addColorStop(0, rgba('#ffc08a', 0.75));
        hg.addColorStop(0.4, rgba(pal.accent, 0.4));
        hg.addColorStop(1, rgba(pal.accent, 0));
        ctx.fillStyle = hg;
        ctx.beginPath(); ctx.arc(cx + s * 20, base - 18, 11, 0, Math.PI * 2); ctx.fill();
      }
      // La litanie : cinq marques gravées au torse, une par corps de l'œuvre. Illisibles.
      gravures(ctx, R, cx - 11, base - 50, 22, 24, pal.accent, 10);
      // La tête : un bloc de fonte sans traits, la bouche seule ouverte, très étroite.
      const hd = poly([P(cx - 8, base - 54), P(cx - 7, base - 68), P(cx + 8, base - 69), P(cx + 9, base - 55)]);
      fill(ctx, hd, pal.body); line(ctx, hd, pal.deep, 0.55, 1.2);
      ctx.fillStyle = rgba('#05040a', 0.9);
      ctx.fillRect(cx - 3, base - 61, 7, 2.4);
      glowDot(ctx, cx + 0.5, base - 60, 1.2, pal.accent, 0.4);
    },
  },

  'scorie-rampante': {
    catalogKey: 'canon.enemy.scorie-rampante',
    name: 'Scorie Rampante', side: 'enemy', role: 'skirmisher', family: 'forgeron', rarity: 'common',
    registre: 'rupture',
    quote: '« Ça rampe. Ça brûle. Ça se souvient d’avoir été un projet. »',
    silhouette: 'Très basse, étalée, asymétrique. Aucune verticale : elle se traîne.',
    pal: { body: '#3a3238', deep: '#150f12', accent: TOKEN.ember, light: '#e07a3a' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      // La trace vitrifiée derrière elle : elle dit d'où elle vient, donc où elle va.
      const tr = new Path2D();
      tr.moveTo(cx - 46, base + 2);
      tr.quadraticCurveTo(cx - 24, base - 6, cx - 4, base - 2);
      tr.quadraticCurveTo(cx - 24, base + 3, cx - 46, base + 2);
      tr.closePath();
      fill(ctx, tr, '#4a3e42', 0.55);
      for (let i = 0; i < 8; i++) {
        glowDot(ctx, cx - 44 + i * 5, base - R2(R, 0, 4), R2(R, 0.6, 1.3), pal.accent, 0.16);
      }
      // La flaque : à demi solidifiée. Croûte sombre dessus, incandescence dans les fissures.
      const bd = new Path2D();
      bd.moveTo(cx - 28, base - 2);
      bd.quadraticCurveTo(cx - 26, base - 18, cx - 6, base - 20);
      bd.quadraticCurveTo(cx + 16, base - 24, cx + 26, base - 12);
      bd.quadraticCurveTo(cx + 28, base - 2, cx + 12, base);
      bd.quadraticCurveTo(cx - 12, base + 2, cx - 28, base - 2);
      bd.closePath();
      const g = ctx.createLinearGradient(cx, base - 24, cx, base);
      g.addColorStop(0, rgba(shade(pal.body, 0.12), 1));
      g.addColorStop(1, rgba(pal.deep, 1));
      ctx.fillStyle = g; ctx.fill(bd);
      ctx.save(); ctx.clip(bd);
      for (let i = 0; i < 11; i++) {
        const fx = cx + R2(R, -26, 26);
        const fp = new Path2D();
        fp.moveTo(fx, base - 22);
        fp.quadraticCurveTo(fx + R2(R, -6, 6), base - 12, fx + R2(R, -9, 9), base);
        line(ctx, fp, R() > 0.5 ? pal.accent : pal.light, R2(R, 0.3, 0.7), R2(R, 1, 2.6));
      }
      // LA FORME QUI S'ÉBAUCHE : une main, à demi levée hors de la masse. Elle retombera.
      const hn = new Path2D();
      hn.moveTo(cx + 6, base - 12);
      hn.quadraticCurveTo(cx + 10, base - 26, cx + 16, base - 32);
      line(ctx, hn, shade(pal.body, 0.16), 0.95, 5);
      for (let i = 0; i < 4; i++) {
        seg(ctx, cx + 16, base - 32, cx + 12 + i * 3, base - 42 - R2(R, 0, 4), shade(pal.body, 0.2), 0.85, 1.8);
      }
      ctx.restore();
      ctx.fillStyle = rgba(shade(pal.body, 0.16), 1);
      ctx.beginPath(); ctx.ellipse(cx + 16, base - 33, 4, 3.4, 0.3, 0, Math.PI * 2); ctx.fill();
      for (let i = 0; i < 7; i++) glowDot(ctx, cx + R2(R, -22, 22), base - 8 - R() * 16, R2(R, 0.7, 1.6), pal.accent, 0.3);
    },
  },

  // ═══ PÉNITENTS DE LA MONTAGNE ════════════════════════════════════════════════════════
  // Pierre, bure, chaînes. Tous courbés : la famille la plus basse du bestiaire. La honte
  // tient lieu d'armure.

  'pelerin-sans-visage': {
    catalogKey: 'canon.enemy.pelerin-sans-visage',
    name: 'Pèlerin Sans Visage', side: 'enemy', role: 'skirmisher', family: 'penitents', rarity: 'common',
    registre: 'effroi',
    quote: '« Il monte depuis si longtemps qu’il a usé son visage contre le vent. »',
    silhouette: 'Étroit, courbé par la pente, penché en avant. Un chapelet pend à la ceinture.',
    pal: { body: '#4a4650', deep: '#1a1820', accent: '#c8b8a0', light: '#8e8a96' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 14, 6, 0.44);
      ctx.save(); ctx.translate(cx, base); ctx.rotate(0.14); ctx.translate(-cx, -base);
      const p = drape(ctx, R, cx, base, { top: base - 54, halfTop: 10, halfBot: 15, col: pal.body, deep: pal.deep, plis: 9 });
      // Le bâton : planté en avant, il porte la moitié du poids. La pente est dans la posture.
      seg(ctx, cx - 20, base + 2, cx - 12, base - 62, '#5a4a38', 1, 3);
      seg(ctx, cx - 8, base - 44, cx - 13, base - 56, pal.body, 1, 3.6);
      // LE VISAGE USÉ : pas de capuche noire, une surface pleine, lisse, comme une pièce.
      const fc = new Path2D();
      fc.moveTo(cx - 10, base - 52);
      fc.quadraticCurveTo(cx - 11, base - 70, cx + 1, base - 72);
      fc.quadraticCurveTo(cx + 12, base - 70, cx + 11, base - 52);
      fc.closePath();
      fill(ctx, fc, pal.body); line(ctx, fc, pal.deep, 0.6, 1.2);
      const worn = new Path2D();
      worn.ellipse(cx + 1, base - 62, 7.4, 8.4, 0, 0, Math.PI * 2);
      const g = ctx.createRadialGradient(cx - 1, base - 65, 1, cx + 1, base - 62, 9);
      g.addColorStop(0, rgba(pal.accent, 0.6));
      g.addColorStop(1, rgba(shade(pal.accent, -0.5), 0.9));
      ctx.fillStyle = g; ctx.fill(worn);
      // Aucun trait. Deux méplats d'usure, et c'est tout : le vent a fait le reste.
      for (const s of [-1, 1]) {
        ctx.strokeStyle = rgba(pal.deep, 0.2); ctx.lineWidth = 1.4;
        ctx.beginPath(); ctx.arc(cx + 1 + s * 3, base - 63, 3.2, 0.6, 2.4); ctx.stroke();
      }
      // Le chapelet : chaque grain est une petite dent. Il pend, et il est trop long.
      let bx = cx + 10, by = base - 40;
      for (let i = 0; i < 9; i++) {
        const nx = bx + R2(R, -1.4, 2), ny = by + 4;
        ctx.fillStyle = rgba('#e0d8c8', 0.75);
        const t = new Path2D();
        t.moveTo(nx - 1.6, ny); t.lineTo(nx, ny - 3.2); t.lineTo(nx + 1.6, ny);
        t.closePath();
        ctx.fill(t);
        seg(ctx, bx, by, nx, ny, pal.accent, 0.4, 0.8);
        bx = nx; by = ny;
      }
      ctx.restore();
    },
  },

  'prieur-lituique': {
    catalogKey: 'canon.enemy.prieur-lituique',
    name: 'Prieur Lituique', side: 'enemy', role: 'support', family: 'penitents', rarity: 'uncommon',
    registre: 'effroi',
    quote: '« Elle restaure — mais nourrit ce qui rôde. Lui, il sait exactement ce qui rôde. »',
    silhouette: 'Vertical, dos trop droit pour la bure. Un encensoir flotte devant lui.',
    pal: { body: '#2a2630', deep: '#0e0c12', accent: TOKEN.gold, light: '#b8b0c4' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 16, 7);
      // Le dos trop droit : la bure tombe à la verticale, sans le fléchissement des pénitents.
      const p = drape(ctx, R, cx, base, { top: base - 62, halfTop: 10, halfBot: 16, col: pal.body, deep: pal.deep, plis: 10, glow: pal.accent });
      // La fumée qui DESCEND : elle part de l'encensoir et coule au sol. Le seul indice.
      for (let i = 0; i < 9; i++) {
        const sy = base - 40 + i * 4.4;
        const g = ctx.createRadialGradient(cx - 20, sy, 1, cx - 20 + R2(R, -4, 4), sy, 16 - i);
        g.addColorStop(0, rgba(pal.light, 0.15 - i * 0.012));
        g.addColorStop(1, rgba(pal.light, 0));
        ctx.fillStyle = g;
        ctx.beginPath(); ctx.ellipse(cx - 20 + R2(R, -5, 5), sy, 15 - i, 6, 0, 0, Math.PI * 2); ctx.fill();
      }
      // L'encensoir : il FLOTTE, sans chaîne ni main. Rien ne le tient.
      const en = new Path2D();
      en.moveTo(cx - 27, base - 46); en.quadraticCurveTo(cx - 20, base - 34, cx - 13, base - 46);
      en.quadraticCurveTo(cx - 20, base - 52, cx - 27, base - 46);
      en.closePath();
      fill(ctx, en, shade(pal.accent, -0.42)); line(ctx, en, pal.accent, 0.7, 1.1);
      glowDot(ctx, cx - 20, base - 44, 1.6, TOKEN.ember, 0.5);
      hood(ctx, cx, base - 68, 10, 11, pal.body, pal.deep);
      // LA BOUCHE COUSUE DE FIL D'OR : cinq points, réguliers, et il prie quand même.
      for (let i = 0; i < 5; i++) {
        const mx = cx - 5 + i * 2.6;
        seg(ctx, mx, base - 64, mx + 1.6, base - 61, pal.accent, 0.85, 1.2);
      }
      seg(ctx, cx - 6, base - 62.5, cx + 6, base - 62.5, shade(pal.accent, -0.3), 0.5, 0.9);
      // Les doigts joints qui craquent en rythme : deux mains serrées, très haut, au menton.
      for (const s of [-1, 1]) {
        seg(ctx, cx + s * 9, base - 52, cx + s * 3, base - 58, pal.light, 0.7, 3);
      }
      for (let i = 0; i < 4; i++) glowDot(ctx, cx + R2(R, -6, 6), base - 58 + R2(R, -3, 3), 0.7, pal.accent, 0.22);
    },
  },

  'frayeur-exhumee': {
    catalogKey: 'canon.enemy.frayeur-exhumee',
    name: 'Frayeur Exhumée', side: 'enemy', role: 'bruiser', family: 'penitents', rarity: 'rare',
    registre: 'effroi',
    quote: '« Depuis la découverte de la chambre, les échos de la frayeur ne cessent de s’agiter. »',
    silhouette: 'Figée en plein recul : bras levés devant le visage, corps rejeté en arrière.',
    pal: { body: '#6a5c4a', deep: '#221c16', accent: '#c8394a', light: '#a89478' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 20, 8, 0.5);
      // La terreur projetée autour de lui, comme une lampe : un halo rouge, bas, immobile.
      const hl = ctx.createRadialGradient(cx, base - 30, 4, cx, base - 30, 54);
      hl.addColorStop(0, rgba(pal.accent, 0.16));
      hl.addColorStop(1, rgba(pal.accent, 0));
      ctx.fillStyle = hl;
      ctx.beginPath(); ctx.ellipse(cx, base - 24, 54, 34, 0, 0, Math.PI * 2); ctx.fill();
      ctx.save(); ctx.translate(cx, base); ctx.rotate(-0.16); ctx.translate(-cx, -base);
      // Le recul : la jambe avant tendue, l'arrière pliée. La posture est un arrêt sur image.
      seg(ctx, cx - 2, base - 30, cx + 16, base - 2, pal.body, 1, 5.4);
      seg(ctx, cx - 4, base - 30, cx - 14, base - 14, pal.body, 1, 5);
      seg(ctx, cx - 14, base - 14, cx - 10, base - 2, pal.body, 1, 4.6);
      const bd = new Path2D();
      bd.moveTo(cx - 12, base - 28);
      bd.quadraticCurveTo(cx - 18, base - 54, cx - 4, base - 66);
      bd.quadraticCurveTo(cx + 12, base - 68, cx + 13, base - 50);
      bd.quadraticCurveTo(cx + 9, base - 34, cx + 7, base - 28);
      bd.closePath();
      fill(ctx, bd, pal.body); line(ctx, bd, pal.deep, 0.6, 1.3);
      // Les bandelettes : serrées, sèches, et une seule pend, décollée. Le corps est momifié.
      ctx.save(); ctx.clip(bd);
      for (let i = 0; i < 13; i++) {
        const yy = base - 28 - i * 3.2;
        ctx.beginPath();
        ctx.moveTo(cx - 18, yy); ctx.quadraticCurveTo(cx, yy - R2(R, 1, 4), cx + 16, yy);
        ctx.strokeStyle = rgba(R() > 0.5 ? pal.light : pal.deep, R2(R, 0.14, 0.34));
        ctx.lineWidth = R2(R, 1.2, 2.4); ctx.stroke();
      }
      ctx.restore();
      const loose = new Path2D();
      loose.moveTo(cx + 10, base - 44);
      loose.quadraticCurveTo(cx + 22, base - 34, cx + 18, base - 18);
      line(ctx, loose, pal.light, 0.55, 2.4);
      // LES BRAS LEVÉS DEVANT UN DANGER QUE PERSONNE NE VOIT. C'est toute la figure.
      for (const [s, hx, hy] of [[-1, -22, -80], [1, 16, -84]]) {
        const ar = new Path2D();
        ar.moveTo(cx + s * 10, base - 62);
        ar.quadraticCurveTo(cx + s * 24, base - 74, cx + hx, base + hy);
        line(ctx, ar, pal.body, 1, 4.4);
        for (let i = 0; i < 4; i++) {
          seg(ctx, cx + hx, base + hy, cx + hx + R2(R, -6, 6), base + hy - R2(R, 5, 11), pal.light, 0.6, 1.6);
        }
      }
      // Le crâne, rejeté en arrière, bouche ouverte : la dernière terreur, au centième près.
      skull(ctx, cx + 2, base - 74, 8, mix(pal.body, pal.light, 0.4), 3);
      for (const s of [-1, 1]) glowDot(ctx, cx + 2 + s * 3, base - 76, 1.1, pal.accent, 0.6);
      ctx.restore();
    },
  },

  // ═══ FAUX HABITANTS DU JARDIN ════════════════════════════════════════════════════════
  // Rien n'a poussé : tout a été fabriqué pour ressembler à ce qui pousse. Les silhouettes
  // sont trop régulières pour être vivantes.

  'promeneur-fige': {
    catalogKey: 'canon.enemy.promeneur-fige',
    name: 'Promeneur Figé', side: 'enemy', role: 'skirmisher', family: 'jardin', rarity: 'common',
    registre: 'deni',
    quote: '« Belle journée, n’est-ce pas ? N’est-ce pas ? N’est-ce pas ? »',
    silhouette: 'Vertical, habits du dimanche, un bras levé en salut qui ne redescend jamais.',
    pal: { body: '#3e3a4a', deep: '#16141c', accent: '#d9a441', light: '#c8c0b0', skin: '#d8c8b0' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 14, 6, 0.44);
      for (const s of [-1, 1]) seg(ctx, cx + s * 4, base - 32, cx + s * 6, base - 1, pal.body, 1, 5);
      // L'habit : coupé net, boutonné, repassé. La régularité est le seul signe de faux.
      const bd = new Path2D();
      bd.moveTo(cx - 12, base - 30);
      bd.lineTo(cx - 13, base - 58);
      bd.quadraticCurveTo(cx, base - 62, cx + 13, base - 58);
      bd.lineTo(cx + 12, base - 30);
      bd.closePath();
      fill(ctx, bd, pal.body); line(ctx, bd, pal.deep, 0.5, 1.2);
      for (let i = 0; i < 4; i++) {
        ctx.fillStyle = rgba(pal.light, 0.5);
        ctx.beginPath(); ctx.arc(cx, base - 54 + i * 6, 1.1, 0, Math.PI * 2); ctx.fill();
      }
      const cl = new Path2D();
      cl.moveTo(cx - 5, base - 58); cl.lineTo(cx, base - 48); cl.lineTo(cx + 5, base - 58);
      cl.closePath();
      fill(ctx, cl, pal.light, 0.85);
      // Le bras qui ne redescend pas : levé, chapeau en main, tenu à la même hauteur toujours.
      seg(ctx, cx + 11, base - 56, cx + 22, base - 72, pal.body, 1, 3.6);
      seg(ctx, cx - 11, base - 54, cx - 15, base - 36, pal.body, 1, 3.4);
      ctx.beginPath(); ctx.ellipse(cx + 23, base - 76, 10, 3.4, -0.2, 0, Math.PI * 2);
      ctx.fillStyle = rgba('#2e2a34', 1); ctx.fill();
      ctx.beginPath(); ctx.ellipse(cx + 23, base - 80, 5.4, 4.4, -0.2, 0, Math.PI * 2);
      ctx.fillStyle = rgba('#3a3540', 1); ctx.fill();
      // Le sourire cordial : un arc, exactement le même à chaque rencontre. Rien d'autre.
      ctx.beginPath(); ctx.ellipse(cx, base - 68, 8, 8.6, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.skin, 1); ctx.fill();
      const sm = new Path2D();
      sm.moveTo(cx - 4.4, base - 66); sm.quadraticCurveTo(cx, base - 62, cx + 4.4, base - 66);
      line(ctx, sm, '#8a6a58', 0.8, 1.4);
      for (const s of [-1, 1]) {
        ctx.fillStyle = rgba('#2a2430', 0.85);
        ctx.beginPath(); ctx.ellipse(cx + s * 3, base - 71, 1.4, 1.1, 0, 0, Math.PI * 2); ctx.fill();
      }
      // La répétition : deux rémanences du salut, très pâles. Il l'a déjà fait, il le refera.
      for (let i = 1; i <= 2; i++) {
        ctx.save(); ctx.globalAlpha = 0.12 - i * 0.03;
        seg(ctx, cx + 11, base - 56, cx + 22 - i * 4, base - 72 + i * 5, pal.accent, 1, 3.4);
        ctx.restore();
      }
    },
  },

  'jardinier-sans-ombre': {
    catalogKey: 'canon.enemy.jardinier-sans-ombre',
    name: 'Jardinier Sans Ombre', side: 'enemy', role: 'disruptor', family: 'jardin', rarity: 'uncommon',
    registre: 'deni',
    quote: '« Les fleurs sont merveilleuses parce que je coupe tout ce qui ne l’est pas. »',
    silhouette: 'Voûté sur ses massifs, sécateur en main. Rien sous lui : aucun contact au sol.',
    pal: { body: '#3e4a44', deep: '#161e1a', accent: '#86dcb4', light: '#8aa08c', skin: '#c8b8a0' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      // AUCUN CONTACT AU SOL — le seul jeton du bestiaire qui n'en a pas. Il a coupé son ombre.
      // À la place : un liseré clair, net, exactement là où l'ombre devrait commencer.
      ctx.strokeStyle = rgba('#e8e4d8', 0.22); ctx.lineWidth = 1.2;
      ctx.beginPath(); ctx.ellipse(cx, base - 1, 19, 7, 0, 0, Math.PI * 2); ctx.stroke();
      const bd = new Path2D();
      bd.moveTo(cx - 16, base - 2);
      bd.quadraticCurveTo(cx - 20, base - 30, cx - 6, base - 44);
      bd.quadraticCurveTo(cx + 14, base - 50, cx + 20, base - 36);
      bd.quadraticCurveTo(cx + 16, base - 14, cx + 14, base - 2);
      bd.closePath();
      fill(ctx, bd, pal.body); line(ctx, bd, pal.deep, 0.55, 1.2);
      ctx.save(); ctx.clip(bd);
      for (let i = 0; i < 9; i++) {
        const fx = cx - 16 + R() * 34;
        seg(ctx, fx, base - 48, fx + R2(R, -4, 4), base, pal.light, R2(R, 0.08, 0.2), R2(R, 1.4, 3));
      }
      ctx.restore();
      // Le contour instable du Disruptor : trois offsets pâles du même dos voûté.
      for (let i = 1; i <= 3; i++) {
        ctx.save(); ctx.translate(i * 2.4, -i * 1.2);
        line(ctx, bd, '#e8e4d8', 0.1, 1.2);
        ctx.restore();
      }
      // Le sécateur, ouvert, en bas : il taille ce qui est déjà parfait, sans s'arrêter.
      seg(ctx, cx + 10, base - 40, cx + 24, base - 22, pal.skin, 0.9, 3.4);
      seg(ctx, cx + 24, base - 22, cx + 36, base - 26, '#9a96a4', 0.95, 2.2);
      seg(ctx, cx + 24, base - 22, cx + 35, base - 15, '#9a96a4', 0.95, 2.2);
      ctx.beginPath(); ctx.arc(cx + 24, base - 22, 1.6, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.light, 0.8); ctx.fill();
      // Les fleurs déjà parfaites : trois têtes identiques, coupées à la même hauteur.
      for (let i = 0; i < 3; i++) {
        const fx = cx + 30 + i * 7, fy = base - 6 - i * 2;
        seg(ctx, fx, base, fx, fy, '#4a6a4e', 0.8, 1.4);
        ctx.fillStyle = rgba('#e0d0d8', 0.85);
        ctx.beginPath(); ctx.arc(fx, fy, 2.6, 0, Math.PI * 2); ctx.fill();
      }
      // La tête baissée sur l'ouvrage : jamais tournée vers vous, jamais vers le soleil.
      ctx.beginPath(); ctx.ellipse(cx - 4, base - 52, 7.6, 7.4, -0.2, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.skin, 1); ctx.fill();
      ctx.beginPath(); ctx.ellipse(cx - 4, base - 56, 13, 3.6, -0.1, 0, Math.PI * 2);
      ctx.fillStyle = rgba('#c8b276', 0.9); ctx.fill();
      glowDot(ctx, cx + 24, base - 22, 1, pal.accent, 0.25);
    },
  },

  // ═══ GARDIENS DE CRYSTAL ═════════════════════════════════════════════════════════════
  // Des volumes, pas des corps. Lumière interne froide, arêtes franches, aucune matière
  // organique. Ils ne bougent pas : ils changent de position entre deux regards.

  'gardien-intemporel': {
    catalogKey: 'canon.enemy.gardien-intemporel',
    name: 'Gardien Intemporel', side: 'enemy', role: 'bruiser', family: 'crystal', rarity: 'rare',
    registre: 'memoire',
    quote: '« Il gardait déjà. Il gardera encore. »',
    silhouette: 'Colosse translucide, épaules larges et plates. On voit à travers lui.',
    pal: { body: '#3a4468', deep: '#141a30', accent: TOKEN.frost, light: '#c0cbff' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 25, 9.5, 0.4);
      for (const s of [-1, 1]) {
        const lg = poly([P(cx + s * 6, base), P(cx + s * 8, base - 34), P(cx + s * 19, base - 32), P(cx + s * 18, base)]);
        ctx.fillStyle = rgba(pal.deep, 0.72); ctx.fill(lg);
        line(ctx, lg, pal.accent, 0.3, 1.1);
      }
      // Le volume : translucide, donc peint en CREUX. Le fond de la salle passe au travers.
      const bd = poly([
        P(cx - 24, base - 28), P(cx - 20, base - 74), P(cx - 6, base - 84),
        P(cx + 12, base - 82), P(cx + 22, base - 66), P(cx + 20, base - 28),
      ]);
      ctx.save();
      ctx.shadowColor = rgba(pal.accent, 0.28); ctx.shadowBlur = 15;
      const g = ctx.createLinearGradient(cx - 24, base - 84, cx + 22, base - 28);
      g.addColorStop(0, rgba(mix(pal.body, pal.light, 0.3), 0.6));
      g.addColorStop(0.5, rgba(pal.body, 0.45));
      g.addColorStop(1, rgba(pal.deep, 0.7));
      ctx.fillStyle = g; ctx.fill(bd);
      ctx.restore();
      line(ctx, bd, pal.light, 0.5, 1.5);
      // LES OBJETS EN SUSPENSION : des prototypes d'époques impossibles, pris dans la masse.
      ctx.save(); ctx.clip(bd);
      // un marteau qui n'est pas celui du Forgeron
      ctx.save(); ctx.translate(cx - 9, base - 62); ctx.rotate(-0.5);
      ctx.fillStyle = rgba('#8a8296', 0.6); ctx.fillRect(-7, -3, 14, 6);
      ctx.fillStyle = rgba('#6a5a48', 0.6); ctx.fillRect(-1.4, 3, 2.8, 14);
      ctx.restore();
      // une craie qui n'est pas celle de l'Enfant
      ctx.save(); ctx.translate(cx + 8, base - 46); ctx.rotate(0.7);
      ctx.fillStyle = rgba('#e0dcd0', 0.55); ctx.fillRect(-1.6, -7, 3.2, 14);
      ctx.restore();
      // une plume qui n'est pas celle de l'Écrivain
      const fe = new Path2D();
      fe.moveTo(cx - 4, base - 34);
      fe.quadraticCurveTo(cx + 4, base - 42, cx + 2, base - 52);
      line(ctx, fe, '#d8d4e0', 0.5, 2.4);
      // les arêtes internes : la seule texture admise sur du cristal
      for (const [x0, y0, x1, y1] of [[-20, -74, -4, -28], [-6, -84, 8, -30], [12, -82, 18, -34], [-20, -60, 20, -54]]) {
        seg(ctx, cx + x0, base + y0, cx + x1, base + y1, pal.light, 0.24, 1);
      }
      ctx.restore();
      // Épaules plates, larges : deux dalles posées, pas deux muscles.
      for (const s of [-1, 1]) {
        const sh = poly([P(cx + s * 16, base - 76), P(cx + s * 34, base - 70), P(cx + s * 32, base - 58), P(cx + s * 15, base - 62)]);
        ctx.fillStyle = rgba(mix(pal.body, pal.light, 0.18), 0.7); ctx.fill(sh);
        line(ctx, sh, pal.light, 0.4, 1.1);
      }
      // Le regard : pas d'yeux, une réfraction. Deux foyers froids, très écartés.
      for (const s of [-1, 1]) glowDot(ctx, cx + 2 + s * 6, base - 76, 1.3, pal.accent, 0.5);
    },
  },

  'eclat-eveille': {
    catalogKey: 'canon.enemy.eclat-eveille',
    name: 'Éclat Éveillé', side: 'enemy', role: 'skirmisher', family: 'crystal', rarity: 'uncommon',
    registre: 'memoire',
    quote: '« Un joyau qui a fini par comprendre qu’on le regardait. »',
    silhouette: 'Un seul solide flottant, à hauteur de poitrine. Rien ne touche le sol.',
    pal: { body: '#465274', deep: '#161c34', accent: TOKEN.frost, light: '#dce4ff' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      // Il flotte : la lumière qu'il projette au sol tient lieu de contact.
      const fg = ctx.createRadialGradient(cx, base - 2, 1, cx, base - 2, 24);
      fg.addColorStop(0, rgba(pal.accent, 0.24));
      fg.addColorStop(1, rgba(pal.accent, 0));
      ctx.fillStyle = fg;
      ctx.beginPath(); ctx.ellipse(cx, base - 2, 24, 9, 0, 0, Math.PI * 2); ctx.fill();
      const y = base - 44;
      // Le battement : trois halos concentriques, écartés. C'est un cœur, pas une lampe.
      for (let i = 0; i < 3; i++) {
        const hg = ctx.createRadialGradient(cx, y, 1, cx, y, 16 + i * 9);
        hg.addColorStop(0, rgba(pal.accent, 0.2 - i * 0.06));
        hg.addColorStop(1, rgba(pal.accent, 0));
        ctx.fillStyle = hg;
        ctx.beginPath(); ctx.arc(cx, y, 16 + i * 9, 0, Math.PI * 2); ctx.fill();
      }
      // La taille d'un cœur, et la forme aussi : cinq facettes, une pointe en bas.
      const bd = poly([P(cx, y - 15), P(cx + 11, y - 5), P(cx + 7, y + 11), P(cx - 6, y + 13), P(cx - 12, y - 3)]);
      const g = ctx.createLinearGradient(cx - 12, y - 15, cx + 11, y + 13);
      g.addColorStop(0, rgba(pal.light, 0.95));
      g.addColorStop(0.5, rgba(pal.body, 1));
      g.addColorStop(1, rgba(pal.deep, 1));
      ctx.fillStyle = g; ctx.fill(bd);
      line(ctx, bd, '#eaf0ff', 0.55, 1.2);
      for (const [x0, y0, x1, y1] of [[0, -15, -6, 13], [11, -5, -12, -3], [0, -15, 7, 11]]) {
        seg(ctx, cx + x0, y + y0, cx + x1, y + y1, pal.light, 0.3, 0.9);
      }
      // Le foyer interne : décentré, et c'est lui qui vous dévisage.
      const core = ctx.createRadialGradient(cx + 1, y - 1, 0.5, cx + 1, y - 1, 8);
      core.addColorStop(0, rgba('#ffffff', 0.9));
      core.addColorStop(0.4, rgba(pal.accent, 0.5));
      core.addColorStop(1, rgba(pal.accent, 0));
      ctx.fillStyle = core;
      ctx.beginPath(); ctx.arc(cx + 1, y - 1, 8, 0, Math.PI * 2); ctx.fill();
      // Les esquilles qui l'escortent : minuscules, à hauteur constante, immobiles.
      for (const [ox, oy, s] of [[-19, -12, 2], [17, 6, 1.6], [6, -22, 1.4]]) {
        const sp = poly([P(cx + ox, y + oy - s * 2), P(cx + ox + s, y + oy), P(cx + ox, y + oy + s * 2), P(cx + ox - s, y + oy)]);
        fill(ctx, sp, pal.light, 0.7);
      }
    },
  },

  // ═══ ÉCHOS D'ÉMOTIONS · suite ════════════════════════════════════════════════════════

  'echo-peur': {
    catalogKey: 'canon.enemy.echo-peur',
    name: 'Écho de Peur', side: 'enemy', role: 'disruptor', family: 'echos', rarity: 'uncommon',
    registre: 'effroi',
    quote: '« Il guette une sortie qui n’existe plus. Vous êtes entre lui et elle. »',
    silhouette: 'Pâle, étroite, plaquée de côté. Jamais tout à fait là où on la regarde.',
    pal: { body: '#242030', deep: '#0c0a12', accent: '#c8394a', light: '#d8d0e0' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      ctx.fillStyle = rgba(pal.body, 0.16);
      ctx.beginPath(); ctx.ellipse(cx, base - 1, 15, 6, 0, 0, Math.PI * 2); ctx.fill();
      const body = (dx, dy, a, col) => {
        const bd = new Path2D();
        bd.moveTo(cx + dx - 8, base + dy - 2);
        bd.quadraticCurveTo(cx + dx - 13, base + dy - 34, cx + dx - 4, base + dy - 52);
        bd.quadraticCurveTo(cx + dx + 7, base + dy - 58, cx + dx + 9, base + dy - 42);
        bd.quadraticCurveTo(cx + dx + 7, base + dy - 20, cx + dx + 6, base + dy - 2);
        bd.closePath();
        ctx.fillStyle = rgba(col, a); ctx.fill(bd);
        return bd;
      };
      // Les saccades : deux positions antérieures, encore visibles. Il n'a pas glissé, il a sauté.
      body(-13, 1, 0.14, pal.light);
      body(-6, 0, 0.22, pal.light);
      const bd = body(0, 0, 0.94, pal.body);
      line(ctx, bd, pal.light, 0.4, 1.2);
      // Plaqué contre le mur : le côté gauche est net, le droit se dissout. Il longe.
      ctx.save(); ctx.clip(bd);
      const g = ctx.createLinearGradient(cx - 10, 0, cx + 9, 0);
      g.addColorStop(0, rgba(pal.light, 0.32));
      g.addColorStop(1, rgba(pal.deep, 0.5));
      ctx.fillStyle = g; ctx.fillRect(cx - 14, base - 60, 26, 60);
      ctx.restore();
      // Les mains à hauteur de poitrine, refermées : le geste d'une porte trouvée fermée.
      for (const s of [-1, 1]) {
        seg(ctx, cx + s * 3, base - 46, cx + s * 9, base - 34, pal.body, 0.9, 3);
        ctx.fillStyle = rgba(pal.light, 0.5);
        ctx.beginPath(); ctx.arc(cx + s * 9, base - 33, 2.6, 0, Math.PI * 2); ctx.fill();
      }
      // Le regard : porté SUR LE CÔTÉ, vers une sortie hors du cadre. Jamais vers vous.
      ctx.beginPath(); ctx.ellipse(cx + 2, base - 56, 7, 7.6, 0.14, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.body, 1); ctx.fill();
      for (let i = 0; i < 2; i++) {
        ctx.fillStyle = rgba(pal.light, 0.8);
        ctx.beginPath(); ctx.ellipse(cx + 5 + i * 2.4, base - 57, 1.6, 1.2, 0, 0, Math.PI * 2); ctx.fill();
      }
      glowDot(ctx, cx + 7, base - 57, 1.1, pal.accent, 0.45);
    },
  },

  'echo-tristesse': {
    catalogKey: 'canon.enemy.echo-tristesse',
    name: 'Écho de Tristesse', side: 'enemy', role: 'support', family: 'echos', rarity: 'uncommon',
    registre: 'melancolie',
    quote: '« Il ne pleure pas. Il constate, longtemps après tout le monde. »',
    silhouette: 'La forme d’une personne assise — même quand elle se déplace. Basse, tassée.',
    pal: { body: '#242a3e', deep: '#0c1020', accent: '#6f96c8', light: '#a8c0e0' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      // L'air plus épais autour de lui : la lenteur est peinte comme une densité, pas un flou.
      for (let i = 0; i < 5; i++) {
        const g = ctx.createRadialGradient(cx, base - 20, 4, cx, base - 20, 34 - i * 3);
        g.addColorStop(0, rgba(pal.body, 0.1));
        g.addColorStop(1, rgba(pal.body, 0));
        ctx.fillStyle = g;
        ctx.beginPath(); ctx.ellipse(cx, base - 16 - i * 3, 34 - i * 3, 16 - i * 2, 0, 0, Math.PI * 2); ctx.fill();
      }
      // ASSIS MÊME EN MOUVEMENT : les genoux repliés, le dos rond, rien ne touche vraiment.
      const bd = new Path2D();
      bd.moveTo(cx - 20, base - 6);
      bd.quadraticCurveTo(cx - 22, base - 34, cx - 6, base - 44);
      bd.quadraticCurveTo(cx + 12, base - 48, cx + 16, base - 32);
      bd.quadraticCurveTo(cx + 20, base - 14, cx + 18, base - 6);
      bd.quadraticCurveTo(cx, base - 2, cx - 20, base - 6);
      bd.closePath();
      const g = ctx.createLinearGradient(cx, base - 48, cx, base);
      g.addColorStop(0, rgba(mix(pal.body, pal.accent, 0.28), 0.9));
      g.addColorStop(1, rgba(pal.deep, 0.6));
      ctx.fillStyle = g; ctx.fill(bd);
      line(ctx, bd, pal.light, 0.24, 1.2);
      // Les genoux, remontés devant : deux courbes franches, plus claires que le dos.
      for (const s of [-1, 1]) {
        ctx.strokeStyle = rgba(pal.light, 0.3); ctx.lineWidth = 1.6;
        ctx.beginPath(); ctx.arc(cx + s * 10, base - 14, 8, Math.PI, Math.PI * 2); ctx.stroke();
      }
      // Les bras autour des genoux : refermés sur soi, jamais tendus vers un allié.
      const ar = new Path2D();
      ar.moveTo(cx - 15, base - 34);
      ar.quadraticCurveTo(cx, base - 12, cx + 14, base - 30);
      line(ctx, ar, pal.body, 0.95, 3.6);
      // La tête posée sur les genoux : le point haut est très bas. C'est toute la figure.
      ctx.beginPath(); ctx.ellipse(cx + 1, base - 44, 8, 8.2, 0.1, 0, Math.PI * 2);
      ctx.fillStyle = rgba(mix(pal.body, pal.light, 0.2), 1); ctx.fill();
      ctx.save();
      ctx.beginPath(); ctx.ellipse(cx + 1, base - 44, 8, 8.2, 0.1, 0, Math.PI * 2); ctx.clip();
      ctx.fillStyle = rgba(pal.deep, 0.55); ctx.fillRect(cx - 8, base - 52, 18, 7);
      ctx.restore();
      // Ce dont on se souvient en le traversant : des mots qui montent, trop tard, très lents.
      for (let i = 0; i < 5; i++) {
        ctx.fillStyle = rgba(pal.accent, R2(R, 0.1, 0.24));
        ctx.fillRect(cx + R2(R, -14, 12), base - 54 - R() * 22, R2(R, 3, 8), 1.2);
      }
      glowDot(ctx, cx + 1, base - 44, 1.4, pal.accent, 0.3);
    },
  },

  // ═══ IMPÉRATRICE DE LA FALAISE ═══════════════════════════════════════════════════════
  // Une seule figure, et elle ne partage sa famille avec personne. Elle n'a pas de bas :
  // le jeton commence à mi-corps, dans la mer. C'est la seule du bestiaire dans ce cas.

  imperatrice: {
    catalogKey: 'canon.enemy.imperatrice',
    name: 'L’Impératrice', side: 'enemy', role: 'bruiser', family: 'imperatrice', rarity: 'legendary',
    registre: 'melancolie',
    quote: '« Malheureux sont ceux qui croiseront l’impératrice dans ce lieu. »',
    silhouette: 'Démesurée, émergeant à mi-corps. La couronne est plus large que les épaules.',
    pal: { body: '#2e2a44', deep: '#0e0c1a', accent: '#8f6fd0', light: '#b8a8e0', sea: '#3a3260' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      // LA MER EST SA ROBE : les vagues sont l'ourlet, et elles occupent tout le bas du jeton.
      for (let i = 0; i < 6; i++) {
        const yy = base - 2 - i * 5;
        const wv = new Path2D();
        wv.moveTo(cx - 44 + i * 2, yy);
        for (let x = -44 + i * 2; x < 44 - i * 2; x += 11) {
          wv.quadraticCurveTo(cx + x + 5, yy - R2(R, 2, 6), cx + x + 11, yy);
        }
        ctx.strokeStyle = rgba(mix(pal.sea, pal.light, i * 0.09), 0.35 + i * 0.06);
        ctx.lineWidth = R2(R, 1.6, 3.2); ctx.stroke(wv);
      }
      const seaMass = new Path2D();
      seaMass.moveTo(cx - 46, base + 2);
      seaMass.quadraticCurveTo(cx, base - 14, cx + 46, base + 2);
      seaMass.lineTo(cx + 46, base + 6); seaMass.lineTo(cx - 46, base + 6);
      seaMass.closePath();
      fill(ctx, seaMass, pal.deep, 0.7);
      // Le corps : il sort de l'eau sans transition. Aucune hanche, aucune jambe, jamais.
      const bd = new Path2D();
      bd.moveTo(cx - 24, base - 20);
      bd.quadraticCurveTo(cx - 26, base - 60, cx - 10, base - 82);
      bd.quadraticCurveTo(cx + 10, base - 90, cx + 20, base - 70);
      bd.quadraticCurveTo(cx + 24, base - 40, cx + 22, base - 20);
      bd.quadraticCurveTo(cx, base - 10, cx - 24, base - 20);
      bd.closePath();
      ctx.save();
      ctx.shadowColor = rgba(pal.accent, 0.26); ctx.shadowBlur = 16;
      const g = ctx.createLinearGradient(cx - 26, base - 90, cx + 24, base - 10);
      g.addColorStop(0, rgba(mix(pal.body, pal.light, 0.3), 1));
      g.addColorStop(0.55, rgba(pal.body, 1));
      g.addColorStop(1, rgba(pal.deep, 1));
      ctx.fillStyle = g; ctx.fill(bd);
      ctx.restore();
      line(ctx, bd, pal.light, 0.26, 1.4);
      // La robe se dissout dans la mer : des plis qui deviennent des vagues, sans coupure.
      ctx.save(); ctx.clip(bd);
      for (let i = 0; i < 11; i++) {
        const px = cx + R2(R, -22, 20);
        const pp = new Path2D();
        pp.moveTo(px, base - 78);
        pp.quadraticCurveTo(px + R2(R, -6, 6), base - 44, px + R2(R, -9, 9), base - 12);
        line(ctx, pp, R() > 0.5 ? pal.light : pal.deep, R2(R, 0.1, 0.26), R2(R, 1.4, 3.4));
      }
      ctx.restore();
      // Les bras : très longs, ouverts, posés sur la mer. Elle ne les lève pas, elle règne.
      for (const s of [-1, 1]) {
        const ar = new Path2D();
        ar.moveTo(cx + s * 18, base - 72);
        ar.quadraticCurveTo(cx + s * 42, base - 52, cx + s * 44, base - 16);
        line(ctx, ar, pal.body, 1, 5);
        line(ctx, ar, pal.light, 0.18, 1.6);
        glowDot(ctx, cx + s * 44, base - 14, 1.6, pal.accent, 0.35);
      }
      // LA COURONNE : un diadème ET une cage thoracique renversée. Les côtes montent.
      const crownY = base - 104;
      for (let i = -4; i <= 4; i++) {
        const rib = new Path2D();
        const sp = Math.abs(i) / 4;
        rib.moveTo(cx + i * 3, crownY + 14);
        rib.quadraticCurveTo(cx + i * 9, crownY - 4 - (1 - sp) * 10, cx + i * 11, crownY - 18 - (1 - sp) * 14);
        line(ctx, rib, mix(pal.light, '#e8e0f4', 0.4), 0.75 - sp * 0.2, 2.4 - sp * 0.8);
      }
      const band = new Path2D();
      band.moveTo(cx - 17, crownY + 14);
      band.quadraticCurveTo(cx, crownY + 8, cx + 17, crownY + 14);
      line(ctx, band, pal.light, 0.85, 3);
      // Le visage : dans l'ombre de la couronne. Deux foyers, très haut, très écartés.
      ctx.beginPath(); ctx.ellipse(cx + 1, base - 90, 9, 10, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.deep, 0.95); ctx.fill();
      for (const s of [-1, 1]) glowDot(ctx, cx + 1 + s * 3.6, base - 91, 1.3, pal.accent, 0.6);
      // La marée suit ses humeurs : une seule crête, très haute, à contretemps des autres.
      const tide = new Path2D();
      tide.moveTo(cx - 46, base - 4);
      tide.quadraticCurveTo(cx - 20, base - 26, cx + 4, base - 8);
      tide.quadraticCurveTo(cx + 26, base - 20, cx + 46, base - 4);
      line(ctx, tide, pal.light, 0.3, 2);
    },
  },
};


// ── Propositions d'extension ───────────────────────────────────────────────────────────
// Ces figures sont peintes et tenables, mais AUCUN ennemi du catalogue ne les invoque : elles
// n'ont pas de `catalogKey`. Elles vivent donc hors de `ROSTER`, qui doit rester le miroir
// exact des ennemis jouables — un roster qui mélange le réel et l'hypothétique ne peut pas
// servir de source de vérité au moteur. Si l'une d'elles entre au catalogue, elle déménage
// dans `ROSTER` avec sa clé, sans être repeinte.
export const ROSTER_PROPOSITIONS = {
  // ═══ 3.1 CHIMÈRES DES PLAINES — l'animal recomposé de mémoire ════════════════════════
  // Personne n'a jamais bien regardé un animal. Les Chimères sont ce qu'il en reste : des
  // pièces justes assemblées dans le mauvais ordre. Fourrure terne, une seule articulation
  // fausse par figure — c'est elle qui fait le malaise, pas le nombre de têtes.

  'chimere-cornue': {
    name: 'Chimère Cornue', side: 'enemy', role: 'guard', family: 'chimeres', rarity: 'common',
    registre: 'rupture',
    quote: '« Elle a des cornes parce qu’on se souvenait qu’il en fallait. »',
    silhouette: 'Basse, large, quadrupède. Les cornes partent d’un endroit où rien ne devrait pousser.',
    pal: { body: '#5a4a3e', deep: '#241c18', accent: '#c8b28a', light: '#8f7a62' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 26, 9, 0.46);
      for (const ox of [-16, -6, 8, 18]) {
        paintMass(ctx, [P(cx + ox - 4, base), P(cx + ox - 3, base - 20), P(cx + ox + 4, base - 20), P(cx + ox + 5, base)], th, R,
          { base: pal.deep, rim: 0.06 });
      }
      const bd = new Path2D();
      bd.moveTo(cx - 24, base - 18);
      bd.quadraticCurveTo(cx - 26, base - 40, cx - 6, base - 42);
      bd.quadraticCurveTo(cx + 18, base - 44, cx + 24, base - 30);
      bd.quadraticCurveTo(cx + 26, base - 20, cx + 20, base - 16);
      bd.quadraticCurveTo(cx - 4, base - 12, cx - 24, base - 18);
      bd.closePath();
      fill(ctx, bd, pal.body); line(ctx, bd, pal.deep, 0.6, 1.3);
      ctx.save(); ctx.clip(bd);
      for (let i = 0; i < 40; i++) {
        const fx = cx - 24 + R() * 48, fy = base - 44 + R() * 32;
        seg(ctx, fx, fy, fx + R2(R, 1, 4), fy + R2(R, 2, 5), R() > 0.5 ? pal.light : pal.deep, R2(R, 0.1, 0.28), 1);
      }
      ctx.restore();
      // Tête basse, portée trop en avant du corps : le cou est plus long qu'un cou.
      seg(ctx, cx - 20, base - 36, cx - 34, base - 26, pal.body, 1, 8);
      const hd = new Path2D();
      hd.moveTo(cx - 30, base - 32); hd.quadraticCurveTo(cx - 44, base - 32, cx - 46, base - 22);
      hd.quadraticCurveTo(cx - 38, base - 17, cx - 28, base - 22); hd.closePath();
      fill(ctx, hd, shade(pal.body, 0.08)); line(ctx, hd, pal.deep, 0.5, 1.1);
      // L'ARTICULATION FAUSSE : les cornes sortent de la mâchoire, pas du crâne.
      for (const s of [0, 1]) {
        const co = new Path2D();
        co.moveTo(cx - 40 + s * 5, base - 24);
        co.quadraticCurveTo(cx - 46 - s * 3, base - 36, cx - 36 - s * 6, base - 44);
        line(ctx, co, pal.accent, 0.9, 3.2);
      }
      ctx.fillStyle = rgba('#0a0810', 0.9);
      ctx.beginPath(); ctx.ellipse(cx - 38, base - 27, 2.2, 1.4, 0.2, 0, Math.PI * 2); ctx.fill();
    },
  },

  'levraut-double': {
    name: 'Levraut Double', side: 'enemy', role: 'swarm', family: 'chimeres', rarity: 'common',
    registre: 'rupture',
    quote: '« Deux corps, un seul souvenir de fuir. »',
    silhouette: 'Petits, par trois. Chacun a une paire de pattes de trop, repliée contre le flanc.',
    pal: { body: '#6e5f52', deep: '#2a2320', accent: '#d8c8a8', light: '#9a8a74' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 30, 10, 0.42);
      for (const [ox, oy, sc] of [[-19, -3, 0.82], [4, -7, 0.95], [19, 3, 0.76]]) {
        ctx.save(); ctx.translate(cx + ox, base + oy); ctx.scale(sc, sc);
        contact(ctx, 0, 1, 11, 4, 0.3);
        const bd = new Path2D();
        bd.moveTo(-11, -5); bd.quadraticCurveTo(-9, -17, 3, -17);
        bd.quadraticCurveTo(13, -16, 14, -7); bd.quadraticCurveTo(6, -2, -11, -5);
        bd.closePath();
        fill(ctx, bd, pal.body); line(ctx, bd, pal.deep, 0.6, 1.1);
        for (const lx of [-6, 4, 10]) seg(ctx, lx, -5, lx + R2(R, -1, 1), 1, pal.deep, 1, 2.4);
        // La paire de trop : repliée haut contre le flanc, inutile, jamais posée au sol.
        for (const s of [-1, 1]) seg(ctx, 0, -12, s * 7, -19, pal.light, 0.75, 2);
        const hd = new Path2D();
        hd.moveTo(10, -14); hd.quadraticCurveTo(20, -14, 20, -8);
        hd.quadraticCurveTo(14, -5, 10, -8); hd.closePath();
        fill(ctx, hd, shade(pal.body, 0.1));
        // Oreilles longues, l'une cassée : elles écoutent une chose qui n'arrive pas.
        seg(ctx, 13, -14, 15, -26, pal.body, 0.9, 2.2);
        seg(ctx, 17, -14, 22, -21, pal.body, 0.9, 2.2);
        ctx.fillStyle = rgba(pal.accent, 0.8);
        ctx.beginPath(); ctx.arc(16, -10, 1.2, 0, Math.PI * 2); ctx.fill();
        ctx.restore();
      }
    },
  },

  'grand-cerf-faux': {
    name: 'Le Grand Cerf Faux', side: 'enemy', role: 'bruiser', family: 'chimeres', rarity: 'rare',
    registre: 'rupture',
    quote: '« Sa ramure continue de pousser vers l’intérieur. »',
    silhouette: 'Très haut, bipède mais chevalin. La ramure occupe plus de place que le corps.',
    pal: { body: '#4a3c38', deep: '#1c1614', accent: '#e0cba0', light: '#8a7466' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 22, 8.5, 0.52);
      for (const s of [-1, 1]) {
        const lg = new Path2D();
        lg.moveTo(cx + s * 7, base - 28);
        lg.quadraticCurveTo(cx + s * 14, base - 18, cx + s * 8, base - 8);
        lg.quadraticCurveTo(cx + s * 4, base - 2, cx + s * 12, base);
        line(ctx, lg, pal.body, 1, 6);
      }
      const bd = new Path2D();
      bd.moveTo(cx - 13, base - 26);
      bd.quadraticCurveTo(cx - 18, base - 54, cx - 6, base - 68);
      bd.quadraticCurveTo(cx + 12, base - 72, cx + 14, base - 52);
      bd.quadraticCurveTo(cx + 13, base - 34, cx + 9, base - 26);
      bd.closePath();
      fill(ctx, bd, pal.body); line(ctx, bd, pal.light, 0.28, 1.2);
      seg(ctx, cx + 6, base - 62, cx + 20, base - 40, pal.body, 1, 4.6);
      const hd = new Path2D();
      hd.moveTo(cx - 4, base - 74); hd.quadraticCurveTo(cx + 8, base - 80, cx + 10, base - 90);
      hd.quadraticCurveTo(cx - 2, base - 94, cx - 8, base - 84); hd.closePath();
      fill(ctx, hd, shade(pal.body, 0.1)); line(ctx, hd, pal.deep, 0.5, 1.1);
      // LA RAMURE QUI POUSSE VERS L'INTÉRIEUR : les pointes reviennent vers le crâne.
      for (const s of [-1, 1]) {
        for (let i = 0; i < 3; i++) {
          const br = new Path2D();
          const y0 = base - 88 - i * 4;
          br.moveTo(cx + s * 3, y0);
          br.quadraticCurveTo(cx + s * (20 + i * 8), y0 - 14 - i * 6, cx + s * (10 + i * 4), y0 - 30 - i * 8);
          br.quadraticCurveTo(cx + s * 2, y0 - 34 - i * 6, cx + s * 6, y0 - 20 - i * 4);
          line(ctx, br, pal.accent, 0.7 - i * 0.12, 2.4 - i * 0.4);
        }
      }
      glowDot(ctx, cx + 3, base - 86, 1.4, pal.accent, 0.4);
    },
  },

  // ═══ 3.2 CRÉATIONS DU FORGERON — l'outil qui a continué sans main ════════════════════
  // Métal battu, rivets, une braise interne. Aucun visage : la chaleur est le seul signe
  // de vie, et elle sort par les joints, jamais par des yeux.

  'automate-soufflet': {
    name: 'Automate à Soufflet', side: 'enemy', role: 'guard', family: 'forgeron', rarity: 'common',
    registre: 'rupture',
    quote: '« Il respire, mais c’est un outil qui respire. »',
    silhouette: 'Cube bas sur trois pieds courts, un soufflet en accordéon sur le dessus.',
    pal: { body: '#4a4650', deep: '#1e1c26', accent: TOKEN.ember, light: '#8e8a98' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 24, 9, 0.5);
      for (const ox of [-15, 0, 15]) {
        paintMass(ctx, [P(cx + ox - 4, base), P(cx + ox - 3, base - 14), P(cx + ox + 3, base - 14), P(cx + ox + 4, base)], th, R,
          { base: pal.deep, rim: 0.1 });
      }
      paintMass(ctx, [P(cx - 22, base - 12), P(cx - 20, base - 42), P(cx + 20, base - 42), P(cx + 22, base - 12)], th, R,
        { base: pal.body, rim: 0.16 });
      // Rivets : réguliers, nombreux, c'est la texture principale.
      for (let i = 0; i < 5; i++) for (const yy of [-38, -18]) {
        const rx = cx - 16 + i * 8;
        ctx.fillStyle = rgba(pal.light, 0.5);
        ctx.beginPath(); ctx.arc(rx, base + yy, 1.3, 0, Math.PI * 2); ctx.fill();
      }
      // La braise sort par le joint horizontal, pas par un œil.
      const jg = ctx.createLinearGradient(cx - 20, base - 28, cx + 20, base - 28);
      jg.addColorStop(0, rgba(pal.accent, 0.05));
      jg.addColorStop(0.5, rgba(pal.accent, 0.75));
      jg.addColorStop(1, rgba(pal.accent, 0.1));
      ctx.fillStyle = jg; ctx.fillRect(cx - 20, base - 29, 40, 2.4);
      // Le soufflet : accordéon, à demi comprimé — il vient d'expirer.
      for (let i = 0; i < 5; i++) {
        const yy = base - 44 - i * 5;
        const hw = 13 - i * 0.6;
        ctx.beginPath();
        ctx.moveTo(cx - hw, yy); ctx.quadraticCurveTo(cx, yy - 3, cx + hw, yy);
        ctx.strokeStyle = rgba(i % 2 ? pal.deep : pal.light, 0.7); ctx.lineWidth = 3.4;
        ctx.stroke();
      }
      seg(ctx, cx, base - 70, cx, base - 78, pal.body, 1, 4);
      glowDot(ctx, cx, base - 80, 1.8, pal.accent, 0.55);
    },
  },

  'enclume-marchante': {
    name: 'Enclume Marchante', side: 'enemy', role: 'bruiser', family: 'forgeron', rarity: 'elite',
    registre: 'rupture',
    quote: '« Ce n’est pas elle qui frappe. C’est elle qu’on frappe, et elle s’est levée. »',
    silhouette: 'Masse d’acier haute et déportée, jambes trop courtes. Le poids est en haut.',
    pal: { body: '#3e3a46', deep: '#16141c', accent: TOKEN.ember, light: '#9a96a6' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 25, 9.5, 0.56);
      for (const s of [-1, 1]) {
        paintMass(ctx, [P(cx + s * 6 - 5, base), P(cx + s * 5 - 5, base - 20), P(cx + s * 13 - 5, base - 20), P(cx + s * 14 - 5, base)], th, R,
          { base: pal.deep, rim: 0.12 });
      }
      // La silhouette d'enclume : corne à gauche, talon à droite, tout le poids en haut.
      const bd = new Path2D();
      bd.moveTo(cx - 12, base - 18);
      bd.lineTo(cx - 16, base - 40);
      bd.quadraticCurveTo(cx - 40, base - 46, cx - 34, base - 54);
      bd.lineTo(cx + 22, base - 58);
      bd.lineTo(cx + 26, base - 44);
      bd.lineTo(cx + 14, base - 40);
      bd.lineTo(cx + 10, base - 18);
      bd.closePath();
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.6); ctx.shadowBlur = 11;
      const g = ctx.createLinearGradient(cx - 34, 0, cx + 26, 0);
      g.addColorStop(0, rgba(shade(pal.body, 0.2), 1));
      g.addColorStop(0.55, rgba(pal.body, 1));
      g.addColorStop(1, rgba(pal.deep, 1));
      ctx.fillStyle = g; ctx.fill(bd);
      ctx.restore();
      line(ctx, bd, pal.light, 0.3, 1.3);
      gravures(ctx, R, cx - 30, base - 56, 52, 16, pal.light, 16);
      // Les impacts de marteau, en creux, sur la face du dessus.
      for (let i = 0; i < 7; i++) {
        const ix = cx - 28 + R() * 50;
        ctx.beginPath(); ctx.ellipse(ix, base - 56, R2(R, 2, 4.5), 1.4, 0, 0, Math.PI * 2);
        ctx.fillStyle = rgba('#000000', R2(R, 0.2, 0.4)); ctx.fill();
      }
      const jg = ctx.createLinearGradient(cx - 16, 0, cx + 14, 0);
      jg.addColorStop(0, rgba(pal.accent, 0.6));
      jg.addColorStop(1, rgba(pal.accent, 0.08));
      ctx.fillStyle = jg; ctx.fillRect(cx - 14, base - 40, 28, 2);
      for (let i = 0; i < 4; i++) glowDot(ctx, cx - 10 + R() * 24, base - 62 - R() * 10, 1, pal.accent, 0.3);
    },
  },

  'clou-vivant': {
    name: 'Clou Vivant', side: 'enemy', role: 'skirmisher', family: 'forgeron', rarity: 'common',
    registre: 'rupture',
    quote: '« Il cherche la planche qu’on lui a promise. »',
    silhouette: 'Une tige de fer debout, penchée, la tête plate en haut. Presque pas de largeur.',
    pal: { body: '#5a5460', deep: '#221e28', accent: '#d8623c', light: '#a8a2b2' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 11, 5, 0.44);
      ctx.save(); ctx.translate(cx, base); ctx.rotate(-0.14);
      const bd = new Path2D();
      bd.moveTo(-2, 0); bd.lineTo(-5, -52); bd.lineTo(5, -52); bd.lineTo(2, 0);
      bd.closePath();
      fill(ctx, bd, pal.body); line(ctx, bd, pal.light, 0.35, 1.1);
      // La tête plate, martelée, un peu de travers : la seule vraie surface de la figure.
      const hd = new Path2D();
      hd.moveTo(-11, -52); hd.lineTo(11, -54); hd.lineTo(9, -60); hd.lineTo(-9, -58);
      hd.closePath();
      fill(ctx, hd, shade(pal.body, 0.16)); line(ctx, hd, pal.deep, 0.6, 1.2);
      // Rouille sèche : la seule note colorée, et elle coule vers le bas.
      for (let i = 0; i < 6; i++) {
        const rx = R2(R, -4, 4);
        seg(ctx, rx, -50, rx + R2(R, -1.5, 1.5), -50 + R2(R, 8, 26), pal.accent, R2(R, 0.15, 0.4), R2(R, 0.8, 1.8));
      }
      // Les deux membres grêles : forgés à part, rivetés au fût. Il court penché.
      for (const s of [-1, 1]) {
        seg(ctx, s * 2, -34, s * 12, -22, pal.body, 0.95, 2.2);
        seg(ctx, s * 12, -22, s * 9 + 4, -6, pal.body, 0.95, 2);
      }
      glowDot(ctx, 0, -56, 1.1, pal.accent, 0.4);
      ctx.restore();
    },
  },

  // ═══ 3.3 PÉNITENTS DE LA MONTAGNE — la posture qui ne se relève pas ═══════════════════
  // Pierre grise, chaînes, cordes. Tous à genoux ou courbés, jamais debout : la silhouette
  // est plus basse que celle de n'importe quelle autre famille. La honte tient lieu d'armure.

  'penitent-agenouille': {
    name: 'Pénitent Agenouillé', side: 'enemy', role: 'guard', family: 'penitents', rarity: 'common',
    registre: 'melancolie',
    quote: '« Il attend un pardon qui n’a jamais été prévu. »',
    silhouette: 'À genoux, front au sol, dos très large. Occupe la case sans dépasser en hauteur.',
    pal: { body: '#4e4c58', deep: '#1e1e28', accent: '#8fa8c0', light: '#7e7c8c' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 27, 9.5, 0.5);
      // Le dos courbé EST la silhouette : une voûte de pierre, front au sol.
      const bk = new Path2D();
      bk.moveTo(cx - 26, base);
      bk.quadraticCurveTo(cx - 24, base - 34, cx + 2, base - 38);
      bk.quadraticCurveTo(cx + 22, base - 36, cx + 24, base - 8);
      bk.quadraticCurveTo(cx + 14, base, cx - 26, base);
      bk.closePath();
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.58); ctx.shadowBlur = 10;
      const g = ctx.createLinearGradient(cx, base - 40, cx, base);
      g.addColorStop(0, rgba(shade(pal.body, 0.14), 1));
      g.addColorStop(1, rgba(pal.deep, 1));
      ctx.fillStyle = g; ctx.fill(bk);
      ctx.restore();
      ctx.save(); ctx.clip(bk);
      for (let i = 0; i < 7; i++) {
        const yy = base - 4 - i * 5;
        ctx.beginPath();
        ctx.moveTo(cx - 26, yy); ctx.quadraticCurveTo(cx, yy - R2(R, 3, 8), cx + 24, yy + R2(R, -2, 2));
        ctx.strokeStyle = rgba('#000000', R2(R, 0.15, 0.32)); ctx.lineWidth = R2(R, 1, 2.2);
        ctx.stroke();
      }
      ctx.restore();
      // La chaîne : elle part de la nuque et va au sol. Aucun anneau n'est ouvert.
      let px = cx - 20, py = base - 32;
      for (let i = 0; i < 8; i++) {
        const nx = px - 1.5 + R2(R, -1, 1), ny = py + 4.2;
        ctx.beginPath(); ctx.ellipse((px + nx) / 2, (py + ny) / 2, 2.2, 3, 0.2, 0, Math.PI * 2);
        ctx.strokeStyle = rgba(pal.light, 0.6); ctx.lineWidth = 1.1; ctx.stroke();
        px = nx; py = ny;
      }
      // Le front touche la case : la tête est le point le plus bas, pas le plus haut.
      ctx.beginPath(); ctx.ellipse(cx + 20, base - 6, 7, 6, 0.3, 0, Math.PI * 2);
      ctx.fillStyle = rgba(shade(pal.body, 0.08), 1); ctx.fill();
      glowDot(ctx, cx + 22, base - 4, 1.1, pal.accent, 0.3);
    },
  },

  'porte-chaine': {
    name: 'Porte-Chaîne', side: 'enemy', role: 'bruiser', family: 'penitents', rarity: 'uncommon',
    registre: 'melancolie',
    quote: '« Le poids est à lui. Il ne le partagera pas. »',
    silhouette: 'Haut mais plié en deux par la charge. Un bloc de pierre pend dans son dos.',
    pal: { body: '#4a4854', deep: '#1c1c26', accent: '#8fa8c0', light: '#82808e', skin: '#a49a92' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 23, 9, 0.54);
      for (const s of [-1, 1]) {
        paintMass(ctx, [P(cx + s * 6 - 3, base), P(cx + s * 5 - 3, base - 26), P(cx + s * 12 - 3, base - 26), P(cx + s * 13 - 3, base)], th, R,
          { base: pal.deep, rim: 0.08 });
      }
      // Plié en deux : le torse part en avant, presque à l'horizontale.
      const bd = new Path2D();
      bd.moveTo(cx - 10, base - 24);
      bd.quadraticCurveTo(cx - 18, base - 48, cx - 2, base - 56);
      bd.quadraticCurveTo(cx + 20, base - 60, cx + 22, base - 46);
      bd.quadraticCurveTo(cx + 12, base - 34, cx + 8, base - 24);
      bd.closePath();
      fill(ctx, bd, pal.body); line(ctx, bd, pal.light, 0.3, 1.2);
      // Le bloc dans le dos : il pèse plus que la figure, et il est peint plus dur qu'elle.
      paintMass(ctx, [P(cx - 34, base - 30), P(cx - 32, base - 58), P(cx - 8, base - 62), P(cx - 6, base - 34)], th, R,
        { base: shade(pal.body, -0.18), rim: 0.2 });
      gravures(ctx, R, cx - 32, base - 58, 24, 26, pal.accent, 12);
      for (let i = 0; i < 6; i++) {
        ctx.beginPath();
        ctx.ellipse(cx - 12 + i * 3, base - 54 + i * 2, 2.4, 3.2, 0.4, 0, Math.PI * 2);
        ctx.strokeStyle = rgba(pal.light, 0.55); ctx.lineWidth = 1.2; ctx.stroke();
      }
      seg(ctx, cx + 10, base - 52, cx + 24, base - 32, pal.skin, 0.85, 3.6);
      // La tête, tournée vers le sol : elle regarde ses propres pieds, jamais la cible.
      ctx.beginPath(); ctx.ellipse(cx + 20, base - 50, 7.4, 7, 0.4, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.skin, 0.95); ctx.fill();
      ctx.save();
      ctx.beginPath(); ctx.ellipse(cx + 20, base - 50, 7.4, 7, 0.4, 0, Math.PI * 2); ctx.clip();
      ctx.fillStyle = rgba(pal.deep, 0.6); ctx.fillRect(cx + 12, base - 58, 18, 6);
      ctx.restore();
    },
  },

  'cierge-marcheur': {
    name: 'Cierge Marcheur', side: 'enemy', role: 'support', family: 'penitents', rarity: 'common',
    registre: 'melancolie',
    quote: '« Tant qu’il brûle, la peine reste comptée. »',
    silhouette: 'Vertical, étroit, immobile. Une flamme unique portée devant, à hauteur de poitrine.',
    pal: { body: '#484654', deep: '#1a1a24', accent: '#e8c98a', light: '#d8d0c0' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 15, 6.5, 0.46);
      const p = drape(ctx, R, cx, base, { top: base - 58, halfTop: 9, halfBot: 15, col: pal.body, deep: pal.deep, plis: 8, glow: pal.accent });
      hood(ctx, cx, base - 64, 9, 10, pal.body, pal.deep);
      // La cire coule sur la bure : elle a coulé longtemps, et personne ne l'a grattée.
      ctx.save(); ctx.clip(p);
      for (let i = 0; i < 5; i++) {
        const wx = cx - 10 + R() * 20;
        const wp = new Path2D();
        wp.moveTo(wx, base - 44);
        wp.quadraticCurveTo(wx + R2(R, -2, 2), base - 30, wx + R2(R, -3, 3), base - 12 - R() * 8);
        line(ctx, wp, pal.light, R2(R, 0.25, 0.5), R2(R, 1.4, 3));
      }
      ctx.restore();
      // Le cierge : tenu à deux mains, devant le corps, exactement à la même hauteur toujours.
      seg(ctx, cx - 7, base - 44, cx - 1, base - 40, pal.light, 0.8, 2.8);
      seg(ctx, cx + 7, base - 44, cx + 1, base - 40, pal.light, 0.8, 2.8);
      ctx.fillStyle = rgba(pal.light, 0.95);
      ctx.fillRect(cx - 2.2, base - 62, 4.4, 22);
      const fl = new Path2D();
      fl.moveTo(cx - 2.4, base - 62);
      fl.quadraticCurveTo(cx - 3, base - 70, cx, base - 76);
      fl.quadraticCurveTo(cx + 3, base - 70, cx + 2.4, base - 62);
      fl.closePath();
      const fg = ctx.createLinearGradient(cx, base - 76, cx, base - 60);
      fg.addColorStop(0, rgba('#fff4dc', 0.95));
      fg.addColorStop(1, rgba(pal.accent, 0.5));
      ctx.fillStyle = fg; ctx.fill(fl);
      glowDot(ctx, cx, base - 68, 2.2, pal.accent, 0.5);
    },
  },

  // ═══ 3.4 FAUX HABITANTS DU JARDIN — la nature refaite à la main ═══════════════════════
  // Vert de sève, cire, taille au sécateur. Rien n'a poussé : tout a été fabriqué pour
  // ressembler à ce qui pousse. Les silhouettes sont trop régulières pour être vivantes.

  'buisson-taille': {
    name: 'Buisson Taillé', side: 'enemy', role: 'guard', family: 'jardin', rarity: 'common',
    registre: 'deni',
    quote: '« Il a la forme d’un animal que personne n’a nommé. »',
    silhouette: 'Masse végétale basse et large, aux angles nets. La taille est géométrique.',
    pal: { body: '#2e4a3a', deep: '#122018', accent: TOKEN.sap, light: '#5e8a68' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 27, 9.5, 0.48);
      // Taille géométrique : les arêtes sont droites, ce qui est le seul indice de faux.
      const bd = new Path2D();
      bd.moveTo(cx - 26, base);
      bd.lineTo(cx - 24, base - 30);
      bd.lineTo(cx - 10, base - 42);
      bd.lineTo(cx + 14, base - 42);
      bd.lineTo(cx + 24, base - 28);
      bd.lineTo(cx + 26, base);
      bd.closePath();
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.5); ctx.shadowBlur = 9;
      const g = ctx.createLinearGradient(cx, base - 44, cx, base);
      g.addColorStop(0, rgba(pal.light, 1));
      g.addColorStop(0.5, rgba(pal.body, 1));
      g.addColorStop(1, rgba(pal.deep, 1));
      ctx.fillStyle = g; ctx.fill(bd);
      ctx.restore();
      ctx.save(); ctx.clip(bd);
      for (let i = 0; i < 90; i++) {
        const lx = cx - 26 + R() * 52, ly = base - 44 + R() * 44;
        ctx.fillStyle = rgba(R() > 0.6 ? pal.light : pal.deep, R2(R, 0.15, 0.5));
        ctx.beginPath(); ctx.ellipse(lx, ly, R2(R, 1, 2.6), R2(R, 0.8, 1.6), R() * 3, 0, Math.PI * 2); ctx.fill();
      }
      ctx.restore();
      // Les coupes fraîches : des sections claires, régulières, sur le pourtour.
      for (let i = 0; i < 10; i++) {
        const a = -Math.PI + R() * Math.PI;
        const px = cx + Math.cos(a) * 24, py = base - 22 + Math.sin(a) * 20;
        ctx.fillStyle = rgba('#c8b884', 0.5);
        ctx.beginPath(); ctx.arc(px, py, 1.1, 0, Math.PI * 2); ctx.fill();
      }
      // Deux fentes sombres où l'on suppose des yeux — le jardinier les a taillées là.
      for (const s of [-1, 1]) {
        ctx.beginPath(); ctx.ellipse(cx + s * 8, base - 32, 2.8, 1.4, 0, 0, Math.PI * 2);
        ctx.fillStyle = rgba('#05080a', 0.85); ctx.fill();
      }
      glowDot(ctx, cx - 8, base - 32, 0.9, pal.accent, 0.3);
    },
  },

  'jardinier-cire': {
    name: 'Jardinier de Cire', side: 'enemy', role: 'support', family: 'jardin', rarity: 'uncommon',
    registre: 'deni',
    quote: '« Tout pousse très bien. Regardez comme tout pousse bien. »',
    silhouette: 'Vertical, tablier, sécateur tenu devant. Le visage a fondu et coulé sur le col.',
    pal: { body: '#3e4a44', deep: '#161e1a', accent: '#e8dcc0', light: '#8aa08c', skin: '#e0d8c4' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 17, 7);
      const p = drape(ctx, R, cx, base, { top: base - 52, halfTop: 11, halfBot: 17, col: pal.body, deep: pal.deep, plis: 7 });
      // Le tablier : une surface claire, plate, en avant du corps. Tachée de sève.
      const ap = new Path2D();
      ap.moveTo(cx - 9, base - 46); ap.lineTo(cx + 9, base - 46);
      ap.lineTo(cx + 11, base - 8); ap.lineTo(cx - 11, base - 8);
      ap.closePath();
      fill(ctx, ap, pal.accent, 0.85); line(ctx, ap, pal.deep, 0.4, 1.1);
      ctx.save(); ctx.clip(ap);
      for (let i = 0; i < 7; i++) {
        ctx.beginPath();
        ctx.ellipse(cx + R2(R, -9, 9), base - 40 + R() * 30, R2(R, 1.4, 4), R2(R, 1, 3), R() * 3, 0, Math.PI * 2);
        ctx.fillStyle = rgba(TOKEN.sap, R2(R, 0.12, 0.32)); ctx.fill();
      }
      ctx.restore();
      // Le sécateur, ouvert, tenu devant : l'objet du Support est toujours en évidence.
      seg(ctx, cx - 9, base - 42, cx + 4, base - 36, pal.skin, 0.85, 3.2);
      seg(ctx, cx + 4, base - 36, cx + 16, base - 44, '#9a96a4', 0.95, 2.4);
      seg(ctx, cx + 4, base - 36, cx + 18, base - 32, '#9a96a4', 0.95, 2.4);
      ctx.beginPath(); ctx.arc(cx + 4, base - 36, 1.6, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.light, 0.8); ctx.fill();
      // La tête : de la cire qui a coulé. Aucun trait ne subsiste, mais la coulée est nette.
      ctx.beginPath(); ctx.ellipse(cx, base - 60, 8, 9, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.skin, 1); ctx.fill();
      const dr = new Path2D();
      dr.moveTo(cx - 5, base - 56);
      dr.quadraticCurveTo(cx - 4, base - 48, cx - 6, base - 44);
      dr.lineTo(cx + 5, base - 44);
      dr.quadraticCurveTo(cx + 4, base - 50, cx + 6, base - 56);
      dr.closePath();
      fill(ctx, dr, pal.skin, 0.95);
      for (const s of [-1, 1]) {
        const dp = new Path2D();
        dp.moveTo(cx + s * 3, base - 60);
        dp.quadraticCurveTo(cx + s * 4, base - 52, cx + s * 2, base - 46);
        line(ctx, dp, shade(pal.skin, -0.28), 0.55, 1.6);
      }
      // Un chapeau de paille, parfaitement droit : le seul élément non fondu.
      ctx.beginPath(); ctx.ellipse(cx, base - 68, 15, 4, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba('#c8b276', 0.9); ctx.fill();
    },
  },

  'epouvantail-poli': {
    name: 'Épouvantail Poli', side: 'enemy', role: 'disruptor', family: 'jardin', rarity: 'common',
    registre: 'deni',
    quote: '« Il s’excuse pendant qu’il vous barre le passage. »',
    silhouette: 'Croix de bois habillée, contour flottant. Un bras est toujours levé, en salut.',
    pal: { body: '#4a4034', deep: '#1c1810', accent: '#d9a441', light: '#8a7a5e' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 13, 5.5, 0.4);
      // Le contour instable du Disruptor : le tissu déborde de la structure, en tremblé.
      ctx.save();
      for (let i = 0; i < 3; i++) {
        const halo = new Path2D();
        halo.moveTo(cx - 20 - i * 3, base - 6);
        halo.quadraticCurveTo(cx - 12, base - 44 - i * 4, cx + 2, base - 56 - i * 3);
        halo.quadraticCurveTo(cx + 16, base - 44, cx + 20 + i * 3, base - 6);
        line(ctx, halo, pal.body, 0.14 - i * 0.03, 4);
      }
      ctx.restore();
      seg(ctx, cx, base, cx, base - 58, '#3e3428', 1, 4.4);
      seg(ctx, cx - 20, base - 44, cx + 22, base - 48, '#3e3428', 1, 3.4);
      // Le vêtement : trop grand, jamais tendu, agité par un air qui n'existe pas.
      const cl = new Path2D();
      cl.moveTo(cx - 16, base - 46);
      cl.quadraticCurveTo(cx - 20, base - 20, cx - 12, base - 4);
      cl.quadraticCurveTo(cx, base - 10, cx + 13, base - 4);
      cl.quadraticCurveTo(cx + 19, base - 22, cx + 16, base - 46);
      cl.closePath();
      fill(ctx, cl, pal.body); line(ctx, cl, pal.deep, 0.5, 1.2);
      ctx.save(); ctx.clip(cl);
      for (let i = 0; i < 9; i++) {
        const sx = cx - 16 + R() * 32;
        seg(ctx, sx, base - 46, sx + R2(R, -4, 4), base - 4, pal.light, R2(R, 0.08, 0.22), R2(R, 1, 2.4));
      }
      ctx.restore();
      // De la paille qui sort aux poignets : la seule note claire, et elle est morte.
      for (const [ex, ey] of [[cx - 20, base - 44], [cx + 22, base - 48]]) {
        for (let i = 0; i < 5; i++) seg(ctx, ex, ey, ex + R2(R, -6, 6), ey + R2(R, -6, 6), '#c8b276', R2(R, 0.3, 0.6), 1);
      }
      // Le bras levé, en salut permanent — c'est le geste, pas la menace, qui inquiète.
      seg(ctx, cx + 22, base - 48, cx + 30, base - 64, '#3e3428', 1, 3);
      // Sac de toile pour tête, cousu, avec un sourire brodé trop régulier.
      ctx.beginPath(); ctx.ellipse(cx, base - 64, 9, 10, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba('#a89a7e', 1); ctx.fill();
      const sm = new Path2D();
      sm.moveTo(cx - 5, base - 62); sm.quadraticCurveTo(cx, base - 57, cx + 5, base - 62);
      line(ctx, sm, '#4a3c28', 0.85, 1.4);
      for (const s of [-1, 1]) {
        ctx.beginPath();
        ctx.moveTo(cx + s * 4 - 2, base - 69); ctx.lineTo(cx + s * 4 + 2, base - 66);
        ctx.moveTo(cx + s * 4 + 2, base - 69); ctx.lineTo(cx + s * 4 - 2, base - 66);
        ctx.strokeStyle = rgba('#4a3c28', 0.9); ctx.lineWidth = 1.2; ctx.stroke();
      }
      glowDot(ctx, cx, base - 71, 1, pal.accent, 0.25);
    },
  },

  // ═══ 3.5 GARDIENS DE CRYSTAL — la lumière qui garde une salle vide ═══════════════════
  // Facettes, arêtes dures, une lumière interne froide. Aucune matière organique : ce sont
  // des volumes. Ils ne bougent pas, ils changent de position entre deux regards.

  'gardien-facette': {
    name: 'Gardien à Facettes', side: 'enemy', role: 'guard', family: 'crystal', rarity: 'common',
    registre: 'silence',
    quote: '« Il tient la porte d’une salle qui n’a plus rien à garder. »',
    silhouette: 'Bloc trapézoïdal large, posé à plat. Arêtes franches, aucune courbe.',
    pal: { body: '#3a4468', deep: '#141a30', accent: TOKEN.frost, light: '#9aa8e0' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 26, 9.5, 0.46);
      const facets = [
        [[-26, 0], [-20, -30], [-4, -36], [-6, 0]],
        [[-6, 0], [-4, -36], [12, -40], [10, 0]],
        [[10, 0], [12, -40], [24, -26], [26, 0]],
      ];
      facets.forEach((f, i) => {
        const p = poly(f.map(([x, y]) => P(cx + x, base + y)));
        const g = ctx.createLinearGradient(cx - 26, base - 40, cx + 26, base);
        g.addColorStop(0, rgba(mix(pal.body, pal.light, 0.3 + i * 0.14), 1));
        g.addColorStop(1, rgba(pal.deep, 1));
        ctx.fillStyle = g; ctx.fill(p);
        line(ctx, p, pal.accent, 0.28 + i * 0.08, 1.1);
      });
      // La lumière est INTERNE : elle sort du cœur du volume, pas de sa surface.
      const core = ctx.createRadialGradient(cx + 2, base - 22, 1, cx + 2, base - 22, 22);
      core.addColorStop(0, rgba('#eaf0ff', 0.6));
      core.addColorStop(0.4, rgba(pal.accent, 0.22));
      core.addColorStop(1, rgba(pal.accent, 0));
      ctx.fillStyle = core;
      ctx.beginPath(); ctx.arc(cx + 2, base - 22, 22, 0, Math.PI * 2); ctx.fill();
      // Éclats satellites : ils flottent, immobiles, à hauteur constante.
      for (const [ox, oy, s] of [[-30, -34, 3], [28, -40, 2.4], [4, -50, 2]]) {
        const sp = poly([P(cx + ox, base + oy - s * 2), P(cx + ox + s, base + oy), P(cx + ox, base + oy + s * 2), P(cx + ox - s, base + oy)]);
        fill(ctx, sp, pal.light, 0.7); line(ctx, sp, '#eaf0ff', 0.5, 0.9);
      }
    },
  },

  'eclat-errant': {
    name: 'Éclat Errant', side: 'enemy', role: 'swarm', family: 'crystal', rarity: 'common',
    registre: 'silence',
    quote: '« Chacun est un morceau de quelque chose de plus grand, qui manque. »',
    silhouette: 'Cinq petits solides flottants, alignés à des hauteurs différentes.',
    pal: { body: '#465274', deep: '#161c34', accent: TOKEN.frost, light: '#b8c4f0' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 28, 9, 0.34);
      const swarm = [[-22, -14, 1], [-8, -30, 0.8], [4, -10, 1.15], [16, -26, 0.9], [26, -16, 0.7]];
      for (const [ox, oy, sc] of swarm) {
        const x = cx + ox, y = base + oy;
        // Chaque éclat projette sa propre tache de lumière au sol : ils flottent vraiment.
        const fg = ctx.createRadialGradient(x, base - 2, 1, x, base - 2, 12 * sc);
        fg.addColorStop(0, rgba(pal.accent, 0.22));
        fg.addColorStop(1, rgba(pal.accent, 0));
        ctx.fillStyle = fg;
        ctx.beginPath(); ctx.ellipse(x, base - 2, 12 * sc, 5 * sc, 0, 0, Math.PI * 2); ctx.fill();
        const p = poly([P(x, y - 11 * sc), P(x + 6 * sc, y - 2 * sc), P(x + 3 * sc, y + 9 * sc), P(x - 4 * sc, y + 8 * sc), P(x - 6 * sc, y - 3 * sc)]);
        const g = ctx.createLinearGradient(x - 6 * sc, y - 11 * sc, x + 6 * sc, y + 9 * sc);
        g.addColorStop(0, rgba(pal.light, 0.95));
        g.addColorStop(0.55, rgba(pal.body, 1));
        g.addColorStop(1, rgba(pal.deep, 1));
        ctx.fillStyle = g; ctx.fill(p);
        line(ctx, p, '#eaf0ff', 0.45, 1);
        glowDot(ctx, x, y - 1, 1.2 * sc, pal.accent, 0.45);
      }
    },
  },

  'prisme-sentinelle': {
    name: 'Prisme Sentinelle', side: 'enemy', role: 'bruiser', family: 'crystal', rarity: 'elite',
    registre: 'silence',
    quote: '« Il vous a déjà vu. Sous six angles à la fois. »',
    silhouette: 'Très haut, étroit, penché. Une colonne de cristal qui se termine en pointe.',
    pal: { body: '#3e4a72', deep: '#121834', accent: TOKEN.frost, light: '#c0cbff' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 18, 7.5, 0.5);
      ctx.save(); ctx.translate(cx, base); ctx.rotate(0.07);
      const shaft = poly([P(-14, 0), P(-9, -60), P(-2, -86), P(6, -58), P(13, 0)]);
      ctx.save();
      ctx.shadowColor = rgba(pal.accent, 0.3); ctx.shadowBlur = 16;
      const g = ctx.createLinearGradient(-14, 0, 13, -86);
      g.addColorStop(0, rgba(pal.deep, 1));
      g.addColorStop(0.5, rgba(pal.body, 1));
      g.addColorStop(1, rgba(pal.light, 0.9));
      ctx.fillStyle = g; ctx.fill(shaft);
      ctx.restore();
      // Les arêtes internes : c'est la seule « texture » admise sur du cristal.
      for (const [x0, y0, x1, y1] of [[-9, -60, -2, 0], [-2, -86, 2, -4], [6, -58, 8, 0], [-9, -60, 6, -58]]) {
        seg(ctx, x0, y0, x1, y1, pal.light, 0.35, 1);
      }
      // Épaules asymétriques : deux éclats greffés à des hauteurs différentes.
      const sh1 = poly([P(-10, -50), P(-26, -44), P(-20, -30), P(-9, -34)]);
      fill(ctx, sh1, pal.body); line(ctx, sh1, pal.light, 0.4, 1);
      const sh2 = poly([P(6, -62), P(22, -54), P(16, -42), P(5, -46)]);
      fill(ctx, sh2, mix(pal.body, pal.light, 0.2)); line(ctx, sh2, pal.light, 0.4, 1);
      // Le regard : pas un œil, une réfraction. Six petits foyers sur la même arête.
      for (let i = 0; i < 6; i++) glowDot(ctx, -3 + i * 1.4, -74 + i * 2.4, 0.8, pal.accent, 0.4);
      ctx.restore();
    },
  },


  'echo-chagrin': {
    name: 'Écho de Chagrin', side: 'enemy', role: 'disruptor', family: 'echos', rarity: 'common',
    registre: 'melancolie',
    quote: '« Il pleure une personne dont il a oublié le nom. »',
    silhouette: 'Verticale, effilée vers le bas, sans pieds. Elle s’égoutte au lieu de marcher.',
    pal: { body: '#22283e', deep: '#0c1020', accent: '#6f96c8', light: '#a8c0e0' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      // Pas de contact franc : elle ne pose rien. Une flaque, pas une empreinte.
      ctx.fillStyle = rgba(pal.accent, 0.14);
      ctx.beginPath(); ctx.ellipse(cx, base - 1, 20, 7, 0, 0, Math.PI * 2); ctx.fill();
      const bd = new Path2D();
      bd.moveTo(cx - 3, base - 2);
      bd.quadraticCurveTo(cx - 14, base - 26, cx - 12, base - 48);
      bd.quadraticCurveTo(cx - 8, base - 66, cx, base - 70);
      bd.quadraticCurveTo(cx + 9, base - 66, cx + 12, base - 48);
      bd.quadraticCurveTo(cx + 14, base - 26, cx + 3, base - 2);
      bd.closePath();
      ctx.save();
      ctx.shadowColor = rgba(pal.accent, 0.28); ctx.shadowBlur = 13;
      const g = ctx.createLinearGradient(cx, base - 70, cx, base);
      g.addColorStop(0, rgba(mix(pal.body, pal.accent, 0.35), 0.92));
      g.addColorStop(1, rgba(pal.deep, 0.5));
      ctx.fillStyle = g; ctx.fill(bd);
      ctx.restore();
      // Le contour instable du Disruptor : trois offsets du même profil, très faibles.
      for (let i = 1; i <= 3; i++) {
        ctx.save(); ctx.translate(R2(R, -2, 2) * i, 0);
        line(ctx, bd, pal.light, 0.1, 1.2);
        ctx.restore();
      }
      // Les gouttes : elles tombent du corps, verticales, à intervalles réguliers.
      for (let i = 0; i < 8; i++) {
        const dx = cx + R2(R, -11, 11), dy = base - 8 - R() * 48;
        const dp = new Path2D();
        dp.moveTo(dx, dy); dp.quadraticCurveTo(dx + 1, dy + 6, dx, dy + 11);
        line(ctx, dp, pal.light, R2(R, 0.2, 0.5), R2(R, 0.9, 1.8));
      }
      // Le visage : deux traînées qui descendent depuis là où seraient les yeux.
      for (const s of [-1, 1]) {
        const tr = new Path2D();
        tr.moveTo(cx + s * 4, base - 62);
        tr.quadraticCurveTo(cx + s * 5, base - 48, cx + s * 3, base - 34);
        line(ctx, tr, pal.light, 0.5, 1.6);
        glowDot(ctx, cx + s * 4, base - 62, 1.2, pal.accent, 0.45);
      }
    },
  },

  'echo-joie-fausse': {
    name: 'Écho de Joie Fausse', side: 'enemy', role: 'skirmisher', family: 'echos', rarity: 'uncommon',
    registre: 'folie',
    quote: '« Il danse. C’est ça, le problème. »',
    silhouette: 'Étroite, en déséquilibre, un pied levé. Toujours en mouvement, jamais posée.',
    pal: { body: '#2c1c30', deep: '#120a16', accent: '#cf3f92', light: '#f0a8d0' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 12, 5, 0.36);
      // Un seul pied au sol, l'autre en l'air : le déséquilibre est permanent.
      seg(ctx, cx - 2, base - 34, cx - 6, base, pal.body, 1, 4.4);
      seg(ctx, cx + 2, base - 34, cx + 16, base - 18, pal.body, 1, 4.2);
      seg(ctx, cx + 16, base - 18, cx + 24, base - 24, pal.body, 1, 3.4);
      const bd = new Path2D();
      bd.moveTo(cx - 9, base - 32);
      bd.quadraticCurveTo(cx - 14, base - 50, cx - 4, base - 60);
      bd.quadraticCurveTo(cx + 10, base - 62, cx + 11, base - 48);
      bd.quadraticCurveTo(cx + 8, base - 36, cx + 6, base - 32);
      bd.closePath();
      fill(ctx, bd, pal.body); line(ctx, bd, pal.accent, 0.4, 1.2);
      // Les bras jetés en arrière, très haut : la posture d'un applaudissement figé.
      for (const s of [-1, 1]) {
        const ar = new Path2D();
        ar.moveTo(cx + s * 6, base - 54);
        ar.quadraticCurveTo(cx + s * 22, base - 58, cx + s * 20, base - 74);
        line(ctx, ar, pal.body, 1, 3.6);
        glowDot(ctx, cx + s * 20, base - 76, 1.3, pal.accent, 0.4);
      }
      // La traînée de mouvement : des rémanences du même contour, décalées, plus pâles.
      for (let i = 1; i <= 3; i++) {
        ctx.save(); ctx.translate(-i * 5, i * 0.6); ctx.globalAlpha = 0.16 - i * 0.035;
        fill(ctx, bd, pal.accent, 1);
        ctx.restore();
      }
      // Le sourire : trop large pour la tête, et c'est la seule chose lisible du visage.
      ctx.beginPath(); ctx.ellipse(cx + 1, base - 66, 8, 8.6, 0.1, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.body, 1); ctx.fill();
      const sm = new Path2D();
      sm.moveTo(cx - 7, base - 66); sm.quadraticCurveTo(cx + 1, base - 56, cx + 9, base - 68);
      line(ctx, sm, pal.light, 0.9, 2);
      for (let i = 0; i < 5; i++) seg(ctx, cx - 5 + i * 3, base - 65, cx - 5 + i * 3, base - 62, pal.deep, 0.6, 1);
    },
  },

  // ═══ 3.7 BRUME — ce qui reste quand une salle a été oubliée ══════════════════════════
  // Aucune arête. Aucune anatomie. La lecture repose entièrement sur la densité : dense au
  // centre, dissipée au bord. La menace se lit à la vitesse à laquelle elle se reforme.

  'voile-marcheur': {
    name: 'Voile Marcheur', side: 'enemy', role: 'disruptor', family: 'brume', rarity: 'common',
    registre: 'silence',
    quote: '« Vous avez traversé quelque chose. Vous ne savez pas quoi. »',
    silhouette: 'Haute, sans bas défini, sans épaules. Une verticale de brume plus dense.',
    pal: { body: '#3a3c50', deep: '#181a28', accent: '#c3bfcc', light: '#e0dee8' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      // Il n'y a pas de contact au sol, seulement une dissipation basse.
      for (let i = 0; i < 5; i++) {
        ctx.fillStyle = rgba(pal.body, 0.1);
        ctx.beginPath(); ctx.ellipse(cx + R2(R, -8, 8), base - i * 3, 22 - i * 2.5, 6 - i, 0, 0, Math.PI * 2); ctx.fill();
      }
      // Densité décroissante vers le haut ET vers les bords : 9 nappes empilées.
      for (let i = 0; i < 9; i++) {
        const yy = base - 8 - i * 7;
        const hw = 16 - Math.abs(i - 3) * 1.6;
        const a = 0.3 - i * 0.026;
        const g = ctx.createRadialGradient(cx, yy, 1, cx, yy, hw * 1.6);
        g.addColorStop(0, rgba(pal.light, a));
        g.addColorStop(0.5, rgba(pal.body, a * 0.8));
        g.addColorStop(1, rgba(pal.body, 0));
        ctx.fillStyle = g;
        ctx.beginPath(); ctx.ellipse(cx + R2(R, -3, 3), yy, hw * 1.6, 9, 0, 0, Math.PI * 2); ctx.fill();
      }
      // Le cœur dense : la seule chose qu'on puisse viser, et elle est haute.
      const core = ctx.createRadialGradient(cx, base - 46, 2, cx, base - 46, 16);
      core.addColorStop(0, rgba(pal.light, 0.42));
      core.addColorStop(1, rgba(pal.light, 0));
      ctx.fillStyle = core;
      ctx.beginPath(); ctx.ellipse(cx, base - 46, 13, 20, 0, 0, Math.PI * 2); ctx.fill();
      // Deux creux sombres à hauteur de visage — une absence, pas des yeux.
      for (const s of [-1, 1]) {
        ctx.fillStyle = rgba('#0a0c16', 0.4);
        ctx.beginPath(); ctx.ellipse(cx + s * 4, base - 52, 2.4, 3.4, 0, 0, Math.PI * 2); ctx.fill();
      }
    },
  },

  'main-de-brume': {
    name: 'Mains de Brume', side: 'enemy', role: 'swarm', family: 'brume', rarity: 'common',
    registre: 'silence',
    quote: '« Elles sortent du sol et retiennent les chevilles. »',
    silhouette: 'Quatre mains basses qui émergent du sol, à des angles différents.',
    pal: { body: '#3e4054', deep: '#181a26', accent: '#c3bfcc', light: '#dcdae6' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      ctx.fillStyle = rgba(pal.body, 0.16);
      ctx.beginPath(); ctx.ellipse(cx, base - 1, 30, 10, 0, 0, Math.PI * 2); ctx.fill();
      for (const [ox, oy, rot, sc] of [[-20, -1, -0.4, 0.9], [-3, -5, 0.15, 1.05], [14, 1, 0.5, 0.85], [24, -4, -0.2, 0.75]]) {
        ctx.save(); ctx.translate(cx + ox, base + oy); ctx.rotate(rot); ctx.scale(sc, sc);
        // La brume d'émergence : elle est plus dense au poignet, dissipée aux doigts.
        const g = ctx.createLinearGradient(0, 0, 0, -26);
        g.addColorStop(0, rgba(pal.body, 0.75));
        g.addColorStop(1, rgba(pal.light, 0.12));
        const pm = new Path2D();
        pm.moveTo(-6, 2);
        pm.quadraticCurveTo(-8, -10, -4, -14);
        pm.quadraticCurveTo(2, -16, 6, -12);
        pm.quadraticCurveTo(8, -4, 6, 2);
        pm.closePath();
        ctx.fillStyle = g; ctx.fill(pm);
        for (let i = 0; i < 4; i++) {
          const fx = -4 + i * 3;
          const fp = new Path2D();
          fp.moveTo(fx, -12);
          fp.quadraticCurveTo(fx + R2(R, -2, 2), -20, fx + R2(R, -3, 3), -26 - R() * 5);
          line(ctx, fp, pal.light, R2(R, 0.2, 0.45), R2(R, 1.4, 2.4));
        }
        ctx.restore();
      }
      for (let i = 0; i < 8; i++) {
        glowDot(ctx, cx + R2(R, -28, 28), base - 6 - R() * 14, 0.7, pal.accent, 0.16);
      }
    },
  },

  'noyau-de-brume': {
    name: 'Noyau de Brume', side: 'enemy', role: 'drain', family: 'brume', rarity: 'rare',
    registre: 'silence',
    quote: '« Tant qu’il est là, la salle continue de s’oublier. »',
    silhouette: 'Déborde de sa case. Un centre très dense, des bras de brume sur les côtés.',
    pal: { body: '#343648', deep: '#141622', accent: '#c3bfcc', light: '#eeecf4' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      // Le Drain déborde : la brume dépasse largement la case, sur les quatre diagonales.
      for (const [ax, ay] of [[-46, -6], [46, -6], [-30, -22], [30, -22]]) {
        const g = ctx.createRadialGradient(cx + ax * 0.5, base + ay, 2, cx + ax * 0.5, base + ay, 34);
        g.addColorStop(0, rgba(pal.body, 0.3));
        g.addColorStop(1, rgba(pal.body, 0));
        ctx.fillStyle = g;
        ctx.beginPath(); ctx.ellipse(cx + ax * 0.5, base + ay, 34, 13, 0, 0, Math.PI * 2); ctx.fill();
      }
      for (let i = 0; i < 14; i++) {
        const a = R() * Math.PI * 2, rr = R2(R, 16, 44);
        const wp = new Path2D();
        const x0 = cx + Math.cos(a) * rr, y0 = base - 16 + Math.sin(a) * rr * 0.4;
        wp.moveTo(x0, y0);
        wp.quadraticCurveTo(cx + Math.cos(a) * rr * 0.4, y0 - R2(R, 2, 10), cx, base - 20);
        line(ctx, wp, pal.light, R2(R, 0.05, 0.16), R2(R, 2, 6));
      }
      // Le centre : très dense, presque solide. C'est là qu'on frappe, et c'est petit.
      const core = ctx.createRadialGradient(cx, base - 24, 1, cx, base - 24, 20);
      core.addColorStop(0, rgba(pal.light, 0.72));
      core.addColorStop(0.35, rgba(pal.body, 0.6));
      core.addColorStop(1, rgba(pal.body, 0));
      ctx.fillStyle = core;
      ctx.beginPath(); ctx.arc(cx, base - 24, 20, 0, Math.PI * 2); ctx.fill();
      ctx.fillStyle = rgba('#0a0c16', 0.5);
      ctx.beginPath(); ctx.ellipse(cx, base - 24, 5, 6.5, 0, 0, Math.PI * 2); ctx.fill();
      glowDot(ctx, cx, base - 24, 1.6, pal.accent, 0.3);
    },
  },

  // ═══ 3.8 LITUISME — ceux qui lisent à voix haute ce qui devrait rester écrit ══════════
  // Ornement liturgique détourné : étole, encensoir, registre ouvert. Or profond sur noir.
  // Ils ne se battent pas, ils officient — la violence est une étape de la cérémonie.

  'officiant-lituique': {
    name: 'Officiant Lituique', side: 'enemy', role: 'support', family: 'lituisme', rarity: 'uncommon',
    registre: 'memoire',
    quote: '« La lecture a commencé. Elle ne s’interrompt pour personne. »',
    silhouette: 'Vertical, immobile, un registre ouvert tenu à hauteur de poitrine.',
    pal: { body: '#2a2432', deep: '#0e0c14', accent: TOKEN.gold, light: '#e0d4b0' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 17, 7);
      const p = drape(ctx, R, cx, base, { top: base - 56, halfTop: 10, halfBot: 18, col: pal.body, deep: pal.deep, plis: 9, glow: pal.accent });
      // L'étole : deux bandes verticales, brodées, parfaitement symétriques.
      for (const s of [-1, 1]) {
        const st = new Path2D();
        st.moveTo(cx + s * 4, base - 54);
        st.lineTo(cx + s * 8, base - 54);
        st.quadraticCurveTo(cx + s * 10, base - 30, cx + s * 9, base - 10);
        st.lineTo(cx + s * 4, base - 10);
        st.closePath();
        fill(ctx, st, shade(pal.accent, -0.45), 0.85);
        line(ctx, st, pal.accent, 0.5, 0.9);
      }
      hood(ctx, cx, base - 62, 10, 11, pal.body, pal.deep);
      // Le registre ouvert : l'objet du Support, tenu devant, texte figuré illisible.
      const bk = new Path2D();
      bk.moveTo(cx - 15, base - 40); bk.quadraticCurveTo(cx, base - 44, cx + 15, base - 40);
      bk.lineTo(cx + 14, base - 26); bk.quadraticCurveTo(cx, base - 30, cx - 14, base - 26);
      bk.closePath();
      fill(ctx, bk, '#d8cfb4'); line(ctx, bk, shade(pal.accent, -0.4), 0.7, 1.2);
      seg(ctx, cx, base - 42, cx, base - 28, '#a89a78', 0.7, 1.2);
      faketext(ctx, R, cx - 12, base - 39, 11, 3, '#3a3020', 0.5, 4);
      faketext(ctx, R, cx + 2, base - 39, 11, 3, '#3a3020', 0.5, 4);
      seg(ctx, cx - 12, base - 46, cx - 15, base - 38, pal.light, 0.75, 2.8);
      seg(ctx, cx + 12, base - 46, cx + 15, base - 38, pal.light, 0.75, 2.8);
      // Les mots qui montent de la page : la lecture est visible, jamais audible.
      for (let i = 0; i < 7; i++) {
        const wx = cx + R2(R, -12, 12), wy = base - 46 - R() * 24;
        ctx.fillStyle = rgba(pal.accent, R2(R, 0.1, 0.32));
        ctx.fillRect(wx, wy, R2(R, 3, 8), 1.2);
      }
      glowDot(ctx, cx, base - 34, 1.4, pal.accent, 0.3);
    },
  },

  'porte-encens': {
    name: 'Porte-Encens', side: 'enemy', role: 'disruptor', family: 'lituisme', rarity: 'common',
    registre: 'memoire',
    quote: '« La fumée efface l’ordre dans lequel les choses sont arrivées. »',
    silhouette: 'Silhouette encapuchonnée noyée dans sa propre fumée. Un encensoir pend bas.',
    pal: { body: '#26222e', deep: '#0c0a10', accent: TOKEN.gold, light: '#b8b0c4' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 15, 6.5, 0.4);
      // La fumée d'abord : elle contient la figure, ce qui rend le contour incertain.
      for (let i = 0; i < 10; i++) {
        const sy = base - 10 - i * 5;
        const g = ctx.createRadialGradient(cx + R2(R, -8, 8), sy, 1, cx, sy, 22);
        g.addColorStop(0, rgba(pal.light, 0.14));
        g.addColorStop(1, rgba(pal.light, 0));
        ctx.fillStyle = g;
        ctx.beginPath(); ctx.ellipse(cx + R2(R, -6, 6), sy, 20, 8, 0, 0, Math.PI * 2); ctx.fill();
      }
      const bd = new Path2D();
      bd.moveTo(cx - 13, base - 2);
      bd.quadraticCurveTo(cx - 12, base - 34, cx - 7, base - 50);
      bd.lineTo(cx + 7, base - 50);
      bd.quadraticCurveTo(cx + 12, base - 32, cx + 13, base - 2);
      bd.closePath();
      fill(ctx, bd, pal.body, 0.9); line(ctx, bd, pal.deep, 0.6, 1.2);
      hood(ctx, cx, base - 56, 9, 10, pal.body, pal.deep);
      // L'encensoir : au bout d'une chaîne, très bas, presque au sol. Il oscille.
      let px = cx + 10, py = base - 44;
      for (let i = 0; i < 6; i++) {
        const nx = px + 1.4, ny = py + 3.4;
        seg(ctx, px, py, nx, ny, pal.accent, 0.5, 1);
        px = nx; py = ny;
      }
      const en = new Path2D();
      en.moveTo(px - 6, py); en.quadraticCurveTo(px, py + 9, px + 6, py);
      en.quadraticCurveTo(px, py - 4, px - 6, py);
      en.closePath();
      fill(ctx, en, shade(pal.accent, -0.4)); line(ctx, en, pal.accent, 0.7, 1.1);
      glowDot(ctx, px, py + 2, 1.6, TOKEN.ember, 0.55);
      for (let i = 0; i < 5; i++) {
        glowDot(ctx, px + R2(R, -5, 5), py - 4 - R() * 16, 0.9, pal.accent, 0.16);
      }
    },
  },

  'lecteur-de-nom': {
    name: 'Lecteur de Nom', side: 'enemy', role: 'bruiser', family: 'lituisme', rarity: 'rare',
    registre: 'memoire',
    quote: '« Il dira votre nom. Après, vous ne l’aurez plus. »',
    silhouette: 'Haut, mitre allongée, épaules très larges. Un rouleau déroulé jusqu’au sol.',
    pal: { body: '#241e2c', deep: '#0c0a12', accent: TOKEN.gold, light: '#e8dcb8' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 22, 8.5, 0.54);
      const p = drape(ctx, R, cx, base, { top: base - 62, halfTop: 15, halfBot: 22, col: pal.body, deep: pal.deep, plis: 10, glow: pal.accent });
      // Épaules très larges : une chape rigide, plus large que le corps, presque un toit.
      const ch = new Path2D();
      ch.moveTo(cx - 26, base - 50);
      ch.quadraticCurveTo(cx, base - 68, cx + 26, base - 50);
      ch.quadraticCurveTo(cx + 14, base - 44, cx, base - 46);
      ch.quadraticCurveTo(cx - 14, base - 44, cx - 26, base - 50);
      ch.closePath();
      fill(ctx, ch, shade(pal.body, 0.14)); line(ctx, ch, pal.accent, 0.45, 1.3);
      gravures(ctx, R, cx - 24, base - 60, 48, 14, pal.accent, 18);
      // La mitre : trop haute, effilée. C'est elle qui donne la hauteur du Bruiser.
      const mi = new Path2D();
      mi.moveTo(cx - 10, base - 62);
      mi.quadraticCurveTo(cx - 8, base - 88, cx, base - 100);
      mi.quadraticCurveTo(cx + 8, base - 88, cx + 10, base - 62);
      mi.closePath();
      fill(ctx, mi, pal.body); line(ctx, mi, pal.accent, 0.6, 1.3);
      seg(ctx, cx, base - 96, cx, base - 66, pal.accent, 0.5, 1.2);
      // Sous la mitre : rien de lisible. Une ombre, et deux points d'or très rapprochés.
      ctx.fillStyle = rgba('#05060c', 0.92);
      ctx.beginPath(); ctx.ellipse(cx, base - 66, 7, 6, 0, 0, Math.PI * 2); ctx.fill();
      for (const s of [-1, 1]) glowDot(ctx, cx + s * 2.4, base - 66, 1, pal.accent, 0.6);
      // Le rouleau : déroulé du torse jusqu'au sol, couvert de noms qu'on ne lit pas.
      const sc = new Path2D();
      sc.moveTo(cx + 4, base - 44);
      sc.quadraticCurveTo(cx + 16, base - 30, cx + 12, base - 2);
      sc.lineTo(cx - 2, base - 2);
      sc.quadraticCurveTo(cx + 2, base - 28, cx - 6, base - 44);
      sc.closePath();
      fill(ctx, sc, '#d8cfb0', 0.92); line(ctx, sc, shade(pal.accent, -0.4), 0.6, 1.1);
      ctx.save(); ctx.clip(sc);
      faketext(ctx, R, cx - 4, base - 40, 14, 7, '#3a3020', 0.55, 5.2);
      ctx.restore();
    },
  },

  // ═══ 3.9 PSYCHÉ — la figure qui vous a déjà vu ═══════════════════════════════════════
  // Miroir, symétrie, dédoublement. Chaque figure contient une copie d'elle-même, mal
  // alignée. Aucune arme : l'agression est de vous montrer quelque chose.

  'miroir-porteur': {
    name: 'Miroir Porteur', side: 'enemy', role: 'guard', family: 'psyche', rarity: 'common',
    registre: 'folie',
    quote: '« Il ne vous attaque pas. Il vous présente. »',
    silhouette: 'Large et bas, un grand miroir tenu de face qui occupe presque toute la case.',
    pal: { body: '#2e2438', deep: '#100c16', accent: '#cf3f92', light: '#c8c0d8' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 26, 9.5, 0.5);
      for (const s of [-1, 1]) {
        paintMass(ctx, [P(cx + s * 12 - 4, base), P(cx + s * 11 - 4, base - 16), P(cx + s * 18 - 4, base - 16), P(cx + s * 19 - 4, base)], th, R,
          { base: pal.deep, rim: 0.08 });
      }
      // Le miroir EST la silhouette : la figure derrière n'est qu'un support.
      const fr = new Path2D();
      fr.moveTo(cx - 24, base - 8);
      fr.quadraticCurveTo(cx - 26, base - 50, cx, base - 56);
      fr.quadraticCurveTo(cx + 26, base - 50, cx + 24, base - 8);
      fr.closePath();
      fill(ctx, fr, shade(pal.accent, -0.55)); line(ctx, fr, pal.accent, 0.5, 1.6);
      const gl = new Path2D();
      gl.moveTo(cx - 19, base - 11);
      gl.quadraticCurveTo(cx - 20, base - 47, cx, base - 51);
      gl.quadraticCurveTo(cx + 20, base - 47, cx + 19, base - 11);
      gl.closePath();
      const g = ctx.createLinearGradient(cx - 19, base - 51, cx + 19, base - 11);
      g.addColorStop(0, rgba('#4a4260', 0.95));
      g.addColorStop(0.45, rgba('#1a1626', 1));
      g.addColorStop(1, rgba('#3a3450', 0.95));
      ctx.fillStyle = g; ctx.fill(gl);
      // Dans le miroir : une silhouette d'allié, floue, MAL ALIGNÉE avec la case.
      ctx.save(); ctx.clip(gl);
      ctx.fillStyle = rgba('#0a0810', 0.6);
      const rf = new Path2D();
      rf.moveTo(cx - 4, base - 14);
      rf.quadraticCurveTo(cx - 10, base - 34, cx - 2, base - 44);
      rf.quadraticCurveTo(cx + 8, base - 42, cx + 7, base - 30);
      rf.quadraticCurveTo(cx + 5, base - 20, cx + 6, base - 14);
      rf.closePath();
      ctx.fill(rf);
      for (let i = 0; i < 3; i++) {
        ctx.save(); ctx.translate(i * 2.4, -i * 1.2); ctx.globalAlpha = 0.16;
        ctx.fillStyle = rgba(pal.light, 1); ctx.fill(rf);
        ctx.restore();
      }
      seg(ctx, cx - 18, base - 44, cx + 16, base - 20, pal.light, 0.14, 6);
      ctx.restore();
      // La tête du porteur dépasse à peine derrière le cadre, décentrée.
      ctx.beginPath(); ctx.ellipse(cx + 14, base - 58, 6.4, 7, 0.2, 0, Math.PI * 2);
      ctx.fillStyle = rgba(pal.body, 1); ctx.fill();
      glowDot(ctx, cx + 13, base - 58, 1, pal.accent, 0.45);
    },
  },

  'reflet-inverse': {
    name: 'Reflet Inverse', side: 'enemy', role: 'skirmisher', family: 'psyche', rarity: 'uncommon',
    registre: 'folie',
    quote: '« Il fait vos gestes une seconde avant vous. »',
    silhouette: 'Deux moitiés du même corps, décalées, comme mal recollées sur l’axe vertical.',
    pal: { body: '#2a2238', deep: '#100c1a', accent: '#cf3f92', light: '#a8a0c8' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 16, 6.5, 0.42);
      // Deux moitiés du MÊME profil, décalées en hauteur et en teinte : le raccord est faux.
      for (const [s, dy, al] of [[-1, 0, 1], [1, -5, 0.9]]) {
        ctx.save(); ctx.translate(cx, base + dy); ctx.scale(s, 1); ctx.globalAlpha = al;
        seg(ctx, 3, -30, 7, 0, pal.body, 1, 4.2);
        const bd = new Path2D();
        bd.moveTo(0, -28);
        bd.quadraticCurveTo(-2, -48, 4, -58);
        bd.quadraticCurveTo(14, -58, 13, -46);
        bd.quadraticCurveTo(10, -34, 8, -28);
        bd.closePath();
        fill(ctx, bd, pal.body); line(ctx, bd, s > 0 ? pal.accent : pal.light, 0.4, 1.2);
        seg(ctx, 8, -52, 20, -40, pal.body, 1, 3.4);
        ctx.beginPath(); ctx.ellipse(7, -63, 6.6, 7.2, 0.1, 0, Math.PI * 2);
        ctx.fillStyle = rgba(shade(pal.body, 0.1), 1); ctx.fill();
        glowDot(ctx, 9, -63, 1.1, s > 0 ? pal.accent : pal.light, 0.5);
        ctx.restore();
      }
      // La couture centrale : une ligne franche là où les deux moitiés ne coïncident pas.
      seg(ctx, cx, base - 2, cx, base - 68, pal.accent, 0.32, 1.4);
      for (let i = 0; i < 6; i++) {
        const yy = base - 8 - i * 11;
        seg(ctx, cx - 4, yy, cx + 4, yy + R2(R, -2, 2), pal.light, R2(R, 0.15, 0.4), 1);
      }
    },
  },

  'pensee-parasite': {
    name: 'Pensées Parasites', side: 'enemy', role: 'swarm', family: 'psyche', rarity: 'common',
    registre: 'folie',
    quote: '« Aucune n’est à vous. Toutes se répondent. »',
    silhouette: 'Un essaim de petites formes ovales, en orbite serrée autour d’un point vide.',
    pal: { body: '#302442', deep: '#120c1c', accent: '#cf3f92', light: '#e0a8d8' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      ctx.fillStyle = rgba(pal.accent, 0.1);
      ctx.beginPath(); ctx.ellipse(cx, base - 1, 24, 8, 0, 0, Math.PI * 2); ctx.fill();
      // Le centre est VIDE : l'essaim tourne autour de quelque chose qui n'est pas là.
      const hole = ctx.createRadialGradient(cx, base - 30, 1, cx, base - 30, 13);
      hole.addColorStop(0, rgba('#05040a', 0.8));
      hole.addColorStop(1, rgba('#05040a', 0));
      ctx.fillStyle = hole;
      ctx.beginPath(); ctx.arc(cx, base - 30, 13, 0, Math.PI * 2); ctx.fill();
      for (let i = 0; i < 11; i++) {
        const a = (i / 11) * Math.PI * 2 + 0.3;
        const rr = 15 + (i % 3) * 7;
        const x = cx + Math.cos(a) * rr, y = base - 30 + Math.sin(a) * rr * 0.62;
        ctx.save(); ctx.translate(x, y); ctx.rotate(a * 0.6);
        const bp = new Path2D();
        bp.ellipse(0, 0, R2(R, 3.4, 6), R2(R, 1.8, 3), 0, 0, Math.PI * 2);
        const g = ctx.createLinearGradient(-6, 0, 6, 0);
        g.addColorStop(0, rgba(pal.light, 0.8));
        g.addColorStop(1, rgba(pal.body, 0.95));
        ctx.fillStyle = g; ctx.fill(bp);
        line(ctx, bp, pal.accent, 0.45, 0.9);
        ctx.restore();
        // Les liens : chaque pensée en répond une autre. Le maillage est le vrai corps.
        const a2 = ((i + 3) / 11) * Math.PI * 2 + 0.3;
        const rr2 = 15 + ((i + 3) % 3) * 7;
        seg(ctx, x, y, cx + Math.cos(a2) * rr2, base - 30 + Math.sin(a2) * rr2 * 0.62, pal.accent, 0.12, 0.8);
      }
      for (let i = 0; i < 6; i++) glowDot(ctx, cx + R2(R, -22, 22), base - 30 + R2(R, -14, 14), 0.7, pal.accent, 0.24);
    },
  },

  // ═══ 3.10 ALCHIMIE — le protocole qui a continué sans opérateur ══════════════════════
  // Verre, cuivre, liquide. Une seule couleur saturée par figure, et elle est DANS le
  // verre : c'est le contenu qui est vivant, le contenant n'est qu'une forme.

  'alambic-marcheur': {
    name: 'Alambic Marcheur', side: 'enemy', role: 'support', family: 'alchimie', rarity: 'common',
    registre: 'deni',
    quote: '« La distillation est en cours. Elle a commencé il y a très longtemps. »',
    silhouette: 'Vertical, une panse de verre au milieu, un col recourbé qui dépasse en haut.',
    pal: { body: '#3e4450', deep: '#161a22', accent: '#86dcb4', light: '#b0a878' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 18, 7.5, 0.46);
      for (const ox of [-10, 0, 10]) {
        paintMass(ctx, [P(cx + ox - 3, base), P(cx + ox - 2.5, base - 18), P(cx + ox + 2.5, base - 18), P(cx + ox + 3, base)], th, R,
          { base: pal.deep, rim: 0.1 });
      }
      // Le trépied de cuivre : la structure. Terne, martelée, sans éclat.
      const st = new Path2D();
      st.moveTo(cx - 14, base - 16); st.lineTo(cx - 11, base - 30);
      st.lineTo(cx + 11, base - 30); st.lineTo(cx + 14, base - 16);
      st.closePath();
      fill(ctx, st, '#6a5a3e'); line(ctx, st, pal.light, 0.35, 1.1);
      // La panse de verre : elle est CREUSE, on voit le fond de la salle à travers.
      const bl = new Path2D();
      bl.ellipse(cx, base - 46, 15, 17, 0, 0, Math.PI * 2);
      ctx.fillStyle = rgba('#0e1018', 0.42); ctx.fill(bl);
      line(ctx, bl, '#a8b4c8', 0.42, 1.4);
      // Le contenu : le seul élément saturé, et il occupe le bas de la panse.
      ctx.save(); ctx.clip(bl);
      const lg = ctx.createLinearGradient(cx, base - 42, cx, base - 30);
      lg.addColorStop(0, rgba(pal.accent, 0.75));
      lg.addColorStop(1, rgba(shade(pal.accent, -0.4), 0.9));
      ctx.fillStyle = lg; ctx.fillRect(cx - 16, base - 42, 32, 14);
      for (let i = 0; i < 7; i++) {
        glowDot(ctx, cx + R2(R, -11, 11), base - 44 - R() * 10, R2(R, 0.6, 1.4), pal.accent, 0.4);
      }
      ctx.restore();
      // Reflet vertical sur le verre : un seul, franc, à gauche. Il fait le volume.
      seg(ctx, cx - 8, base - 56, cx - 11, base - 42, '#e8f0ff', 0.3, 2.4);
      // Le col recourbé : il monte, se plie, et redescend goutter à côté de la figure.
      const nk = new Path2D();
      nk.moveTo(cx + 4, base - 62);
      nk.quadraticCurveTo(cx + 22, base - 74, cx + 26, base - 52);
      nk.quadraticCurveTo(cx + 27, base - 40, cx + 24, base - 34);
      line(ctx, nk, '#a8b4c8', 0.5, 3.4);
      glowDot(ctx, cx + 24, base - 32, 1.4, pal.accent, 0.5);
      const vp = new Path2D();
      vp.moveTo(cx - 10, base - 62); vp.quadraticCurveTo(cx, base - 68, cx + 10, base - 62);
      line(ctx, vp, '#6a5a3e', 0.9, 3);
    },
  },

  'homoncule-verre': {
    name: 'Homoncule de Verre', side: 'enemy', role: 'skirmisher', family: 'alchimie', rarity: 'uncommon',
    registre: 'deni',
    quote: '« Il n’est pas fini. Personne ne compte le finir. »',
    silhouette: 'Petit, étroit, penché en avant. Transparent, sauf le liquide dans son torse.',
    pal: { body: '#48505e', deep: '#181c26', accent: '#e8c94a', light: '#c8d4e8' },
    paint(k) {
      const { ctx, R, cx, base, pal } = k;
      contact(ctx, cx, base, 13, 5.5, 0.34);
      ctx.save(); ctx.translate(cx, base); ctx.rotate(-0.12);
      // Le verre : contour clair, intérieur presque vide. On voit le sol à travers lui.
      for (const s of [-1, 1]) {
        const lg = new Path2D();
        lg.moveTo(s * 3, -22); lg.quadraticCurveTo(s * 8, -12, s * 5, 0);
        line(ctx, lg, pal.light, 0.5, 3);
      }
      const bd = new Path2D();
      bd.moveTo(-8, -22);
      bd.quadraticCurveTo(-11, -38, -3, -46);
      bd.quadraticCurveTo(8, -47, 9, -36);
      bd.quadraticCurveTo(7, -26, 6, -22);
      bd.closePath();
      ctx.fillStyle = rgba('#0e1018', 0.34); ctx.fill(bd);
      line(ctx, bd, pal.light, 0.55, 1.4);
      // Le liquide : au fond du torse, il bouge quand la figure bouge. C'est l'organe.
      ctx.save(); ctx.clip(bd);
      const lq = ctx.createLinearGradient(0, -34, 0, -22);
      lq.addColorStop(0, rgba(pal.accent, 0.7));
      lq.addColorStop(1, rgba(shade(pal.accent, -0.45), 0.9));
      ctx.fillStyle = lq; ctx.fillRect(-12, -34, 24, 14);
      ctx.restore();
      seg(ctx, -5, -42, -7, -28, '#e8f0ff', 0.28, 1.8);
      // Bras longs, tendus vers l'avant, terminés en pointe soufflée.
      seg(ctx, 5, -40, 20, -30, pal.light, 0.5, 2.4);
      seg(ctx, 20, -30, 27, -32, pal.light, 0.5, 1.6);
      seg(ctx, -6, -38, -16, -26, pal.light, 0.45, 2.2);
      // La tête : une bulle scellée, vide, avec la marque du souffleur au sommet.
      ctx.beginPath(); ctx.ellipse(2, -52, 7, 7.6, 0.1, 0, Math.PI * 2);
      ctx.fillStyle = rgba('#0e1018', 0.3); ctx.fill();
      ctx.strokeStyle = rgba(pal.light, 0.6); ctx.lineWidth = 1.3; ctx.stroke();
      seg(ctx, 2, -60, 2, -64, pal.light, 0.6, 2);
      glowDot(ctx, 1, -50, 1.2, pal.accent, 0.45);
      ctx.restore();
    },
  },

  'creuset-vivant': {
    name: 'Creuset Vivant', side: 'enemy', role: 'bruiser', family: 'alchimie', rarity: 'elite',
    registre: 'rupture',
    quote: '« Ce qui bout là-dedans a un avis. »',
    silhouette: 'Trapu mais haut, épaules de pierre réfractaire. Le contenu déborde par le haut.',
    pal: { body: '#403a3e', deep: '#181416', accent: TOKEN.ember, light: '#8a8288' },
    paint(k) {
      const { ctx, R, cx, base, th, pal } = k;
      contact(ctx, cx, base, 24, 9, 0.56);
      for (const s of [-1, 1]) {
        paintMass(ctx, [P(cx + s * 6 - 4, base), P(cx + s * 5 - 4, base - 22), P(cx + s * 14 - 4, base - 22), P(cx + s * 15 - 4, base)], th, R,
          { base: pal.deep, rim: 0.12 });
      }
      // Le corps : un creuset réfractaire, épais, évasé vers le haut. Rien d'humain.
      const bd = new Path2D();
      bd.moveTo(cx - 14, base - 20);
      bd.lineTo(cx - 22, base - 58);
      bd.lineTo(cx + 22, base - 58);
      bd.lineTo(cx + 14, base - 20);
      bd.closePath();
      ctx.save();
      ctx.shadowColor = rgba('#000000', 0.6); ctx.shadowBlur = 11;
      const g = ctx.createLinearGradient(cx - 22, 0, cx + 22, 0);
      g.addColorStop(0, rgba(shade(pal.body, 0.16), 1));
      g.addColorStop(0.6, rgba(pal.body, 1));
      g.addColorStop(1, rgba(pal.deep, 1));
      ctx.fillStyle = g; ctx.fill(bd);
      ctx.restore();
      line(ctx, bd, pal.light, 0.28, 1.3);
      // Les fissures : elles suivent la hauteur, et la chaleur passe par elles.
      for (let i = 0; i < 6; i++) {
        const fx = cx + R2(R, -18, 18);
        const fp = new Path2D();
        fp.moveTo(fx, base - 56);
        fp.quadraticCurveTo(fx + R2(R, -5, 5), base - 42, fx + R2(R, -7, 7), base - 24);
        line(ctx, fp, pal.accent, R2(R, 0.2, 0.5), R2(R, 0.9, 2));
      }
      // Le contenu qui déborde : la seule masse saturée, et elle est en haut.
      const ml = new Path2D();
      ml.moveTo(cx - 22, base - 58);
      ml.quadraticCurveTo(cx, base - 66, cx + 22, base - 58);
      ml.quadraticCurveTo(cx, base - 54, cx - 22, base - 58);
      ml.closePath();
      const mg = ctx.createLinearGradient(cx, base - 66, cx, base - 54);
      mg.addColorStop(0, rgba('#ffd08a', 0.95));
      mg.addColorStop(1, rgba(pal.accent, 0.85));
      ctx.fillStyle = mg; ctx.fill(ml);
      // Deux coulées qui descendent sur la paroi : le débordement est en cours.
      for (const s of [-1, 1]) {
        const dp = new Path2D();
        dp.moveTo(cx + s * 15, base - 58);
        dp.quadraticCurveTo(cx + s * 17, base - 44, cx + s * 13, base - 30);
        line(ctx, dp, pal.accent, 0.6, 2.4);
      }
      // Les épaules : deux blocs de pierre posés sur le bord, sans cou entre eux.
      for (const s of [-1, 1]) {
        paintMass(ctx, [P(cx + s * 18, base - 58), P(cx + s * 20, base - 72), P(cx + s * 30, base - 70), P(cx + s * 28, base - 56)], th, R,
          { base: shade(pal.body, -0.12), rim: 0.18 });
      }
      for (let i = 0; i < 6; i++) glowDot(ctx, cx + R2(R, -18, 18), base - 66 - R() * 16, R2(R, 0.7, 1.5), pal.accent, 0.3);
    },
  },
};

export const PROPOSITION_IDS = Object.keys(ROSTER_PROPOSITIONS);

// ── Cuisson ────────────────────────────────────────────────────────────────────────────
export const ROSTER_IDS = Object.keys(ROSTER);

export const FAMILIES = {
  allies: { label: 'Alliés', note: 'Chaleureuses au milieu d’un décor hostile. Elles respirent.' },
  boss: { label: 'Boss', note: 'Placeholder — une ombre menaçante, en attente des fiches définitives.' },
  veilleurs: { label: 'Veilleurs du Seuil', note: 'La livrée, le service, la révérence. Aucun n’a de visage.' },
  copistes: { label: 'Copistes', note: 'Le papier, l’encre, l’acte d’écrire. Ils enregistrent le combat.' },
  squelettes: { label: 'Squelettes de Souvenirs', note: 'L’os gris cendre, la gravure illisible, l’objet incongru.' },
  blouses: { label: 'Blouses Blanches', note: 'Le blanc amidonné, le soin retourné en menace. Personne n’a d’yeux.' },
  imperatrice: { label: 'Impératrice de la Falaise', note: 'Une seule figure. Elle n’a pas de bas : le jeton commence à mi-corps, dans la mer.' },
  predateurs: { label: 'Prédateurs', note: 'L’ombre qui chasse. Noir sans texture, gueule plus claire.' },
  chimeres: { label: 'Chimères des Plaines', note: 'L’animal recomposé de mémoire. Une articulation fausse par figure.' },
  forgeron: { label: 'Créations du Forgeron', note: 'Métal battu, rivets, une braise interne. La chaleur sort par les joints.' },
  penitents: { label: 'Pénitents de la Montagne', note: 'Pierre et chaînes. Aucun ne se relève : la famille la plus basse du bestiaire.' },
  jardin: { label: 'Faux Habitants du Jardin', note: 'Rien n’a poussé. Vert de sève, cire, taille au sécateur.' },
  crystal: { label: 'Gardiens de Crystal', note: 'Des volumes, pas des corps. Lumière interne froide, arêtes franches.' },
  echos: { label: 'Échos d’Émotions', note: 'Presque pas de corps : un contour et la couleur de son registre.' },
  brume: { label: 'Brume', note: 'Aucune arête. La lecture repose sur la densité, jamais sur la forme.' },
  lituisme: { label: 'Lituisme', note: 'Ornement liturgique détourné. Ils officient — la violence est une étape.' },
  psyche: { label: 'Psyché', note: 'Miroir et dédoublement. L’agression est de vous montrer quelque chose.' },
  alchimie: { label: 'Alchimie', note: 'Verre, cuivre, liquide. C’est le contenu qui est vivant, pas le contenant.' },
};

/** Peint un combattant. `variant` sélectionne une déclinaison (objet tenu, échelle…). */
export function bakeCombatant(id, variant = 0) {
  const e = ROSTER[id] || ROSTER_PROPOSITIONS[id];
  const cv = makeCanvas(SPRITE_W, PROP_SPRITE_H);
  const ctx = cv.getContext('2d');
  if (!ctx || !e) return cv;
  ctx.translate(0, PROP_EXTRA_H);
  const R = makeRng(hashSeed('combatant:' + id + ':' + variant));
  // Thème de peinture synthétique : paintMass attend un thème de salle, on lui en fabrique
  // un à partir de la palette de la figure pour que le grain reste celui du plateau.
  const th = {
    riser: e.pal.body, riserDeep: e.pal.deep ?? shade(e.pal.body, -0.5),
    top: e.pal.light ?? e.pal.body, accent: e.pal.accent ?? TOKEN.gold,
    glow: e.pal.light ?? TOKEN.ink,
  };
  e.paint({ ctx, R, cx: SPRITE_W / 2, base: centerY(0) + 4, th, pal: e.pal, v: variant });
  return cv;
}

const SPRITE_CACHE = new Map();
/** Version mémoïsée : en combat, le jeton est redessiné 60 fois par seconde. On ne recuit
 *  jamais un sprite déjà peint — la cuisson coûte des millisecondes, le blit rien. */
export function getCombatantSprite(id, variant = 0) {
  const key = id + ':' + variant;
  let cv = SPRITE_CACHE.get(key);
  if (!cv) { cv = bakeCombatant(id, variant); SPRITE_CACHE.set(key, cv); }
  return cv;
}

/** Fiche lisible d'un combattant, pour l'atelier et le handoff. */export function combatantInfo(id) {
  const e = ROSTER[id] || ROSTER_PROPOSITIONS[id];
  if (!e) return null;
  return {
    id, name: e.name, side: e.side, role: e.role, roleNote: ROLES[e.role],
    family: e.family, familyLabel: (FAMILIES[e.family] || {}).label ?? e.family,
    rarity: e.rarity, boss: !!e.boss,
    registre: e.registre ? { key: e.registre, ...REGISTRES[e.registre] } : null,
    quote: e.quote, silhouette: e.silhouette,
    variants: e.variants ?? null,
    palette: Object.entries(e.pal).map(([k, v]) => ({ key: k, col: Array.isArray(v) ? v : v })),
  };
}
