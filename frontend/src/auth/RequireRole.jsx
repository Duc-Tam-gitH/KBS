import { useAuth } from './AuthContext'

export function RequireRole({ roles = ['Customer', 'Admin'], children, fallback = null }) {
  const { user } = useAuth()
  if (!user || !roles.includes(user.roleName)) return fallback
  return children
}
