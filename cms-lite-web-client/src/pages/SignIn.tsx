import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import {
  Card,
  CardPreview,
  Text,
  Input,
  Button,
  Field,
  makeStyles,
  tokens,
  Body1,
  Caption1,
  MessageBar,
} from '@fluentui/react-components'
import { PersonRegular, LockClosedRegular } from '@fluentui/react-icons'
import { useAuth } from '../contexts/AuthContext'
import { BREAKPOINTS } from '../layout/layoutConstants'

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '100vh',
    backgroundColor: tokens.colorNeutralBackground1,
    padding: tokens.spacingHorizontalM,
  },
  card: {
    width: '100%',
    maxWidth: '450px',
    minWidth: '280px',
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalXL,
    [`@media (max-width: ${BREAKPOINTS.MOBILE}px)`]: {
      padding: tokens.spacingVerticalL,
      gap: tokens.spacingVerticalM,
    },
  },
  header: {
    textAlign: 'center',
    marginBottom: tokens.spacingVerticalL,
    width: '100%',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
  },
  credentialsHint: {
    backgroundColor: tokens.colorNeutralBackground2,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusLarge,
    marginTop: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    textAlign: 'center',
  },
  input: {
    width: '100%',
  },
  btn: {
    alignSelf: 'center',
    marginTop: tokens.spacingVerticalM,
    minWidth: '160px',
    [`@media (max-width: ${BREAKPOINTS.MOBILE}px)`]: {
      width: '100%',
    },
  },
})

export const SignIn = () => {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const { login, isAuthenticated, isLoading } = useAuth()
  const styles = useStyles()

  // Redirect if already authenticated
  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')

    if (!email || !password) {
      setError('Please fill in all fields')
      return
    }

    const success = await login(email, password)
    if (!success) {
      setError('Invalid email or password')
    }
  }

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <CardPreview>
          <form onSubmit={handleSubmit} className={styles.form}>
            <Text as="h2" size={500} weight="semibold" className={styles.header}>
              Sign In
            </Text>

            {error && (
              <MessageBar intent="error">
                {error}
              </MessageBar>
            )}

            <Field label="Email">
              <Input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="Enter your email"
                contentBefore={<PersonRegular />}
                className={styles.input}
                required
              />
            </Field>

            <Field label="Password">
              <Input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter your password"
                className={styles.input}
                contentBefore={<LockClosedRegular />}
                required
              />
            </Field>

            <Button
              type="submit"
              appearance="primary"
              disabled={isLoading}
              className={styles.btn}
              size="medium"
            >
              {isLoading ? 'Signing In...' : 'Sign In'}
            </Button>

            <div className={styles.credentialsHint}>
              <Body1><strong>Demo Credentials:</strong></Body1>
              <Caption1>Email: admin@email.com</Caption1>
              <Caption1>Password: abc</Caption1>
            </div>
          </form>
        </CardPreview>
      </Card>
    </div>
  )
}
