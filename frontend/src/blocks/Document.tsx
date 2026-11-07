import { ErrorText, Field, Input, Muted, Stack } from '../components/ui'
import { mediaUrl } from '../services/api'
import { useUpload } from '../services/media'
import { str, type EditorProps, type ViewProps } from './types'

export function DocumentEditor({ data, onChange }: EditorProps) {
  const upload = useUpload()
  return (
    <Stack>
      <Field label="PDF document">
        <input
          type="file"
          accept="application/pdf"
          onChange={(e) => {
            const file = e.target.files?.[0]
            if (file) upload.mutate(file, { onSuccess: (m) => onChange({ ...data, mediaId: m.id, title: str(data, 'title') || m.fileName }) })
          }}
        />
      </Field>
      {upload.isPending && <span role="status">Uploading…</span>}
      {upload.error && <ErrorText>{upload.error.message}</ErrorText>}
      {str(data, 'mediaId') && <Muted>File attached.</Muted>}
      <Field label="Title"><Input value={str(data, 'title')} onChange={(e) => onChange({ ...data, title: e.target.value })} /></Field>
    </Stack>
  )
}

export function DocumentView({ data }: ViewProps) {
  const id = str(data, 'mediaId')
  if (!id) return <p><em>No document attached.</em></p>
  return (
    <p>
      📄 <a href={mediaUrl(id)} target="_blank" rel="noopener noreferrer">{str(data, 'title') || 'Document'}</a> (PDF)
    </p>
  )
}
