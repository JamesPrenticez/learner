import type { ComponentType } from 'react'

export type BlockData = Record<string, unknown>

export type Block = { id: number; type: string; order: number; data: BlockData }

export type LessonEdit = { id: number; title: string; courseId: number; moduleId: number; blocks: Block[]; competencyIds: number[] }

export type EditorProps = { data: BlockData; onChange: (data: BlockData) => void; courseId: number }
export type ViewProps = { data: BlockData }

/**
 * Everything the app needs to know about a block type: how authors configure it and how learners see it.
 * The server holds the matching validation schema (BlockSchemas.cs).
 */
export type BlockDef = {
  type: string
  label: string
  defaults: BlockData
  Editor: ComponentType<EditorProps>
  View: ComponentType<ViewProps>
}

/** Read a string field from loosely-typed block data. */
export const str = (data: BlockData, key: string) => (typeof data[key] === 'string' ? (data[key] as string) : '')
