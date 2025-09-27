import type {CSSProperties, ReactNode} from 'react'
import {useCallback, useEffect, useMemo, useState} from 'react'
import {useNavigate} from 'react-router-dom'
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
} from '../store/slices/directoryTree'
import {FileDetailsModal} from '../components/FileDetailsModal'
import customAxios from '../utilities/custom-axios'
import type {ContentItemDetails} from '../types/content'

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

interface FileDetailsState {
    open: boolean
    isLoading: boolean
    error: string | null
    data: ContentItemDetails | null
    resourceId: string | null
}

export const AppLayout = ({children}: AppLayoutProps) => {
    const styles = useStyles()
    const dispatch = useDispatch<AppDispatch>()
    const navigate = useNavigate()
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
    const [detailsState, setDetailsState] = useState<FileDetailsState>({
        open: false,
        isLoading: false,
        error: null,
        data: null,
        resourceId: null,
    })

    useEffect(() => {
        const tenantName = user?.tenant?.name
        if (!isAuthenticated || !tenantName) {
            return
        }
        const alreadyFetchedForTenant = lastFetchedTenant === tenantName
        if (alreadyFetchedForTenant || directoryLoading) {
            return
        }
        void dispatch(fetchDirectoryTree(tenantName));
    }, [dispatch, directoryLoading, isAuthenticated, lastFetchedTenant, user?.tenant?.name])

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

    const loadFileDetails = useCallback(async (tenantName: string, resourceId: string) => {
        setDetailsState(prev => ({...prev, isLoading: true, error: null}))
        try {
            const {data} = await customAxios.get<ContentItemDetails>(`/v1/${tenantName}/${encodeURIComponent(resourceId)}/details`)
            setDetailsState(prev => ({...prev, isLoading: false, data}))
        } catch (error) {
            let message = 'Failed to load file details'
            if (error instanceof Error) {
                message = error.message
            }
            setDetailsState(prev => ({...prev, isLoading: false, error: message}))
        }
    }, [])

    const effectiveDirectory = useMemo(() => currentDirectory ?? rootDirectory ?? null, [currentDirectory, rootDirectory])

    const handleSeeDetails = () => {
        const tenantName = user?.tenant?.name
        if (selectedFiles.length === 0 || !effectiveDirectory || !tenantName) {
            return
        }

        const fileId = selectedFiles[0]
        const file = effectiveDirectory.contentItems.find(item => item.id === fileId)
        if (!file) {
            return
        }

        setDetailsState({
            open: true,
            isLoading: true,
            error: null,
            data: null,
            resourceId: file.resource,
        })

        void loadFileDetails(tenantName, file.resource)
    }

    const handleCloseDetails = () => {
        setDetailsState(prev => ({...prev, open: false}))
    }

    const handleRetryDetails = () => {
        const tenantName = user?.tenant?.name
        if (!detailsState.resourceId || !tenantName) {
            return
        }
        setDetailsState(prev => ({...prev, isLoading: true, error: null}))
        void loadFileDetails(tenantName, detailsState.resourceId)
    }

    const handleOpenJsonViewer = (resourceId: string, details: ContentItemDetails | null) => {
        if (!resourceId) {
            return
        }

        const tenantName = user?.tenant?.name

        setDetailsState(prev => ({...prev, open: false}))
        navigate('/tools/json-viewer', {
            state: {
                resourceId,
                metadata: details,
                tenantName,
                contentType: details?.contentType,
                fileExtension: details?.metadata?.fileExtension,
                version: details?.latestVersion,
                viewer: 'json' as const,
            },
        })
    }

    const handleRefresh = () => {
        const tenantName = user?.tenant?.name
        if (tenantName) {
            void dispatch(fetchDirectoryTree(tenantName))
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
                open={detailsState.open}
                details={detailsState.data}
                isLoading={detailsState.isLoading}
                error={detailsState.error}
                resourceId={detailsState.resourceId}
                onClose={handleCloseDetails}
                onRetry={detailsState.error ? handleRetryDetails : undefined}
                onOpenJsonViewer={handleOpenJsonViewer}
            />
        </div>
    )
}

// Context will be added later if needed for sharing state between components
