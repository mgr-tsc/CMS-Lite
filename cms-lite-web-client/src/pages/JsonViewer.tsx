import { useEffect, useMemo, useState, type CSSProperties } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import {
  Button,
  Textarea,
  makeStyles,
  tokens,
  Subtitle1,
  Text,
  MessageBar,
  Card,
  CardHeader,
  CardFooter,
} from '@fluentui/react-components'
import { ArrowLeftRegular, CheckmarkRegular, ArrowResetRegular } from '@fluentui/react-icons'
import JsonView from '@uiw/react-json-view'
import { MainLayout } from '../layout'
import type { ContentItemDetails } from '../types/content'

const SAMPLE_JSON = {
  title: 'Content Item',
  status: 'draft',
  tags: ['news', 'release'],
  metadata: {
    author: 'CMS Lite',
    createdAt: '2025-01-12T09:45:00.000Z',
    lastEditedBy: 'editor@example.com',
  },
  body: {
    blocks: [
      { type: 'heading', level: 2, text: 'JSON Viewer Page' },
      { type: 'paragraph', text: 'Paste JSON in the panel to inspect the structure.' },
    ],
  },
}

const useStyles = makeStyles({
  pageRoot: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXL,
    height: '100%',
    maxWidth: '1200px',
    width: '100%',
    margin: '0 auto',
  },
  headerRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalM,
  },
  editorsWrapper: {
    display: 'grid',
    gap: tokens.spacingHorizontalXL,
    gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
    alignItems: 'stretch',
  },
  textarea: {
    minHeight: '320px',
  },
  viewerCard: {
    height: '100%',
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground2,
    borderColor: tokens.colorNeutralStroke1,
  },
  viewerContainer: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingHorizontalL,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke3}`,
  },
})

const viewerTheme: CSSProperties = {
  base00: tokens.colorNeutralBackground1,
  base01: tokens.colorNeutralBackground2,
  base02: tokens.colorNeutralBackground3,
  base03: tokens.colorNeutralForeground2,
  base04: tokens.colorNeutralForeground3,
  base05: tokens.colorNeutralForeground1,
  base06: tokens.colorNeutralForegroundDisabled,
  base07: tokens.colorNeutralForeground2,
  string: tokens.colorPaletteGreenForeground2,
  number: tokens.colorPaletteBlueForeground2,
  boolean: tokens.colorPaletteTealForeground2,
  null: tokens.colorPaletteMarigoldForeground2,
  backgroundColor: tokens.colorNeutralBackground1,
}

type JsonViewerRouteState = {
  resourceId?: string
  metadata?: ContentItemDetails | Record<string, unknown>
  json?: Record<string, unknown> | Array<unknown>
  rawJson?: string
}

type JsonLike = Record<string, unknown> | Array<unknown>

export const JsonViewer = () => {
  const styles = useStyles()
  const navigate = useNavigate()
  const location = useLocation()
  const routeState = location.state as JsonViewerRouteState | null
  const sourceResourceId = routeState?.resourceId
  const isMetadataPayload = Boolean(
    routeState?.metadata && !routeState?.rawJson && !routeState?.json,
  )

  const initialParsed = useMemo<JsonLike>(() => {
    if (routeState?.json) {
      return routeState.json
    }

    if (routeState?.rawJson) {
      try {
        const parsed = JSON.parse(routeState.rawJson)
        return parsed as JsonLike
      } catch (error) {
        console.warn('Failed to parse rawJson from navigation state', error)
      }
    }

    if (routeState?.metadata) {
      return routeState.metadata as JsonLike
    }

    return SAMPLE_JSON
  }, [routeState])

  const initialInput = useMemo(() => {
    if (routeState?.rawJson) {
      try {
        return JSON.stringify(JSON.parse(routeState.rawJson), null, 2)
      } catch {
        return routeState.rawJson
      }
    }

    return JSON.stringify(initialParsed, null, 2)
  }, [initialParsed, routeState?.rawJson])

  const [input, setInput] = useState(() => initialInput)
  const [parsedJson, setParsedJson] = useState<JsonLike>(
    () => initialParsed,
  )
  const [parseError, setParseError] = useState<string | null>(null)

  useEffect(() => {
    setInput(initialInput)
    setParsedJson(initialParsed)
    setParseError(null)
  }, [initialInput, initialParsed])

  const handleFormat = () => {
    try {
      const candidate = JSON.parse(input)
      setParsedJson(candidate as JsonLike)
      setParseError(null)
    } catch (error) {
      setParseError(error instanceof Error ? error.message : 'Unable to parse JSON input')
    }
  }

  const handleReset = () => {
    setInput(initialInput)
    setParsedJson(initialParsed)
    setParseError(null)
  }

  return (
    <MainLayout variant="viewer">
      <div className={styles.pageRoot}>
        <div className={styles.headerRow}>
          <Button
            icon={<ArrowLeftRegular />}
            appearance="secondary"
            onClick={() => navigate('/dashboard')}
          >
            Back to Content Explorer
          </Button>
          <Subtitle1>
            {sourceResourceId
              ? `Inspect payload for ${sourceResourceId}`
              : 'Inspect JSON payloads without leaving the Fluent UI shell.'}
          </Subtitle1>
        </div>

        <div className={styles.editorsWrapper}>
          <Card>
            <CardHeader
              header={<Text weight="semibold">JSON Input</Text>}
              description="Paste or edit the payload, then format to refresh the viewer."
            />
            <Textarea
              className={styles.textarea}
              value={input}
              onChange={(event, data) => setInput(data.value)}
              resize="vertical"
              appearance="outline"
            />
            <CardFooter>
              <Button icon={<CheckmarkRegular />} appearance="primary" onClick={handleFormat}>
                Format JSON
              </Button>
              <Button icon={<ArrowResetRegular />} appearance="subtle" onClick={handleReset}>
                Reset sample
              </Button>
            </CardFooter>
          </Card>

          <Card className={styles.viewerCard}>
            <CardHeader
              header={<Text weight="semibold">Viewer</Text>}
              description="Expand the tree to inspect deeply nested structures."
            />
            {isMetadataPayload && (
              <MessageBar intent="info">
                Showing file metadata. Load the JSON payload to inspect the full content.
              </MessageBar>
            )}
            {parseError ? (
              <MessageBar intent="error">{parseError}</MessageBar>
            ) : (
              <div className={styles.viewerContainer}>
                <JsonView
                  value={parsedJson}
                  displayDataTypes={false}
                  collapsed={2}
                  enableClipboard={false}
                  style={{
                    ...viewerTheme,
                    fontFamily: '"Segoe UI", sans-serif',
                    fontSize: '14px',
                    lineHeight: '1.5',
                  }}
                />
              </div>
            )}
          </Card>
        </div>
      </div>
    </MainLayout>
  )
}
