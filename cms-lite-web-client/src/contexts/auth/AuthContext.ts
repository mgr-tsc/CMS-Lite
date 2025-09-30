import { createContext } from 'react'
import type { AuthContextType } from '../../types/auth'

/**
 * Authentication Context
 * 
 * This context provides authentication state and methods throughout the application.
 * It should be used with the AuthProvider component and accessed via the useAuth hook.
 */
export const AuthContext = createContext<AuthContextType | undefined>(undefined)