import { Text, makeStyles, tokens } from '@fluentui/react-components'

const useStyles = makeStyles({
  footer: {
    backgroundColor: tokens.colorNeutralBackground2,
    color: tokens.colorNeutralForeground2,
    padding: tokens.spacingVerticalL,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
    textAlign: 'center',
    marginTop: 'auto',
  },
})

export const Footer = () => {
  const styles = useStyles()

  return (
    <footer className={styles.footer}>
      <Text size={200}>
        © 2024 CMS Lite - Lightweight Content Management System
      </Text>
    </footer>
  )
}