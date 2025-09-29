import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  List,
  ListItem,
  Text,
  makeStyles,
  tokens,
  MessageBar,
} from '@fluentui/react-components'

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    minWidth: 'min(480px, 90vw)',
  },
  listWrapper: {
    maxHeight: '240px',
    overflowY: 'auto',
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  listHeader: {
    margin: 0,
  },
})

export interface SoftDeleteItem {
  id: string
  name: string
  path: string
}

export interface SoftDeleteDialogProps {
  open: boolean
  items: SoftDeleteItem[]
  isSubmitting: boolean
  errorMessage?: string | null
  onCancel: () => void
  onConfirm: () => void
}

export const SoftDeleteDialog = ({
  open,
  items,
  isSubmitting,
  errorMessage,
  onCancel,
  onConfirm,
}: SoftDeleteDialogProps) => {
  const styles = useStyles()

  return (
    <Dialog
      open={open}
      modalType="modal"
      onOpenChange={(_, data) => {
        if (!data.open) {
          onCancel()
        }
      }}
    >
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Delete selected content?</DialogTitle>
          <DialogContent className={styles.content}>
            <Text>
              You are about to remove {items.length}{' '}
              {items.length === 1 ? 'item' : 'items'}. This action cannot be undone.
            </Text>

            {errorMessage && <MessageBar intent="error">{errorMessage}</MessageBar>}

            <div className={styles.listWrapper}>
              <List aria-label="Selected resources">
                {items.map((item) => (
                  <ListItem key={item.id}>
                    <Text weight="semibold">{item.name}</Text>
                    <Text as="span" role="presentation" style={{ color: tokens.colorNeutralForeground3 }}>
                      {' '}
                      ({item.path})
                    </Text>
                  </ListItem>
                ))}
              </List>
            </div>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onCancel} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button appearance="primary" onClick={onConfirm} disabled={isSubmitting || items.length === 0}>
              Delete
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}

export default SoftDeleteDialog
