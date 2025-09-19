import { makeStyles, tokens, Text } from '@fluentui/react-components'
import { FileListView } from './FileListView'
import { BREAKPOINTS, MAIN_CONTENT } from './layoutConstants'

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

interface ContentAreaProps {
  selectedItem: NavItem | null
  selectedFiles: string[]
  onFileSelect: (fileIds: string[]) => void
}

export const ContentArea = ({ selectedItem, selectedFiles, onFileSelect }: ContentAreaProps) => {
  const styles = useStyles()

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
