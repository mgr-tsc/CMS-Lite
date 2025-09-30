import { useContext } from "react";
import { AuthContext } from "./AuthContext";

/**
 * useAuth Hook
 * 
 * Custom hook to access authentication context throughout the application.
 * Provides type-safe access to authentication state and methods.
 * 
 * @throws {Error} If used outside of AuthProvider
 * @returns {AuthContextType} Authentication context value
 * 
 * @example
 * ```tsx
 * const { user, isAuthenticated, login, logout, isLoading } = useAuth()
 * 
 * // Check if user is logged in
 * if (isAuthenticated) {
 *   console.log('User:', user.email)
 * }
 * 
 * // Login user
 * const handleLogin = async () => {
 *   const success = await login(email, password)
 *   if (success) {
 *     // Redirect or show success
 *   }
 * }
 * ```
 */
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};