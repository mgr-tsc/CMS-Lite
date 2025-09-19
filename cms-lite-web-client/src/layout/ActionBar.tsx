import { Button, makeStyles, tokens } from '@fluentui/react-components'
import {
  AddRegular,
  EditRegular,
  DeleteRegular,
  EyeRegular,
  DocumentRegular,
  ArrowSyncRegular,
} from '@fluentui/react-icons'
import { ACTION_BAR, BREAKPOINTS } from './layoutConstants'

const useStyles = makeStyles({
  actionBar: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    minHeight: `${ACTION_BAR.HEIGHT}px`,
    width: '100%',
    zIndex: ACTION_BAR.Z_INDEX,
    boxSizing: 'border-box',
    flexWrap: 'wrap',
    rowGap: tokens.spacingVerticalS,
    [`@media (max-width: ${BREAKPOINTS.TABLET}px)`]: {
      justifyContent: 'center',
      gap: tokens.spacingHorizontalS,
    },
  },
  buttonGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
    [`@media (max-width: ${BREAKPOINTS.TABLET}px)`]: {
      width: '100%',
      justifyContent: 'center',
    },
    [`@media (max-width: ${BREAKPOINTS.MOBILE}px)`]: {
      flexDirection: 'column',
      alignItems: 'stretch',
      gap: tokens.spacingVerticalS,
    },
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
