// Procedural soft-edged radial-gradient sprite texture, shared by every FogCloud
// instance (generating one per cell would be wasteful — this is pure paint data, not
// per-cell state). Built at runtime from a canvas rather than a shipped image: the
// project's "no external assets" constraint means "no fetched files", not "no
// generated textures" — this is the same idiom every Three.js soft-particle/glow demo
// uses to fake a fluffy blob out of a billboard instead of a hard-edged polygon.
import * as THREE from 'three';

let sharedTexture: THREE.Texture | null = null;

export function getFogSpriteTexture(): THREE.Texture | null {
  if (sharedTexture) return sharedTexture;
  if (typeof document === 'undefined') return null;

  const size = 128;
  const canvas = document.createElement('canvas');
  canvas.width = size;
  canvas.height = size;
  const ctx = canvas.getContext('2d');
  if (!ctx) return null;

  const gradient = ctx.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
  gradient.addColorStop(0, 'rgba(255,255,255,0.9)');
  gradient.addColorStop(0.35, 'rgba(255,255,255,0.55)');
  gradient.addColorStop(0.7, 'rgba(255,255,255,0.18)');
  gradient.addColorStop(1, 'rgba(255,255,255,0)');
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, size, size);

  sharedTexture = new THREE.CanvasTexture(canvas);
  return sharedTexture;
}
