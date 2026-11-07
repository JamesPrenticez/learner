import { useMutation } from '@tanstack/react-query'
import { api } from './api'

export type MediaAsset = { id: string; fileName: string; contentType: string }

export function useUpload() {
  return useMutation({
    mutationFn: (file: File) => {
      const form = new FormData()
      form.append('file', file)
      return api<MediaAsset>('/api/media', { body: form })
    },
  })
}
