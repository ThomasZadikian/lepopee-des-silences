// @vitest-environment jsdom
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { ref, computed } from 'vue';
import DevToolsPanel from './DevToolsPanel.vue';

// Mock useDevTools avec des Ref/ComputedRef valides
vi.mock('../composables/useDevTools', () => ({
  useDevTools: () => ({
    token: ref(''),
    hasToken: computed(() => false),
    status: ref('unknown'),
    statusEnvironment: ref<string | null>(null),
    isLoading: ref(false),
    message: ref<string | null>(null),
    error: ref<string | null>(null),
    saveToken: vi.fn(),
    clearToken: vi.fn(),
    checkStatus: vi.fn(),
    runAction: vi.fn().mockResolvedValue(false),
  }),
}));

function mountPanel() {
  return mount(DevToolsPanel);
}

describe('DevToolsPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders correctly', () => {
    const wrapper = mountPanel();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays error message when devTools.error exists', async () => {
    const { useDevTools } = await import('../composables/useDevTools');
    vi.mocked(useDevTools).mockReturnValueOnce({
      token: ref(''),
      hasToken: computed(() => false),
      status: ref('unknown'),
      statusEnvironment: ref<string | null>(null),
      isLoading: ref(false),
      message: ref<string | null>(null),
      error: ref('Token devtools absent.'),
      saveToken: vi.fn(),
      clearToken: vi.fn(),
      checkStatus: vi.fn(),
      runAction: vi.fn().mockResolvedValue(false),
    });

    const wrapper = mountPanel();
    expect(wrapper.text()).toContain('Token devtools absent.');
  });
});
