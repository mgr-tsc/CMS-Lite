import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { FluentProvider, teamsLightTheme } from '@fluentui/react-components'
import { AuthProvider } from './contexts'
import { ProtectedRoute } from './components'
import { SignIn, Dashboard } from './pages'
import store from './store/store'
import './App.css'
import { useEffect } from 'react'

function App() {
  useEffect(() => {
    console.log('REACT_READY');
    console.log('Initial Redux state:', store.getState());
  }, [])
  return (
    <FluentProvider theme={teamsLightTheme}>
      <AuthProvider>
        <Router>
          <Routes>
            <Route path="/login" element={<SignIn />} />
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
