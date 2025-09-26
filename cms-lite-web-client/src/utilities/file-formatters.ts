export const formatFileDate = (isoDate?: string): string => {
    if (!isoDate) {
        return 'Unknown'
    }

    const date = new Date(isoDate)
    if (Number.isNaN(date.getTime())) {
        return 'Unknown'
    }

    return date.toLocaleString()
}
