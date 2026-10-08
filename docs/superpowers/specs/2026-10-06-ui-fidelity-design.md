# Hue Travel GIS Desktop — UI Fidelity Pass (Spec Addendum)

**Date:** 2026-10-06
**Status:** Design approved in chat; awaiting written-spec review
**Extends:** `2026-10-06-ui-shell-design.md` (shell already built; this pass only closes the gap to the mockup)

## 1. Goal

Make the WinForms app visually match `GiaoDien/screen.png` (with `code.html` and
`DESIGN.md` as references) as closely as WinForms + MapWinGIS allow: logo,
layout, icons, fonts, colors, map content. The map area stays a real
MapWinGIS `AxMap`. UI-only: sample data, no backend (same non-goals as the shell spec).

Target repo: `D:\BTL_CSDL\Hue_travel`. `Project_HQTCSDLKG` is out of scope and untouched.

## 2. Gap analysis (current code vs. mockup)

| Area | Mockup | Current |
|---|---|---|
| Logo | 28px round logo + "Cố Đô GIS" | Text label only |
| Icons | Material Symbols Outlined on ribbon, rail, panels, map controls | None (text glyphs `✕ ☐ —` only) |
| Font | Plus Jakarta Sans | Segoe UI fallback |
| Title bar | Org name "Trung tâm Xúc tiến Du lịch Huế" + avatar "H" | Missing |
| Ribbon | Auto-width icon buttons, separators, + "Xem toàn cảnh Huế", "Đo khoảng cách", status pill "Đang xem bản đồ thành phố" | Fixed 140px text buttons, 3 items missing |
| Map | River, citadel, bridges, roads, route, labeled pins | Blank `AxMap` |
| Map overlays | Top info pill, basemap toggle stack, compass, zoom/pan stack | None |
| Panels | Open by default as in screenshot | Hidden by default |

## 3. Assets (user approved download in chat)

Downloaded once, committed under `Hue_travel/Resources/`, embedded as
Embedded Resources so the app needs no installed fonts.

| Asset | Source | License |
|---|---|---|
| `MaterialSymbolsOutlined.ttf` | Google `material-design-icons` repo (static variable font) | Apache 2.0 |
| `PlusJakartaSans-{Regular,Medium,SemiBold,Bold}.ttf` | Google Fonts repo | OFL |
| `logo.png` | URL in `code.html` (`<img alt="Logo Cố Đô GIS">`) | As supplied by user's mockup |

Fallback if a download fails: report to the user; do not substitute silently.
Licenses are recorded in `Resources/LICENSES.md`.

## 4. Design

### 4.1 Theme additions (`Theme/`)
- `FontLoader` — loads embedded TTFs into a static `PrivateFontCollection` once. `ThemeFonts` returns Plus Jakarta Sans fonts from it, falling back to Segoe UI if loading throws (design-time safety).
- `IconFont` — `Bitmap Get(string name, int px, Color color)`; name→codepoint table for the ~40 icons used; renders the glyph with GDI+ (`TextRenderingHint.AntiAlias`), cached by (name, px, color). Called directly from `InitializeComponent()` so the Designer renders icons.
- `ThemeColors` realigned to the **`code.html` tailwind config** (`primary #0284c7`, `surface #f8fafc`, …) where it differs from the longer DESIGN.md palette, because `screen.png` was rendered from `code.html`. Thematic accents (`#f59e0b`, `#6366f1`, `#f43f5e`, `#06b6d4`) added for layer swatches.

### 4.2 Shell
- `TitleBar`: logo `PictureBox` (28px) + title; org name label + round avatar "H"; auth button with `account_circle` icon; min/max/close drawn with `minimize`, `crop_square`, `close` icons; close hover turns red as in mockup.
- `RibbonBar`: buttons become auto-width (`TextRenderer.MeasureText` + icon + padding) in a flow layout; thin separators; added "Xem toàn cảnh Huế" (`travel_explore`, → `MapHost.ZoomToFull`) and "Đo khoảng cách" (`straighten`, toggles MapWinGIS measure cursor mode); right-aligned status pill with green dot.
- `CategoryRail`: 48px rail, icon + 9px caption per category, active item `sky-100` fill, amber/orange/blue/cyan icon tints per mockup.
- `StatusBar`: icons + texts as in screenshot (area, weather, scale `1 : 15,000` in mono font, "Bản đồ du lịch sẵn sàng" green dot).

### 4.3 MapEngine
- New `MapScene` (in `MapEngine/`) builds in-memory MapWinGIS `Shapefile`s from the mockup's 1600×900 canvas coordinates, mapped to map units (Y flipped): river polygon (gradient approximated with a two-tone fill + outline), Cồn Hến, Hoàng Thành (outer + inner polygon), roads (white casing + grey line), three bridges, route polyline (blue line + white dashed overlay), text labels (`SÔNG HƯƠNG` rotated, bridge names, `HOÀNG THÀNH HUẾ`, `Cồn Hến`) via MapWinGIS `Labels`.
- Pins (Ga Huế, Đại Nội, Chùa Thiên Mụ, Quán Bún Bò Mụ Rơi) are a point shapefile; each pin image (label pill + circular icon + stem) is drawn with GDI+ and set as `ShapeDrawingOptions.Picture`, so pins pan/zoom with the map.
- Basemap switching (Chuẩn sáng / Vệ tinh / Ngắm đêm) changes `AxMap.BackColor` and layer tints; no tile download in this phase.
- `MapHost` exposes `ZoomIn/ZoomOut/ZoomToFull/SetMeasureMode/SetBaseMap`.

### 4.4 Map overlays (`MapEngine/Overlays/`)
Small child controls on `MapHost`, solid ~95% white (airspace constraint, see shell spec §7): `MapInfoPill` (top center), `BaseMapToggleStack`, `CompassButton`, `ZoomPanStack` (top-right column positions as in `code.html`), each ≤ one file with `.Designer.cs`.

### 4.5 Panels
All existing panels (`LayerControl`, `RouteOptimizer`, `SearchPoi`, `TravelGuide`, `PoiIdentifyCard`, `BaseMapSwitcherPopup`, `AuthModal`) keep their Form-based structure. For each: re-check against `code.html` (header bar color/height, paddings, font sizes, row layout, radii, shadows approximated by a 1px border), add missing icons, correct text. Default state on launch: LayerControl, SearchPoi, RouteOptimizer, TravelGuide and PoiIdentifyCard visible with the positions in `screen.png`; ribbon buttons still toggle them. `AuthModal` hidden until the auth button is clicked.

## 5. Constraints & decisions

- Backdrop blur and soft shadows cannot composite over the `AxMap` ActiveX window; approximated with near-opaque fill + 1px `#e2e8f0` border.
- Designer-renderability rule from the shell spec still holds: layout and icon/colour assignment live in `InitializeComponent()`; runtime-only logic stays in `.cs`.
- Domain-first folders unchanged; new code goes in `Theme/`, `MapEngine/`, `MapEngine/Overlays/`, `Resources/`.
- Git: repo exists (15 commits). Changes are made as small, reviewable steps; commits only when the user asks.

## 6. Verification

1. Build with VS2019 MSBuild (x86). Must compile with 0 errors.
2. Launch the app, capture the window with `System.Drawing.CopyFromScreen` (first task of the plan proves this works; if it does not, fall back to the user checking visually in VS).
3. Compare capture against `GiaoDien/screen.png` region by region (title bar, ribbon, rail, map, each panel, status bar); iterate until differences are limited to the known constraints in §5.
4. The repo currently has no test project, so there are no automated tests to run. Pure-logic helpers added in this pass (`IconFont` name table lookup, `MapScene` coordinate mapping) get small unit tests only if the plan adds a test project; otherwise verification is the build plus the screenshot comparison above.

## 7. Out of scope

Real data/backend/auth/routing, tile basemaps, dark theme, responsive resizing beyond Dock/Anchor behaviour already present.
