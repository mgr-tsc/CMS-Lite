import { Text, Caption1, Button, makeStyles, tokens } from '@fluentui/react-components'
import { SignOutRegular, NavigationRegular } from '@fluentui/react-icons'
import { useAuth } from '../contexts/AuthContext'

const useStyles = makeStyles({
  header: {
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
    padding: tokens.spacingVerticalL,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  leftSection: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  titleSection: {
    display: 'flex',
    flexDirection: 'column',
  },
  userSection: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
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
          style={{ color: tokens.colorNeutralForegroundOnBrand }}
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
          <div>
            <Text size={300} style={{ color: tokens.colorNeutralForegroundOnBrand }}>
              Welcome, {user.name}
            </Text>
            <br />
            <Caption1 style={{ color: tokens.colorNeutralForegroundOnBrand }}>
              {user.email}
            </Caption1>
          </div>
          <Button
            icon={<SignOutRegular />}
            appearance="subtle"
            onClick={logout}
            style={{ color: tokens.colorNeutralForegroundOnBrand }}
          >
            Sign Out
          </Button>
        </div>
      )}
    </header>
  )
}