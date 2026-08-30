// @ts-nocheck

// Sorts — effets peints, un par registre et par grammaire de forme.
//
// Le catalogue compte 138 sorts. On n'en peint pas 138 : on peint le VOCABULAIRE dont ils
// sont faits, pour que la production restante soit du remplissage et non de la conception.
//
// Trois règles, valables pour les 138 :
//
//   1. LA FORME AVANT LA COULEUR. Le joueur doit reconnaître la zone touchée (case / croix /
//      losange / toute la carte) avant d'identifier le registre. La surbrillance annonce la
//      forme ; l'effet la CONFIRME, il ne la contredit jamais.
//   2. UN SORT EST UN ÉVÉNEMENT DE LUMIÈRE, PAS DE PARTICULES. Ce qui rend un impact lisible
//      sur une grille isométrique sombre, c'est une variation d'éclairement au sol — jamais
//      un nuage. Le sol s'allume, la case répond, la lumière s'éteint. Trois temps.
//   3. TOUT SE JOUE DANS LE PLAN DE LA CASE. Chaque effet est peint sur le losange du sol,
//      remonté de l'élévation. Rien ne flotte à une hauteur arbitraire : ce qui monte part
//      du sol et y revient, sinon la profondeur de la scène se casse.
//
// L'effet reçoit un temps normalisé `p` de 0 à 1 — jamais une horloge absolue. C'est ce qui
// permet au client de le rejouer au ralenti, de le mettre en pause, ou de le sauter.

import { PAINT } from './tilecraft.js';

const { rgba, mix, shade, makeRng, hashSeed, R2 } = PAINT;

/** Les quatre formes de zone. Aucune autre n'est autorisée : quatre formes apprises une fois
 *  valent mieux que douze formes devinées à chaque combat. */
export const SHAPES = {
  single: { label: 'Case', glyph: '▪', note: 'Une case. La cible, rien autour.' },
  cross: { label: 'Croix', glyph: '✛', note: 'La case et ses quatre voisines orthogonales.' },
  diamond: { label: 'Losange', glyph: '◈', note: 'Rayon 2 en distance de Manhattan.' },
  map: { label: 'Carte', glyph: '⬢', note: 'Toute la salle. N’épargne aucun camp.' },
};

/** Cases touchées par une forme centrée sur (x,y). Identique au calcul de `Combat tactique`. */
export function shapeCells(shape, x, y) {
  if (shape === 'single') return [{ x, y, d: 0 }];
  if (shape === 'map') return null; // la salle entière : c'est l'appelant qui la connaît
  const r = shape === 'diamond' ? 2 : 1;
  const out = [];
  for (let dy = -r; dy <= r; dy++) {
    for (let dx = -r; dx <= r; dx++) {
      const d = Math.abs(dx) + Math.abs(dy);
      if (d <= r) out.push({ x: x + dx, y: y + dy, d });
    }
  }
  return out;
}

// ── Outils de peinture au sol ──────────────────────────────────────────────────────────
// Tout part du losange de la case : `ux` est sa demi-largeur, `uy` sa demi-hauteur.

function cellPath(cx, cy, ux, uy, k = 1) {
  const p = new Path2D();
  p.moveTo(cx, cy - uy * k);
  p.lineTo(cx + ux * k, cy);
  p.lineTo(cx, cy + uy * k);
  p.lineTo(cx - ux * k, cy);
  p.closePath();
  return p;
}

/** Le sol s'allume : un halo isométrique, écrasé, exactement dans le plan de la case. */
function groundGlow(ctx, cx, cy, ux, uy, col, a, k = 1.6) {
  ctx.save();
  ctx.translate(cx, cy);
  ctx.scale(1, uy / ux);
  const g = ctx.createRadialGradient(0, 0, 1, 0, 0, ux * k);
  g.addColorStop(0, rgba(col, a));
  g.addColorStop(0.45, rgba(col, a * 0.4));
  g.addColorStop(1, rgba(col, 0));
  ctx.fillStyle = g;
  ctx.beginPath(); ctx.arc(0, 0, ux * k, 0, Math.PI * 2); ctx.fill();
  ctx.restore();
}

/** Onde au sol : un cercle en perspective qui s'ouvre et s'amincit. */
function groundRing(ctx, cx, cy, ux, uy, r, col, a, w = 2) {
  ctx.save();
  ctx.strokeStyle = rgba(col, a); ctx.lineWidth = w;
  ctx.beginPath(); ctx.ellipse(cx, cy, ux * r, uy * r, 0, 0, Math.PI * 2); ctx.stroke();
  ctx.restore();
}

/** Colonne de lumière : elle part du sol de la case et monte. Base large, sommet dissipé. */
function shaft(ctx, cx, cy, ux, uy, h, col, a, wk = 0.5) {
  ctx.save();
  const g = ctx.createLinearGradient(cx, cy, cx, cy - h);
  g.addColorStop(0, rgba(col, a));
  g.addColorStop(0.5, rgba(col, a * 0.45));
  g.addColorStop(1, rgba(col, 0));
  ctx.fillStyle = g;
  const p = new Path2D();
  p.moveTo(cx - ux * wk, cy);
  p.quadraticCurveTo(cx - ux * wk * 0.4, cy - h * 0.6, cx - ux * wk * 0.15, cy - h);
  p.lineTo(cx + ux * wk * 0.15, cy - h);
  p.quadraticCurveTo(cx + ux * wk * 0.4, cy - h * 0.6, cx + ux * wk, cy);
  p.closePath();
  ctx.fill(p);
  ctx.restore();
}

/** Éclats projetés depuis la case, dans le plan du sol : ils partent, ils ne montent pas. */
function shards(ctx, R, cx, cy, ux, uy, r, col, a, n = 9) {
  for (let i = 0; i < n; i++) {
    const ang = (i / n) * Math.PI * 2 + R() * 0.4;
    const rr = r * R2(R, 0.6, 1.15);
    const x0 = cx + Math.cos(ang) * ux * rr * 0.4;
    const y0 = cy + Math.sin(ang) * uy * rr * 0.4;
    const x1 = cx + Math.cos(ang) * ux * rr;
    const y1 = cy + Math.sin(ang) * uy * rr;
    ctx.beginPath(); ctx.moveTo(x0, y0); ctx.lineTo(x1, y1);
    ctx.strokeStyle = rgba(col, a * R2(R, 0.4, 1)); ctx.lineWidth = R2(R, 1, 2.6);
    ctx.lineCap = 'round'; ctx.stroke();
  }
}

/** Ce qui monte : des motes qui quittent le sol de la case et s'éteignent en haut. */
function motes(ctx, R, cx, cy, ux, uy, p, col, n = 12, hMax = 2.2) {
  for (let i = 0; i < n; i++) {
    const ph = (p + i / n) % 1;
    const ang = R() * Math.PI * 2;
    const rr = R2(R, 0.15, 0.8);
    const x = cx + Math.cos(ang) * ux * rr;
    const y = cy + Math.sin(ang) * uy * rr - ph * ux * hMax;
    const a = Math.sin(ph * Math.PI) * 0.75;
    const rad = R2(R, 0.9, 2.1);
    const g = ctx.createRadialGradient(x, y, 0.3, x, y, rad * 3);
    g.addColorStop(0, rgba('#ffffff', a));
    g.addColorStop(0.35, rgba(col, a * 0.7));
    g.addColorStop(1, rgba(col, 0));
    ctx.fillStyle = g;
    ctx.beginPath(); ctx.arc(x, y, rad * 3, 0, Math.PI * 2); ctx.fill();
  }
}

/** Enveloppe en trois temps : montée franche, tenue courte, extinction longue. C'est la
 *  courbe qui donne à tous les sorts la même respiration, quel que soit leur registre. */
function env(p, rise = 0.18, hold = 0.24) {
  if (p <= 0 || p >= 1) return 0;
  if (p < rise) return Math.pow(p / rise, 0.6);
  if (p < rise + hold) return 1;
  const q = (p - rise - hold) / (1 - rise - hold);
  return Math.pow(1 - q, 1.7);
}

/** Décalage de la case par sa distance au centre : l'onde met du temps à arriver au bord. */
function local(p, d, spread = 0.13) {
  return Math.max(0, Math.min(1, (p - d * spread) / (1 - d * spread || 1)));
}

const seeded = (key) => makeRng(hashSeed('sort:' + key));

// ── Le catalogue ───────────────────────────────────────────────────────────────────────
// fx(k) reçoit : ctx, cx/cy (ancre au sol de la case, élévation déjà appliquée), ux/uy
// (demi-diagonales du losange), p (0→1), d (distance au centre en cases), center (bool),
// R (aléatoire déterministe, réinitialisé à chaque case).

export const SORTS = {

  // ═══ MÉMOIRE — la pierre, l'archive, ce qui a été posé et ne bouge plus ══════════════

  fondations: {
    name: 'Fondations', caster: 'Thomas', side: 'ally', shape: 'single',
    range: 1, dmg: 10, col: '#e0b45f',
    phrase: 'La case se souvient d’avoir été un mur.',
    note: 'Une dalle sort du sol sous la cible et se rétracte. Le sol garde le joint tracé.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, R } = k;
      const e = env(p, 0.14, 0.3);
      const rise = Math.min(1, p / 0.32);
      const h = uy * 1.9 * (rise - Math.max(0, (p - 0.62) / 0.38) * rise);
      // La dalle : trois faces, comme une tuile en élévation. Elle appartient au terrain.
      if (h > 0.5) {
        const topP = cellPath(cx, cy - h, ux * 0.82, uy * 0.82);
        const g = ctx.createLinearGradient(cx, cy - h - uy, cx, cy);
        g.addColorStop(0, rgba(mix('#6a6480', k.col, 0.28), 0.95));
        g.addColorStop(1, rgba('#231f34', 0.95));
        ctx.fillStyle = rgba('#1a1728', 0.95);
        const side = new Path2D();
        side.moveTo(cx - ux * 0.82, cy - h);
        side.lineTo(cx, cy - h + uy * 0.82);
        side.lineTo(cx + ux * 0.82, cy - h);
        side.lineTo(cx + ux * 0.82, cy);
        side.lineTo(cx, cy + uy * 0.82);
        side.lineTo(cx - ux * 0.82, cy);
        side.closePath();
        ctx.fill(side);
        ctx.fillStyle = g; ctx.fill(topP);
        ctx.strokeStyle = rgba(k.col, 0.5 * e); ctx.lineWidth = 1.4; ctx.stroke(topP);
        // Les joints gravés sur la face du dessus : c'est ce qui la rend « construite ».
        for (let i = -1; i <= 1; i++) {
          ctx.beginPath();
          ctx.moveTo(cx - ux * 0.7, cy - h + i * uy * 0.34);
          ctx.lineTo(cx + ux * 0.7, cy - h + i * uy * 0.34);
          ctx.strokeStyle = rgba('#000000', 0.3); ctx.lineWidth = 1; ctx.stroke();
        }
      }
      groundGlow(ctx, cx, cy, ux, uy, k.col, 0.3 * e, 1.3);
      // La poussière du soulèvement reste au ras du sol : rien ne s'envole.
      for (let i = 0; i < 7; i++) {
        const ang = R() * Math.PI * 2, rr = R2(R, 0.5, 1.2) * (0.4 + p);
        ctx.fillStyle = rgba('#b8a888', 0.3 * e * R2(R, 0.4, 1));
        ctx.beginPath();
        ctx.ellipse(cx + Math.cos(ang) * ux * rr, cy + Math.sin(ang) * uy * rr, R2(R, 2, 5), R2(R, 1, 2.4), 0, 0, Math.PI * 2);
        ctx.fill();
      }
    },
  },

  rempart: {
    name: 'Rempart', caster: 'Thomas', side: 'ally', shape: 'cross',
    range: 2, dmg: 7, col: '#e0b45f',
    phrase: 'Quatre murs bas, le temps d’un tour. Personne ne passe, vous non plus.',
    note: 'La croix se relève d’un cran. Les cases du bord montent après le centre.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, d, center } = k;
      const lp = local(p, d, 0.16);
      const e = env(lp, 0.2, 0.3);
      const h = uy * (center ? 0.5 : 1.5) * Math.min(1, lp / 0.4) * (1 - Math.max(0, (p - 0.7) / 0.3));
      if (h > 0.4) {
        const wallTop = cellPath(cx, cy - h, ux * 0.9, uy * 0.9);
        ctx.fillStyle = rgba('#241f34', 0.9);
        const side = new Path2D();
        side.moveTo(cx - ux * 0.9, cy - h); side.lineTo(cx, cy - h + uy * 0.9);
        side.lineTo(cx + ux * 0.9, cy - h); side.lineTo(cx + ux * 0.9, cy);
        side.lineTo(cx, cy + uy * 0.9); side.lineTo(cx - ux * 0.9, cy);
        side.closePath();
        ctx.fill(side);
        ctx.fillStyle = rgba(mix('#5e5876', k.col, 0.2), 0.92); ctx.fill(wallTop);
        ctx.strokeStyle = rgba(k.col, 0.45 * e); ctx.lineWidth = 1.3; ctx.stroke(wallTop);
      }
      groundGlow(ctx, cx, cy, ux, uy, k.col, 0.22 * e, 1.1);
      // Le liseré qui court d'une case à l'autre : le rempart est UNE structure, pas cinq.
      const ring = cellPath(cx, cy, ux, uy, 0.98);
      ctx.strokeStyle = rgba(k.col, 0.5 * e); ctx.lineWidth = 1.6; ctx.stroke(ring);
    },
  },

  dictee: {
    name: 'Dictée', caster: 'Copiste Aveugle', side: 'enemy', shape: 'single',
    range: 4, dmg: 10, col: '#d8cfb4',
    phrase: 'Ce qui est dit est écrit. Ce qui est écrit vous concerne.',
    note: 'Des lignes de texte figuré tombent sur la case et s’impriment dans le sol.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, R } = k;
      const e = env(p, 0.12, 0.34);
      groundGlow(ctx, cx, cy, ux, uy, k.col, 0.2 * e, 1.15);
      // Les lignes tombent d'en haut, se posent, puis restent gravées un instant.
      for (let i = 0; i < 6; i++) {
        const ph = Math.max(0, Math.min(1, (p - i * 0.06) / 0.42));
        if (ph <= 0) continue;
        const y = cy - ux * 1.5 * (1 - ph) - i * 2.2;
        const a = (ph < 1 ? ph : 1) * e;
        let x = cx - ux * 0.55;
        while (x < cx + ux * 0.45) {
          const w = R2(R, 3, 10);
          ctx.fillStyle = rgba(k.col, 0.6 * a * R2(R, 0.4, 1));
          ctx.fillRect(x, y, w, 1.4);
          x += w + R2(R, 2, 4);
        }
      }
      // Le sol garde l'empreinte : un rectangle de texte trop régulier pour être naturel.
      if (p > 0.4) {
        const stamp = cellPath(cx, cy, ux, uy, 0.72);
        ctx.save(); ctx.clip(stamp);
        for (let i = 0; i < 4; i++) {
          let x = cx - ux * 0.6;
          const y = cy - uy * 0.3 + i * 4;
          while (x < cx + ux * 0.5) {
            const w = R2(R, 3, 9);
            ctx.fillStyle = rgba('#2a2418', 0.5 * e);
            ctx.fillRect(x, y, w, 1.2);
            x += w + R2(R, 2, 4);
          }
        }
        ctx.restore();
      }
    },
  },

  'nom-lu': {
    name: 'Nom lu à voix haute', caster: 'Lecteur de Nom', side: 'enemy', shape: 'diamond',
    range: 5, dmg: 11, col: '#e6c273',
    phrase: 'Il dira votre nom. Après, vous ne l’aurez plus.',
    note: 'Une onde d’or part du centre. Chaque case atteinte perd sa couleur une seconde.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, d, center, R } = k;
      const lp = local(p, d, 0.15);
      const e = env(lp, 0.16, 0.22);
      // Le blanchiment : la case perd sa couleur avant de recevoir la lumière.
      const wash = cellPath(cx, cy, ux, uy, 0.98);
      ctx.save();
      ctx.globalCompositeOperation = 'saturation';
      ctx.fillStyle = rgba('#808080', 0.9 * e);
      ctx.fill(wash);
      ctx.restore();
      groundGlow(ctx, cx, cy, ux, uy, k.col, 0.28 * e, 1.3);
      if (center) {
        groundRing(ctx, cx, cy, ux, uy, 0.4 + p * 4.6, k.col, 0.55 * (1 - p), 2.4);
        shaft(ctx, cx, cy, ux, uy, ux * 2.4 * e, k.col, 0.24 * e, 0.34);
      }
      // Les lettres qui se détachent de la case et montent : le nom part avec elles.
      if (e > 0.1) {
        for (let i = 0; i < 5; i++) {
          const ph = (lp * 1.4 + i / 5) % 1;
          ctx.fillStyle = rgba(k.col, 0.5 * e * Math.sin(ph * Math.PI));
          ctx.fillRect(cx + R2(R, -ux * 0.5, ux * 0.4), cy - ph * ux * 1.6, R2(R, 3, 7), 1.3);
        }
      }
    },
  },

  // ═══ RUPTURE — le choc, la cassure, ce qui a été frappé trop fort ════════════════════

  impulsivite: {
    name: 'Impulsivité', caster: 'Mané', side: 'ally', shape: 'single',
    range: 1, dmg: 15, col: '#d1662c',
    phrase: 'Elle frappe avant d’avoir décidé de frapper.',
    note: 'Un arc unique, traversant la case en diagonale. Aucun temps de préparation.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, R } = k;
      const e = env(p, 0.05, 0.12); // presque pas de montée : c'est le point du sort
      // L'arc : une seule courbe franche, en travers du losange, dans le plan du sol.
      const sw = 1 - Math.pow(1 - Math.min(1, p / 0.3), 2);
      const a0 = -2.5, a1 = a0 + 3.1 * sw;
      ctx.save();
      ctx.translate(cx, cy); ctx.scale(1, uy / ux);
      for (const [wd, al, cl] of [[7, 0.28, k.col], [3.4, 0.8, '#ffb47a'], [1.4, 0.95, '#fff0e0']]) {
        ctx.beginPath();
        ctx.arc(0, 0, ux * 0.86, a0, a1);
        ctx.strokeStyle = rgba(cl, al * e); ctx.lineWidth = wd; ctx.lineCap = 'round';
        ctx.stroke();
      }
      ctx.restore();
      groundGlow(ctx, cx, cy, ux, uy, k.col, 0.34 * e, 1.2);
      shards(ctx, R, cx, cy, ux, uy, 0.6 + p * 0.9, '#ffb47a', 0.6 * e, 7);
    },
  },

  'frappe-enclume': {
    name: 'Frappe d’enclume', caster: 'Enclume Marchante', side: 'enemy', shape: 'cross',
    range: 1, dmg: 14, col: '#e0793f',
    phrase: 'Le sol reçoit le coup avant vous.',
    note: 'Impact au centre, onde qui part dans les quatre voisines. Le sol se fend.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, d, center, R } = k;
      const lp = local(p, d, 0.14);
      const e = env(lp, 0.06, 0.2);
      if (center) {
        // Le poids qui tombe : une masse sombre qui descend et disparaît dans la case.
        const drop = Math.min(1, p / 0.16);
        if (drop < 1) {
          const h = ux * 1.7 * (1 - drop);
          ctx.fillStyle = rgba('#16141c', 0.9);
          ctx.beginPath();
          ctx.ellipse(cx, cy - h, ux * 0.42, uy * 0.5, 0, 0, Math.PI * 2);
          ctx.fill();
        }
        groundGlow(ctx, cx, cy, ux, uy, k.col, 0.5 * e, 1.5);
        groundRing(ctx, cx, cy, ux, uy, 0.3 + p * 3.2, '#ffc08a', 0.6 * (1 - p), 2.6);
      } else {
        groundGlow(ctx, cx, cy, ux, uy, k.col, 0.3 * e, 1.1);
      }
      // Les fentes : elles partent du centre de la case vers ses sommets, et restent.
      if (lp > 0.05) {
        const grow = Math.min(1, lp / 0.4);
        for (let i = 0; i < 5; i++) {
          const ang = (i / 5) * Math.PI * 2 + 0.5;
          const p2 = new Path2D();
          p2.moveTo(cx, cy);
          let x = cx, y = cy;
          for (let s = 1; s <= 3; s++) {
            x += Math.cos(ang) * ux * 0.3 * grow + R2(R, -3, 3);
            y += Math.sin(ang) * uy * 0.3 * grow + R2(R, -2, 2);
            p2.lineTo(x, y);
          }
          ctx.strokeStyle = rgba('#0a0810', 0.55 * e); ctx.lineWidth = R2(R, 1, 2.4);
          ctx.stroke(p2);
          ctx.strokeStyle = rgba(k.col, 0.5 * e); ctx.lineWidth = 0.9;
          ctx.stroke(p2);
        }
      }
      if (center) motes(ctx, R, cx, cy, ux, uy, p, '#ffc08a', 10, 1.6);
    },
  },

  'colere-echo': {
    name: 'Colère sans objet', caster: 'Écho de Colère', side: 'enemy', shape: 'diamond',
    range: 2, dmg: 12, col: '#d1662c',
    phrase: 'Il ne sait plus contre qui. Cela ne l’arrête pas.',
    note: 'Toutes les cases du losange s’allument en même temps — aucune propagation.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, d, R } = k;
      // Pas de propagation : la colère n'a pas de direction. Tout frappe à l'instant zéro.
      const e = env(p, 0.04, 0.18);
      groundGlow(ctx, cx, cy, ux, uy, k.col, (0.42 - d * 0.06) * e, 1.3);
      const edge = cellPath(cx, cy, ux, uy, 0.96);
      ctx.strokeStyle = rgba('#ffb47a', 0.55 * e); ctx.lineWidth = 1.8; ctx.stroke(edge);
      // Les fractures internes montent, très courtes, dans un désordre complet.
      for (let i = 0; i < 6; i++) {
        const x0 = cx + R2(R, -ux * 0.6, ux * 0.6), y0 = cy + R2(R, -uy * 0.5, uy * 0.5);
        const pp = new Path2D();
        pp.moveTo(x0, y0);
        pp.lineTo(x0 + R2(R, -5, 5), y0 - R2(R, 6, 16) * e);
        ctx.strokeStyle = rgba(R() > 0.5 ? '#ffd0a0' : k.col, 0.6 * e * R2(R, 0.4, 1));
        ctx.lineWidth = R2(R, 1, 2.2); ctx.stroke(pp);
      }
    },
  },

  // ═══ MÉLANCOLIE — l'eau, le froid, ce qui descend ════════════════════════════════════

  larme: {
    name: 'Larme d’Elise', caster: 'Elise', side: 'ally', shape: 'single',
    range: 4, dmg: 10, col: '#6f96c8',
    phrase: 'Une seule. Elle tombe droit, et la case se creuse.',
    note: 'Chute verticale, impact ponctuel, deux ondes concentriques. Rien ne brûle.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, R } = k;
      // La chute occupe le premier tiers : elle est visible, lente, inévitable.
      if (p < 0.34) {
        const q = p / 0.34;
        const y = cy - ux * 2.1 * (1 - q);
        const dp = new Path2D();
        dp.moveTo(cx, y - 7); dp.quadraticCurveTo(cx + 2.6, y, cx, y + 4);
        dp.quadraticCurveTo(cx - 2.6, y, cx, y - 7);
        ctx.fillStyle = rgba('#cfe0ff', 0.9); ctx.fill(dp);
        ctx.strokeStyle = rgba(k.col, 0.4); ctx.lineWidth = 1; ctx.stroke(dp);
      }
      const e = env(Math.max(0, (p - 0.3) / 0.7), 0.1, 0.26);
      groundGlow(ctx, cx, cy, ux, uy, k.col, 0.34 * e, 1.35);
      const q2 = Math.max(0, (p - 0.3) / 0.7);
      groundRing(ctx, cx, cy, ux, uy, 0.15 + q2 * 1.5, '#cfe0ff', 0.6 * (1 - q2), 1.8);
      groundRing(ctx, cx, cy, ux, uy, 0.15 + q2 * 2.6, k.col, 0.35 * (1 - q2), 1.2);
      // La case reste mouillée : une flaque sombre, réfléchissante, qui ne s'évapore pas vite.
      if (q2 > 0.1) {
        const pool = cellPath(cx, cy, ux, uy, 0.5 + q2 * 0.2);
        ctx.fillStyle = rgba('#101828', 0.34 * e);
        ctx.fill(pool);
        for (let i = 0; i < 4; i++) {
          ctx.strokeStyle = rgba('#cfe0ff', 0.14 * e);
          ctx.beginPath();
          ctx.moveTo(cx - ux * 0.4, cy - uy * 0.2 + i * 3);
          ctx.lineTo(cx + ux * 0.35, cy - uy * 0.2 + i * 3);
          ctx.lineWidth = 1; ctx.stroke();
        }
      }
    },
  },

  'berceuse-inversee': {
    name: 'Berceuse inversée', caster: 'Mina', side: 'ally', shape: 'diamond',
    range: 3, dmg: 6, col: '#6f96c8',
    phrase: 'Elle chante la fin d’abord. Personne ne s’endort.',
    note: 'Trois ondes lentes, du bord vers le centre : la seule zone qui se ferme.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, d, center } = k;
      // L'inversion est LITTÉRALE : l'onde part du bord et se referme sur le centre.
      const lp = local(p, 2 - d, 0.15);
      const e = env(lp, 0.22, 0.2);
      groundGlow(ctx, cx, cy, ux, uy, k.col, 0.26 * e, 1.2);
      const edge = cellPath(cx, cy, ux, uy, 0.94);
      ctx.strokeStyle = rgba('#a8c4e8', 0.4 * e); ctx.lineWidth = 1.4; ctx.stroke(edge);
      if (center) {
        for (let i = 0; i < 3; i++) {
          const q = Math.max(0, Math.min(1, (p - i * 0.14) / 0.7));
          if (q <= 0) continue;
          groundRing(ctx, cx, cy, ux, uy, 3.4 * (1 - q) + 0.2, '#cfe0ff', 0.45 * q * (1 - q) * 4, 1.8);
        }
        shaft(ctx, cx, cy, ux, uy, ux * 1.6 * e, k.col, 0.2 * e, 0.3);
      }
    },
  },

  'chagrin-goutte': {
    name: 'Goutte-à-goutte', caster: 'Écho de Chagrin', side: 'enemy', shape: 'single',
    range: 3, dmg: 9, col: '#6f96c8',
    phrase: 'Il pleure une personne dont il a oublié le nom.',
    note: 'Sept gouttes espacées sur la durée. Chacune est un petit impact séparé.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, R } = k;
      const glow = env(p, 0.1, 0.5);
      groundGlow(ctx, cx, cy, ux, uy, k.col, 0.18 * glow, 1.1);
      // Sept gouttes, décalées : la douleur n'est pas un choc, c'est une répétition.
      for (let i = 0; i < 7; i++) {
        const off = i * 0.11;
        const q = (p - off) / 0.3;
        if (q <= 0 || q >= 1.9) continue;
        const ox = R2(R, -ux * 0.45, ux * 0.45), oy = R2(R, -uy * 0.35, uy * 0.35);
        if (q < 1) {
          const y = cy + oy - ux * 1.5 * (1 - q);
          ctx.fillStyle = rgba('#cfe0ff', 0.8);
          ctx.beginPath(); ctx.ellipse(cx + ox, y, 1.5, 3.4, 0, 0, Math.PI * 2); ctx.fill();
        } else {
          const r = (q - 1) / 0.9;
          groundRing(ctx, cx + ox, cy + oy, ux, uy, 0.1 + r * 0.5, '#cfe0ff', 0.5 * (1 - r), 1.2);
        }
      }
    },
  },

  // ═══ SILENCE — le retrait, le blanc, ce qui coupe le son ═════════════════════════════

  'silence-partage': {
    name: 'Silence partagé', caster: 'Elise', side: 'ally', shape: 'map',
    range: 99, dmg: 8, col: '#c3bfcc', once: true,
    phrase: 'Le silence tombe sur tout le monde. Vous compris.',
    note: 'Le seul sort qui n’épargne aucune case. Toute la salle blanchit, puis se rend.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, dist } = k;
      // La vague part de la lanceuse et couvre la salle : `dist` est en cases réelles.
      const lp = local(p, Math.min(9, dist ?? 0), 0.045);
      const e = env(lp, 0.14, 0.3);
      // Blanchiment : la case perd sa couleur. C'est le sort le plus violent visuellement,
      // et il ne contient pas une seule particule.
      const wash = cellPath(cx, cy, ux, uy, 1);
      ctx.save();
      ctx.globalCompositeOperation = 'saturation';
      ctx.fillStyle = rgba('#808080', e);
      ctx.fill(wash);
      ctx.restore();
      ctx.fillStyle = rgba(k.col, 0.16 * e); ctx.fill(wash);
      const edge = cellPath(cx, cy, ux, uy, 0.97);
      ctx.strokeStyle = rgba('#efedf7', 0.5 * e); ctx.lineWidth = 1.2; ctx.stroke(edge);
    },
  },

  'se-taire': {
    name: 'Se taire', caster: 'Voile Marcheur', side: 'enemy', shape: 'diamond',
    range: 3, dmg: 7, col: '#c3bfcc',
    phrase: 'Vous avez traversé quelque chose. Vous ne savez pas quoi.',
    note: 'La lumière du sol est RETIRÉE au lieu d’être ajoutée : la zone s’assombrit.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, d, center } = k;
      const lp = local(p, d, 0.12);
      const e = env(lp, 0.2, 0.3);
      // L'inverse de tous les autres sorts : on éteint la case. C'est ce qui le rend lisible
      // au milieu d'un jeu où tout s'allume.
      const cell = cellPath(cx, cy, ux, uy, 1);
      ctx.fillStyle = rgba('#0a0b12', 0.62 * e);
      ctx.fill(cell);
      const edge = cellPath(cx, cy, ux, uy, 0.95);
      ctx.strokeStyle = rgba(k.col, 0.4 * e); ctx.lineWidth = 1.3; ctx.stroke(edge);
      if (center) {
        // Le seul élément clair : un point dense au centre, très petit.
        groundGlow(ctx, cx, cy, ux, uy, '#efedf7', 0.3 * e, 0.5);
        groundRing(ctx, cx, cy, ux, uy, 0.2 + p * 2.8, k.col, 0.3 * (1 - p), 1.4);
      }
    },
  },

  'flamme-froide': {
    name: 'Flamme froide', caster: 'Gardien à Facettes', side: 'enemy', shape: 'cross',
    range: 3, dmg: 11, col: '#b3bdf2',
    phrase: 'Elle brûle sous le marbre, pas au-dessus.',
    note: 'La flamme est SOUS la case : elle éclaire les joints du sol par en dessous.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, d, center, R } = k;
      const lp = local(p, d, 0.13);
      const e = env(lp, 0.2, 0.28);
      // La lumière vient de dessous : le losange est éclairé de l'intérieur, aux arêtes.
      const cell = cellPath(cx, cy, ux, uy, 1);
      ctx.save(); ctx.clip(cell);
      const g = ctx.createRadialGradient(cx, cy, 1, cx, cy, ux);
      g.addColorStop(0, rgba('#e0e6ff', 0.5 * e));
      g.addColorStop(0.5, rgba(k.col, 0.3 * e));
      g.addColorStop(1, rgba(k.col, 0));
      ctx.fillStyle = g; ctx.fillRect(cx - ux, cy - uy, ux * 2, uy * 2);
      // Les joints du sol s'allument : des lignes fines, froides, qui suivent le losange.
      for (let i = -2; i <= 2; i++) {
        ctx.beginPath();
        ctx.moveTo(cx - ux, cy + i * uy * 0.4);
        ctx.lineTo(cx + ux, cy + i * uy * 0.4 + uy * 0.2);
        ctx.strokeStyle = rgba('#e8ecff', 0.28 * e); ctx.lineWidth = 1; ctx.stroke();
      }
      ctx.restore();
      ctx.strokeStyle = rgba('#e8ecff', 0.6 * e); ctx.lineWidth = 1.6; ctx.stroke(cell);
      // Les langues de flamme froide : basses, larges, sans mouvement vertical marqué.
      for (let i = 0; i < 5; i++) {
        const ang = (i / 5) * Math.PI * 2 + p * 0.6;
        const fx = cx + Math.cos(ang) * ux * 0.5, fy = cy + Math.sin(ang) * uy * 0.5;
        const h = uy * R2(R, 1.1, 2.2) * e;
        const fp = new Path2D();
        fp.moveTo(fx - 4, fy);
        fp.quadraticCurveTo(fx - 2, fy - h * 0.6, fx, fy - h);
        fp.quadraticCurveTo(fx + 2, fy - h * 0.6, fx + 4, fy);
        fp.closePath();
        const fg = ctx.createLinearGradient(fx, fy - h, fx, fy);
        fg.addColorStop(0, rgba('#f0f4ff', 0.06 * e));
        fg.addColorStop(1, rgba(k.col, 0.42 * e));
        ctx.fillStyle = fg; ctx.fill(fp);
      }
      if (center) motes(ctx, R, cx, cy, ux, uy, p, k.col, 9, 1.4);
    },
  },

  // ═══ DÉNI — le blanc propre, le protocole, ce qui prétend que tout va bien ═══════════

  'regard-infantile': {
    name: 'Regard infantile', caster: 'Mina', side: 'ally', shape: 'single',
    range: 4, dmg: 7, col: '#d9a441',
    phrase: 'Elle regarde. La cible cesse d’avoir raison.',
    note: 'Aucun projectile : la case est simplement vue. Un cercle net, sans bavure.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p } = k;
      const e = env(p, 0.22, 0.3);
      // Le contraire d'un impact : un cercle propre, net, qui se contracte. Rien n'explose.
      const r = 2.6 - 2 * Math.min(1, p / 0.5);
      groundRing(ctx, cx, cy, ux, uy, Math.max(0.3, r), k.col, 0.7 * e, 1.8);
      groundRing(ctx, cx, cy, ux, uy, Math.max(0.2, r * 0.6), k.col, 0.4 * e, 1.1);
      groundGlow(ctx, cx, cy, ux, uy, k.col, 0.3 * e, 0.9);
      // La pupille : un aplat sombre au centre, cerné d'or. Le regard est le sort.
      ctx.fillStyle = rgba('#1a1508', 0.6 * e);
      ctx.beginPath(); ctx.ellipse(cx, cy, ux * 0.16, uy * 0.16, 0, 0, Math.PI * 2); ctx.fill();
      ctx.strokeStyle = rgba('#f0d898', 0.8 * e); ctx.lineWidth = 1.3; ctx.stroke();
    },
  },

  'injection-blanche': {
    name: 'Injection blanche', caster: 'Infirmière du Déni', side: 'enemy', shape: 'single',
    range: 3, dmg: 9, col: '#e8e4ee',
    phrase: 'Vous n’avez pas mal. Regardez le dossier.',
    note: 'Une seule ligne droite, très fine, et la case devient blanche et propre.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p } = k;
      // La ligne : chirurgicale, sans halo, sans éclat. C'est un geste, pas une explosion.
      if (p < 0.3) {
        const q = p / 0.3;
        ctx.strokeStyle = rgba('#ffffff', 0.85);
        ctx.lineWidth = 1.2; ctx.lineCap = 'butt';
        ctx.beginPath();
        ctx.moveTo(cx + ux * 0.9 - ux * 1.4 * q, cy - uy * 1.9 + uy * 2.4 * q);
        ctx.lineTo(cx + ux * 0.75 - ux * 1.4 * q, cy - uy * 1.6 + uy * 2.4 * q);
        ctx.stroke();
      }
      const e = env(Math.max(0, (p - 0.26) / 0.74), 0.1, 0.4);
      // La case blanchit uniformément : aucune texture, aucun grain. C'est ça, le déni.
      const cell = cellPath(cx, cy, ux, uy, 0.98);
      ctx.fillStyle = rgba('#e8e4ee', 0.3 * e); ctx.fill(cell);
      ctx.strokeStyle = rgba('#ffffff', 0.5 * e); ctx.lineWidth = 1.1; ctx.stroke(cell);
      groundGlow(ctx, cx, cy, ux, uy, '#ffffff', 0.2 * e, 1);
      // Un point de ponction, minuscule, et une seule perle sombre.
      ctx.fillStyle = rgba('#6a2028', 0.5 * e);
      ctx.beginPath(); ctx.arc(cx, cy, 1.6, 0, Math.PI * 2); ctx.fill();
    },
  },

  // ═══ EFFROI — le rouge, la vitesse, ce qui chasse ════════════════════════════════════

  frappe: {
    name: 'Frappe', caster: 'Ombre menaçante', side: 'enemy', shape: 'cross',
    range: 2, dmg: 15, col: '#c8394a', boss: true,
    phrase: 'Quelque chose occupe la salle. On ne voit pas quoi.',
    note: 'Trois griffures parallèles en travers de la croix. Le rouge est un événement.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, d, center, R } = k;
      const lp = local(p, d, 0.08); // très rapide : la frappe arrive presque partout à la fois
      const e = env(lp, 0.05, 0.16);
      groundGlow(ctx, cx, cy, ux, uy, k.col, (center ? 0.44 : 0.28) * e, 1.3);
      // Trois griffures parallèles, dans le plan du sol, orientées pareil sur toute la croix :
      // c'est UN geste qui traverse cinq cases, pas cinq gestes.
      const sw = Math.min(1, lp / 0.22);
      for (let i = 0; i < 3; i++) {
        const off = (i - 1) * uy * 0.34;
        const x0 = cx - ux * 0.95, y0 = cy + off + uy * 0.3;
        const x1 = x0 + ux * 1.9 * sw, y1 = y0 - uy * 0.6 * sw;
        for (const [wd, al, cl] of [[5.5, 0.22, k.col], [2.6, 0.7, '#ff7a6a'], [1, 0.95, '#ffe0d8']]) {
          ctx.beginPath(); ctx.moveTo(x0, y0); ctx.lineTo(x1, y1);
          ctx.strokeStyle = rgba(cl, al * e); ctx.lineWidth = wd; ctx.lineCap = 'round';
          ctx.stroke();
        }
      }
      if (center) {
        groundRing(ctx, cx, cy, ux, uy, 0.2 + p * 3, k.col, 0.4 * (1 - p), 2);
        shards(ctx, R, cx, cy, ux, uy, 0.7 + p, '#ff7a6a', 0.5 * e, 8);
      }
    },
  },

  curee: {
    name: 'Curée', caster: 'Voraces', side: 'enemy', shape: 'single',
    range: 1, dmg: 13, col: '#c8394a',
    phrase: 'Elles dévorent les énergies. Elles ne se pressent pas.',
    note: 'La case s’assombrit par les bords vers le centre — quelque chose s’y referme.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, R } = k;
      const e = env(p, 0.12, 0.24);
      // La mâchoire : deux arcs qui se referment sur la case, dans le plan du sol.
      const close = Math.min(1, p / 0.36);
      ctx.save();
      ctx.translate(cx, cy); ctx.scale(1, uy / ux);
      for (const s of [-1, 1]) {
        const jp = new Path2D();
        const a = 1.5 - close * 1.35;
        jp.arc(0, 0, ux * 0.95, s > 0 ? -a : Math.PI - a, s > 0 ? a : Math.PI + a);
        ctx.strokeStyle = rgba('#0a0508', 0.7 * e); ctx.lineWidth = 7; ctx.lineCap = 'round';
        ctx.stroke(jp);
        ctx.strokeStyle = rgba(k.col, 0.55 * e); ctx.lineWidth = 2; ctx.stroke(jp);
      }
      ctx.restore();
      // Les dents : courtes, tournées vers l'intérieur, plus claires que le reste.
      for (let i = 0; i < 10; i++) {
        const ang = (i / 10) * Math.PI * 2;
        const x0 = cx + Math.cos(ang) * ux * 0.9, y0 = cy + Math.sin(ang) * uy * 0.9;
        const x1 = cx + Math.cos(ang) * ux * 0.62, y1 = cy + Math.sin(ang) * uy * 0.62;
        ctx.beginPath(); ctx.moveTo(x0, y0); ctx.lineTo(x1, y1);
        ctx.strokeStyle = rgba('#e8dce0', 0.55 * e * close); ctx.lineWidth = 1.6; ctx.stroke();
      }
      // Le centre est vidé : la lumière est aspirée, pas émise.
      ctx.fillStyle = rgba('#08050a', 0.5 * e);
      ctx.beginPath(); ctx.ellipse(cx, cy, ux * 0.5 * close, uy * 0.5 * close, 0, 0, Math.PI * 2); ctx.fill();
      shards(ctx, R, cx, cy, ux, uy, 0.5 + p * 0.6, k.col, 0.35 * e, 6);
    },
  },

  // ═══ FOLIE — le rose, la symétrie fausse, ce qui vous montre quelque chose ═══════════

  'vol-a-la-tire': {
    name: 'Vol à la tire', caster: 'John', side: 'ally', shape: 'single',
    range: 1, dmg: 13, col: '#cf3f92',
    phrase: 'Il prend quelque chose. On ne saura pas quoi avant longtemps.',
    note: 'Un fil part de la case vers le lanceur. L’effet se lit au RETOUR, pas à l’aller.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, from, R } = k;
      const e = env(p, 0.1, 0.2);
      groundGlow(ctx, cx, cy, ux, uy, k.col, 0.24 * e, 1);
      // Le fil tiré : de la case vers le lanceur. C'est le seul sort dont l'animation
      // va à contre-sens — et c'est ce qui fait comprendre qu'il a PRIS.
      if (from) {
        const q = Math.min(1, Math.max(0, (p - 0.12) / 0.5));
        const mx = cx + (from.x - cx) * q, my = cy + (from.y - cy) * q - Math.sin(q * Math.PI) * ux * 0.5;
        const th = new Path2D();
        th.moveTo(cx, cy);
        th.quadraticCurveTo((cx + mx) / 2, (cy + my) / 2 - ux * 0.3, mx, my);
        ctx.strokeStyle = rgba(k.col, 0.5 * e); ctx.lineWidth = 1.2; ctx.stroke(th);
        // L'objet volé : une petite forme claire, indéterminée, qui remonte le fil.
        if (q > 0 && q < 1) {
          ctx.fillStyle = rgba('#f0c8e4', 0.9 * e);
          ctx.beginPath(); ctx.ellipse(mx, my, 3, 2.2, q * 3, 0, Math.PI * 2); ctx.fill();
        }
      }
      // Sur la case : une absence. Un contour vide là où l'objet était.
      const gap = cellPath(cx, cy, ux, uy, 0.34);
      ctx.strokeStyle = rgba(k.col, 0.6 * e); ctx.lineWidth = 1.4;
      ctx.setLineDash([3, 3]); ctx.stroke(gap); ctx.setLineDash([]);
      for (let i = 0; i < 5; i++) {
        ctx.fillStyle = rgba('#f0c8e4', 0.4 * e * R2(R, 0.3, 1));
        const ang = R() * Math.PI * 2, rr = R2(R, 0.3, 0.9);
        ctx.beginPath(); ctx.arc(cx + Math.cos(ang) * ux * rr, cy + Math.sin(ang) * uy * rr, R2(R, 0.8, 1.6), 0, Math.PI * 2); ctx.fill();
      }
    },
  },

  'reflet-force': {
    name: 'Reflet forcé', caster: 'Miroir Porteur', side: 'enemy', shape: 'diamond',
    range: 2, dmg: 10, col: '#cf3f92',
    phrase: 'Il ne vous attaque pas. Il vous présente.',
    note: 'Chaque case reçoit une copie inversée de la case opposée. Symétrie fausse.',
    fx(k) {
      const { ctx, cx, cy, ux, uy, p, d, center } = k;
      const lp = local(p, d, 0.1);
      const e = env(lp, 0.16, 0.26);
      const cell = cellPath(cx, cy, ux, uy, 0.98);
      // La case devient réfléchissante : un aplat froid, un liseré rose, rien de plus.
      ctx.fillStyle = rgba('#1a1626', 0.42 * e); ctx.fill(cell);
      const g = ctx.createLinearGradient(cx - ux, cy - uy, cx + ux, cy + uy);
      g.addColorStop(0, rgba('#6a5a80', 0.3 * e));
      g.addColorStop(0.5, rgba('#0f0c18', 0.1 * e));
      g.addColorStop(1, rgba(k.col, 0.28 * e));
      ctx.save(); ctx.clip(cell); ctx.fillStyle = g; ctx.fillRect(cx - ux, cy - uy, ux * 2, uy * 2); ctx.restore();
      ctx.strokeStyle = rgba(k.col, 0.55 * e); ctx.lineWidth = 1.3; ctx.stroke(cell);
      // Le décalage : le même liseré, répété deux fois, jamais aligné. C'est la faille.
      for (let i = 1; i <= 2; i++) {
        ctx.save(); ctx.translate(i * 2.4, -i * 1.4);
        ctx.strokeStyle = rgba('#f0c8e4', 0.16 * e); ctx.lineWidth = 1; ctx.stroke(cell);
        ctx.restore();
      }
      if (center) {
        groundRing(ctx, cx, cy, ux, uy, 0.2 + p * 3.4, '#f0c8e4', 0.34 * (1 - p), 1.4);
        shaft(ctx, cx, cy, ux, uy, ux * 1.4 * e, k.col, 0.18 * e, 0.4);
      }
    },
  },

  // ═══ GÉNÉRIQUE — le vocabulaire lui-même, pour les ~117 sorts du catalogue qui n'ont pas
  // encore leur peinture dédiée (voir la note en tête de fichier : 138 sorts, 21 peints). Sans
  // ceci un sort non couvert s'exécutait en silence visuel complet ; un flash générique, correct
  // sur sa forme et sa couleur (physique/magique), bat un geste qui ne se voit pas du tout —
  // le remplacer par un effet dédié reste la bonne prochaine étape, au cas par cas.
  ...Object.fromEntries(['single', 'cross', 'diamond', 'map'].flatMap((shape) => (
    [['physique', '#c8394a'], ['magique', '#8b9dcf']].map(([flavor, col]) => [
      `generique-${flavor}-${shape}`,
      {
        name: `Geste ${flavor === 'physique' ? 'physique' : 'magique'} générique`,
        caster: null, side: null, shape, range: 0, dmg: 0, col,
        note: 'Repli générique : pas encore de peinture dédiée pour ce sort.',
        fx(k) {
          const { ctx, cx, cy, ux, uy, p, d, center } = k;
          const lp = local(p, d, 0.12);
          const e = env(lp, 0.14, 0.2);
          groundGlow(ctx, cx, cy, ux, uy, k.col, (center ? 0.4 : 0.24) * e, 1.4);
          groundRing(ctx, cx, cy, ux, uy, 0.3 + p * 2.4, k.col, 0.4 * (1 - p), 1.8);
          if (center) shaft(ctx, cx, cy, ux, uy, ux * 1.2 * e, k.col, 0.22 * e, 0.36);
        },
      },
    ])
  ))),
};

export const SORT_IDS = Object.keys(SORTS);

/** Fiche lisible d'un sort, pour l'atelier et le handoff. */
export function sortInfo(id) {
  const s = SORTS[id];
  if (!s) return null;
  return {
    id, name: s.name, caster: s.caster, side: s.side,
    shape: s.shape, shapeLabel: SHAPES[s.shape].label, shapeGlyph: SHAPES[s.shape].glyph,
    range: s.range, dmg: s.dmg, col: s.col, once: !!s.once, boss: !!s.boss,
    phrase: s.phrase, note: s.note,
  };
}

/** Joue un sort sur une liste de cases déjà projetées à l'écran.
 *
 *  `cells` : [{ cx, cy, ux, uy, d, center, dist }] — cy est l'ancre au sol, élévation
 *  comprise. `from` : point écran du lanceur, pour les sorts qui tirent un fil.
 *  Les cases sont peintes du fond vers l'avant : c'est l'appelant qui les a triées. */
export function playSort(ctx, id, cells, p, from = null, catalogColor = null) {
  const s = SORTS[id];
  if (!s || p <= 0 || p >= 1) return;
  for (const c of cells) {
    const R = seeded(id + ':' + c.cx.toFixed(1) + ':' + c.cy.toFixed(1));
    ctx.save();
    s.fx({
      ctx, R, p, from, col: catalogColor ?? s.col,
      cx: c.cx, cy: c.cy, ux: c.ux, uy: c.uy,
      d: c.d ?? 0, center: !!c.center, dist: c.dist ?? c.d ?? 0,
    });
    ctx.restore();
  }
}
