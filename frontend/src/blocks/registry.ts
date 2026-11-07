import { CalloutEditor, CalloutView } from './Callout'
import { DocumentEditor, DocumentView } from './Document'
import { ExerciseBlockEditor, ExerciseBlockView } from './Exercise'
import { ImageEditor, ImageView } from './Image'
import { QuizEditor, QuizView } from './Quiz'
import { RichTextEditor, RichTextView } from './RichText'
import type { BlockDef } from './types'
import { VideoEditor, VideoView } from './Video'

/**
 * The block registry. To add a block type: write its Editor + View components, add one entry here,
 * and add its schema line in the API's BlockSchemas.cs. Nothing else in the builder or learner app changes.
 */
export const blockDefs: BlockDef[] = [
  { type: 'richtext', label: 'Text', defaults: { markdown: '' }, Editor: RichTextEditor, View: RichTextView },
  { type: 'image', label: 'Image', defaults: { mediaId: '', alt: '', caption: '' }, Editor: ImageEditor, View: ImageView },
  { type: 'video', label: 'Video', defaults: { url: '', caption: '', description: '', transcript: '' }, Editor: VideoEditor, View: VideoView },
  { type: 'document', label: 'Document', defaults: { mediaId: '', title: '' }, Editor: DocumentEditor, View: DocumentView },
  { type: 'callout', label: 'Callout', defaults: { variant: 'info', text: '' }, Editor: CalloutEditor, View: CalloutView },
  { type: 'knowledgecheck', label: 'Knowledge check', defaults: {}, Editor: QuizEditor, View: QuizView },
  { type: 'quiz', label: 'Quiz', defaults: {}, Editor: QuizEditor, View: QuizView },
  { type: 'exercise', label: 'Exercise', defaults: {}, Editor: ExerciseBlockEditor, View: ExerciseBlockView },
]

export const blockDef = (type: string) => blockDefs.find((d) => d.type === type)
