import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { FluentProvider, teamsLightTheme } from '@fluentui/react-components'
import { AuthProvider } from './contexts/AuthContext'
import { ProtectedRoute } from './components'
import { SignIn, Dashboard } from './pages'
import './App.css'

function App() {
  return (
    <FluentProvider theme={teamsLightTheme}>
      <AuthProvider>
        <Router>
          <Routes>
            <Route path="/signin" element={<SignIn />} />
            <Route
              path="/dashboard"
              element={
                <ProtectedRoute>
                  <Dashboard />
                </ProtectedRoute>
              }
            />
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </Router>
      </AuthProvider>
    </FluentProvider>
  )
}

export default App
