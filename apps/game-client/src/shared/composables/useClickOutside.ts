import { onBeforeUnmount, onMounted, type Ref } from 'vue';

/**
 * Calls `onOutside` on the first mousedown that lands outside `target`'s
 * element. Uses the capture phase on mousedown (fires before any click
 * handler) so a click that *opens* a panel never immediately closes it —
 * only clicks made while it's already open can trigger a close.
 */
export function useClickOutside(
  target: Ref<HTMLElement | null | undefined>,
  onOutside: () => void,
  options?: { ignoreSelectors?: string[] },
) {
  function handlePointerDown(event: MouseEvent) {
    const el = target.value;
    if (!el) return;
    const clicked = event.target as Node | null;
    if (!clicked || el.contains(clicked)) return;
    if (options?.ignoreSelectors?.some((selector) => (clicked as HTMLElement).closest?.(selector))) return;
    onOutside();
  }

  onMounted(() => document.addEventListener('mousedown', handlePointerDown, true));
  onBeforeUnmount(() => document.removeEventListener('mousedown', handlePointerDown, true));
}
