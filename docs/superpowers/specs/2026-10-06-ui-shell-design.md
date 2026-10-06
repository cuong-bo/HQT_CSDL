# Hue Travel GIS Desktop — UI Shell Design Spec

**Date:** 2026-10-06
**Status:** Approved in chat — Phase 1 (visual shell only, no real backend logic)
**Author:** Claude Code (with user)

## 1. Goal

Build the WinForms UI shell for "Cố Đô GIS" (Hue City tourism GIS desktop app),
visually matching the mockup in `GiaoDien/` (`DESIGN.md` design tokens,
`code.html` reference markup, `screen.png` reference screenshot), with:

- A real MapWinGIS (`AxMap`) control as the central map canvas (no POI/road
  data loaded yet — placeholder/blank basemap is fine).
- Every floating panel and modal built as **WinForms-Designer-renderable**
  UserControls (all layout in generated `InitializeComponent()` code), so the
  user can open each form/control in Visual Studio's Designer view and see /
  drag-position it without running the app.
- Mock/sample data only (hardcoded lists matching the mockup's sample POIs).
- Code organized strictly **by domain/feature**, not by technical type, so
  each feature area is self-contained and easy to maintain independently.

## 2. Non-goals (this phase)

- Real POI database / CRUD / persistence of any kind.
- Real shortest-route algorithm (the "Tìm đường ngắn nhất" button is UI-only).
- Real authentication (login/register/OTP flows are UI-only, no backend).
- Real Hue basemap data (shapefiles/tiles) loaded into MapWinGIS.

These are explicitly deferred to later specs (see §8).

## 3. Code organization (domain-first)

Each feature is a self-contained folder + namespace under `Hue_travel/`,
containing its own `.cs` + `.Designer.cs` (+ `.resx` if needed). A domain
folder never reaches into another domain's internals directly — cross-domain
communication happens only through events/public methods, wired up by the
composition root (`MainForm`).

```
Hue_travel/
  Shell/                      # App chrome: window, ribbon, status bar
    MainForm.cs / .Designer.cs
    TitleBar.cs / .Designer.cs        (UserControl)
    RibbonBar.cs / .Designer.cs       (UserControl)
    CategoryRail.cs / .Designer.cs    (UserControl, left icon rail)
    StatusBar.cs / .Designer.cs       (UserControl)
  MapEngine/
    MapHost.cs / .Designer.cs         (UserControl wrapping AxMap + chrome)
  Panels/
    LayerControl/
      LayerControlPanel.cs / .Designer.cs
    RouteOptimizer/
      RouteOptimizerPanel.cs / .Designer.cs
    SearchPoi/
      SearchPoiPanel.cs / .Designer.cs
    TravelGuide/
      TravelGuidePanel.cs / .Designer.cs
    PoiIdentify/
      PoiIdentifyCard.cs / .Designer.cs
    BaseMapSwitcher/
      BaseMapSwitcherPopup.cs / .Designer.cs
  Auth/
    AuthModal.cs / .Designer.cs       (login / register / forgot-password, UI-only)
  Theme/
    ThemeColors.cs
    ThemeFonts.cs
    ThemeMetrics.cs                   (radii, spacing from DESIGN.md)
  Models/
    Poi.cs, PoiCategory.cs, RouteStep.cs, TourSuggestion.cs   (POCOs)
  SampleData/
    SamplePois.cs                     (static mock data matching mockup)
```

Each `Panels/<Feature>/` folder owns its UI and exposes a small public
surface (e.g. `SearchPoiPanel.PoiSelected` event). `MainForm` is the only
class allowed to know about every panel; panels do not reference each other.

## 4. Dependencies

- **Guna.UI2.WinForms** (NuGet) — rounded panels/buttons/inputs, used
  throughout `Shell/` and `Panels/` to approximate the mockup's flat/rounded
  look without hand-rolled GDI+ painting.
- **Interop.MapWinGIS.dll** / **AxInterop.MapWinGIS.dll** — referenced
  locally from a `lib/` folder in the repo (generated via `tlbimp`/`aximp`
  from the installed `MapWinGIS.ocx`, or copied from the user's existing
  VS2019 MapWinGIS integration if available) so the project is reproducible
  without depending on another machine's global COM registration.

## 5. MainForm (composition root)

- `FormBorderStyle = None`; custom title bar (`TitleBar` control: logo, "Đăng
  nhập / Đăng ký" button, minimize/maximize/close buttons wired to real
  window operations).
- `RibbonBar` under the title bar: buttons toggle panel visibility
  (`LayerControlPanel`, `RouteOptimizerPanel`, `SearchPoiPanel`,
  `TravelGuidePanel`) and trigger zoom/pan/basemap actions on `MapHost`.
- `CategoryRail` docked left (Tất cả / Di tích / Ẩm thực / Nghỉ ngơi / Sông
  Hương quick-filter buttons — UI only, no real filtering of real data yet).
- `MapHost` fills the remaining central area.
- `StatusBar` docked bottom (coordinates / scale / mock weather text).
- Floating panels are added as child controls, `BringToFront()`-ed over
  `MapHost`, manually positioned (`Location`) to match the mockup, with a
  simple mouse-drag-by-header behavior on their title strip.
- `AuthModal` shown as a full-form overlay (semi-opaque `Guna2Panel` backdrop
  + centered modal), toggled by the title bar's auth button.

## 6. Theming

`Theme/ThemeColors.cs`, `ThemeFonts.cs`, `ThemeMetrics.cs` hold constants
transcribed directly from `GiaoDien/DESIGN.md` (hex colors, corner radii,
spacing scale). Every `InitializeComponent()` assigns colors/fonts from
these constants directly (not computed at runtime) so the Designer renders
the final look exactly. Plus Jakarta Sans is unlikely to be installed on a
stock Windows machine — fallback to Segoe UI, documented in `ThemeFonts.cs`.

## 7. Known technical constraints

- **ActiveX "airspace":** `AxMap` is a windowed COM control. Solid-background
  floating panels over it work fine via normal Z-order (`BringToFront`), but
  true alpha-blended/blurred panels (`backdrop-blur` in the mockup) will not
  composite correctly over it — panels use a near-opaque solid background
  (~95% white) instead.
- **Designer compatibility:** `AxMap` must render in the VS2019 WinForms
  Designer like any other toolboxed control — verified once the interop
  references are wired into the `.csproj` and `MainForm.Designer.cs`
  declares it like a normal control.
- **Visual verification:** this environment cannot run/screenshot a WinForms
  GUI app. The user verifies the result visually in Visual Studio (Designer
  view and/or F5) against `GiaoDien/screen.png`.
- **No git repository** currently exists in this project; this spec is saved
  to disk but not committed. If the user wants history tracked, a separate
  `git init` is a distinct, explicit decision.

## 8. Deferred to later specs (not this phase)

- Phase 2: Real database schema + data access (POI CRUD).
- Phase 3: Real shortest-route algorithm over a road network graph.
- Phase 4: Real authentication + OTP backend.
- Phase 5: Real Hue basemap data (shapefiles/tiles) loaded into MapWinGIS.

## 9. Verification plan (this phase)

- `msbuild`/`dotnet build` the solution to confirm it compiles with the new
  NuGet + COM references.
- User opens `MainForm` in the VS Designer to confirm layout renders without
  designer errors and matches `screen.png`.
- User can drag/reposition any panel in the Designer as a sanity check that
  layout is fully Designer-editable (not runtime-only code).
