import styled from 'styled-components'
import { mediaUrl } from '../services/api'
import { theme } from '../styles/theme'

const Frame = styled.div`
  aspect-ratio: 16 / 9;
  background: ${theme.color.primarySoft};
  border-radius: ${theme.radius};
  overflow: hidden;
  img { width: 100%; height: 100%; object-fit: cover; display: block; }
`

/** Course cover image, or a tinted placeholder. Decorative: the course title sits next to it. */
export function Cover({ id }: { id: string | null }) {
  return <Frame aria-hidden="true">{id && <img src={mediaUrl(id)} alt="" />}</Frame>
}
