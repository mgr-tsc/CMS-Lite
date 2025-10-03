import {
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  Text,
  makeStyles,
  tokens,
  Checkbox,
  mergeClasses,
} from '@fluentui/react-components';
import { DocumentRegular, FolderRegular } from '@fluentui/react-icons'
import { BREAKPOINTS } from './layoutConstants'
import type { DirectoryNode, ContentItemNode } from '../types/directories.ts'

const useStyles = makeStyles({
  container: {
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusLarge,
    padding: tokens.spacingVerticalL,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    height: '100%',
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    overflow: 'hidden',
    [`@media (max-width: ${BREAKPOINTS.TABLET}px)`]: {
      padding: tokens.spacingVerticalM,
      gap: tokens.spacingVerticalM,
    },
  },
  header: {
    marginBottom: tokens.spacingVerticalL,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    [`@media (max-width: ${BREAKPOINTS.MOBILE}px)`]: {
      flexWrap: 'wrap',
      rowGap: tokens.spacingVerticalS,
    },
  },
  tableWrapper: {
    overflowX: 'auto',
  },
  table: {
    width: '100%',
    minWidth: '600px',
  },
  tableRow: {
    cursor: 'pointer',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground2,
    },
  },
  selectedRow: {
    backgroundColor: tokens.colorNeutralBackground4,
    boxShadow: `inset 3px 0 0 ${tokens.colorBrandStroke1}`,
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground5,
    },
  },
  fileIcon: {
    marginRight: tokens.spacingHorizontalS,
  },
  fileName: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
})

interface FileListViewProps {
  directory: DirectoryNode
  selectedFiles: string[]
  onFileSelect: (fileIds: string[]) => void
}

export const FileListView = ({ directory, selectedFiles, onFileSelect }: FileListViewProps) => {
  const styles = useStyles()
  const files: ContentItemNode[] = directory.contentItems || []

  const handleRowClick = (fileId: string) => {
    if (selectedFiles.includes(fileId)) {
      onFileSelect(selectedFiles.filter(id => id !== fileId))
    } else {
      onFileSelect([...selectedFiles, fileId])
    }
  }

  const handleSelectAll = (checked: boolean) => {
    if (checked) {
      onFileSelect(files.map(file => file.id))
    } else {
      onFileSelect([])
    }
  }

  const isAllSelected = files.length > 0 && files.every(file => selectedFiles.includes(file.id))
  const isSomeSelected = files.some(file => selectedFiles.includes(file.id))

  const renderSize = (file: ContentItemNode): string => {
    return file.size || "N/A";
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <FolderRegular />
        <Text size={600} weight="semibold">
          {directory.name}
        </Text>
        <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
          ({files.length} {files.length === 1 ? 'file' : 'files'})
        </Text>
      </div>

      {files.length > 0 ? (
        <div className={styles.tableWrapper}>
          <Table className={styles.table} aria-label="File list">
            <TableHeader>
              <TableRow>
                <TableHeaderCell>
                  <Checkbox
                    checked={isAllSelected ? true : (isSomeSelected ? 'mixed' : false)}
                    onChange={(_, data) => handleSelectAll(data.checked === true)}
                  />
                </TableHeaderCell>
                <TableHeaderCell>
                  <Text weight="semibold">File Name</Text>
                </TableHeaderCell>
                <TableHeaderCell>
                  <Text weight="semibold">Type</Text>
                </TableHeaderCell>
                <TableHeaderCell>
                  <Text weight="semibold">Latest Version</Text>
                </TableHeaderCell>
                <TableHeaderCell>
                  <Text weight="semibold">Size</Text>
                </TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {files.map((file) => {
                const isSelected = selectedFiles.includes(file.id)
                return (
                  <TableRow
                    key={file.id}
                    className={mergeClasses(styles.tableRow, isSelected ? styles.selectedRow : undefined)}
                    onClick={() => handleRowClick(file.id)}
                  >
                    <TableCell>
                      <Checkbox
                        checked={isSelected}
                        onChange={() => handleRowClick(file.id)}
                      />
                    </TableCell>
                    <TableCell>
                      <div className={styles.fileName}>
                        <DocumentRegular className={styles.fileIcon} />
                        <Text>{file.resource}</Text>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Text>{file.contentType || 'Unknown'}</Text>
                    </TableCell>
                    <TableCell>
                      <Text>{file.latestVersion}</Text>
                    </TableCell>
                    <TableCell>
                      <Text>{renderSize(file)}</Text>
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        </div>
      ) : (
        <div style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          height: '200px',
          color: tokens.colorNeutralForeground3
        }}>
          <Text size={400}>This directory is empty</Text>
        </div>
      )}
    </div>
  )
}
