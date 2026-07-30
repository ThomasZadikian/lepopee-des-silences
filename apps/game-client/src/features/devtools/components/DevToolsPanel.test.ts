// @vitest-environment jsdom
import { createPinia } from 'pinia';
import { computed, ref } from 'vue';
import { shallowMount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { useDevTools } from '../composables/useDevTools';
import DevToolsPanel from './DevToolsPanel.vue';

vi.mock('../composables/useDevTools', () => ({
  useDevTools: vi.fn(),
}));

function devToolsState(errorMessage: string | null = null) {
  return {
    token: ref(''),
    hasToken: computed(() => false),
    status: ref<'unknown'>('unknown'),
    statusEnvironment: ref<string | null>(null),
    isLoading: ref(false),
    message: ref<string | null>(null),
    error: ref<string | null>(errorMessage),
    saveToken: vi.fn(),
    clearToken: vi.fn(),
    checkStatus: vi.fn(),
    runAction: vi.fn().mockResolvedValue(false),
  };
}

function mountPanel() {
  return shallowMount(DevToolsPanel, {
    props: {
      runId: 'run-1',
      combat: null,
    },
    global: {
      plugins: [createPinia()],
    },
  });
}

describe('DevToolsPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useDevTools).mockReturnValue(devToolsState());
  });

  it('renders correctly', () => {
    const wrapper = mountPanel();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays error message when devTools.error exists', async () => {
    vi.mocked(useDevTools).mockReturnValueOnce(devToolsState('Token devtools absent.'));

    const wrapper = mountPanel();
    expect(wrapper.text()).toContain('Token devtools absent.');
  });
});
