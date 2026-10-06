---
name: Hue Tourism GIS Desktop System
colors:
  surface: '#faf8ff'
  surface-dim: '#d2d9f4'
  surface-bright: '#faf8ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f2f3ff'
  surface-container: '#eaedff'
  surface-container-high: '#e2e7ff'
  surface-container-highest: '#dae2fd'
  on-surface: '#131b2e'
  on-surface-variant: '#3f4850'
  inverse-surface: '#283044'
  inverse-on-surface: '#eef0ff'
  outline: '#707881'
  outline-variant: '#bfc7d2'
  surface-tint: '#006398'
  primary: '#006194'
  on-primary: '#ffffff'
  primary-container: '#007bb9'
  on-primary-container: '#fdfcff'
  inverse-primary: '#93ccff'
  secondary: '#576065'
  on-secondary: '#ffffff'
  secondary-container: '#dbe4ea'
  on-secondary-container: '#5d666b'
  tertiary: '#825100'
  on-tertiary: '#ffffff'
  tertiary-container: '#a36700'
  on-tertiary-container: '#fffbff'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#cce5ff'
  primary-fixed-dim: '#93ccff'
  on-primary-fixed: '#001d31'
  on-primary-fixed-variant: '#004b73'
  secondary-fixed: '#dbe4ea'
  secondary-fixed-dim: '#bfc8ce'
  on-secondary-fixed: '#141d21'
  on-secondary-fixed-variant: '#3f484d'
  tertiary-fixed: '#ffddb8'
  tertiary-fixed-dim: '#ffb95f'
  on-tertiary-fixed: '#2a1700'
  on-tertiary-fixed-variant: '#653e00'
  background: '#faf8ff'
  on-background: '#131b2e'
  surface-variant: '#dae2fd'
typography:
  headline-xl:
    fontFamily: Plus Jakarta Sans
    fontSize: 24px
    fontWeight: '700'
    lineHeight: 32px
  headline-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 18px
    fontWeight: '700'
    lineHeight: 26px
  headline-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 15px
    fontWeight: '600'
    lineHeight: 22px
  body-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 14px
    fontWeight: '500'
    lineHeight: 20px
  body-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 18px
  body-sm:
    fontFamily: Plus Jakarta Sans
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
  label-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 12px
    fontWeight: '600'
    lineHeight: 16px
  label-sm:
    fontFamily: Plus Jakarta Sans
    fontSize: 11px
    fontWeight: '600'
    lineHeight: 14px
  code-coords:
    fontFamily: JetBrains Mono
    fontSize: 11px
    fontWeight: '500'
    lineHeight: 14px
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  gutter: 0.75rem
  margin: 1rem
  space-xs: 0.25rem
  space-sm: 0.375rem
  space-md: 0.75rem
  space-lg: 1rem
  space-xl: 1.5rem
---

## Brand & Style

This design system is tailored for a professional desktop geographic information system (GIS) focused on tourism, cultural heritage exploration, and spatial planning in Hue City. Built for high-performance C# desktop environments (WPF, WinForms with WebView2, and MapWinGIS), the system balances data-dense analytical capabilities with an airy, refined, and culturally resonant aesthetic.

### Visual Identity & Movement
The design language adopts **Modern Fluent / Soft Precision**:
- **Clarity over Clutter:** GIS desktop software often suffers from visual exhaustion. This system uses generous white space, distinct surface elevation, and soft sky-blue tints to maintain low cognitive load during prolonged analytical sessions.
- **Poetic Precision:** Inspired by the serene landscape of the Perfume River (Sông Hương) and Hue's imperial architecture, crisp structural lines meet soft pastel blue backdrops and deliberate heritage accents.
- **Ergonomic Workspace:** Tailored for mouse-and-keyboard desktop workflows with structured tool ribbons, dockable sidebars, high-contrast layer toggles, and contextual floating map inspection overlays.

## Colors

The color palette centers on a crisp oceanic and sky-blue primary scale paired with pristine slates, complemented by specific functional accent colors mapped directly to GIS thematic layers (Di tích, Lưu trú, Ẩm thực, Thủy hệ):

### Primary & Interface Foundations
- **Primary Brand / Action (`#0284c7`):** Primary action buttons, active tool highlights, selection bounding boxes, and map centroid markers.
- **Primary Light / Canvas Tint (`#f0f9ff` to `#e0f2fe`):** Canvas backgrounds, dockpanel header states, and selected row highlights in attribute tables.
- **Neutral Base (`#0f172a`):** Primary text and sharp UI glyphs.
- **Neutral Surface / White (`#ffffff` & `#f8fafc`):** Floating inspector panels, dialog modals, dock background surfaces, and map widget controls.
- **Border Neutral (`#e2e8f0`):** Fine, hairline panel separators and grid dividers.

### GIS Thematic Accents (Layer Categorization)
- **Heritage & Culture Points (Di tích / Du lịch):** `#f59e0b` (Imperial Amber Gold) — High visibility over both vector and satellite basemaps.
- **Hospitality & Accommodations (Cơ sở lưu trú):** `#6366f1` (Royal Indigo) — Distinct differentiation from blue water networks.
- **Hue Culinary & Dining (Ẩm thực Huế):** `#f43f5e` (Spicy Coral Rose) — Warm, inviting tone identifying restaurants, cafes, and local specialty hubs.
- **Hydrological Network (Thủy hệ Sông Hương / Hồ hào):** `#06b6d4` (Huong Cyan) — Linear river flows and polygon water bodies.
- **Administrative & Transportation (Giao thông & Ranh giới):** `#64748b` (Slate Neutral) for road geometry; `#8b5cf6` for transport hubs (Đầu mối giao thông).

## Typography

Typography prioritizes legibility in high-density desktop interfaces, crisp rendering on standard Windows ClearType DPI scaling, and support for Vietnamese diacritics.

- **Plus Jakarta Sans:** Primary UI and typographic workhorse. Its open aperture and balanced x-height ensure crisp display within tool ribbons, layer trees, attribute tables, and popup cards.
- **JetBrains Mono:** Dedicated to coordinate outputs (VN-2000, WGS84 coordinates), map scale ratios (e.g., `1:25,000`), projection metadata, and SQL query filters in spatial attribute finders.

## Layout & Spacing

The layout structure is engineered for multi-pane C# desktop software (DockPanel / AvalonDock / WinForms SplitContainer):

### Desktop GIS Shell
- **Top Utility & Ribbon Bar (Height: 48px – 64px):** Houses map interaction tools (Pan, Zoom, Identify, Measure, Spatial Buffer, Route Query).
- **Left Panel (Width: 280px – 340px):** Layer hierarchy tree (TOC - Table of Contents), legend controls, layer transparency sliders, and symbology toggles.
- **Central Canvas (Flexible):** Unobstructed MapWinGIS viewport.
- **Right Inspector / Flyout (Width: 320px – 380px):** Spatial search, routing calculation, point of interest (POI) details, and photo gallery.
- **Bottom Drawer (Collapsible, Height: 180px – 260px):** Tabular data viewer (Shapefile / PostGIS Attribute Table).
- **Status Bar (Height: 28px):** Cursor coordinates, active CRS projection, scale bar, and render engine state.

## Elevation & Depth

This system utilizes soft, low-contrast desktop elevation to separate actionable GIS tools from the interactive map canvas without creating harsh visual breaks.

- **Layer 0 (Canvas Base):** Map rendering surface (MapWinGIS COM control / Tile layer).
- **Layer 1 (Fixed Panes):** Sidebar panel, attribute table, and top command ribbon. Rendered in `#ffffff` with a clean `1px solid #e2e8f0` right/bottom border. No shadow is applied to avoid visual noise along structural frame edges.
- **Layer 2 (Floating Map Controls):** Navigation compass, zoom bar, layer switcher, and measure tool popouts. Styled with `background: rgba(255, 255, 255, 0.95)`, `backdrop-filter: blur(8px)`, `border: 1px solid #e2e8f0`, and `box-shadow: 0 4px 12px rgba(15, 23, 42, 0.08)`.
- **Layer 3 (Inspectors & Feature Popups):** Feature info balloon positioned over clicked points. Uses `box-shadow: 0 10px 25px -5px rgba(2, 132, 199, 0.12), 0 8px 10px -6px rgba(15, 23, 42, 0.06)` with a crisp `#0284c7` active accent border.
- **Layer 4 (Modals & Symbology Pickers):** Spatial query builders, export shapefile dialogs. Backed by a soft `#0f172a` overlay at 25% opacity.

## Shapes

The design system maintains a refined, clean structural geometry appropriate for analytical software.

- **Level 1 (Soft - 4px to 8px):** Standard buttons, text boxes, tree-view selection nodes, and dropdown lists use `4px` corner radii to maximize pixel alignment on desktop screens.
- **Floating Overlays & Cards:** Floating map panels, POI info cards, and toolbars adopt `8px` (`rounded-lg`) corner radii for a modern tactile feel.
- **Map Pin Indicators & Status Badges:** Fully rounded pills (`9999px`) are reserved solely for thematic layer item counts, operational status badges (e.g., "Mở cửa", "Trùng tu"), and active spatial selection tags.

## Components

### 1. GIS Layer Manager (Table of Contents Tree)
- **Container:** Borderless list inside the left docking panel with alternating row hover (`#f8fafc`).
- **Layer Item:** Consists of a custom checkbox (checked in `#0284c7`), custom legend swatch (thematic colored circle or polyline glyph), layer name in `body-sm`, and an optional right-aligned opacity slider or eye icon.
- **Categorical Group Headers:** Bold `label-sm` in `#64748b` uppercase with an expandable chevron.

### 2. Map Floating Toolbar
- **Container:** Horizontal or vertical pill-softened dock placed directly on the MapWinGIS control canvas.
- **Buttons:** 36x36px icon buttons with transparent background, turning `#f0f9ff` on hover with a `#0284c7` border and icon tint. Active tool states receive a solid `#0284c7` fill with white iconography.

### 3. Attribute Table & Grid View
- **Header:** Height 32px, background `#f8fafc`, text in `label-sm` with a right divider border `#e2e8f0`. Includes filter and sort indicators.
- **Rows:** Height 28px, text in `body-sm`. Alternate row background `#ffffff` and `#fcfdfd`. Active selection highlighted in `#e0f2fe` with a left 3px indicator line in `#0284c7`.

### 4. POI Information Popup (Identify Card)
- **Header:** Heritage amber, indigo, or rose badge specifying the category (e.g., "Di tích Lịch sử", "Ẩm thực", "Khách sạn"), followed by the POI title in `headline-md`.
- **Content:** Quick metadata list (Address, opening hours, contact, entry fee), a 16:9 thumbnail preview, and coordinate readout in `code-coords`.
- **Actions:** Button group at bottom with "Chỉ đường" (Primary `#0284c7`), "Xem thuộc tính" (Secondary outlined), and "Thu phóng đến" (Zoom to layer).

### 5. Input Fields & Spatial Search
- **Search Bar:** Height 36px, embedded search icon, placeholder "Tìm kiếm di tích, đường phố, nhà hàng Huế...". Active focus state creates a `#0284c7` border ring with `0 0 0 3px rgba(2, 132, 199, 0.15)`.
- **Numeric & Coordinate Inputs:** Structured tabular inputs using `code-coords` font for exact X/Y or Lat/Long navigation.