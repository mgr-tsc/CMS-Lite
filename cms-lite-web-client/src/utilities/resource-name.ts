export const sanitizeResourceName = (filename: string): string => {
    if (!filename || filename.trim() === '') {
        return 'resource'
    }

    const trimmed = filename.trim()
    const lastDotIndex = trimmed.lastIndexOf('.')

    let name = trimmed
    let extension = ''

    if (lastDotIndex > 0) {
        name = trimmed.substring(0, lastDotIndex)
        extension = trimmed.substring(lastDotIndex).toLowerCase()
    }

    let sanitized = name
        .toLowerCase()
        .replace(/\s+/g, '-')
        .replace(/[^a-z0-9_-]/g, '')
        .replace(/-+/g, '-')
        .replace(/^-+|-+$/g, '')

    if (!sanitized) {
        sanitized = 'resource'
    }

    return `${sanitized}${extension}`
}
