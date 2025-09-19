import { useState } from 'react'
import {
  Text,
  Card,
  CardHeader,
  CardPreview,
  makeStyles,
  tokens,
  Body1,
  Caption1,
} from '@fluentui/react-components'
import { DocumentRegular } from '@fluentui/react-icons'
import { MainLayout, NavMenu } from '../layout'

const useStyles = makeStyles({
  cardGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingVerticalL,
  },
  card: {
    height: '200px',
  },
})

export const Dashboard = () => {
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

  const handleNewContent = () => {
    console.log('New content clicked')
  }

  const handleViewAll = () => {
    console.log('View all clicked')
  }

  const handleEditSelected = () => {
    console.log('Edit selected:', selectedItems)
  }

  const handleDeleteSelected = () => {
    console.log('Delete selected:', selectedItems)
  }

  return (
    <MainLayout>
      <NavMenu
        selectedItems={selectedItems}
        onNewContent={handleNewContent}
        onViewAll={handleViewAll}
        onEditSelected={handleEditSelected}
        onDeleteSelected={handleDeleteSelected}
      />

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
    </MainLayout>
  )
}