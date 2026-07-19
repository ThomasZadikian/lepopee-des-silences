// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';

import GrimoirePaginationControls from './GrimoirePaginationControls.vue';

describe('GrimoirePaginationControls', () => {
  it('shows the current page and total', () => {
    const wrapper = mount(GrimoirePaginationControls, { props: { currentPage: 2, totalPages: 4 } });

    expect(wrapper.find('.grimoire-page-indicator').text()).toBe('Page 2 / 4');
  });

  it('disables Précédent on the first page and Suivant on the last page', () => {
    const first = mount(GrimoirePaginationControls, { props: { currentPage: 1, totalPages: 3 } });
    expect(first.findAll('.grimoire-page-btn')[0].attributes('disabled')).toBeDefined();
    expect(first.findAll('.grimoire-page-btn')[1].attributes('disabled')).toBeUndefined();

    const last = mount(GrimoirePaginationControls, { props: { currentPage: 3, totalPages: 3 } });
    expect(last.findAll('.grimoire-page-btn')[0].attributes('disabled')).toBeUndefined();
    expect(last.findAll('.grimoire-page-btn')[1].attributes('disabled')).toBeDefined();
  });

  it('emits previous and next on click', async () => {
    const wrapper = mount(GrimoirePaginationControls, { props: { currentPage: 2, totalPages: 3 } });

    await wrapper.findAll('.grimoire-page-btn')[0].trigger('click');
    await wrapper.findAll('.grimoire-page-btn')[1].trigger('click');

    expect(wrapper.emitted('previous')).toHaveLength(1);
    expect(wrapper.emitted('next')).toHaveLength(1);
  });
});
