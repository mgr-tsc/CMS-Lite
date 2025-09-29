import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Text,
  Spinner,
} from '@fluentui/react-components'

export interface InfoDialogProps {
  open: boolean
  title: string
  description: string
  primaryLabel?: string
  isLoading?: boolean
  onDismiss: () => void
}

export const InfoDialog = ({
  open,
  title,
  description,
  primaryLabel = 'Close',
  isLoading = false,
  onDismiss,
}: InfoDialogProps) => {
  return (
    <Dialog
      open={open}
      modalType="modal"
      onOpenChange={(_, data) => {
        if (!data.open) {
          onDismiss()
        }
      }}
    >
      <DialogSurface>
        <DialogBody>
          <DialogTitle>{title}</DialogTitle>
          <DialogContent>
            {isLoading ? <Spinner label={description} /> : <Text>{description}</Text>}
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" onClick={onDismiss} disabled={isLoading}>
              {primaryLabel}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}

export default InfoDialog
