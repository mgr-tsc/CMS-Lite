import type { CSSProperties, ReactNode } from 'react'
import { useEffect, useState } from 'react'
import { makeStyles } from '@fluentui/react-components'
import { Header } from './Header'
import { Footer } from './Footer'
import { ActionBar } from './ActionBar'
import { NavMenu } from './NavMenu'
import { ContentArea } from './ContentArea'
import {
  ANIMATIONS,
  BREAKPOINTS,
  getNavMenuWidth,
} from './layoutConstants'

const useStyles = makeStyles({
  appContainer: {
    display: 'flex',
    flexDirection: 'column',
    minHeight: '100vh',
    backgroundColor: 'inherit',
  },
  mainContainer: {
    display: 'grid',
    flex: 1,
    minHeight: 0,
    position: 'relative',
    width: '100%',
    gridTemplateColumns: `${getNavMenuWidth(false)}px 1fr`,
    transition: ANIMATIONS.CONTENT_TRANSITION,
    [`@media (max-width: ${BREAKPOINTS.TABLET}px)`]: {
      gridTemplateColumns: '1fr',
    },
  },
  contentWrapper: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    transition: ANIMATIONS.CONTENT_TRANSITION,
    overflow: 'hidden',
    minWidth: 0,
    gridColumn: 2,
    [`@media (max-width: ${BREAKPOINTS.TABLET}px)`]: {
      gridColumn: '1',
    },
  },
  actionBarWrapper: {
    width: '100%',
    zIndex: 1,
  },
  mainContent: {
    flex: 1,
    overflow: 'auto',
    minHeight: 0,
  },
  overlayBackdrop: {
    position: 'fixed',
    inset: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.35)',
    zIndex: 9,
    opacity: 1,
    transition: 'opacity 0.3s ease',
    [`@media (min-width: ${BREAKPOINTS.TABLET + 1}px)`]: {
      display: 'none',
    },
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
  const [viewportWidth, setViewportWidth] = useState(() =>
    typeof window === 'undefined' ? BREAKPOINTS.DESKTOP : window.innerWidth,
  )
  const [isNavMenuCollapsed, setIsNavMenuCollapsed] = useState<boolean>(() =>
    typeof window === 'undefined' ? false : window.innerWidth < BREAKPOINTS.TABLET,
  )
  const [selectedFiles, setSelectedFiles] = useState<string[]>([])

  useEffect(() => {
    if (typeof window === 'undefined') {
      return undefined
    }

    const handleResize = () => {
      setViewportWidth(window.innerWidth)
    }

    handleResize()
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])

  const isOverlayNav = viewportWidth < BREAKPOINTS.TABLET
  const navWidth = isOverlayNav ? 0 : getNavMenuWidth(isNavMenuCollapsed)
  const mainContainerStyle: CSSProperties = {
    gridTemplateColumns: isOverlayNav ? '1fr' : `${navWidth}px 1fr`,
  }

  const handleItemSelect = (item: NavItem) => {
    setSelectedItem(item)
    setSelectedFiles([]) // Clear file selection when switching directories
    if (isOverlayNav) {
      setIsNavMenuCollapsed(true)
    }
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
    setIsNavMenuCollapsed(prev => !prev)
  }

  return (
    <div className={styles.appContainer}>
      {/* Header spans full width */}
      <Header onToggleNavMenu={handleToggleNavMenu} />

      {isOverlayNav && !isNavMenuCollapsed && (
        <div
          className={styles.overlayBackdrop}
          onClick={() => setIsNavMenuCollapsed(true)}
        />
      )}

      <div className={styles.mainContainer} style={mainContainerStyle}>
        {/* Fixed sidebar */}
        <NavMenu
          onItemSelect={handleItemSelect}
          selectedItemId={selectedItem?.id}
          isCollapsed={isNavMenuCollapsed}
          isOverlay={isOverlayNav}
          onDismissOverlay={() => setIsNavMenuCollapsed(true)}
        />

        {/* Main content area */}
        <div className={styles.contentWrapper}>
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
