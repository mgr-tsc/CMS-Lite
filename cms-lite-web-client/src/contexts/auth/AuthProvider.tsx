import { useState, useEffect } from 'react'
import type { ReactNode } from 'react'
import axios from 'axios'
import { useDispatch, useSelector } from 'react-redux'
import type { AuthContextType, LoginResponseSuccess } from '../../types/auth'
import customAxios from '../../utilities/custom-axios'
import { AuthContext } from './AuthContext'
import { logInUser, logOutUser } from '../../store/slices/user'
import type { AppDispatch, RootState } from '../../store/store'
import { clearDirectoryTree } from '../../store/slices/directoryTree'

interface AuthProviderProps {
    children: ReactNode
}

export const AuthProvider = ({ children }: AuthProviderProps) => {
    const [isLoading, setIsLoading] = useState(true)
    const dispatch = useDispatch<AppDispatch>()
    const userState = useSelector((state: RootState) => state.user)
    useEffect(() => {
        //TODO: Clean all this mechanism to restore user state from localStorage and token validation
        // The logic should be handled by redux-persist or similar library
        // This is a temporary solution until then
        const token = localStorage.getItem('jwtToken')
        const savedUser = localStorage.getItem('cms-lite-user')

        const isTokenExpired = (jwt: string): boolean => {
            const parts = jwt.split('.')
            if (parts.length < 2) {
                return true
            }

            try {
                let payload = parts[1].replace(/-/g, '+').replace(/_/g, '/')
                while (payload.length % 4 !== 0) {
                    payload += '='
                }
                const decoded = JSON.parse(atob(payload)) as { exp?: number }
                if (typeof decoded.exp !== 'number') {
                    return true
                }
                return decoded.exp * 1000 <= Date.now()
            } catch (error) {
                console.error('Error decoding token:', error)
                return true
            }
        }

        if (!token || isTokenExpired(token)) {
            // Clean up expired or invalid tokens
            localStorage.removeItem('cms-lite-user')
            localStorage.removeItem('jwtToken')
            dispatch(logOutUser())
            dispatch(clearDirectoryTree())
            setIsLoading(false)
            return
        }

        // Token is valid, restore user state
        if (savedUser) {
            try {
                const userData = JSON.parse(savedUser)
                dispatch(logInUser(userData))
            } catch (error) {
                console.error('Error parsing saved user:', error)
                // Clean up corrupted data
                localStorage.removeItem('cms-lite-user')
                localStorage.removeItem('jwtToken')
                dispatch(logOutUser())
                dispatch(clearDirectoryTree())
            }
        }

        setIsLoading(false)
    }, [dispatch])

    const login = async (email: string, password: string): Promise<boolean> => {
        setIsLoading(true)
        try {
            const { data } = await customAxios.post<LoginResponseSuccess>('/auth/login', { email, password })
            if (!data?.token || !data?.user) {
                console.error('Login API response missing required fields')
                return false
            }
            localStorage.setItem('jwtToken', data.token);
            const { id, email: userEmail, firstName, lastName, tenant} = data.user
            dispatch(logInUser({
                id,
                email: userEmail,
                firstName,
                lastName,
                tenant,
            }));
            localStorage.setItem('cms-lite-user', JSON.stringify({
                id,
                email: userEmail,
                firstName,
                lastName,
                tenant,
            }));
            return true;
        } catch (error) {
            if (axios.isAxiosError(error)) {
                const message = error.response?.data?.message || error.message || 'Unknown login error'
                console.error('Login API error:', message)
            } else {
                console.error('Login error:', error)
            }
            return false
        } finally {
            setIsLoading(false)
        }
    }

    const logout = () => {
        dispatch(logOutUser())
        dispatch(clearDirectoryTree())
        localStorage.removeItem('jwtToken')
        localStorage.removeItem('cms-lite-user')
    }

    const value: AuthContextType = {
        user: userState.isAuthenticated ? userState : null,
        isAuthenticated: userState.isAuthenticated,
        login,
        logout,
        isLoading
    }

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    )
}
