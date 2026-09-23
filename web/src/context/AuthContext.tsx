
import { createContext, useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { authApi } from '../api/resources'
import { getToken, setToken } from '../api/client'
import type { User } from '../types'

interface AuthContextValue {
  user: User | null

  
  isRestoring: boolean

  login: (email: string, password: string) => Promise<User>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)


export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [isRestoring, setIsRestoring] = useState(true)

  
  useEffect(() => {
    let cancelled = false

    async function restore() {
      if (!getToken()) {
        setIsRestoring(false)
        return
      }

      try {
        const profile = await authApi.me()
        if (!cancelled) setUser(profile)
      } catch {
        
        if (!cancelled) setUser(null)
      } finally {
        if (!cancelled) setIsRestoring(false)
      }
    }

    void restore()

   
    return () => {
      cancelled = true
    }
  }, [])

 
  const login = useCallback(async (email: string, password: string) => {
    const result = await authApi.login(email, password)

    setToken(result.accessToken)
    setUser(result.user)

    return result.user
  }, [])

  
  const logout = useCallback(() => {
    setToken(null)
    setUser(null)
  }, [])

  
  const value = useMemo(
    () => ({ user, isRestoring, login, logout }),
    [user, isRestoring, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
