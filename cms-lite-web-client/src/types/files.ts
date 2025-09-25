export interface FileNode {
    id: string
    parentDirectoryId: string
    name: string
    extension: string
    size: number
    isVisible: boolean
}

export interface FileListByDirectoryApiResponse {
    directory: null;
    contentItems: FileNodeV2[];
    totalCount: number;
    nextCursor: string | null;
}

export interface FileNodeV2 {
    id: string;
    name: string;
    latestVersion: number;
    contentType: string;
    byteSize: number;
    eTag: string;
    updatedAtUtc: string;
    createdAtUtc: string;
}

