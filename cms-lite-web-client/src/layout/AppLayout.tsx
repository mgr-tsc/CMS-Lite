import type {CSSProperties, ReactNode} from 'react'
import {useCallback, useEffect, useMemo, useState} from 'react'
import {isAxiosError} from 'axios'
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
import {CreateDirectoryDialog} from '../components/CreateDirectoryDialog'
import {SoftDeleteDialog, type SoftDeleteItem} from '../components/SoftDeleteDialog'
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

interface CreateDirectoryState {
    open: boolean
    name: string
    isSubmitting: boolean
    error: string | null
}

interface SoftDeleteState {
    open: boolean
    items: SoftDeleteItem[]
    isSubmitting: boolean
    error: string | null
    successMessage: string | null
}

const findDirectoryPathSegments = (root: DirectoryNode | null, targetId: string | null): string[] => {
    if (!root || !targetId) {
        return []
    }

    const traverse = (node: DirectoryNode, trail: string[]): string[] | null => {
        const nextTrail = [...trail, node.name]
        if (node.id === targetId) {
            return nextTrail
        }
        for (const child of node.subDirectories) {
            const result = traverse(child, nextTrail)
            if (result) {
                return result
            }
        }
        return null
    }

    return traverse(root, []) ?? []
}

const buildPathString = (segments: string[]): string => {
    if (segments.length === 0) {
        return '/'
    }
    return `/${segments.join('/')}`
}

const appendPathSegment = (basePath: string, segment: string): string => {
    const trimmedSegment = segment.trim()
    if (!trimmedSegment) {
        return basePath === '/' ? '/' : `${basePath}/`
    }
    if (basePath === '/' || basePath.length === 0) {
        return `/${trimmedSegment}`
    }
    return `${basePath}/${trimmedSegment}`
}

const delay = (ms: number) => new Promise<void>((resolve) => {
    setTimeout(resolve, ms)
})

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
    const [createDirectoryState, setCreateDirectoryState] = useState<CreateDirectoryState>({
        open: false,
        name: '',
        isSubmitting: false,
        error: null,
    })
    const [softDeleteState, setSoftDeleteState] = useState<SoftDeleteState>({
        open: false,
        items: [],
        isSubmitting: false,
        error: null,
        successMessage: null,
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
        dispatch(setCurrentDirectory(item.id));
        setSelectedFiles([]); // Clear file selection when switching directories
        if (isOverlayNav) {
            setIsNavMenuCollapsed(true);
        }
    }

    const handleFileSelect = (fileIds: string[]) => {
        setSelectedFiles(fileIds)
    }

    const handleNewDirectory = () => {
        setCreateDirectoryState({
            open: true,
            name: '',
            isSubmitting: false,
            error: null,
        })
    }

    const handleEditContent = () => {
        if (selectedFiles.length > 0) {
            console.log('Editing content:', selectedFiles)
        }
    }

    const handleDeleteContent = () => {
        if (!effectiveDirectory || selectedFiles.length === 0) {
            return
        }

        const parentPath = parentPathDisplay
        const items: SoftDeleteItem[] = selectedFiles
            .map((fileId) => effectiveDirectory.contentItems.find((item) => item.id === fileId))
            .filter((item): item is NonNullable<typeof item> => Boolean(item))
            .map((item) => ({
                id: item.id,
                name: item.resource,
                path: appendPathSegment(parentPath, item.resource),
                resource: item.resource,
            }))

        if (items.length === 0) {
            return
        }

        setSoftDeleteState({
            open: true,
            items,
            isSubmitting: false,
            error: null,
            successMessage: null,
        })
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
    const parentPathSegments = useMemo(
        () => findDirectoryPathSegments(rootDirectory, effectiveDirectory?.id ?? null),
        [rootDirectory, effectiveDirectory?.id],
    )
    const parentPathDisplay = useMemo(() => buildPathString(parentPathSegments), [parentPathSegments])
    const proposedDirectoryPath = useMemo(
        () => appendPathSegment(parentPathDisplay, createDirectoryState.name.trim() || '(directory-name)'),
        [parentPathDisplay, createDirectoryState.name],
    )

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

    const handleDirectoryNameChange = (value: string) => {
        setCreateDirectoryState(prev => ({
            ...prev,
            name: value,
            error: null,
        }))
    }

    const handleCancelCreateDirectory = () => {
        if (createDirectoryState.isSubmitting) {
            return
        }
        setCreateDirectoryState({
            open: false,
            name: '',
            isSubmitting: false,
            error: null,
        })
    }

    const handleSubmitCreateDirectory = async () => {
        const tenantName = user?.tenant?.name
        const parentDirectory = effectiveDirectory

        if (!tenantName || !parentDirectory) {
            setCreateDirectoryState(prev => ({
                ...prev,
                error: 'Missing tenant or directory context. Please try again.',
            }))
            return
        }

        const trimmedName = createDirectoryState.name.trim()
        if (!trimmedName) {
            setCreateDirectoryState(prev => ({
                ...prev,
                error: 'Directory name is required.',
            }))
            return
        }

        const hasDuplicate = parentDirectory.subDirectories.some(
            (directory) => directory.name.toLowerCase() === trimmedName.toLowerCase(),
        )

        if (hasDuplicate) {
            setCreateDirectoryState(prev => ({
                ...prev,
                error: `A directory named "${trimmedName}" already exists here.`,
            }))
            return
        }

        setCreateDirectoryState(prev => ({
            ...prev,
            isSubmitting: true,
            error: null,
        }))

        try {
            await customAxios.post(`/v1/${tenantName}/directories`, {
                name: trimmedName,
                parentId: parentDirectory.id,
            })

            setCreateDirectoryState({
                open: false,
                name: '',
                isSubmitting: false,
                error: null,
            })

            await dispatch(fetchDirectoryTree(tenantName))
        } catch (error: unknown) {
            let message = 'Failed to create directory.'
            if (isAxiosError(error)) {
                if (error.response?.status === 409) {
                    message = `A directory named "${trimmedName}" already exists here.`
                } else if (error.response?.data && typeof error.response.data === 'object') {
                    const dataMessage = (error.response.data as { message?: string }).message
                    if (dataMessage) {
                        message = dataMessage
                    }
                } else if (error.message) {
                    message = error.message
                }
            } else if (error instanceof Error) {
                message = error.message
            }

            setCreateDirectoryState(prev => ({
                ...prev,
                isSubmitting: false,
                error: message,
            }))
        }
    }

    const handleCancelSoftDelete = () => {
        if (softDeleteState.isSubmitting) {
            return
        }
        setSoftDeleteState({
            open: false,
            items: [],
            isSubmitting: false,
            error: null,
            successMessage: null,
        })
    }

    const handleConfirmSoftDelete = async () => {
        const tenantName = user?.tenant?.name
        if (!tenantName || softDeleteState.items.length === 0) {
            return
        }

        setSoftDeleteState(prev => ({
            ...prev,
            isSubmitting: true,
            error: null,
        }))

        try {
            await delay(2000)

            const resources = softDeleteState.items.map((item) => item.resource)
            await customAxios.delete(`/v1/${tenantName}/bulk-delete`, {
                data: {
                    resources,
                },
            })

            const removedCount = resources.length

            setSoftDeleteState({
                open: true,
                items: [],
                isSubmitting: false,
                error: null,
                successMessage: `${removedCount} file${removedCount === 1 ? '' : 's'} successfully removed.`,
            })

            setSelectedFiles([])

            if (rootDirectory?.id) {
                dispatch(setCurrentDirectory(rootDirectory.id))
            }

            await dispatch(fetchDirectoryTree(tenantName))
        } catch (error: unknown) {
            let message = 'Failed to delete selected items.'
            if (isAxiosError(error)) {
                const apiMessage = (error.response?.data as { message?: string } | undefined)?.message
                if (apiMessage) {
                    message = apiMessage
                } else if (error.message) {
                    message = error.message
                }
            } else if (error instanceof Error) {
                message = error.message
            }

            setSoftDeleteState(prev => ({
                ...prev,
                isSubmitting: false,
                error: message,
                successMessage: null,
            }))
        }
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
                            onNewDirectory={handleNewDirectory}
                            disableNewDirectory={!effectiveDirectory || !user?.tenant?.name}
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

            <CreateDirectoryDialog
                open={createDirectoryState.open}
                directoryName={createDirectoryState.name}
                parentPath={proposedDirectoryPath}
                isSubmitting={createDirectoryState.isSubmitting}
                errorMessage={createDirectoryState.error}
                onNameChange={handleDirectoryNameChange}
                onCancel={handleCancelCreateDirectory}
                onSubmit={handleSubmitCreateDirectory}
            />

            <SoftDeleteDialog
                open={softDeleteState.open}
                items={softDeleteState.items}
                isSubmitting={softDeleteState.isSubmitting}
                errorMessage={softDeleteState.error}
                successMessage={softDeleteState.successMessage}
                onCancel={handleCancelSoftDelete}
                onConfirm={handleConfirmSoftDelete}
            />

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
