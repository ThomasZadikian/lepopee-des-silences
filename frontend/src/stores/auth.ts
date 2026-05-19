import { authApi, type LoginRequest, type RegisterRequest } from '@/api/auth'
import router from '@/router'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

function decodeJwt(jwt: string) {
    try {
        return JSON.parse(atob(jwt.split('.')[1]))
    } catch {
        return null
    }
}

function getRoleFromToken(jwt: string | null): string | null {
    if (!jwt) return null
    const payload = decodeJwt(jwt)
    return payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? null
}

function getUsernameFromToken(jwt: string | null): string | null {
    if (!jwt) return null
    const payload = decodeJwt(jwt)
    return payload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ?? null
}

export const useAuthStore = defineStore('auth', () => {
    const token    = ref<string | null>(localStorage.getItem('token'))
    const userId   = ref<number | null>(Number(localStorage.getItem('userId')) || null)
    const role     = ref<string | null>(getRoleFromToken(token.value))
    const username = ref<string | null>(getUsernameFromToken(token.value))

    // Token MFA temporaire — en mémoire uniquement, jamais en localStorage
    const pendingMfaToken  = ref<string | null>(null)
    const pendingUserId    = ref<number | null>(null)

    const isAuthenticated = computed(() => !!token.value)
    const isAdmin         = computed(() => role.value === 'Admin')

    async function login(data: LoginRequest) {
        const res = await authApi.login(data)

        if (res.data.requiresMfaSetup) {
            pendingUserId.value   = res.data.userId
            pendingMfaToken.value = res.data.token ?? null
            return { requiresMfa: false, requiresMfaSetup: true }
        }

        if (res.data.requiresMfa) {
            pendingUserId.value = res.data.userId
            return { requiresMfa: true, requiresMfaSetup: false }
        }

        _setSession(res.data.token, res.data.userId)
        return { requiresMfa: false, requiresMfaSetup: false }
    }

    async function register(data: RegisterRequest) {
        const res = await authApi.register(data)
        pendingUserId.value   = res.data.userId
        pendingMfaToken.value = res.data.token ?? null
        await router.push({ name: 'MfaSetup' })
    }

    async function setupMfa() {
        if (!pendingUserId.value || !pendingMfaToken.value)
            throw new Error('Session expirée')

        const res = await authApi.setupMfa({
            userId:   pendingUserId.value,
            mfaToken: pendingMfaToken.value
        })
        return res.data
    }

    async function verifyMfaSetup(code: string) {
        if (!pendingUserId.value || !pendingMfaToken.value)
            throw new Error('Session expirée')

        const res = await authApi.verifyMfaWithToken({
            userId:   pendingUserId.value,
            mfaToken: pendingMfaToken.value,
            code
        })
        pendingUserId.value   = null
        pendingMfaToken.value = null
        _setSession(res.data.token, res.data.userId)
    }

    async function verifyMfa(code: string) {
        if (!pendingUserId.value) throw new Error('Session expirée')

        const res = await authApi.mfa({
            userId: pendingUserId.value,
            code
        })
        pendingUserId.value   = null
        pendingMfaToken.value = null
        _setSession(res.data.token, res.data.userId)
    }

    function logout() {
        token.value           = null
        userId.value          = null
        username.value        = null
        role.value            = null
        pendingMfaToken.value = null
        pendingUserId.value   = null
        localStorage.clear()
        router.push('/login')
    }

    function _setSession(jwt: string, id: number) {
        token.value    = jwt
        userId.value   = id
        role.value     = getRoleFromToken(jwt)
        username.value = getUsernameFromToken(jwt)
        localStorage.setItem('token', jwt)
        localStorage.setItem('userId', String(id))
    }

    return {
        token, userId, username, role,
        isAuthenticated, isAdmin,
        login, register, setupMfa, verifyMfa, verifyMfaSetup, logout,
        _setSession
    }
})