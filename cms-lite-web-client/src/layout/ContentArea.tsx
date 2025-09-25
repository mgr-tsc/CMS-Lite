import { makeStyles, tokens, Text } from '@fluentui/react-components'
import { LoadingSpinner } from '../components'
import { FileListView } from './FileListView'
import { BREAKPOINTS, MAIN_CONTENT } from './layoutConstants'
import type { DirectoryNode } from '../types/directories.ts'

const useStyles = makeStyles({
  contentContainer: {
    padding: `${MAIN_CONTENT.PADDING}px`,
    height: '100%',
    overflow: 'auto',
    backgroundColor: tokens.colorNeutralBackground1,
    [`@media (max-width: ${BREAKPOINTS.TABLET}px)`]: {
      padding: tokens.spacingVerticalL,
    },
    [`@media (max-width: ${BREAKPOINTS.MOBILE}px)`]: {
      padding: tokens.spacingVerticalM,
    },
  },
  emptyState: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    height: '200px',
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusLarge,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    color: tokens.colorNeutralForeground3,
  },
})

interface ContentAreaProps {
  selectedItem: DirectoryNode | null
  selectedFiles: string[]
  onFileSelect: (fileIds: string[]) => void
  isLoading: boolean
  error: string | null
}

export const ContentArea = ({ selectedItem, selectedFiles, onFileSelect, isLoading, error }: ContentAreaProps) => {
  const styles = useStyles()

  if (isLoading) {
    return (
      <div className={styles.contentContainer}>
        <LoadingSpinner message="Loading directories..." />
      </div>
    )
  }

  if (error) {
    return (
      <div className={styles.contentContainer}>
        <div className={styles.emptyState}>
          <Text size={400} style={{ color: tokens.colorPaletteRedForeground1 }}>
            {error}
          </Text>
        </div>
      </div>
    )
  }

  return (
    <div className={styles.contentContainer}>
      {selectedItem ? (
        <FileListView
          directory={selectedItem}
          selectedFiles={selectedFiles}
          onFileSelect={onFileSelect}
        />
      ) : (
        <div className={styles.emptyState}>
          <Text size={500}>
            Select a directory from the Content Explorer to view files
          </Text>
        </div>
      )}
    </div>
  )
}
