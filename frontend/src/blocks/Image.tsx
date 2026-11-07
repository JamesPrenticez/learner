import { ErrorText, Field, Input, Stack } from '../components/ui'
import { mediaUrl } from '../services/api'
import { useUpload } from '../services/media'
import { str, type EditorProps, type ViewProps } from './types'

export function ImageEditor({ data, onChange }: EditorProps) {
  const upload = useUpload()
  const id = str(data, 'mediaId')
  return (
    <Stack>
      <Field label="Image file">
        <input
          type="file"
          accept="image/png,image/jpeg,image/gif,image/webp"
          onChange={(e) => {
            const file = e.target.files?.[0]
            if (file) upload.mutate(file, { onSuccess: (m) => onChange({ ...data, mediaId: m.id }) })
          }}
        />
      </Field>
      {upload.isPending && <span role="status">Uploading…</span>}
      {upload.error && <ErrorText>{upload.error.message}</ErrorText>}
      {id && <img src={mediaUrl(id)} alt="" style={{ maxHeight: 120, alignSelf: 'flex-start' }} />}
      <Field label="Alternative text (describe the image for screen readers)">
        <Input value={str(data, 'alt')} onChange={(e) => onChange({ ...data, alt: e.target.value })} />
      </Field>
      <Field label="Caption">
        <Input value={str(data, 'caption')} onChange={(e) => onChange({ ...data, caption: e.target.value })} />
      </Field>
    </Stack>
  )
}

export function ImageView({ data }: ViewProps) {
  const id = str(data, 'mediaId')
  if (!id) return <p><em>No image selected.</em></p>
  return (
    <figure style={{ margin: 0 }}>
      <img src={mediaUrl(id)} alt={str(data, 'alt')} />
      {str(data, 'caption') && <figcaption>{str(data, 'caption')}</figcaption>}
    </figure>
  )
}
