import * as authApi from '@/api/auth'
import { useAuthStore } from '@/stores/auth'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('@/api/auth', () => ({
    authApi: {
        login:              vi.fn(),
        register:           vi.fn(),
        mfa:                vi.fn(),
        setupMfa:           vi.fn(),
        verifyMfaWithToken: vi.fn(),
    },
    default: { interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } } }
}))

vi.mock('@/router', () => ({
    default: { push: vi.fn() }
}))

function makeJwt(payload: object): string {
    const encoded = btoa(JSON.stringify(payload))
    return `header.${encoded}.signature`
}

describe('useAuthStore', () => {
    beforeEach(() => {
        vi.resetModules()
        localStorage.clear()
        vi.clearAllMocks()
        setActivePinia(createPinia())
    })

    it('isAuthenticated est false par défaut', () => {
        const store = useAuthStore()
        expect(store.isAuthenticated).toBe(false)
    })

    it('isAuthenticated est true si un token est dans le localStorage', () => {
        localStorage.setItem('token', 'fake-token')
        const store = useAuthStore()
        expect(store.isAuthenticated).toBe(true)
    })

    it('login sans MFA stocke le token et retourne requiresMfa false', async () => {
        const jwt = makeJwt({
            'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Player',
            'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'testuser',
        })
        vi.mocked(authApi.authApi.login).mockResolvedValue({
            data: { token: jwt, requiresMfa: false, requiresMfaSetup: false, userId: 1 }
        } as any)

        const store = useAuthStore()
        const result = await store.login({ username: 'testuser', password: 'pass' })

        expect(result.requiresMfa).toBe(false)
        expect(result.requiresMfaSetup).toBe(false)
        expect(store.isAuthenticated).toBe(true)
        expect(localStorage.getItem('token')).toBe(jwt)
    })

    it('login avec MFA requis retourne requiresMfa true', async () => {
        vi.mocked(authApi.authApi.login).mockResolvedValue({
            data: { token: '', requiresMfa: true, requiresMfaSetup: false, userId: 42 }
        } as any)

        const store = useAuthStore()
        const result = await store.login({ username: 'user', password: 'pass' })

        expect(result.requiresMfa).toBe(true)
        expect(result.requiresMfaSetup).toBe(false)
    })

    it('login avec MFA setup requis retourne requiresMfaSetup true', async () => {
        const mfaToken = 'mfa-setup-token'
        vi.mocked(authApi.authApi.login).mockResolvedValue({
            data: { token: mfaToken, requiresMfa: false, requiresMfaSetup: true, userId: 99 }
        } as any)

        const store = useAuthStore()
        const result = await store.login({ username: 'newuser', password: 'pass' })

        expect(result.requiresMfaSetup).toBe(true)
        expect(result.requiresMfa).toBe(false)
        // Token MFA en mémoire — pas en localStorage
        expect(localStorage.getItem('mfaSetupToken')).toBeNull()
    })

    it('setupMfa appelle authApi.setupMfa et retourne qrCodeUri', async () => {
        const mfaToken = 'mfa-setup-token'
        vi.mocked(authApi.authApi.login).mockResolvedValue({
            data: { token: mfaToken, requiresMfa: false, requiresMfaSetup: true, userId: 1 }
        } as any)
        vi.mocked(authApi.authApi.setupMfa).mockResolvedValue({
            data: { qrCodeUri: 'otpauth://totp/test', secret: 'ABCD', message: 'ok' }
        } as any)

        const store = useAuthStore()
        await store.login({ username: 'user', password: 'pass' })
        const res = await store.setupMfa()

        expect(res.qrCodeUri).toBe('otpauth://totp/test')
        expect(res.secret).toBe('ABCD')
    })

    it('setupMfa lève une erreur si pas de session MFA en cours', async () => {
        const store = useAuthStore()
        await expect(store.setupMfa()).rejects.toThrow('Session expirée')
    })

    it('verifyMfaSetup stocke le token après vérification réussie', async () => {
        const jwt = makeJwt({
            'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Player',
            'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'testuser',
        })
        vi.mocked(authApi.authApi.login).mockResolvedValue({
            data: { token: 'mfa-token', requiresMfa: false, requiresMfaSetup: true, userId: 1 }
        } as any)
        vi.mocked(authApi.authApi.verifyMfaWithToken).mockResolvedValue({
            data: { token: jwt, requiresMfa: false, requiresMfaSetup: false, userId: 1 }
        } as any)

        const store = useAuthStore()
        await store.login({ username: 'user', password: 'pass' })
        await store.verifyMfaSetup('123456')

        expect(store.isAuthenticated).toBe(true)
        expect(localStorage.getItem('token')).toBe(jwt)
    })

    it('logout vide le store et le localStorage', () => {
        localStorage.setItem('token', 'fake')
        localStorage.setItem('userId', '1')
        const store = useAuthStore()
        store.logout()
        expect(store.isAuthenticated).toBe(false)
        expect(localStorage.getItem('token')).toBeNull()
    })

    it('isAdmin est true si le rôle est Admin dans le JWT', () => {
        const jwt = makeJwt({
            'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Admin',
            'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'admin',
        })
        localStorage.setItem('token', jwt)
        const store = useAuthStore()
        expect(store.isAdmin).toBe(true)
    })

    it('isAdmin est false si le rôle est Player dans le JWT', () => {
        const jwt = makeJwt({
            'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Player',
            'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'player',
        })
        localStorage.setItem('token', jwt)
        const store = useAuthStore()
        expect(store.isAdmin).toBe(false)
    })

    it('le rôle n\'est pas lu depuis localStorage directement', () => {
        // Modifier le localStorage ne doit pas changer isAdmin
        localStorage.setItem('token', 'invalid-token')
        localStorage.setItem('role', 'Admin') // Tentative de manipulation
        const store = useAuthStore()
        // Token invalide → role null → isAdmin false
        expect(store.isAdmin).toBe(false)
    })
})