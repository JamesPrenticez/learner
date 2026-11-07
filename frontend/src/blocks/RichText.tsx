import ReactMarkdown from 'react-markdown'
import { Field, Muted, Textarea } from '../components/ui'
import { str, type EditorProps, type ViewProps } from './types'

export function RichTextEditor({ data, onChange }: EditorProps) {
  return (
    <Field label="Text">
      <Textarea style={{ minHeight: 160 }} value={str(data, 'markdown')} onChange={(e) => onChange({ ...data, markdown: e.target.value })} />
      <Muted>Markdown: # Heading, - list item, **bold**, [link](https://…)</Muted>
    </Field>
  )
}

// react-markdown does not render raw HTML, so author content cannot inject scripts.
export function RichTextView({ data }: ViewProps) {
  return <ReactMarkdown>{str(data, 'markdown')}</ReactMarkdown>
}
