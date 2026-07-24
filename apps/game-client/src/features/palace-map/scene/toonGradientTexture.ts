// Procedural gradient-map texture for MeshToonMaterial's cel-shading bands, shared by
// every toon material in the scene (same "generate at runtime, no external asset"
// idiom as fogTexture.ts). 4 discrete steps with NearestFilter — no interpolation
// between bands — gives the crisp, cartoon-flat shading edge the technique is for.
import * as THREE from 'three';

let sharedTexture: THREE.Texture | null = null;

export function getToonGradientTexture(): THREE.Texture | null {
  if (sharedTexture) return sharedTexture;
  if (typeof document === 'undefined') return null;

  const bands = [70, 130, 190, 255];
  const canvas = document.createElement('canvas');
  canvas.width = bands.length;
  canvas.height = 1;
  const ctx = canvas.getContext('2d');
  if (!ctx) return null;

  bands.forEach((value, i) => {
    ctx.fillStyle = `rgb(${value}, ${value}, ${value})`;
    ctx.fillRect(i, 0, 1, 1);
  });

  const texture = new THREE.CanvasTexture(canvas);
  texture.magFilter = THREE.NearestFilter;
  texture.minFilter = THREE.NearestFilter;
  sharedTexture = texture;
  return texture;
}
