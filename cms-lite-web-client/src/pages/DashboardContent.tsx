import { useSelector } from 'react-redux'
import { 
  makeStyles, 
  tokens, 
  Card, 
  CardPreview,
  Body1,
  Title2,
  Title3,
  Caption1
} from '@fluentui/react-components'
import type { RootState } from '../store/store'
import {
  selectDashboardStats,
  selectRecentFiles,
  selectActivities,
  selectDashboardErrors,
} from '../store/slices/dashboard'

const useDashboardStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    height: '100%',
    overflow: 'auto',
  },
  
  statsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalL,
  },
  
  statCard: {
    padding: tokens.spacingVerticalM,
    textAlign: 'center',
  },
  
  contentGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingHorizontalL,
  },
  
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  
  sectionTitle: {
    marginBottom: tokens.spacingVerticalM,
  },
  
  listItem: {
    padding: tokens.spacingVerticalS,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  
  errorMessage: {
    color: tokens.colorPaletteRedForeground1,
    padding: tokens.spacingVerticalM,
    textAlign: 'center',
  },
})

/**
 * Dashboard Content Component
 * 
 * Displays the actual dashboard content when data is loaded.
 * Shows stats, recent files, activities, and handles error states.
 */
export const DashboardContent = () => {
  const styles = useDashboardStyles()
  
  // Redux selectors
  const stats = useSelector((state: RootState) => selectDashboardStats(state))
  const recentFiles = useSelector((state: RootState) => selectRecentFiles(state))
  const activities = useSelector((state: RootState) => selectActivities(state))
  const errors = useSelector((state: RootState) => selectDashboardErrors(state))
  
  return (
    <div className={styles.container}>
      {/* Stats Section */}
      {stats && (
        <div className={styles.statsGrid}>
          <Card className={styles.statCard}>
            <CardPreview>
              <Title2>{stats.totalFiles}</Title2>
              <Body1>Total Files</Body1>
            </CardPreview>
          </Card>
          <Card className={styles.statCard}>
            <CardPreview>
              <Title2>{stats.totalFolders}</Title2>
              <Body1>Total Folders</Body1>
            </CardPreview>
          </Card>
          <Card className={styles.statCard}>
            <CardPreview>
              <Title2>{stats.storageUsed}</Title2>
              <Body1>Storage Used</Body1>
            </CardPreview>
          </Card>
          <Card className={styles.statCard}>
            <CardPreview>
              <Title2>{stats.recentActivity}</Title2>
              <Body1>Recent Activity</Body1>
            </CardPreview>
          </Card>
        </div>
      )}
      
      {/* Error display for stats */}
      {errors.stats && (
        <div className={styles.errorMessage}>
          <Body1>Error loading stats: {errors.stats}</Body1>
        </div>
      )}
      
      {/* Main Content Grid */}
      <div className={styles.contentGrid}>
        {/* Recent Files Section */}
        <div className={styles.section}>
          <Title3 className={styles.sectionTitle}>Recent Files</Title3>
          {errors.recentFiles ? (
            <div className={styles.errorMessage}>
              <Body1>Error loading recent files: {errors.recentFiles}</Body1>
            </div>
          ) : (
            <Card>
              <CardPreview>
                {recentFiles.length > 0 ? (
                  recentFiles.map((file) => (
                    <div key={file.id} className={styles.listItem}>
                      <div>
                        <Body1>{file.name}</Body1>
                        <Caption1>{file.type} • {file.size}</Caption1>
                      </div>
                      <Caption1>{file.modifiedDate}</Caption1>
                    </div>
                  ))
                ) : (
                  <div className={styles.listItem}>
                    <Body1>No recent files found</Body1>
                  </div>
                )}
              </CardPreview>
            </Card>
          )}
        </div>
        
        {/* Activities Section */}
        <div className={styles.section}>
          <Title3 className={styles.sectionTitle}>Recent Activities</Title3>
          {errors.activities ? (
            <div className={styles.errorMessage}>
              <Body1>Error loading activities: {errors.activities}</Body1>
            </div>
          ) : (
            <Card>
              <CardPreview>
                {activities.length > 0 ? (
                  activities.map((activity) => (
                    <div key={activity.id} className={styles.listItem}>
                      <div>
                        <Body1>{activity.action}</Body1>
                        <Caption1>{activity.fileName} by {activity.user}</Caption1>
                      </div>
                      <Caption1>{activity.timestamp}</Caption1>
                    </div>
                  ))
                ) : (
                  <div className={styles.listItem}>
                    <Body1>No recent activities found</Body1>
                  </div>
                )}
              </CardPreview>
            </Card>
          )}
        </div>
      </div>
    </div>
  )
}