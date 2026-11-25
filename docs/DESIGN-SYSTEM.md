# SYNC SOUND — Design System (Phase 2)

## Visual Direction

Warm, calm, Apple-inspired consumer software without copying Apple products. All audio-engine complexity is kept out of primary screens.

## Color Tokens (`Themes/Tokens.xaml`)

| Token | Hex | Purpose |
| :--- | :--- | :--- |
| `Color.Canvas.Primary` | `#F7F5F0` | Warm Canvas main background |
| `Color.Canvas.Rail` | `#EFECE5` | Navigation rail warmth |
| `Color.Surface.Card` | `#FFFFFF` | Pure Surface elevated cards |
| `Color.Ink.Primary` | `#18181B` | Deep Ink headings & labels |
| `Color.Ink.Secondary` | `#52525B` | Secondary descriptions |
| `Color.Ink.Muted` | `#8A8A93` | Captions & metadata |
| `Color.Accent.Primary` | `#E85D3F` | Signal Coral primary CTAs & active indicators |

## Shape, Elevation & Motion

- Rounded card corners (`18px` main cards, `22px` floating Quick Sidebar, `24px` pill CTAs).
- Soft ambient drop shadows (`Shadow.SoftCard`) instead of heavy borders.
- Purposeful micro-transitions (`180–200 ms` CubicEase fade/slide) respecting `SystemParameters.ClientAreaAnimation`.
