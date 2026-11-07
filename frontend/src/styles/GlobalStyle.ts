import { createGlobalStyle } from 'styled-components'
import { theme } from './theme'

export const GlobalStyle = createGlobalStyle`
  *, *::before, *::after { box-sizing: border-box; }

  body {
    margin: 0;
    font: 16px/1.5 ${theme.font};
    color: ${theme.color.text};
    background: ${theme.color.bg};
  }

  h1, h2, h3 { line-height: 1.25; margin: 0 0 0.5em; }
  h1 { font-size: 1.75rem; }
  h2 { font-size: 1.35rem; }
  h3 { font-size: 1.1rem; }
  p { margin: 0 0 1em; }
  a { color: ${theme.color.primary}; }
  img, video, iframe { max-width: 100%; }

  .sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    overflow: hidden;
    clip: rect(0 0 0 0);
    white-space: nowrap;
  }

  :focus-visible { outline: 3px solid ${theme.color.primary}; outline-offset: 2px; }
`
