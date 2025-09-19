import { Button, makeStyles, tokens } from '@fluentui/react-components'
import {
  AddRegular,
  EditRegular,
  DeleteRegular,
  EyeRegular,
  DocumentRegular,
  ArrowSyncRegular,
} from '@fluentui/react-icons'

const useStyles = makeStyles({
  actionBar: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'flex-end',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    minHeight: '56px',
    maxWidth: '1280px',
    margin: '0 auto',
    width: '100%',
  },
  buttonGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
})

interface ActionBarProps {
  hasSelection: boolean
  onNewContent?: () => void
  onEditContent?: () => void
  onDeleteContent?: () => void
  onSeeDetails?: () => void
  onRefresh?: () => void
  onViewAll?: () => void
}

export const ActionBar = ({
  hasSelection,
  onNewContent,
  onEditContent,
  onDeleteContent,
  onSeeDetails,
  onRefresh,
  onViewAll,
}: ActionBarProps) => {
  const styles = useStyles()

  return (
    <div className={styles.actionBar}>
      <div className={styles.buttonGroup}>
        <Button
          icon={<AddRegular />}
          appearance="primary"
          onClick={onNewContent}
        >
          New Content
        </Button>

        <Button
          icon={<DocumentRegular />}
          onClick={onViewAll}
        >
          View All
        </Button>

        <Button
          icon={<ArrowSyncRegular />}
          onClick={onRefresh}
        >
          Refresh
        </Button>
      </div>

      <div className={styles.buttonGroup}>
        <Button
          icon={<EyeRegular />}
          disabled={!hasSelection}
          onClick={onSeeDetails}
        >
          See Details
        </Button>

        <Button
          icon={<EditRegular />}
          disabled={!hasSelection}
          onClick={onEditContent}
        >
          Edit
        </Button>

        <Button
          icon={<DeleteRegular />}
          disabled={!hasSelection}
          onClick={onDeleteContent}
        >
          Delete
        </Button>
      </div>
    </div>
  )
}