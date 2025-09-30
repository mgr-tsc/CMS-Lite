import { Text, makeStyles, tokens, Link } from '@fluentui/react-components'

const useStyles = makeStyles({
  footer: {
    backgroundColor: tokens.colorNeutralBackground2,
    color: tokens.colorNeutralForeground2,
    padding: tokens.spacingVerticalL,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
    textAlign: 'center',
    marginTop: 'auto',
  },
  links: {
    marginTop: tokens.spacingVerticalS,
    display: 'block',
    '& a': {
      margin: `0 ${tokens.spacingHorizontalS}`,
    },
  },
})

export const Footer = () => {
  const styles = useStyles()

  return (
    <footer className={styles.footer}>
      <Text size={200}>
          © 2025 FileKeeper - Lightweight Content Management System. <span style={{fontWeight: 'bold'}}>An application of Timekeeper</span>. All rights reserved.
      </Text>
      <div className={styles.links}>
        <Link href="#">Privacy Policy</Link>
        <Text> | </Text>
        <Link href="#">Terms of Service</Link>
        <Text> | </Text>
        <Link href="#">Contact Us</Link>
      </div>
    </footer>
  )
}