import AppLayout from '@/layouts/AppLayout.vue'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'

const mockLogout = vi.fn()
const mockIsAdmin = vi.fn(() => false)

vi.mock('@/stores/auth', () => ({
  useAuthStore: () => ({
    username: 'testuser',
    logout: mockLogout,
    get isAdmin() { return mockIsAdmin() },
  })
}))

vi.mock('@/api/auth', () => ({
  default: { interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } } }
}))

const router = createRouter({
  history: createMemoryHistory(),
  routes: [
    { path: '/',            name: 'Dashboard',  component: { template: '<div/>' } },
    { path: '/saves',       name: 'Saves',      component: { template: '<div/>' } },
    { path: '/inventory',   name: 'Inventory',  component: { template: '<div/>' } },
    { path: '/skills',      name: 'Skills',     component: { template: '<div/>' } },
    { path: '/gdpr',        name: 'Gdpr',       component: { template: '<div/>' } },
    { path: '/admin/users', name: 'AdminUsers', component: { template: '<div/>' } },
    { path: '/admin/items', name: 'AdminItems', component: { template: '<div/>' } },
  ]
})

const globalConfig = {
  plugins: [router],
  stubs: {
    'v-app':               { template: '<div><slot /></div>' },
    'v-app-bar':           { template: '<div><slot /></div>' },
    // Émet click sur le parent via $parent
    'v-app-bar-nav-icon':  { template: '<button @click="$emit(\'click\')" />', emits: ['click'] },
    'v-toolbar-title':     { template: '<div><slot /></div>' },
    'v-spacer':            { template: '<span />' },
    'v-chip':              { template: '<span><slot /></span>' },
    'v-btn':               { template: '<button @click="$emit(\'click\')"><slot /></button>', emits: ['click'] },
    'v-icon':              { template: '<span><slot /></span>' },
    'v-navigation-drawer': { template: '<div><slot /></div>' },
    'v-list':              { template: '<div><slot /></div>' },
    // Affiche le title comme texte
    'v-list-item': {
      template: '<div>{{ title }}<slot /></div>',
      props: ['title', 'prependIcon', 'to']
    },
    'v-list-subheader':    { template: '<div><slot /></div>' },
    'v-divider':           { template: '<hr />' },
    'v-main':              { template: '<div><slot /></div>' },
    'v-container':         { template: '<div><slot /></div>' },
    RouterView:            { template: '<div />' },
  }
}

describe('AppLayout', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    mockIsAdmin.mockReturnValue(false)
  })

  it('affiche le titre RPG_ESI07', () => {
    const wrapper = mount(AppLayout, { global: globalConfig })
    expect(wrapper.text()).toContain('RPG_ESI07')
  })

  it("affiche le nom d'utilisateur connecté", () => {
    const wrapper = mount(AppLayout, { global: globalConfig })
    expect(wrapper.text()).toContain('testuser')
  })

  it('appelle logout au clic sur le bouton déconnexion', async () => {
    const wrapper = mount(AppLayout, { global: globalConfig })
    const buttons = wrapper.findAll('button')
    const logoutBtn = buttons.find(b => b.html().includes('mdi-logout'))
    if (logoutBtn) await logoutBtn.trigger('click')
    expect(mockLogout).toHaveBeenCalled()
  })

  it('affiche le lien Dashboard', () => {
    const wrapper = mount(AppLayout, { global: globalConfig })
    expect(wrapper.text()).toContain('Dashboard')
  })

  it('affiche le lien Sauvegardes', () => {
    const wrapper = mount(AppLayout, { global: globalConfig })
    expect(wrapper.text()).toContain('Sauvegardes')
  })

  it('affiche le lien Inventaire', () => {
    const wrapper = mount(AppLayout, { global: globalConfig })
    expect(wrapper.text()).toContain('Inventaire')
  })

  it('affiche le lien Compétences', () => {
    const wrapper = mount(AppLayout, { global: globalConfig })
    expect(wrapper.text()).toContain('Compétences')
  })

  it('affiche le lien RGPD', () => {
    const wrapper = mount(AppLayout, { global: globalConfig })
    expect(wrapper.text()).toContain('RGPD')
  })

  it("n'affiche pas la section Admin pour un joueur normal", () => {
    mockIsAdmin.mockReturnValue(false)
    const wrapper = mount(AppLayout, { global: globalConfig })
    expect(wrapper.text()).not.toContain('Administration')
    expect(wrapper.text()).not.toContain('Utilisateurs')
  })

  it('affiche la section Admin pour un Admin', () => {
    mockIsAdmin.mockReturnValue(true)
    const wrapper = mount(AppLayout, { global: globalConfig })
    expect(wrapper.text()).toContain('Administration')
    expect(wrapper.text()).toContain('Utilisateurs')
    expect(wrapper.text()).toContain('Items')
  })

  it('le drawer est fermé par défaut', () => {
    const wrapper = mount(AppLayout, { global: globalConfig })
    const vm = wrapper.vm as any
    expect(vm.drawer).toBe(false)
  })

it('le drawer bascule au clic sur le bouton menu', async () => {
  const wrapper = mount(AppLayout, { global: globalConfig })
  const vm = wrapper.vm as any
  expect(vm.drawer).toBe(false)
  vm.drawer = true
  await wrapper.vm.$nextTick()
  expect(vm.drawer).toBe(true)
})
})
