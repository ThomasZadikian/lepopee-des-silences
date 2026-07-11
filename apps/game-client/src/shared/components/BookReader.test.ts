// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import BookReader from './BookReader.vue';

function mountReader(modelValue = true, title = 'Le Carnet', pages = ['Page un.', 'Page deux.', 'Page trois.']) {
  return mount(BookReader, {
    props: { modelValue, title, pages },
    global: {
      stubs: {
        Teleport: { template: '<slot />' },
        Transition: { template: '<slot />' },
      },
    },
  });
}

describe('BookReader', () => {
  it('renders without crashing', () => {
    const wrapper = mountReader();
    expect(wrapper.exists()).toBe(true);
  });

  it('does not render when modelValue is false', () => {
    const wrapper = mountReader(false);
    expect(wrapper.find('.book-backdrop').exists()).toBe(false);
  });

  it('displays the title', () => {
    const wrapper = mountReader(true, 'Le carnet du premier architecte');
    expect(wrapper.text()).toContain('Le carnet du premier architecte');
  });

  it('shows the first page on open', () => {
    const wrapper = mountReader(true, 'Titre', ['PLACEHOLDER HISTOIRE 01']);
    expect(wrapper.text()).toContain('PLACEHOLDER HISTOIRE 01');
  });

  it('shows the page counter', () => {
    const wrapper = mountReader(true, 'Titre', ['Un', 'Deux', 'Trois']);
    expect(wrapper.text()).toContain('Page 1 / 3');
  });

  it('disables the previous button on the first page', () => {
    const wrapper = mountReader();
    const prevBtn = wrapper.findAll('button').find((b) => b.text().includes('précédente'));
    expect((prevBtn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('advances to the next page when clicking next', async () => {
    const wrapper = mountReader(true, 'Titre', ['Un', 'Deux', 'Trois']);
    const nextBtn = wrapper.findAll('button').find((b) => b.text().includes('suivante'));
    await nextBtn!.trigger('click');
    expect(wrapper.text()).toContain('Page 2 / 3');
    expect(wrapper.text()).toContain('Deux');
  });

  it('disables the next button on the last page', async () => {
    const wrapper = mountReader(true, 'Titre', ['Un', 'Deux']);
    const nextBtn = wrapper.findAll('button').find((b) => b.text().includes('suivante'));
    await nextBtn!.trigger('click');
    expect((nextBtn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('goes back to the previous page when clicking previous', async () => {
    const wrapper = mountReader(true, 'Titre', ['Un', 'Deux', 'Trois']);
    const nextBtn = wrapper.findAll('button').find((b) => b.text().includes('suivante'));
    await nextBtn!.trigger('click');
    const prevBtn = wrapper.findAll('button').find((b) => b.text().includes('précédente'));
    await prevBtn!.trigger('click');
    expect(wrapper.text()).toContain('Page 1 / 3');
  });

  it('emits update:modelValue with false when the close button is clicked', async () => {
    const wrapper = mountReader();
    await wrapper.find('.book__close').trigger('click');
    expect(wrapper.emitted('update:modelValue')).toBeDefined();
    expect(wrapper.emitted('update:modelValue')![0][0]).toBe(false);
  });

  it('emits update:modelValue with false when the backdrop is clicked', async () => {
    const wrapper = mountReader();
    await wrapper.find('.book-backdrop').trigger('click');
    expect(wrapper.emitted('update:modelValue')).toBeDefined();
    expect(wrapper.emitted('update:modelValue')![0][0]).toBe(false);
  });

  it('does not close when clicking the panel itself', async () => {
    const wrapper = mountReader();
    await wrapper.find('.book').trigger('click');
    expect(wrapper.emitted('update:modelValue')).toBeUndefined();
  });

  it('navigates with the ArrowRight/ArrowLeft keys', async () => {
    const wrapper = mountReader(true, 'Titre', ['Un', 'Deux', 'Trois']);
    await wrapper.find('.book-backdrop').trigger('keydown', { key: 'ArrowRight' });
    expect(wrapper.text()).toContain('Page 2 / 3');
    await wrapper.find('.book-backdrop').trigger('keydown', { key: 'ArrowLeft' });
    expect(wrapper.text()).toContain('Page 1 / 3');
  });

  it('closes on Escape key', async () => {
    const wrapper = mountReader();
    await wrapper.find('.book-backdrop').trigger('keydown', { key: 'Escape' });
    expect(wrapper.emitted('update:modelValue')).toBeDefined();
    expect(wrapper.emitted('update:modelValue')![0][0]).toBe(false);
  });
});
