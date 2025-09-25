import type {CSSProperties, ReactNode} from 'react'
import {useEffect, useMemo, useState} from 'react'
import {useDispatch, useSelector} from 'react-redux'
import {makeStyles} from '@fluentui/react-components'
import {Header} from './Header'
import {Footer} from './Footer'
import {ActionBar} from './ActionBar'
import {NavMenu} from './NavMenu'
import {ContentArea} from './ContentArea'
import {
    ANIMATIONS,
    BREAKPOINTS,
    getNavMenuWidth,
} from './layoutConstants'
import {useAuth} from '../hooks/useAuth'
import type {AppDispatch} from '../store/store'
import {
    fetchDirectoryTree,
    selectDirectoryTreeRoot,
    selectDirectoryTreeLoading,
    selectDirectoryTreeError,
    selectDirectoryTreeCurrentDirectory,
    selectDirectoryTreeLastFetchedTenant,
    setCurrentDirectory,
    type DirectoryNode,
    type ContentItemNode,
} from '../store/slices/directoryTree'
import {FileDetailsModal} from '../components/FileDetailsModal'

const useStyles = makeStyles({
    appContainer: {
        display: 'flex',
        flexDirection: 'column',
        minHeight: '100vh',
        backgroundColor: 'inherit',
    },
    mainContainer: {
        display: 'grid',
        flex: 1,
        minHeight: 0,
        position: 'relative',
        width: '100%',
        gridTemplateColumns: `${getNavMenuWidth(false)}px 1fr`,
        transition: ANIMATIONS.CONTENT_TRANSITION,
        [`@media (max-width: ${BREAKPOINTS.TABLET}px)`]: {
            gridTemplateColumns: '1fr',
        },
    },
    contentWrapper: {
        flex: 1,
        display: 'flex',
        flexDirection: 'column',
        transition: ANIMATIONS.CONTENT_TRANSITION,
        overflow: 'hidden',
        minWidth: 0,
        gridColumn: 2,
        [`@media (max-width: ${BREAKPOINTS.TABLET}px)`]: {
            gridColumn: '1',
        },
    },
    actionBarWrapper: {
        width: '100%',
        zIndex: 1,
    },
    mainContent: {
        flex: 1,
        overflow: 'auto',
        minHeight: 0,
    },
    overlayBackdrop: {
        position: 'fixed',
        inset: 0,
        backgroundColor: 'rgba(0, 0, 0, 0.35)',
        zIndex: 9,
        opacity: 1,
        transition: 'opacity 0.3s ease',
        [`@media (min-width: ${BREAKPOINTS.TABLET + 1}px)`]: {
            display: 'none',
        },
    },
})

interface AppLayoutProps {
    children?: ReactNode
}

export const AppLayout = ({children}: AppLayoutProps) => {
    const styles = useStyles()
    const dispatch = useDispatch<AppDispatch>()
    const {user, isAuthenticated} = useAuth()
    const rootDirectory = useSelector(selectDirectoryTreeRoot)
    const directoryLoading = useSelector(selectDirectoryTreeLoading)
    const directoryError = useSelector(selectDirectoryTreeError)
    const currentDirectory = useSelector(selectDirectoryTreeCurrentDirectory)
    const lastFetchedTenant = useSelector(selectDirectoryTreeLastFetchedTenant)
    const [viewportWidth, setViewportWidth] = useState(() =>
        typeof window === 'undefined' ? BREAKPOINTS.DESKTOP : window.innerWidth,
    )
    const [isNavMenuCollapsed, setIsNavMenuCollapsed] = useState<boolean>(() =>
        typeof window === 'undefined' ? false : window.innerWidth < BREAKPOINTS.TABLET,
    )
    const [selectedFiles, setSelectedFiles] = useState<string[]>([])
    const [isDetailsOpen, setIsDetailsOpen] = useState(false)
    const [detailItem, setDetailItem] = useState<ContentItemNode | null>(null)

    useEffect(() => {
        if (!isAuthenticated || !user?.tenantId) {
            return
        }
        const alreadyFetchedForTenant = lastFetchedTenant === user.tenantName
        if (alreadyFetchedForTenant || directoryLoading) {
            return
        }
        void dispatch(fetchDirectoryTree(user.tenantName));
    }, [dispatch, directoryLoading, isAuthenticated, lastFetchedTenant, user?.tenantName, user?.tenantId])

    useEffect(() => {
        setSelectedFiles([])
    }, [currentDirectory?.id])

    useEffect(() => {
        if (typeof window === 'undefined') {
            return undefined
        }

        let rAFId: number | null = null

        const handleResize = () => {
            if (rAFId !== null) {
                return
            }
            rAFId = window.requestAnimationFrame(() => {
                setViewportWidth(window.innerWidth)
                rAFId = null
            })
        }

        handleResize()
        window.addEventListener('resize', handleResize)
        return () => {
            window.removeEventListener('resize', handleResize)
            if (rAFId !== null) {
                window.cancelAnimationFrame(rAFId)
            }
        }
    }, [])

    const isOverlayNav = viewportWidth < BREAKPOINTS.TABLET
    const navWidth = isOverlayNav ? 0 : getNavMenuWidth(isNavMenuCollapsed)
    const mainContainerStyle: CSSProperties = {
        gridTemplateColumns: isOverlayNav ? '1fr' : `${navWidth}px 1fr`,
    }

    const handleItemSelect = (item: DirectoryNode) => {
        dispatch(setCurrentDirectory(item.id))
        setSelectedFiles([]) // Clear file selection when switching directories
        if (isOverlayNav) {
            setIsNavMenuCollapsed(true)
        }
    }

    const handleFileSelect = (fileIds: string[]) => {
        setSelectedFiles(fileIds)
    }

    const handleNewContent = () => {
        console.log('Creating new content...')
    }

    const handleEditContent = () => {
        if (selectedFiles.length > 0) {
            console.log('Editing content:', selectedFiles)
        }
    }

    const handleDeleteContent = () => {
        if (selectedFiles.length > 0) {
            console.log('Deleting content:', selectedFiles)
        }
    }

    const handleSeeDetails = () => {
        if (selectedFiles.length === 0) {
            return
        }

        const fileId = selectedFiles[0]
        const file = effectiveDirectory?.contentItems.find(item => item.id === fileId) ?? null
        setDetailItem(file)
        setIsDetailsOpen(true)
    }

    const handleRefresh = () => {
        if (user?.tenantName) {
            void dispatch(fetchDirectoryTree(user.tenantName))
        }
    }

    const handleViewAll = () => {
        if (rootDirectory) {
            dispatch(setCurrentDirectory(rootDirectory.id))
        }
    }

    const handleToggleNavMenu = () => {
        setIsNavMenuCollapsed(prev => !prev)
    }

    const effectiveDirectory = useMemo(() => currentDirectory ?? rootDirectory ?? null, [currentDirectory, rootDirectory])

    useEffect(() => {
        if (!isDetailsOpen) {
            setDetailItem(null)
            return
        }

        if (!selectedFiles.length || !effectiveDirectory) {
            setIsDetailsOpen(false)
            return
        }

        const active = effectiveDirectory.contentItems.find(item => item.id === selectedFiles[0]) ?? null
        if (!active) {
            setIsDetailsOpen(false)
        } else {
            setDetailItem(active)
        }
    }, [isDetailsOpen, selectedFiles, effectiveDirectory])

    return (
        <div className={styles.appContainer}>
            {/* Header spans full width */}
            <Header onToggleNavMenu={handleToggleNavMenu}/>

            {isOverlayNav && !isNavMenuCollapsed && (
                <div
                    className={styles.overlayBackdrop}
                    onClick={() => setIsNavMenuCollapsed(true)}
                    role="presentation"
                    aria-hidden="true"
                />
            )}

            <div className={styles.mainContainer} style={mainContainerStyle}>
                {/* Fixed sidebar */}
                <NavMenu
                    root={rootDirectory}
                    onItemSelect={handleItemSelect}
                    selectedItemId={effectiveDirectory?.id ?? null}
                    isCollapsed={isNavMenuCollapsed}
                    isOverlay={isOverlayNav}
                    onDismissOverlay={() => setIsNavMenuCollapsed(true)}
                    isLoading={directoryLoading}
                    error={directoryError}
                />

                {/* Main content area */}
                <div className={styles.contentWrapper}>
                    {/* ActionBar */}
                    <div className={styles.actionBarWrapper}>
                        <ActionBar
                            hasSelection={selectedFiles.length > 0}
                            onNewContent={handleNewContent}
                            onEditContent={handleEditContent}
                            onDeleteContent={handleDeleteContent}
                            onSeeDetails={handleSeeDetails}
                            onRefresh={handleRefresh}
                            onViewAll={handleViewAll}
                        />
                    </div>

                    {/* Main content */}
                    <div className={styles.mainContent}>
                        <ContentArea
                            selectedItem={effectiveDirectory}
                            selectedFiles={selectedFiles}
                            onFileSelect={handleFileSelect}
                            isLoading={directoryLoading}
                            error={directoryError}
                        />
                        {children}
                    </div>
                </div>
            </div>

            {/* Footer spans full width */}
            <Footer/>

            <FileDetailsModal
                open={isDetailsOpen}
                item={detailItem}
                onClose={() => setIsDetailsOpen(false)}
            />
        </div>
    )
}

// Context will be added later if needed for sharing state between components
