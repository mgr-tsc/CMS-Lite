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
} from '@fluentui/react-components'
import {
  DocumentRegular,
  FolderRegular,
} from '@fluentui/react-icons'

const useStyles = makeStyles({
  container: {
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusLarge,
    padding: tokens.spacingVerticalL,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    height: '100%',
    overflow: 'auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalL,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  table: {
    width: '100%',
  },
  tableRow: {
    cursor: 'pointer',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground2,
    },
  },
  selectedRow: {
    backgroundColor: tokens.colorBrandBackgroundSelected,
    '&:hover': {
      backgroundColor: tokens.colorBrandBackgroundPressed,
    },
  },
  fileIcon: {
    marginRight: tokens.spacingHorizontalS,
  },
  fileName: {
    display: 'flex',
    alignItems: 'center',
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

interface FileListViewProps {
  directory: NavItem
  selectedFiles: string[]
  onFileSelect: (fileIds: string[]) => void
}

export const FileListView = ({ directory, selectedFiles, onFileSelect }: FileListViewProps) => {
  const styles = useStyles()
  const files = directory.files || []

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
                <Text weight="semibold">Id</Text>
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
                  className={`${styles.tableRow} ${isSelected ? styles.selectedRow : ''}`}
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
                      <Text>{file.name}</Text>
                    </div>
                  </TableCell>
                  <TableCell>
                    <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
                      {file.id}
                    </Text>
                  </TableCell>
                  <TableCell>
                    <Text>{file.version}</Text>
                  </TableCell>
                  <TableCell>
                    <Text>{file.size}</Text>
                  </TableCell>
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
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