/**
 * Authentication Module
 * 
 * Centralized exports for all authentication-related functionality.
 * This module provides a clean API for authentication throughout the application.
 * 
 * @module auth
 */

// Core authentication context
export { AuthContext } from './AuthContext'

// Authentication provider component
export { AuthProvider } from './AuthProvider'

// Authentication hook
export { useAuth } from './useAuth'

// Re-export types for convenience
export type { 
  User, 
  AuthContextType, 
  LoginResponseSuccess,
  TenantInfo
} from '../../types/auth'