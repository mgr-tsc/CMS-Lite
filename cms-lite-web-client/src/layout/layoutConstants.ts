/**
 * Global layout constants for consistent component dimensions across the CMS application
 * All dimensions are in pixels unless otherwise specified
 */

// Navigation Menu dimensions
export const NAV_MENU = {
  EXPANDED_WIDTH: 300,
  COLLAPSED_WIDTH: 60,
  TOP_OFFSET: 140, // Header + ActionBar height
  BOTTOM_OFFSET: 80, // Footer height with padding
  Z_INDEX: 10,
} as const

// Header dimensions
export const HEADER = {
  HEIGHT: 80, // Approximate height with padding
  Z_INDEX: 20,
} as const

// Footer dimensions
export const FOOTER = {
  HEIGHT: 80, // Approximate height with padding
  Z_INDEX: 1,
} as const

// ActionBar dimensions
export const ACTION_BAR = {
  HEIGHT: 56, // minHeight as defined in component
  PADDING_VERTICAL: 16, // Approximate based on spacingVerticalM
  TOTAL_HEIGHT: 56 + 16, // minHeight + padding
  MAX_WIDTH: 1280,
  Z_INDEX: 5,
} as const

// Main content area calculations
export const MAIN_CONTENT = {
  MAX_WIDTH: 1280,
  PADDING: 32, // spacingVerticalXL
} as const

// Layout calculation utilities
export const getNavMenuWidth = (isCollapsed: boolean): number => {
  return isCollapsed ? NAV_MENU.COLLAPSED_WIDTH : NAV_MENU.EXPANDED_WIDTH
}

export const getMainContentMarginLeft = (isCollapsed: boolean): number => {
  return getNavMenuWidth(isCollapsed)
}

export const getAvailableContentWidth = (isCollapsed: boolean): string => {
  return `calc(100vw - ${getNavMenuWidth(isCollapsed)}px)`
}

export const getMaxContentWidth = (isCollapsed: boolean): string => {
  const availableWidth = `calc(100vw - ${getNavMenuWidth(isCollapsed)}px)`
  return `min(${MAIN_CONTENT.MAX_WIDTH}px, ${availableWidth})`
}

export const getNavMenuHeight = (): string => {
  return `calc(100vh - ${NAV_MENU.TOP_OFFSET}px - ${NAV_MENU.BOTTOM_OFFSET}px)`
}

// CSS custom properties for dynamic values
export const CSS_VARIABLES = {
  '--nav-menu-expanded-width': `${NAV_MENU.EXPANDED_WIDTH}px`,
  '--nav-menu-collapsed-width': `${NAV_MENU.COLLAPSED_WIDTH}px`,
  '--header-height': `${HEADER.HEIGHT}px`,
  '--footer-height': `${FOOTER.HEIGHT}px`,
  '--action-bar-height': `${ACTION_BAR.TOTAL_HEIGHT}px`,
  '--main-content-max-width': `${MAIN_CONTENT.MAX_WIDTH}px`,
} as const

// Animation constants
export const ANIMATIONS = {
  NAV_MENU_TRANSITION: 'width 0.3s ease',
  CONTENT_TRANSITION: 'all 0.3s ease',
} as const

// Breakpoints for responsive design
export const BREAKPOINTS = {
  MOBILE: 768,
  TABLET: 1024,
  DESKTOP: 1280,
} as const
