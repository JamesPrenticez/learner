import { Stack } from '../components/ui'
import { blockDef } from './registry'
import type { Block } from './types'

/** Renders blocks the way a learner sees them. Used by the builder's live preview and the learner's lesson page. */
export function BlockList({ blocks }: { blocks: Pick<Block, 'id' | 'type' | 'data'>[] }) {
  return (
    <Stack $gap={20}>
      {blocks.map((b) => {
        const def = blockDef(b.type)
        return def ? <def.View key={b.id} data={b.data} /> : <p key={b.id}><em>Unsupported block type “{b.type}”.</em></p>
      })}
    </Stack>
  )
}
