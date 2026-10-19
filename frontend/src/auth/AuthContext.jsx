import { createContext, useContext, useMemo, useState } from 'react'
import { api, authStore } from '../services/api'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(authStore.user)
  const value = useMemo(() => ({
    user,
    isAuthenticated: Boolean(user),
    isAdmin: user?.roleName === 'Admin',
    login: async (credentials) => { const session = await api.login(credentials); setUser(session.user); return session.user },
    logout: () => { authStore.clear(); setUser(null) },
  }), [user])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth phải được dùng bên trong AuthProvider.')
  return context
}
