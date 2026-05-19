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
    const token = ref<string | null>(localStorage.getItem('token'))
    const userId = ref<number | null>(Number(localStorage.getItem('userId')) || null)

    // Rôle et username toujours extraits du JWT — jamais du localStorage
    const role = ref<string | null>(getRoleFromToken(token.value))
    const username = ref<string | null>(getUsernameFromToken(token.value))

    const isAuthenticated = computed(() => !!token.value)
    const isAdmin = computed(() => role.value === 'Admin')

    async function login(data: LoginRequest) {
        const res = await authApi.login(data)
        if (res.data.requiresMfa) {
            localStorage.setItem('pendingUserId', String(res.data.userId))
            return { requiresMfa: true }
        }
        _setSession(res.data.token, res.data.userId)
        return { requiresMfa: false }
    }

    async function register(data: RegisterRequest) {
        await authApi.register(data)
        await router.push('/login')
    }

    async function verifyMfa(code: string) {
        const pendingId = Number(localStorage.getItem('pendingUserId'))
        const res = await authApi.mfa({ userId: pendingId, code })
        localStorage.removeItem('pendingUserId')
        _setSession(res.data.token, res.data.userId)
    }

    function logout() {
        token.value = null
        userId.value = null
        username.value = null
        role.value = null
        localStorage.clear()
        router.push('/login')
    }

    function _setSession(jwt: string, id: number) {
        token.value = jwt
        userId.value = id

        role.value = getRoleFromToken(jwt)
        username.value = getUsernameFromToken(jwt)

        localStorage.setItem('token', jwt)
        localStorage.setItem('userId', String(id))
    }

    return { token, userId, username, role, isAuthenticated, isAdmin, login, register, verifyMfa, logout }
})