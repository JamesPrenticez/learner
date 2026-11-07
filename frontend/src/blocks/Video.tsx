import { Field, Input, Stack, Textarea } from '../components/ui'
import { str, type EditorProps, type ViewProps } from './types'

/** YouTube/Vimeo watch links become embeds; anything else is treated as a direct video file. */
function embedUrl(raw: string): string | null {
  try {
    const u = new URL(raw)
    if (u.hostname.endsWith('youtube.com') && u.searchParams.get('v')) return `https://www.youtube.com/embed/${u.searchParams.get('v')}`
    if (u.hostname === 'youtu.be') return `https://www.youtube.com/embed${u.pathname}`
    if (u.hostname.endsWith('vimeo.com') && /^\/\d+/.test(u.pathname)) return `https://player.vimeo.com/video${u.pathname}`
  } catch {
    // not a URL yet
  }
  return null
}

export function VideoEditor({ data, onChange }: EditorProps) {
  const set = (k: string) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => onChange({ ...data, [k]: e.target.value })
  return (
    <Stack>
      <Field label="Video URL (YouTube, Vimeo or a direct .mp4/.webm link)">
        <Input type="url" value={str(data, 'url')} onChange={set('url')} />
      </Field>
      <Field label="Caption"><Input value={str(data, 'caption')} onChange={set('caption')} /></Field>
      <Field label="Description"><Textarea value={str(data, 'description')} onChange={set('description')} /></Field>
      <Field label="Transcript (for accessibility)"><Textarea value={str(data, 'transcript')} onChange={set('transcript')} /></Field>
    </Stack>
  )
}

export function VideoView({ data }: ViewProps) {
  const url = str(data, 'url')
  if (!/^https?:\/\//.test(url)) return <p><em>No video set.</em></p>
  const embed = embedUrl(url)
  return (
    <figure style={{ margin: 0 }}>
      {embed ? (
        <iframe src={embed} title={str(data, 'caption') || 'Video'} width="100%" height="360" allowFullScreen style={{ border: 0 }} />
      ) : (
        <video src={url} controls style={{ width: '100%' }} />
      )}
      {str(data, 'caption') && <figcaption>{str(data, 'caption')}</figcaption>}
      {str(data, 'description') && <p>{str(data, 'description')}</p>}
      {str(data, 'transcript') && (
        <details>
          <summary>Transcript</summary>
          <p style={{ whiteSpace: 'pre-wrap' }}>{str(data, 'transcript')}</p>
        </details>
      )}
    </figure>
  )
}
