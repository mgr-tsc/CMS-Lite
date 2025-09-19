import { useState } from 'react'
import {
  makeStyles,
  tokens,
  Text,
} from '@fluentui/react-components'
import {
  FolderRegular,
  FolderOpenRegular,
  ChevronRightRegular,
  ChevronDownRegular,
} from '@fluentui/react-icons'

const useStyles = makeStyles({
  navMenu: {
    position: 'fixed',
    top: '140px', // Header (~80px) + ActionBar (~56px) + padding
    left: '0',
    bottom: '80px', // Footer height + padding
    backgroundColor: tokens.colorNeutralBackground1,
    borderRight: `1px solid ${tokens.colorNeutralStroke1}`,
    padding: tokens.spacingVerticalM,
    overflowY: 'auto',
    transition: 'width 0.3s ease',
    zIndex: 10,
  },
  expanded: {
    width: '300px',
  },
  collapsed: {
    width: '60px',
  },
  header: {
    padding: tokens.spacingVerticalS,
    marginBottom: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  treeContainer: {
    width: '100%',
  },
  treeItem: {
    display: 'flex',
    width: '100%',
    textAlign: 'left',
    marginBottom: '2px',
    padding: tokens.spacingVerticalS,
    border: 'none',
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
    cursor: 'pointer',
    color: tokens.colorNeutralForeground1,
    '&:hover': {
      backgroundColor: tokens.colorBrandBackground,
      color: tokens.colorNeutralForegroundOnBrand,
    },
  },
  selectedItem: {
    backgroundColor: tokens.colorBrandBackgroundSelected,
    color: tokens.colorNeutralForegroundOnBrand,
    '&:hover': {
      backgroundColor: tokens.colorBrandBackgroundPressed,
      color: tokens.colorNeutralForegroundOnBrand,
    },
  },
  childrenContainer: {
    marginLeft: tokens.spacingHorizontalL,
    borderLeft: `1px solid ${tokens.colorNeutralStroke2}`,
    paddingLeft: tokens.spacingHorizontalS,
  },
  itemContent: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
})

interface NavItem {
  id: string
  name: string
  type: 'folder'
  children?: NavItem[]
  files?: FileItem[]
}

interface FileItem {
  id: string
  name: string
  type: 'file'
  version: string
  size: string
  lastModified: string
}

interface NavMenuProps {
  onItemSelect?: (item: NavItem) => void
  selectedItemId?: string
  isCollapsed?: boolean
}

// Mock data structure for the CMS content tree (directories only in nav)
const mockNavData: NavItem[] = [
  {
    id: 'pages',
    name: 'Pages',
    type: 'folder',
    children: [
      {
        id: 'pages-static',
        name: 'Static Pages',
        type: 'folder',
        files: [
          { id: 'home-page', name: 'home.json', type: 'file', version: 'v2.1', size: '2.3 KB', lastModified: '2024-01-15' },
          { id: 'about-page', name: 'about.json', type: 'file', version: 'v1.5', size: '1.8 KB', lastModified: '2024-01-10' },
          { id: 'contact-page', name: 'contact.json', type: 'file', version: 'v1.2', size: '1.1 KB', lastModified: '2024-01-05' }
        ]
      },
      {
        id: 'pages-dynamic',
        name: 'Dynamic Pages',
        type: 'folder',
        files: [
          { id: 'landing-page', name: 'landing.json', type: 'file', version: 'v3.0', size: '4.2 KB', lastModified: '2024-01-20' },
          { id: 'promo-page', name: 'promo.json', type: 'file', version: 'v1.0', size: '2.9 KB', lastModified: '2024-01-18' }
        ]
      }
    ],
    files: [
      { id: 'page-config', name: 'page-config.json', type: 'file', version: 'v1.0', size: '0.8 KB', lastModified: '2024-01-01' }
    ]
  },
  {
    id: 'blog',
    name: 'Blog Posts',
    type: 'folder',
    files: [
      { id: 'blog-1', name: 'getting-started-cms.json', type: 'file', version: 'v2.3', size: '5.7 KB', lastModified: '2024-01-22' },
      { id: 'blog-2', name: 'best-practices.json', type: 'file', version: 'v1.8', size: '8.1 KB', lastModified: '2024-01-20' },
      { id: 'blog-3', name: 'advanced-features.json', type: 'file', version: 'v1.0', size: '6.4 KB', lastModified: '2024-01-19' }
    ]
  },
  {
    id: 'products',
    name: 'Products',
    type: 'folder',
    children: [
      {
        id: 'products-catalog',
        name: 'Catalog',
        type: 'folder',
        files: [
          { id: 'product-list', name: 'product-catalog.json', type: 'file', version: 'v4.2', size: '12.5 KB', lastModified: '2024-01-25' },
          { id: 'categories', name: 'categories.json', type: 'file', version: 'v2.1', size: '3.2 KB', lastModified: '2024-01-23' }
        ]
      },
      {
        id: 'products-featured',
        name: 'Featured',
        type: 'folder',
        files: [
          { id: 'featured-items', name: 'featured-products.json', type: 'file', version: 'v1.9', size: '7.8 KB', lastModified: '2024-01-24' }
        ]
      }
    ]
  },
  {
    id: 'media',
    name: 'Media Files',
    type: 'folder',
    children: [
      {
        id: 'media-images',
        name: 'Images',
        type: 'folder',
        files: [
          { id: 'hero-banner', name: 'hero-banner.json', type: 'file', version: 'v1.3', size: '1.5 KB', lastModified: '2024-01-21' },
          { id: 'gallery-config', name: 'gallery-config.json', type: 'file', version: 'v2.0', size: '2.1 KB', lastModified: '2024-01-20' }
        ]
      },
      {
        id: 'media-documents',
        name: 'Documents',
        type: 'folder',
        files: [
          { id: 'terms-service', name: 'terms-of-service.json', type: 'file', version: 'v1.1', size: '9.3 KB', lastModified: '2024-01-15' },
          { id: 'privacy-policy', name: 'privacy-policy.json', type: 'file', version: 'v1.4', size: '7.6 KB', lastModified: '2024-01-18' }
        ]
      }
    ]
  }
]

// Utility function to find directory by ID and get its files
export const findDirectoryById = (id: string): NavItem | null => {
  const findInTree = (items: NavItem[]): NavItem | null => {
    for (const item of items) {
      if (item.id === id) {
        return item
      }
      if (item.children) {
        const found = findInTree(item.children)
        if (found) return found
      }
    }
    return null
  }
  return findInTree(mockNavData)
}

export const NavMenu = ({ onItemSelect, selectedItemId, isCollapsed = false }: NavMenuProps) => {
  const styles = useStyles()
  const [expandedItems, setExpandedItems] = useState<Set<string>>(new Set(['pages', 'blog']))

  const handleItemClick = (item: NavItem) => {
    // Always expand/collapse folders in the nav
    setExpandedItems(prev => {
      const newExpanded = new Set(prev)
      if (newExpanded.has(item.id)) {
        newExpanded.delete(item.id)
      } else {
        newExpanded.add(item.id)
      }
      return newExpanded
    })

    // Also notify parent about folder selection to show files in main view
    onItemSelect?.(item)
  }

  const renderTreeItem = (item: NavItem, level: number = 0) => {
    const isExpanded = expandedItems.has(item.id) && !isCollapsed
    const isSelected = selectedItemId === item.id
    const hasChildren = item.children && item.children.length > 0

    return (
      <div key={item.id}>
        <button
          className={`${styles.treeItem} ${isSelected ? styles.selectedItem : ''}`}
          onClick={() => !isCollapsed && handleItemClick(item)}
          style={{
            marginLeft: isCollapsed ? '0' : `${level * 16}px`,
            justifyContent: isCollapsed ? 'center' : 'flex-start',
            alignItems: 'center'
          }}
          title={isCollapsed ? item.name : undefined}
        >
          <div className={styles.itemContent}>
            {!isCollapsed && (hasChildren || item.files) && (
              isExpanded ? <ChevronDownRegular /> : <ChevronRightRegular />
            )}
            {isExpanded ? <FolderOpenRegular /> : <FolderRegular />}
            {!isCollapsed && <Text size={300}>{item.name}</Text>}
          </div>
        </button>

        {hasChildren && isExpanded && !isCollapsed && (
          <div>
            {item.children!.map(child => renderTreeItem(child, level + 1))}
          </div>
        )}
      </div>
    )
  }

  return (
    <div className={`${styles.navMenu} ${isCollapsed ? styles.collapsed : styles.expanded}`}>
      {!isCollapsed && (
        <div className={styles.header}>
          <Text weight="semibold" size={400}>
            Content Explorer
          </Text>
        </div>
      )}

      <div className={styles.treeContainer}>
        {mockNavData.map(item => renderTreeItem(item))}
      </div>
    </div>
  )
}