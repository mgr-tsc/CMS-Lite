import { Dialog, DialogSurface, DialogBody, DialogTitle, DialogContent, DialogActions, Button, Text, makeStyles, tokens } from '@fluentui/react-components'
import type { ContentItemNode } from '../store/slices/directoryTree'

interface FileDetailsModalProps {
  open: boolean
  item: ContentItemNode | null
  onClose: () => void
}

const useStyles = makeStyles({
  content: {
    display: 'grid',
    gap: tokens.spacingVerticalM,
    minWidth: 'min(420px, 90vw)',
  },
  row: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  label: {
    color: tokens.colorNeutralForeground3,
    textTransform: 'uppercase',
    fontSize: tokens.fontSizeBase200,
    letterSpacing: '0.04rem',
  },
})

const formatSize = (bytes?: number): string => {
  if (bytes === undefined || Number.isNaN(bytes)) {
    return 'Unknown'
  }

  if (bytes === 0) {
    return '0 B'
  }

  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  const value = bytes / Math.pow(k, i)
  return `${value.toFixed(value < 10 && i > 0 ? 1 : 0)} ${sizes[i]}`
}

const formatDate = (isoDate?: string): string => {
  if (!isoDate) {
    return 'Unknown'
  }

  const date = new Date(isoDate)
  if (Number.isNaN(date.getTime())) {
    return 'Unknown'
  }

  return date.toLocaleString()
}

export const FileDetailsModal = ({ open, item, onClose }: FileDetailsModalProps) => {
  const styles = useStyles()

  return (
    <Dialog open={open} onOpenChange={(_, data) => { if (!data.open) onClose() }}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>File Details</DialogTitle>
          <DialogContent className={styles.content}>
            <div className={styles.row}>
              <span className={styles.label}>Name</span>
              <Text weight="semibold">{item?.resource ?? 'Unknown'}</Text>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Type</span>
              <Text>{item?.contentType ?? 'Unknown'}</Text>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Size</span>
              <Text>{formatSize(item?.byteSize)}</Text>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Latest Version</span>
              <Text>{item?.latestVersion ?? 'Unknown'}</Text>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Created At</span>
              <Text>{formatDate(item?.createdAtUtc)}</Text>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Status</span>
              <Text>{item?.isDeleted ? 'Deleted' : 'Active'}</Text>
            </div>
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" onClick={onClose}>Close</Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}

export default FileDetailsModal
