import { useState } from 'react'
import {
  FluentProvider,
  teamsLightTheme,
  Text,
  Button,
  Card,
  CardHeader,
  CardPreview,
  makeStyles,
  tokens,
  Body1,
  Caption1,
} from '@fluentui/react-components'
import {
  DocumentRegular,
  EditRegular,
  DeleteRegular,
  AddRegular,
} from '@fluentui/react-icons'
import './App.css'

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    minHeight: '100vh',
    backgroundColor: tokens.colorNeutralBackground1,
  },
  header: {
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
    padding: tokens.spacingVerticalL,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  main: {
    flex: 1,
    padding: tokens.spacingVerticalXL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  cardGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingVerticalL,
  },
  card: {
    height: '200px',
  },
  toolbar: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalL,
    flexWrap: 'wrap',
  },
})

function App() {
  const [selectedItems, setSelectedItems] = useState<string[]>([])
  const styles = useStyles()

  // Mock data for CMS content
  const contentItems = [
    {
      id: 'homepage',
      title: 'Homepage Content',
      description: 'Main landing page content',
      content: 'Welcome to our CMS Lite system. This is the homepage content that visitors see first.',
      lastUpdated: '2 hours ago',
    },
    {
      id: 'about',
      title: 'About Page',
      description: 'Company information',
      content: 'About our company and mission statement. We are dedicated to providing excellent service.',
      lastUpdated: '1 day ago',
    },
    {
      id: 'contact',
      title: 'Contact Information',
      description: 'Contact details and locations',
      content: 'Contact details and office locations. Reach us at info@company.com or call (555) 123-4567.',
      lastUpdated: '3 days ago',
    },
  ]

  return (
    <FluentProvider theme={teamsLightTheme}>
      <div className={styles.container}>
        <header className={styles.header}>
          <Text as="h1" size={800} weight="bold">
            CMS Lite - Content Management System
          </Text>
          <Caption1>Manage your dynamic content with ease</Caption1>
        </header>

        <main className={styles.main}>
          <div className={styles.toolbar}>
            <Button icon={<AddRegular />} appearance="primary">
              New Content
            </Button>
            <Button icon={<DocumentRegular />}>View All</Button>
            <Button 
              icon={<EditRegular />} 
              disabled={selectedItems.length === 0}
            >
              Edit Selected
            </Button>
            <Button 
              icon={<DeleteRegular />} 
              disabled={selectedItems.length === 0}
            >
              Delete Selected
            </Button>
          </div>

          <Text as="h2" size={600} weight="semibold">
            Recent Content ({contentItems.length} items)
          </Text>

          <div className={styles.cardGrid}>
            {contentItems.map((item) => (
              <Card 
                key={item.id} 
                className={styles.card}
                onClick={() => {
                  if (selectedItems.includes(item.id)) {
                    setSelectedItems(selectedItems.filter(id => id !== item.id))
                  } else {
                    setSelectedItems([...selectedItems, item.id])
                  }
                }}
                style={{
                  cursor: 'pointer',
                  border: selectedItems.includes(item.id) 
                    ? `2px solid ${tokens.colorBrandBackground}` 
                    : undefined
                }}
              >
                <CardHeader
                  image={<DocumentRegular />}
                  header={<Body1><b>{item.title}</b></Body1>}
                  description={<Caption1>Last updated: {item.lastUpdated}</Caption1>}
                />
                <CardPreview>
                  <Body1>{item.content}</Body1>
                </CardPreview>
              </Card>
            ))}
          </div>

          {selectedItems.length > 0 && (
            <div style={{ 
              padding: tokens.spacingVerticalM, 
              backgroundColor: tokens.colorBrandBackground2,
              borderRadius: tokens.borderRadiusLarge,
              marginTop: tokens.spacingVerticalL
            }}>
              <Text>
                {selectedItems.length} item(s) selected: {selectedItems.join(', ')}
              </Text>
            </div>
          )}
        </main>
      </div>
    </FluentProvider>
  )
}

export default App
