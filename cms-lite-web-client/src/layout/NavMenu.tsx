import { Button, makeStyles, tokens } from '@fluentui/react-components'
import {
  DocumentRegular,
  EditRegular,
  DeleteRegular,
  AddRegular,
} from '@fluentui/react-icons'

const useStyles = makeStyles({
  toolbar: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalL,
    flexWrap: 'wrap',
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusLarge,
  },
})

interface NavMenuProps {
  selectedItems: string[]
  onNewContent?: () => void
  onViewAll?: () => void
  onEditSelected?: () => void
  onDeleteSelected?: () => void
}

export const NavMenu = ({
  selectedItems,
  onNewContent,
  onViewAll,
  onEditSelected,
  onDeleteSelected
}: NavMenuProps) => {
  const styles = useStyles()

  return (
    <div className={styles.toolbar}>
      <Button icon={<AddRegular />} appearance="primary" onClick={onNewContent}>
        New Content
      </Button>
      <Button icon={<DocumentRegular />} onClick={onViewAll}>
        View All
      </Button>
      <Button
        icon={<EditRegular />}
        disabled={selectedItems.length === 0}
        onClick={onEditSelected}
      >
        Edit Selected
      </Button>
      <Button
        icon={<DeleteRegular />}
        disabled={selectedItems.length === 0}
        onClick={onDeleteSelected}
      >
        Delete Selected
      </Button>
    </div>
  )
}