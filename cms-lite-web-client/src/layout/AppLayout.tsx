import type { ReactNode } from 'react'
import { useState } from 'react'
import { makeStyles } from '@fluentui/react-components'
import { Header } from './Header'
import { Footer } from './Footer'
import { ActionBar } from './ActionBar'
import { NavMenu } from './NavMenu'
import { ContentArea } from './ContentArea'
import {
  ANIMATIONS,
  getMainContentMarginLeft
} from './layoutConstants'

const useStyles = makeStyles({
  appContainer: {
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
    overflow: 'hidden',
  },
  mainContainer: {
    display: 'flex',
    flex: 1,
    position: 'relative',
  },
  contentWrapper: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    transition: ANIMATIONS.CONTENT_TRANSITION,
    overflow: 'hidden',
  },
  actionBarWrapper: {
    width: '100%',
  },
  mainContent: {
    flex: 1,
    overflow: 'auto',
  },
})

interface FileItem {
  id: string
  name: string
  type: 'file'
  version: string
  size: string
  lastModified: string
}

interface NavItem {
  id: string
  name: string
  type: 'folder'
  children?: NavItem[]
  files?: FileItem[]
}

interface AppLayoutProps {
  children?: ReactNode
}

export const AppLayout = ({ children }: AppLayoutProps) => {
  const styles = useStyles()
  const [selectedItem, setSelectedItem] = useState<NavItem | null>(null)
  const [isNavMenuCollapsed, setIsNavMenuCollapsed] = useState<boolean>(false)
  const [selectedFiles, setSelectedFiles] = useState<string[]>([])

  const handleItemSelect = (item: NavItem) => {
    setSelectedItem(item)
    setSelectedFiles([]) // Clear file selection when switching directories
  }

  const handleFileSelect = (fileIds: string[]) => {
    setSelectedFiles(fileIds)
  }

  const handleNewContent = () => {
    console.log('Creating new content...')
  }

  const handleEditContent = () => {
    if (selectedFiles.length > 0) {
      console.log('Editing content:', selectedFiles)
    }
  }

  const handleDeleteContent = () => {
    if (selectedFiles.length > 0) {
      console.log('Deleting content:', selectedFiles)
    }
  }

  const handleSeeDetails = () => {
    if (selectedFiles.length > 0) {
      console.log('Showing details for:', selectedFiles)
    }
  }

  const handleRefresh = () => {
    console.log('Refreshing content...')
  }

  const handleViewAll = () => {
    console.log('Viewing all content...')
    setSelectedItem(null)
  }

  const handleToggleNavMenu = () => {
    setIsNavMenuCollapsed(!isNavMenuCollapsed)
  }

  return (
    <div className={styles.appContainer}>
      {/* Header spans full width */}
      <Header onToggleNavMenu={handleToggleNavMenu} />

      <div className={styles.mainContainer}>
        {/* Fixed sidebar */}
        <NavMenu
          onItemSelect={handleItemSelect}
          selectedItemId={selectedItem?.id}
          isCollapsed={isNavMenuCollapsed}
        />

        {/* Main content area */}
        <div
          className={styles.contentWrapper}
          style={{
            marginLeft: `${getMainContentMarginLeft(isNavMenuCollapsed)}px`,
          }}
        >
          {/* ActionBar */}
          <div className={styles.actionBarWrapper}>
            <ActionBar
              hasSelection={selectedFiles.length > 0}
              onNewContent={handleNewContent}
              onEditContent={handleEditContent}
              onDeleteContent={handleDeleteContent}
              onSeeDetails={handleSeeDetails}
              onRefresh={handleRefresh}
              onViewAll={handleViewAll}
            />
          </div>

          {/* Main content */}
          <div className={styles.mainContent}>
            <ContentArea
              selectedItem={selectedItem}
              selectedFiles={selectedFiles}
              onFileSelect={handleFileSelect}
            />
            {children}
          </div>
        </div>
      </div>

      {/* Footer spans full width */}
      <Footer />
    </div>
  )
}

// Context will be added later if needed for sharing state between components