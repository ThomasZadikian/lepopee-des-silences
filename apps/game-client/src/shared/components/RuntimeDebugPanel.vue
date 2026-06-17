<script setup lang="ts">
defineProps<{
  data: unknown;
  label?: string;
}>();

function pretty(value: unknown): string {
  try { return JSON.stringify(value, null, 2); }
  catch { return String(value); }
}
</script>

<template>
  <details class="rdp">
    <summary class="rdp__summary">
      <span class="rdp__badge">ALPHA</span>
      <span class="rdp__label">{{ label ?? 'Runtime Debug' }}</span>
      <span class="rdp__hint">▸</span>
    </summary>
    <div class="rdp__body">
      <pre class="rdp__pre"><code>{{ pretty(data) }}</code></pre>
    </div>
  </details>
</template>

<style scoped>
.rdp {
  border: 1px solid oklch(0.45 0.12 85 / .35);
  border-radius: 4px;
  overflow: hidden;
}

.rdp__summary {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 7px 12px;
  cursor: pointer;
  user-select: none;
  background: oklch(0.22 0.025 85 / .15);
  list-style: none;
}
.rdp__summary::-webkit-details-marker { display: none; }

.rdp__badge {
  font-family: var(--caps, monospace);
  font-size: 8.5px;
  letter-spacing: 0.18em;
  color: var(--gold, oklch(.72 .1 85));
  border: 1px solid oklch(0.72 .1 85 / .5);
  padding: 1px 5px;
  border-radius: 2px;
  flex-shrink: 0;
}

.rdp__label {
  font-family: var(--caps, monospace);
  font-size: 9.5px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4);
  flex: 1;
}

.rdp__hint {
  color: var(--ink-4);
  font-size: 10px;
  transition: transform .2s;
}
details[open] .rdp__hint {
  transform: rotate(90deg);
}

.rdp__body {
  border-top: 1px solid oklch(0.45 0.12 85 / .25);
  background: oklch(0.15 0.02 270 / .8);
  max-height: 400px;
  overflow-y: auto;
}

.rdp__pre {
  margin: 0;
  padding: 12px 14px;
  font-family: var(--mono, monospace);
  font-size: 10.5px;
  line-height: 1.55;
  color: var(--ink-3);
  white-space: pre;
  overflow-x: auto;
}
</style>
