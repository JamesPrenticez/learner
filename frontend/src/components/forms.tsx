import { useState } from 'react'
import { Button, Input, Row } from './ui'

/** One text input + submit button; clears itself after submit. */
export function AddForm({ label, button = 'Add', onAdd, disabled }: { label: string; button?: string; onAdd: (text: string) => void; disabled?: boolean }) {
  const [text, setText] = useState('')
  return (
    <form
      onSubmit={(e) => {
        e.preventDefault()
        if (!text.trim()) return
        onAdd(text.trim())
        setText('')
      }}
    >
      <Row $wrap={false}>
        <Input aria-label={label} placeholder={label} value={text} onChange={(e) => setText(e.target.value)} />
        <Button type="submit" disabled={disabled}>{button}</Button>
      </Row>
    </form>
  )
}

/** Text with a Rename button that swaps to an input. */
export function InlineEdit({ value, onSave }: { value: string; onSave: (v: string) => void }) {
  const [editing, setEditing] = useState(false)
  const [text, setText] = useState(value)

  if (!editing) {
    return (
      <Row>
        <strong>{value}</strong>
        <Button $variant="ghost" aria-label={`Rename ${value}`} onClick={() => { setText(value); setEditing(true) }}>Rename</Button>
      </Row>
    )
  }
  return (
    <form
      onSubmit={(e) => {
        e.preventDefault()
        setEditing(false)
        if (text.trim() && text.trim() !== value) onSave(text.trim())
      }}
    >
      <Row $wrap={false}>
        <Input aria-label="New title" autoFocus value={text} onChange={(e) => setText(e.target.value)} />
        <Button type="submit">Save</Button>
        <Button type="button" $variant="ghost" onClick={() => setEditing(false)}>Cancel</Button>
      </Row>
    </form>
  )
}
