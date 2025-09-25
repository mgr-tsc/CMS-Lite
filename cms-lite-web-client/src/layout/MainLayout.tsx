import type { ReactNode } from 'react'
import { useState } from 'react'
import { makeStyles, tokens, Text } from '@fluentui/react-components'
import { Header } from './Header'
import { Footer } from './Footer'
import { ActionBar } from './ActionBar'
import { NavMenu } from './NavMenu'
import { FileListView } from './FileListView'
import {
  MAIN_CONTENT,
  ANIMATIONS,
  getMainContentMarginLeft
} from './layoutConstants'

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    minHeight: '100vh',
    backgroundColor: tokens.colorNeutralBackground1,
    overflowX: 'hidden', // Prevent horizontal scrolling
    width: '100vw',
  },
  content: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
  },
  workspaceContainer: {
    flex: 1,
    display: 'flex',
    minHeight: 0,
    overflowX: 'hidden', // Prevent horizontal overflow
  },
  mainContent: {
    flex: 1,
    padding: `${MAIN_CONTENT.PADDING}px`,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    margin: '0 auto',
    width: '100%',
    overflow: 'auto',
    transition: ANIMATIONS.CONTENT_TRANSITION,
    boxSizing: 'border-box',
  },
  contentArea: {
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusLarge,
    padding: tokens.spacingVerticalL,
    minHeight: '400px',
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  emptyState: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    height: '200px',
    color: tokens.colorNeutralForeground3,
  },
});

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

interface MainLayoutProps {
  children?: ReactNode
}

export const MainLayout = ({ children }: MainLayoutProps) => {
  const styles = useStyles();
  const [selectedItem, setSelectedItem] = useState<NavItem | null>(null);
  const [isNavMenuCollapsed, setIsNavMenuCollapsed] = useState<boolean>(false);
  const [selectedFiles, setSelectedFiles] = useState<string[]>([]);

  // const handleItemSelect = (item: NavItem) => {
  //   setSelectedItem(item)
  //   setSelectedFiles([]) // Clear file selection when switching directories
  // }

  const handleFileSelect = (fileIds: string[]) => {
    setSelectedFiles(fileIds)
  }

  const handleNewContent = () => {
    console.log('Creating new content...')
  }

  const handleEditContent = () => {
    if (selectedItem) {
      console.log('Editing content:', selectedItem.name)
    }
  }

  const handleDeleteContent = () => {
    if (selectedItem) {
      console.log('Deleting content:', selectedItem.name)
    }
  }

  const handleSeeDetails = () => {
    if (selectedItem) {
      console.log('Showing details for:', selectedItem.name)
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
    <div className={styles.container}>
      <Header onToggleNavMenu={handleToggleNavMenu} />

      <div className={styles.content}>
        <div
          style={{
            marginLeft: `${getMainContentMarginLeft(isNavMenuCollapsed)}px`,
            transition: ANIMATIONS.CONTENT_TRANSITION,
            width: `calc(100vw - ${getMainContentMarginLeft(isNavMenuCollapsed)}px)`,
            boxSizing: 'border-box',
          }}
        >
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

        <NavMenu
          //onItemSelect={handleItemSelect}
          selectedItemId={selectedItem?.id}
          isCollapsed={isNavMenuCollapsed}
        />

        <div
          className={styles.workspaceContainer}
          style={{
            marginLeft: `${getMainContentMarginLeft(isNavMenuCollapsed)}px`,
            transition: ANIMATIONS.CONTENT_TRANSITION,
            width: `calc(100vw - ${getMainContentMarginLeft(isNavMenuCollapsed)}px)`,
            boxSizing: 'border-box',
          }}
        >
          <main className={styles.mainContent}>
            {selectedItem ? (
              <FileListView
                directory={selectedItem}
                selectedFiles={selectedFiles}
                onFileSelect={handleFileSelect}
              />
            ) : (
              <div className={styles.contentArea}>
                <div className={styles.emptyState}>
                  <Text size={500}>
                    Select a directory from the Content Explorer to view files
                  </Text>
                </div>
              </div>
            )}

            {children}
          </main>
        </div>
      </div>

      <Footer />
    </div>
  )
}
