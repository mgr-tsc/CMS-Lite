import { Text, Caption1, Button, makeStyles, tokens } from '@fluentui/react-components'
import { SignOutRegular, NavigationRegular } from '@fluentui/react-icons'
import { useAuth } from '../hooks/useAuth'
import { BREAKPOINTS } from './layoutConstants'

const useStyles = makeStyles({
  header: {
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
    padding: tokens.spacingVerticalL,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: tokens.spacingHorizontalL,
    flexWrap: 'wrap',
    [`@media (max-width: ${BREAKPOINTS.TABLET}px)`]: {
      alignItems: 'flex-start',
    },
  },
  leftSection: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  titleSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  userSection: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    [`@media (max-width: ${BREAKPOINTS.TABLET}px)`]: {
      alignItems: 'flex-start',
    },
    [`@media (max-width: ${BREAKPOINTS.MOBILE}px)`]: {
      flexDirection: 'column',
      gap: tokens.spacingVerticalS,
      width: '100%',
    },
  },
  signOutButton: {
    color: tokens.colorNeutralForegroundOnBrand,
    ':hover': {
      backgroundColor: tokens.colorBrandBackgroundHover,
      color: tokens.colorNeutralForegroundOnBrand,
    },
    ':active': {
      backgroundColor: tokens.colorBrandBackgroundPressed,
    },
  },
  navToggleButton: {
    color: tokens.colorNeutralForegroundOnBrand,
    ':hover': {
      backgroundColor: tokens.colorBrandBackgroundHover,
      color: tokens.colorNeutralForegroundOnBrand,
    },
    ':active': {
      backgroundColor: tokens.colorBrandBackgroundPressed,
    },
  },
  userDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    color: tokens.colorNeutralForegroundOnBrand,
  },
})

interface HeaderProps {
  onToggleNavMenu?: () => void
}

export const Header = ({ onToggleNavMenu }: HeaderProps) => {
  const styles = useStyles()
  const { user, logout } = useAuth()

  return (
    <header className={styles.header}>
      <div className={styles.leftSection}>
        <Button
          icon={<NavigationRegular />}
          appearance="subtle"
          onClick={onToggleNavMenu}
          className={styles.navToggleButton}
        />
        <div className={styles.titleSection}>
          <Text as="h1" size={800} weight="bold">
            CMS Lite - Content Management System
          </Text>
          <Caption1>Manage your dynamic content with ease</Caption1>
        </div>
      </div>

      {user && (
        <div className={styles.userSection}>
          <div className={styles.userDetails}>
            <Text size={300}>
              Welcome, admin
            </Text>
            <Caption1>
              {user.email}
            </Caption1>
          </div>
          <Button
            icon={<SignOutRegular />}
            appearance="subtle"
            onClick={logout}
            className={styles.signOutButton}
          >
            Sign Out
          </Button>
        </div>
      )}
    </header>
  )
}
