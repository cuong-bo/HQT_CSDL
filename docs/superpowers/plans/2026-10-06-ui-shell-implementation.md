# Hue Travel GIS Desktop UI Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Designer-renderable WinForms UI shell for "Cố Đô GIS" (Hue tourism GIS desktop app) — real MapWinGIS map canvas, Guna.UI2-styled chrome and floating panels, mock data only, organized strictly by domain/feature folder.

**Architecture:** A `MainForm` composition root hosts a `MapHost` (wraps `AxMap`) filling the client area, with `TitleBar`/`RibbonBar`/`CategoryRail`/`StatusBar` chrome controls and six floating `Panels/*` UserControls positioned over the map via Z-order (`BringToFront`), plus an `AuthModal` overlay. Every control's layout lives in Designer-generated `InitializeComponent()` so Visual Studio's Designer view renders and lets the user drag-reposition pieces. Pure data/logic (sample POIs, filtering, selection ordering) is factored out of Designer code into small testable methods/classes covered by xUnit tests; Designer-only layout code is verified by compilation + manual Designer inspection, not fabricated unit tests.

**Tech Stack:** C# / .NET Framework 4.7.2, WinForms, Guna.UI2.WinForms (NuGet), MapWinGIS `AxMap` (COM interop, local `lib/` DLLs), xUnit + FluentAssertions (new `Hue_travel.Tests` project), MSBuild 16 (`C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe`).

**Spec:** `docs/superpowers/specs/2026-10-06-ui-shell-design.md`

## Global Constraints

- Target framework stays .NET Framework 4.7.2 — do not retarget.
- UI chrome/panels use **Guna.UI2.WinForms** controls (Guna2Panel, Guna2Button, Guna2TextBox, Guna2CheckBox, Guna2HtmlLabel) for rounded/flat styling — no hand-rolled GDI+ painting for corners/shadows.
- Map engine is **MapWinGIS `AxMap`** only, referenced from local `lib/Interop.MapWinGIS.dll` + `lib/AxInterop.MapWinGIS.dll` — no other map library.
- All control layout must be written in generated `InitializeComponent()` methods (Designer-compatible) — no runtime-only dynamic layout construction for anything the user should see in the Designer.
- No real database, no real routing algorithm, no real authentication backend in this plan — mock/sample data and UI-only button handlers only (per spec §2).
- Domain folder structure from spec §3 is mandatory: `Shell/`, `MapEngine/`, `Panels/<Feature>/`, `Auth/`, `Theme/`, `Models/`, `SampleData/`. Panels never reference each other directly; only `MainForm` wires cross-panel behavior.
- Floating panels use solid/near-opaque backgrounds (`~95% white`, i.e. `Color.FromArgb(242, 255, 255, 255)` is **not** usable as a true alpha blend over `AxMap` — WinForms `Control.BackColor` alpha is ignored for top-level compositing, so panels must use a fully opaque near-white color like `#F8FAFC`, not real transparency).
- Font: "Plus Jakarta Sans" is requested in the mockup but not present on a stock Windows box. `ThemeFonts` must reference `"Segoe UI"` as the actual `FontFamily` and document the substitution in a comment — do not reference a font family that may not resolve at runtime.
- Icons: no icon font/image assets were supplied with the mockup. Phase 1 buttons/labels use short text captions only (e.g. "Danh mục", "Tìm đường", "Tra cứu") — no Material Symbols glyphs. Recorded here as an explicit scope decision, not a gap.
- Colors are taken from `GiaoDien/code.html`'s actual `tailwind.config` (the literal reference implementation behind `screen.png`), **not** the `DESIGN.md` YAML front-matter (whose `primary: '#006194'` disagrees with code.html's `primary:"#0284c7"` — code.html wins as ground truth).

## Review Focus

- **Designer fails to open `MainForm`** because the `AxMap` COM reference can't resolve at design time (wrong bitness, missing TLB registration, or interop DLLs not copied into `lib/`) — Task 1 and Task 5 both include an explicit build-then-manual-Designer-open check, and Task 5 documents the exact fallback if the Designer throws.
- **Vietnamese diacritics render as boxes/mojibake** if any file is saved with the wrong encoding or a non-Unicode-safe font is referenced — Task 2's `ThemeFonts` pins `"Segoe UI"` (full Vietnamese glyph coverage on Windows) and Task 4's sample-data test asserts the literal diacritic strings round-trip correctly.
- **`SearchPoiPanel` filter logic breaks on empty keyword or no category selected** (a reasonable user types nothing, or clears all category chips) — Task 9 tests both cases explicitly, not just the "happy path" keyword match.
- **`RouteOptimizerPanel` selection ordering breaks when a user unchecks a middle item** (expected: remaining items renumber 1..N, not keep gaps) — Task 10 tests uncheck-then-recompute, not just "add in order."
- **DPI/auto-scale mismatch** makes the pixel positions copied from `code.html` look wrong on a non-96-DPI monitor — Task 13 sets `MainForm.AutoScaleMode = AutoScaleMode.Dpi` and `AutoScaleDimensions` explicitly rather than leaving the WinForms default, and Task 14's manual checklist includes checking this on the user's actual display.

---

## Task 1: Solution & Tooling Setup

**Files:**
- Modify: `Hue_travel.sln`
- Modify: `Hue_travel\Hue_travel.csproj`
- Create: `Hue_travel\lib\Interop.MapWinGIS.dll` (generated, binary)
- Create: `Hue_travel\lib\AxInterop.MapWinGIS.dll` (generated, binary)
- Create: `Hue_travel.Tests\Hue_travel.Tests.csproj`
- Create: `Hue_travel.Tests\PlaceholderTests.cs`
- Create: `.gitignore`

**Interfaces:**
- Produces: `lib/Interop.MapWinGIS.dll`, `lib/AxInterop.MapWinGIS.dll` referenced by `Hue_travel.csproj` for all later tasks that touch `AxMap`.
- Produces: `Hue_travel.Tests` project referencing `Hue_travel` for all later tasks with real unit tests.

- [ ] **Step 1: Generate the MapWinGIS interop assemblies**

Run (PowerShell), from the repo root:

```powershell
$tlbimp = 'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\TlbImp.exe'
$aximp  = 'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\AxImp.exe'
$ocx    = 'C:\Program Files\MapWinGIS\MapWinGIS.ocx'
New-Item -ItemType Directory -Force -Path '.\Hue_travel\lib' | Out-Null
& $tlbimp $ocx /out:'.\Hue_travel\lib\Interop.MapWinGIS.dll'
& $aximp  $ocx /out:'.\Hue_travel\lib\AxInterop.MapWinGIS.dll'
```

Expected: both DLLs created under `Hue_travel\lib\`. If `AxImp` errors because the `.ocx` isn't COM-registered, run `regsvr32 "C:\Program Files\MapWinGIS\MapWinGIS.ocx"` **as Administrator** first (ask the user to run this one command themselves if the agent's shell isn't elevated), then retry.

- [ ] **Step 2: Reference the interop DLLs and add Guna.UI2.WinForms to the main project**

Edit `Hue_travel\Hue_travel.csproj`, inside the existing `<ItemGroup>` that has `<Reference Include="System.Windows.Forms" />`, add:

```xml
<Reference Include="Interop.MapWinGIS">
  <HintPath>lib\Interop.MapWinGIS.dll</HintPath>
  <EmbedInteropTypes>False</EmbedInteropTypes>
</Reference>
<Reference Include="AxInterop.MapWinGIS">
  <HintPath>lib\AxInterop.MapWinGIS.dll</HintPath>
</Reference>
```

Then add a `PackageReference` item group (PackageReference works in classic .NET Framework csproj with MSBuild 16+):

```xml
<ItemGroup>
  <PackageReference Include="Guna.UI2.WinForms" Version="2.0.4.6" />
</ItemGroup>
```

- [ ] **Step 3: Restore and build to verify the references resolve**

Run:

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Restore
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Build /p:Configuration=Debug
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Create the `Hue_travel.Tests` xUnit project**

Create `Hue_travel.Tests\Hue_travel.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Hue_travel\Hue_travel.csproj" />
  </ItemGroup>
</Project>
```

Create `Hue_travel.Tests\PlaceholderTests.cs`:

```csharp
using Xunit;

namespace Hue_travel.Tests
{
    public class PlaceholderTests
    {
        [Fact]
        public void TestProject_IsWired_Up()
        {
            Assert.True(true);
        }
    }
}
```

- [ ] **Step 5: Add the test project to the solution and rebuild**

Run:

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Restore
dotnet test Hue_travel.Tests\Hue_travel.Tests.csproj 2>$null
```

If `dotnet` isn't on PATH, instead run via `vstest.console.exe` from the VS2019 install, or open the solution once in Visual Studio so it adds `Hue_travel.Tests` to `Hue_travel.sln` and restores/builds it there. Expected: `PlaceholderTests.TestProject_IsWired_Up` passes.

Manually add the new project to `Hue_travel.sln` if MSBuild didn't do it automatically — open `Hue_travel.sln` in a text editor and add a `Project(...)` entry for `Hue_travel.Tests\Hue_travel.Tests.csproj` mirroring the existing `Hue_travel` entry's GUID format (use a freshly generated GUID), plus matching `ProjectConfigurationPlatforms` lines for Debug/Release.

- [ ] **Step 6: Initialize git and make the first commit**

Create `.gitignore` at the repo root:

```
bin/
obj/
*.user
.vs/
Hue_travel/lib/*.dll
```

Run:

```powershell
git init
git add .
git commit -m "chore: scaffold MapWinGIS/Guna2 references and test project"
```

Note: `lib/*.dll` is excluded because the interop DLLs are machine-generated from the locally installed `MapWinGIS.ocx` — Step 1's PowerShell block is the reproducible source of truth, not the binary.

---

## Task 2: Theme Module

**Files:**
- Create: `Hue_travel\Theme\ThemeColors.cs`
- Create: `Hue_travel\Theme\ThemeFonts.cs`
- Create: `Hue_travel\Theme\ThemeMetrics.cs`
- Test: `Hue_travel.Tests\Theme\ThemeColorsTests.cs`

**Interfaces:**
- Produces: `ThemeColors.Primary`, `.PrimaryContainer`, `.OnPrimary`, `.Surface`, `.Background`, `.SurfaceContainerLowest`, `.SurfaceContainerLow`, `.SurfaceContainer`, `.SurfaceContainerHigh`, `.OnSurface`, `.OnSurfaceVariant`, `.Secondary`, `.Tertiary`, `.Outline`, `.OutlineVariant`, `.Error`, `.CategoryHeritage`, `.CategoryFood`, `.CategoryStay`, `.CategoryRiver`, `.StatusOnline`, `.BadgeAmber` — all `Color` (static readonly).
- Produces: `ThemeFonts.FamilyName` (`"Segoe UI"`), `.HeadlineXl`, `.HeadlineLg`, `.HeadlineMd`, `.BodyLg`, `.BodyMd`, `.BodySm`, `.LabelMd`, `.LabelSm` — all `Font` (static readonly).
- Produces: `ThemeMetrics.RadiusSm` (4), `.RadiusLg` (8), `.RadiusFull` (9999), `.SpaceXs` (4), `.SpaceSm` (6), `.SpaceMd` (12), `.SpaceLg` (16), `.SpaceXl` (24) — all `int`.
- Consumed by: every later task's `InitializeComponent()`.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using System.Drawing;
using Xunit;

namespace Hue_travel.Tests.Theme
{
    public class ThemeColorsTests
    {
        [Fact]
        public void Primary_MatchesCodeHtmlReference()
        {
            Hue_travel.Theme.ThemeColors.Primary.Should().Be(ColorTranslator.FromHtml("#0284C7"));
        }

        [Fact]
        public void CategoryColors_MatchCodeHtmlReference()
        {
            Hue_travel.Theme.ThemeColors.CategoryHeritage.Should().Be(ColorTranslator.FromHtml("#D97706"));
            Hue_travel.Theme.ThemeColors.CategoryFood.Should().Be(ColorTranslator.FromHtml("#EA580C"));
            Hue_travel.Theme.ThemeColors.CategoryStay.Should().Be(ColorTranslator.FromHtml("#2563EB"));
            Hue_travel.Theme.ThemeColors.CategoryRiver.Should().Be(ColorTranslator.FromHtml("#0891B2"));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Hue_travel.Tests --filter ThemeColorsTests` (or `vstest.console.exe` equivalent)
Expected: FAIL — `Hue_travel.Theme` namespace / `ThemeColors` type does not exist yet.

- [ ] **Step 3: Implement `ThemeColors`**

```csharp
using System.Drawing;

namespace Hue_travel.Theme
{
    /// <summary>Color tokens transcribed from GiaoDien/code.html's tailwind.config (ground truth over DESIGN.md's front-matter).</summary>
    public static class ThemeColors
    {
        public static readonly Color Primary = ColorTranslator.FromHtml("#0284C7");
        public static readonly Color PrimaryContainer = ColorTranslator.FromHtml("#0369A1");
        public static readonly Color OnPrimary = ColorTranslator.FromHtml("#FFFFFF");

        public static readonly Color Surface = ColorTranslator.FromHtml("#F8FAFC");
        public static readonly Color Background = ColorTranslator.FromHtml("#F0F9FF");
        public static readonly Color SurfaceContainerLowest = ColorTranslator.FromHtml("#FFFFFF");
        public static readonly Color SurfaceContainerLow = ColorTranslator.FromHtml("#F0F7FF");
        public static readonly Color SurfaceContainer = ColorTranslator.FromHtml("#E0F2FE");
        public static readonly Color SurfaceContainerHigh = ColorTranslator.FromHtml("#BAE6FD");

        public static readonly Color OnSurface = ColorTranslator.FromHtml("#0F172A");
        public static readonly Color OnSurfaceVariant = ColorTranslator.FromHtml("#334155");

        public static readonly Color Secondary = ColorTranslator.FromHtml("#64748B");
        public static readonly Color Tertiary = ColorTranslator.FromHtml("#B45309");

        public static readonly Color Outline = ColorTranslator.FromHtml("#94A3B8");
        public static readonly Color OutlineVariant = ColorTranslator.FromHtml("#CBD5E1");

        public static readonly Color Error = ColorTranslator.FromHtml("#DC2626");

        public static readonly Color CategoryHeritage = ColorTranslator.FromHtml("#D97706");
        public static readonly Color CategoryFood = ColorTranslator.FromHtml("#EA580C");
        public static readonly Color CategoryStay = ColorTranslator.FromHtml("#2563EB");
        public static readonly Color CategoryRiver = ColorTranslator.FromHtml("#0891B2");

        public static readonly Color StatusOnline = ColorTranslator.FromHtml("#10B981");
        public static readonly Color BadgeAmber = ColorTranslator.FromHtml("#F59E0B");
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Hue_travel.Tests --filter ThemeColorsTests`
Expected: PASS.

- [ ] **Step 5: Implement `ThemeFonts` and `ThemeMetrics` (no dedicated test — plain constant declarations, pinned indirectly by every later Designer file that references them)**

```csharp
using System.Drawing;

namespace Hue_travel.Theme
{
    /// <summary>
    /// The mockup specifies "Plus Jakarta Sans", which is not installed on a stock
    /// Windows machine. We substitute "Segoe UI" (full Vietnamese diacritic coverage,
    /// present on every supported Windows version) rather than referencing a font
    /// family that may fail to resolve at runtime.
    /// </summary>
    public static class ThemeFonts
    {
        public const string FamilyName = "Segoe UI";

        public static readonly Font HeadlineXl = new Font(FamilyName, 18F, FontStyle.Bold);
        public static readonly Font HeadlineLg = new Font(FamilyName, 13.5F, FontStyle.Bold);
        public static readonly Font HeadlineMd = new Font(FamilyName, 11.25F, FontStyle.Bold);
        public static readonly Font BodyLg = new Font(FamilyName, 10.5F, FontStyle.Regular);
        public static readonly Font BodyMd = new Font(FamilyName, 9.75F, FontStyle.Regular);
        public static readonly Font BodySm = new Font(FamilyName, 9F, FontStyle.Regular);
        public static readonly Font LabelMd = new Font(FamilyName, 9F, FontStyle.Bold);
        public static readonly Font LabelSm = new Font(FamilyName, 8.25F, FontStyle.Bold);
    }

    public static class ThemeMetrics
    {
        public const int RadiusSm = 4;
        public const int RadiusLg = 8;
        public const int RadiusFull = 9999;

        public const int SpaceXs = 4;
        public const int SpaceSm = 6;
        public const int SpaceMd = 12;
        public const int SpaceLg = 16;
        public const int SpaceXl = 24;
    }
}
```

- [ ] **Step 6: Build and commit**

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Build
git add Hue_travel/Theme Hue_travel.Tests/Theme
git commit -m "feat: add Theme module with color/font/metric tokens"
```

---

## Task 3: Domain Models

**Files:**
- Create: `Hue_travel\Models\PoiCategory.cs`
- Create: `Hue_travel\Models\Poi.cs`
- Create: `Hue_travel\Models\RouteStep.cs`
- Create: `Hue_travel\Models\TourSuggestion.cs`

**Interfaces:**
- Produces: `PoiCategory` enum (`Heritage, Food, Stay, River, Transport`), `Poi` class, `RouteStep` class, `TourSuggestion` class — consumed by `SampleData` (Task 4) and every `Panels/*` task.

These are plain POCOs with no computed behavior, so there is no meaningful unit test to write for them (per the plan's no-fake-tests rule) — verification is compilation plus their use in Task 4's data-integrity test.

- [ ] **Step 1: Create `PoiCategory.cs`**

```csharp
namespace Hue_travel.Models
{
    public enum PoiCategory
    {
        Heritage,   // Di tích lịch sử & Điểm tham quan
        Food,       // Món ngon & Quán đặc sản Huế
        Stay,       // Khách sạn & Nơi lưu trú
        River,      // Sông Hương & Các hồ cảnh quan
        Transport   // Ga, bến xe, bến thuyền
    }
}
```

- [ ] **Step 2: Create `Poi.cs`**

```csharp
namespace Hue_travel.Models
{
    public sealed class Poi
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public PoiCategory Category { get; set; }
        public string Address { get; set; }
        public string OpeningHours { get; set; }
        public string TicketPrice { get; set; }
        public double Rating { get; set; }
        public double DistanceKm { get; set; }
        public string Description { get; set; }
    }
}
```

- [ ] **Step 3: Create `RouteStep.cs`**

```csharp
namespace Hue_travel.Models
{
    public sealed class RouteStep
    {
        public int Order { get; set; }
        public Poi From { get; set; }
        public Poi To { get; set; }
        public double DistanceKm { get; set; }
        public int DurationMinutes { get; set; }
    }
}
```

- [ ] **Step 4: Create `TourSuggestion.cs`**

```csharp
namespace Hue_travel.Models
{
    public sealed class TourSuggestion
    {
        public string Title { get; set; }
        public string Badge { get; set; }
        public string Description { get; set; }
    }
}
```

- [ ] **Step 5: Build and commit**

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Build
git add Hue_travel/Models
git commit -m "feat: add Poi/RouteStep/TourSuggestion domain models"
```

---

## Task 4: Sample Data

**Files:**
- Create: `Hue_travel\SampleData\SamplePois.cs`
- Create: `Hue_travel\SampleData\SampleTours.cs`
- Test: `Hue_travel.Tests\SampleData\SamplePoisTests.cs`

**Interfaces:**
- Consumes: `Poi`, `PoiCategory`, `TourSuggestion` (Task 3).
- Produces: `SamplePois.All` (`IReadOnlyList<Poi>`), `SampleTours.All` (`IReadOnlyList<TourSuggestion>`) — consumed by `Panels/SearchPoi`, `Panels/RouteOptimizer`, `Panels/TravelGuide`, `Panels/PoiIdentify` (Tasks 9-11).

- [ ] **Step 1: Write the failing test**

```csharp
using System.Linq;
using FluentAssertions;
using Hue_travel.Models;
using Hue_travel.SampleData;
using Xunit;

namespace Hue_travel.Tests.SampleData
{
    public class SamplePoisTests
    {
        [Fact]
        public void All_ContainsExpectedCount()
        {
            SamplePois.All.Should().HaveCount(7);
        }

        [Fact]
        public void All_ContainsDaiNoi_WithCorrectDiacritics()
        {
            var daiNoi = SamplePois.All.SingleOrDefault(p => p.Id == "dai-noi");
            daiNoi.Should().NotBeNull();
            daiNoi.Name.Should().Be("Đại Nội - Hoàng Thành Huế");
            daiNoi.Category.Should().Be(PoiCategory.Heritage);
        }

        [Fact]
        public void All_ContainsThreeBunBoVariants()
        {
            SamplePois.All.Count(p => p.Category == PoiCategory.Food).Should().Be(3);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Hue_travel.Tests --filter SamplePoisTests`
Expected: FAIL — `Hue_travel.SampleData` namespace does not exist yet.

- [ ] **Step 3: Implement `SamplePois.cs`**

```csharp
using System.Collections.Generic;
using Hue_travel.Models;

namespace Hue_travel.SampleData
{
    /// <summary>Hardcoded sample data matching GiaoDien/code.html's reference POIs. No persistence — Phase 1 UI-only.</summary>
    public static class SamplePois
    {
        public static readonly IReadOnlyList<Poi> All = new List<Poi>
        {
            new Poi
            {
                Id = "ga-hue",
                Name = "Ga Huế (Điểm xuất phát)",
                Category = PoiCategory.Transport,
                Address = "Đường Bùi Thị Xuân, TP. Huế",
                OpeningHours = "24/7",
                TicketPrice = "Miễn phí",
                Rating = 0,
                DistanceKm = 0,
                Description = "Điểm xuất phát mặc định cho lộ trình tham quan."
            },
            new Poi
            {
                Id = "dai-noi",
                Name = "Đại Nội - Hoàng Thành Huế",
                Category = PoiCategory.Heritage,
                Address = "Đường 23 Tháng 8, Phường Thuận Hòa, TP. Huế",
                OpeningHours = "07:00 - 17:30 (Mở cửa tất cả các ngày trong tuần)",
                TicketPrice = "200.000 VNĐ / người lớn (Miễn phí trẻ dưới 1m)",
                Rating = 4.8,
                DistanceKm = 3.2,
                Description = "Di tích lịch sử Hoàng Cung triều Nguyễn."
            },
            new Poi
            {
                Id = "thien-mu",
                Name = "Chùa Thiên Mụ",
                Category = PoiCategory.Heritage,
                Address = "Đường Nguyễn Phúc Nguyên, TP. Huế",
                OpeningHours = "06:00 - 18:00",
                TicketPrice = "Miễn phí",
                Rating = 4.7,
                DistanceKm = 4.6,
                Description = "Ngắm cảnh ven bờ sông Hương thơ mộng."
            },
            new Poi
            {
                Id = "khai-dinh",
                Name = "Lăng Khải Định",
                Category = PoiCategory.Heritage,
                Address = "Xã Thủy Bằng, TP. Huế",
                OpeningHours = "07:00 - 17:30",
                TicketPrice = "150.000 VNĐ / người lớn",
                Rating = 4.6,
                DistanceKm = 10.1,
                Description = "Kiến trúc lăng tẩm độc đáo Tây - Đông."
            },
            new Poi
            {
                Id = "bun-bo-mu-roi",
                Name = "Quán Bún Bò Huế Mụ Rơi",
                Category = PoiCategory.Food,
                Address = "40 Nguyễn Chí Diểu, Thuận Thành, TP. Huế",
                OpeningHours = "Mở cửa",
                TicketPrice = "35k - 50k",
                Rating = 4.8,
                DistanceKm = 1.2,
                Description = "Thưởng thức ẩm thực trưa xứ Cố Đô."
            },
            new Poi
            {
                Id = "bun-bo-ba-tuyet",
                Name = "Bún Bò Huế Bà Tuyết",
                Category = PoiCategory.Food,
                Address = "47 Nguyễn Công Trứ, Phú Hội, TP. Huế",
                OpeningHours = "Mở cửa",
                TicketPrice = "35k - 50k",
                Rating = 4.7,
                DistanceKm = 2.1,
                Description = "Quán bún bò Huế lâu năm được yêu thích."
            },
            new Poi
            {
                Id = "bun-bo-cam",
                Name = "Bún Bò Cẩm - Vị Cố Đô",
                Category = PoiCategory.Food,
                Address = "38 Trần Cao Vân, Phú Hội, TP. Huế",
                OpeningHours = "Mở cửa",
                TicketPrice = "35k - 50k",
                Rating = 4.6,
                DistanceKm = 2.6,
                Description = "Vị bún bò đậm đà chuẩn Cố Đô."
            }
        };
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Hue_travel.Tests --filter SamplePoisTests`
Expected: PASS.

- [ ] **Step 5: Implement `SampleTours.cs` (no dedicated test — static display data consumed by Task 11)**

```csharp
using System.Collections.Generic;
using Hue_travel.Models;

namespace Hue_travel.SampleData
{
    public static class SampleTours
    {
        public static readonly IReadOnlyList<TourSuggestion> All = new List<TourSuggestion>
        {
            new TourSuggestion
            {
                Title = "Tour 1 ngày: Di Sản Cố Đô",
                Badge = "Khuyên dùng",
                Description = "Ga Huế → Đại Nội → Chùa Thiên Mụ → Lăng Tự Đức"
            },
            new TourSuggestion
            {
                Title = "Tour Đêm Sông Hương & Ca Huế",
                Badge = "19:00 - 21:00",
                Description = "Thuyền rồng Bến Tòa Khâm, thả hoa đăng ngắm cầu Trường Tiền"
            },
            new TourSuggestion
            {
                Title = "Tour Ẩm Thực Cố Đô Về Chiều",
                Badge = "Ẩm thực",
                Description = "Bún bò Mụ Rơi, bánh bèo nậm lọc Bà Đỏ, chè hẻm Hùng Vương"
            }
        };
    }
}
```

- [ ] **Step 6: Build and commit**

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Build
git add Hue_travel/SampleData Hue_travel.Tests/SampleData
git commit -m "feat: add sample POI and tour data matching mockup"
```

---

## Task 5: MapEngine — MapHost UserControl

**Files:**
- Create: `Hue_travel\MapEngine\MapHost.cs`
- Create: `Hue_travel\MapEngine\MapHost.Designer.cs`

**Interfaces:**
- Produces: `MapHost` (UserControl), public method `MapHost.ZoomIn()`, `MapHost.ZoomOut()`, `MapHost.PanMode(bool enabled)`, property `MapHost.Map` (the underlying `AxMap`, exposed so `MainForm`/`RibbonBar` wiring in Task 13 can call `ZoomToMaxExtents()` etc.) — consumed by Task 13.

- [ ] **Step 1: Create `MapHost.Designer.cs`**

```csharp
using AxMapWinGIS;

namespace Hue_travel.MapEngine
{
    partial class MapHost
    {
        private System.ComponentModel.IContainer components = null;
        private AxMap axMap;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.axMap = new AxMap();
            ((System.ComponentModel.ISupportInitialize)(this.axMap)).BeginInit();
            this.SuspendLayout();

            this.axMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axMap.Name = "axMap";
            this.axMap.TabIndex = 0;

            this.Controls.Add(this.axMap);
            this.Name = "MapHost";
            this.Size = new System.Drawing.Size(900, 700);

            ((System.ComponentModel.ISupportInitialize)(this.axMap)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
```

- [ ] **Step 2: Create `MapHost.cs`**

```csharp
using System.Windows.Forms;
using AxMapWinGIS;

namespace Hue_travel.MapEngine
{
    public partial class MapHost : UserControl
    {
        public MapHost()
        {
            InitializeComponent();
        }

        public AxMap Map => axMap;

        public void ZoomIn() => axMap.ZoomIn(0.5);

        public void ZoomOut() => axMap.ZoomOut(0.5);

        public void PanMode(bool enabled)
        {
            axMap.CursorMode = enabled
                ? AxMapWinGIS.tkCursorMode.cmPan
                : AxMapWinGIS.tkCursorMode.cmNone;
        }
    }
}
```

- [ ] **Step 3: Add both files to the project and build**

Add to `Hue_travel.csproj`'s `<ItemGroup>` containing `Form1.cs`:

```xml
<Compile Include="MapEngine\MapHost.cs">
  <SubType>UserControl</SubType>
</Compile>
<Compile Include="MapEngine\MapHost.Designer.cs">
  <DependentUpon>MapHost.cs</DependentUpon>
</Compile>
```

Run:

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Build
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Manually verify Designer compatibility**

Open the project in Visual Studio 2019, double-click `MapHost.cs`, switch to Designer view. Expected: a blank map canvas renders with no Designer-load exception. If the Designer throws a COM error, the most common cause is the `.ocx` not being registered for the **same bitness** as the Designer process (VS2019's Designer typically runs 32-bit) — re-run `regsvr32` for the correct bitness, or re-generate the interop DLLs with `/machine:X86` passed to `TlbImp`/`AxImp` in Task 1 Step 1.

- [ ] **Step 5: Commit**

```bash
git add Hue_travel/MapEngine Hue_travel/Hue_travel.csproj
git commit -m "feat: add MapHost UserControl wrapping AxMap"
```

---

## Task 6: Shell Chrome — TitleBar + RibbonBar

**Files:**
- Create: `Hue_travel\Shell\TitleBar.cs`
- Create: `Hue_travel\Shell\TitleBar.Designer.cs`
- Create: `Hue_travel\Shell\RibbonBar.cs`
- Create: `Hue_travel\Shell\RibbonBar.Designer.cs`

**Interfaces:**
- Consumes: `ThemeColors`, `ThemeFonts` (Task 2).
- Produces: `TitleBar.AuthClicked`, `TitleBar.MinimizeClicked`, `TitleBar.MaximizeClicked`, `TitleBar.CloseClicked` (events, no args). `RibbonBar.LayerControlToggled`, `.RouteOptimizerToggled`, `.SearchPoiToggled`, `.TravelGuideToggled`, `.ZoomInClicked`, `.ZoomOutClicked`, `.BaseMapSwitcherToggled` (events, no args) — all consumed by `MainForm` in Task 13.

- [ ] **Step 1: Create `TitleBar.Designer.cs`**

```csharp
using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class TitleBar
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelRoot;
        private System.Windows.Forms.Label lblLogo;
        private Guna2Button btnAuth;
        private Guna2Button btnMinimize;
        private Guna2Button btnMaximize;
        private Guna2Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelRoot = new Guna2Panel();
            this.lblLogo = new System.Windows.Forms.Label();
            this.btnAuth = new Guna2Button();
            this.btnMinimize = new Guna2Button();
            this.btnMaximize = new Guna2Button();
            this.btnClose = new Guna2Button();
            this.SuspendLayout();

            this.panelRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRoot.FillColor = ThemeColors.SurfaceContainerLow;
            this.panelRoot.BorderRadius = 0;
            this.panelRoot.Controls.Add(this.lblLogo);
            this.panelRoot.Controls.Add(this.btnAuth);
            this.panelRoot.Controls.Add(this.btnMinimize);
            this.panelRoot.Controls.Add(this.btnMaximize);
            this.panelRoot.Controls.Add(this.btnClose);

            this.lblLogo.Text = "Cố Đô GIS";
            this.lblLogo.Font = ThemeFonts.LabelMd;
            this.lblLogo.ForeColor = ThemeColors.OnSurface;
            this.lblLogo.AutoSize = true;
            this.lblLogo.Location = new System.Drawing.Point(12, 10);

            this.btnClose.Size = new System.Drawing.Size(32, 28);
            this.btnClose.Location = new System.Drawing.Point(958, 4);
            this.btnClose.Text = "✕";
            this.btnClose.FillColor = ThemeColors.SurfaceContainerLow;
            this.btnClose.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnClose.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnClose.Click += (s, e) => CloseClicked?.Invoke(this, System.EventArgs.Empty);

            this.btnMaximize.Size = new System.Drawing.Size(32, 28);
            this.btnMaximize.Location = new System.Drawing.Point(922, 4);
            this.btnMaximize.Text = "☐";
            this.btnMaximize.FillColor = ThemeColors.SurfaceContainerLow;
            this.btnMaximize.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnMaximize.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnMaximize.Click += (s, e) => MaximizeClicked?.Invoke(this, System.EventArgs.Empty);

            this.btnMinimize.Size = new System.Drawing.Size(32, 28);
            this.btnMinimize.Location = new System.Drawing.Point(886, 4);
            this.btnMinimize.Text = "—";
            this.btnMinimize.FillColor = ThemeColors.SurfaceContainerLow;
            this.btnMinimize.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnMinimize.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnMinimize.Click += (s, e) => MinimizeClicked?.Invoke(this, System.EventArgs.Empty);

            this.btnAuth.Size = new System.Drawing.Size(170, 26);
            this.btnAuth.Location = new System.Drawing.Point(706, 5);
            this.btnAuth.Text = "Đăng nhập / Đăng ký";
            this.btnAuth.Font = ThemeFonts.LabelSm;
            this.btnAuth.FillColor = ThemeColors.Primary;
            this.btnAuth.ForeColor = ThemeColors.OnPrimary;
            this.btnAuth.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnAuth.Click += (s, e) => AuthClicked?.Invoke(this, System.EventArgs.Empty);

            this.Controls.Add(this.panelRoot);
            this.Name = "TitleBar";
            this.Size = new System.Drawing.Size(1000, 36);
            this.ResumeLayout(false);
        }
    }
}
```

- [ ] **Step 2: Create `TitleBar.cs`**

```csharp
using System;
using System.Windows.Forms;

namespace Hue_travel.Shell
{
    public partial class TitleBar : UserControl
    {
        public event EventHandler AuthClicked;
        public event EventHandler MinimizeClicked;
        public event EventHandler MaximizeClicked;
        public event EventHandler CloseClicked;

        public TitleBar()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 3: Create `RibbonBar.Designer.cs`**

```csharp
using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class RibbonBar
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelRoot;
        private Guna2Button btnLayerControl;
        private Guna2Button btnRouteOptimizer;
        private Guna2Button btnSearchPoi;
        private Guna2Button btnTravelGuide;
        private Guna2Button btnZoomIn;
        private Guna2Button btnZoomOut;
        private Guna2Button btnBaseMap;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private Guna2Button MakeButton(string text, int x)
        {
            var btn = new Guna2Button
            {
                Text = text,
                Location = new System.Drawing.Point(x, 4),
                Size = new System.Drawing.Size(140, 32),
                Font = ThemeFonts.LabelSm,
                FillColor = ThemeColors.SurfaceContainer,
                ForeColor = ThemeColors.Primary,
                BorderColor = ThemeColors.SurfaceContainerHigh,
                BorderThickness = 1,
                BorderRadius = ThemeMetrics.RadiusSm
            };
            return btn;
        }

        private void InitializeComponent()
        {
            this.panelRoot = new Guna2Panel();
            this.SuspendLayout();

            this.panelRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRoot.FillColor = ThemeColors.SurfaceContainerLowest;
            this.panelRoot.BorderRadius = 0;

            this.btnLayerControl = MakeButton("Danh mục địa điểm", 8);
            this.btnLayerControl.Click += (s, e) => LayerControlToggled?.Invoke(this, System.EventArgs.Empty);

            this.btnRouteOptimizer = MakeButton("Tìm đường ngắn nhất", 156);
            this.btnRouteOptimizer.Click += (s, e) => RouteOptimizerToggled?.Invoke(this, System.EventArgs.Empty);

            this.btnSearchPoi = MakeButton("Tra cứu địa điểm", 304);
            this.btnSearchPoi.Click += (s, e) => SearchPoiToggled?.Invoke(this, System.EventArgs.Empty);

            this.btnTravelGuide = MakeButton("Hướng dẫn du lịch", 452);
            this.btnTravelGuide.Click += (s, e) => TravelGuideToggled?.Invoke(this, System.EventArgs.Empty);

            this.btnZoomIn = MakeButton("Phóng to (+)", 608);
            this.btnZoomIn.FillColor = ThemeColors.SurfaceContainerLowest;
            this.btnZoomIn.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnZoomIn.Click += (s, e) => ZoomInClicked?.Invoke(this, System.EventArgs.Empty);

            this.btnZoomOut = MakeButton("Thu nhỏ (-)", 756);
            this.btnZoomOut.FillColor = ThemeColors.SurfaceContainerLowest;
            this.btnZoomOut.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnZoomOut.Click += (s, e) => ZoomOutClicked?.Invoke(this, System.EventArgs.Empty);

            this.btnBaseMap = MakeButton("Bản đồ nền ▾", 904);
            this.btnBaseMap.Click += (s, e) => BaseMapSwitcherToggled?.Invoke(this, System.EventArgs.Empty);

            this.panelRoot.Controls.Add(this.btnLayerControl);
            this.panelRoot.Controls.Add(this.btnRouteOptimizer);
            this.panelRoot.Controls.Add(this.btnSearchPoi);
            this.panelRoot.Controls.Add(this.btnTravelGuide);
            this.panelRoot.Controls.Add(this.btnZoomIn);
            this.panelRoot.Controls.Add(this.btnZoomOut);
            this.panelRoot.Controls.Add(this.btnBaseMap);

            this.Controls.Add(this.panelRoot);
            this.Name = "RibbonBar";
            this.Size = new System.Drawing.Size(1000, 40);
            this.ResumeLayout(false);
        }
    }
}
```

- [ ] **Step 4: Create `RibbonBar.cs`**

```csharp
using System;
using System.Windows.Forms;

namespace Hue_travel.Shell
{
    public partial class RibbonBar : UserControl
    {
        public event EventHandler LayerControlToggled;
        public event EventHandler RouteOptimizerToggled;
        public event EventHandler SearchPoiToggled;
        public event EventHandler TravelGuideToggled;
        public event EventHandler ZoomInClicked;
        public event EventHandler ZoomOutClicked;
        public event EventHandler BaseMapSwitcherToggled;

        public RibbonBar()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 5: Register files in csproj, build, commit**

Add the four new `.cs`/`.Designer.cs` files to `Hue_travel.csproj` (same `<Compile Include>` + `<SubType>UserControl</SubType>` / `<DependentUpon>` pattern as Task 5 Step 3).

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Build
git add Hue_travel/Shell Hue_travel/Hue_travel.csproj
git commit -m "feat: add TitleBar and RibbonBar shell controls"
```

---

## Task 7: Shell Chrome — CategoryRail + StatusBar

**Files:**
- Create: `Hue_travel\Shell\CategoryRail.cs`
- Create: `Hue_travel\Shell\CategoryRail.Designer.cs`
- Create: `Hue_travel\Shell\StatusBar.cs`
- Create: `Hue_travel\Shell\StatusBar.Designer.cs`

**Interfaces:**
- Consumes: `ThemeColors`, `ThemeFonts`, `PoiCategory` (Tasks 2, 3).
- Produces: `CategoryRail.CategorySelected` (`EventHandler<PoiCategory?>`, `null` = "Tất cả") — consumed by Task 13. `StatusBar.SetCoordinates(double lat, double lon)`, `.SetScale(string text)` — consumed by Task 13.

- [ ] **Step 1: Create `CategoryRail.Designer.cs`**

```csharp
using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class CategoryRail
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelRoot;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private Guna2Button MakeRailButton(string text, int y)
        {
            return new Guna2Button
            {
                Text = text,
                Location = new System.Drawing.Point(4, y),
                Size = new System.Drawing.Size(40, 40),
                Font = ThemeFonts.LabelSm,
                FillColor = ThemeColors.SurfaceContainerLowest,
                ForeColor = ThemeColors.OnSurfaceVariant,
                BorderRadius = ThemeMetrics.RadiusLg
            };
        }

        private void InitializeComponent()
        {
            this.panelRoot = new Guna2Panel();
            this.SuspendLayout();

            this.panelRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRoot.FillColor = ThemeColors.SurfaceContainerLowest;

            var btnAll = MakeRailButton("Tất cả", 8);
            btnAll.FillColor = ThemeColors.SurfaceContainer;
            btnAll.ForeColor = ThemeColors.Primary;
            btnAll.Click += (s, e) => CategorySelected?.Invoke(this, null);

            var btnHeritage = MakeRailButton("Di tích", 52);
            btnHeritage.Click += (s, e) => CategorySelected?.Invoke(this, Models.PoiCategory.Heritage);

            var btnFood = MakeRailButton("Ẩm thực", 96);
            btnFood.Click += (s, e) => CategorySelected?.Invoke(this, Models.PoiCategory.Food);

            var btnStay = MakeRailButton("Nghỉ ngơi", 140);
            btnStay.Click += (s, e) => CategorySelected?.Invoke(this, Models.PoiCategory.Stay);

            var btnRiver = MakeRailButton("Sông Hương", 184);
            btnRiver.Click += (s, e) => CategorySelected?.Invoke(this, Models.PoiCategory.River);

            this.panelRoot.Controls.Add(btnAll);
            this.panelRoot.Controls.Add(btnHeritage);
            this.panelRoot.Controls.Add(btnFood);
            this.panelRoot.Controls.Add(btnStay);
            this.panelRoot.Controls.Add(btnRiver);

            this.Controls.Add(this.panelRoot);
            this.Name = "CategoryRail";
            this.Size = new System.Drawing.Size(48, 700);
            this.ResumeLayout(false);
        }
    }
}
```

- [ ] **Step 2: Create `CategoryRail.cs`**

```csharp
using System;
using System.Windows.Forms;
using Hue_travel.Models;

namespace Hue_travel.Shell
{
    public partial class CategoryRail : UserControl
    {
        public event EventHandler<PoiCategory?> CategorySelected;

        public CategoryRail()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 3: Create `StatusBar.Designer.cs`**

```csharp
using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class StatusBar
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelRoot;
        private System.Windows.Forms.Label lblCoordinates;
        private System.Windows.Forms.Label lblScale;
        private System.Windows.Forms.Label lblWeather;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelRoot = new Guna2Panel();
            this.lblCoordinates = new System.Windows.Forms.Label();
            this.lblScale = new System.Windows.Forms.Label();
            this.lblWeather = new System.Windows.Forms.Label();
            this.SuspendLayout();

            this.panelRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRoot.FillColor = ThemeColors.SurfaceContainerLowest;

            this.lblCoordinates.Font = ThemeFonts.BodySm;
            this.lblCoordinates.ForeColor = ThemeColors.OnSurfaceVariant;
            this.lblCoordinates.AutoSize = true;
            this.lblCoordinates.Location = new System.Drawing.Point(12, 7);
            this.lblCoordinates.Text = "Khu vực đang xem: Trung tâm Cố đô Huế & Bờ sông Hương";

            this.lblWeather.Font = ThemeFonts.BodySm;
            this.lblWeather.ForeColor = ThemeColors.OnSurfaceVariant;
            this.lblWeather.AutoSize = true;
            this.lblWeather.Location = new System.Drawing.Point(560, 7);
            this.lblWeather.Text = "Thời tiết hôm nay: Nắng đẹp, 26°C";

            this.lblScale.Font = ThemeFonts.BodySm;
            this.lblScale.ForeColor = ThemeColors.OnSurfaceVariant;
            this.lblScale.AutoSize = true;
            this.lblScale.Location = new System.Drawing.Point(900, 7);
            this.lblScale.Text = "Tỉ lệ: 1:15,000";

            this.panelRoot.Controls.Add(this.lblCoordinates);
            this.panelRoot.Controls.Add(this.lblWeather);
            this.panelRoot.Controls.Add(this.lblScale);

            this.Controls.Add(this.panelRoot);
            this.Name = "StatusBar";
            this.Size = new System.Drawing.Size(1000, 28);
            this.ResumeLayout(false);
        }
    }
}
```

- [ ] **Step 4: Create `StatusBar.cs`**

```csharp
using System.Windows.Forms;

namespace Hue_travel.Shell
{
    public partial class StatusBar : UserControl
    {
        public StatusBar()
        {
            InitializeComponent();
        }

        public void SetCoordinates(double lat, double lon)
        {
            lblCoordinates.Text = $"Tọa độ: {lat:F5}, {lon:F5}";
        }

        public void SetScale(string text)
        {
            lblScale.Text = $"Tỉ lệ: {text}";
        }
    }
}
```

- [ ] **Step 5: Register files in csproj, build, commit**

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Build
git add Hue_travel/Shell Hue_travel/Hue_travel.csproj
git commit -m "feat: add CategoryRail and StatusBar shell controls"
```

---

## Task 8: Panels — LayerControlPanel + BaseMapSwitcherPopup

**Files:**
- Create: `Hue_travel\Panels\LayerControl\LayerControlPanel.cs`
- Create: `Hue_travel\Panels\LayerControl\LayerControlPanel.Designer.cs`
- Create: `Hue_travel\Panels\BaseMapSwitcher\BaseMapSwitcherPopup.cs`
- Create: `Hue_travel\Panels\BaseMapSwitcher\BaseMapSwitcherPopup.Designer.cs`

**Interfaces:**
- Consumes: `ThemeColors`, `ThemeFonts`, `ThemeMetrics` (Task 2).
- Produces: `LayerControlPanel.CloseRequested` (`EventHandler`), `BaseMapSwitcherPopup.BaseMapSelected` (`EventHandler<string>`) — consumed by Task 13.

- [ ] **Step 1: Create `LayerControlPanel.Designer.cs`**

```csharp
using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.LayerControl
{
    partial class LayerControlPanel
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private Guna2Button btnClose;
        private Guna2Panel panelBody;
        private Guna2CheckBox chkHeritage;
        private Guna2CheckBox chkFood;
        private Guna2CheckBox chkStay;
        private Guna2CheckBox chkRoads;
        private Guna2CheckBox chkRiver;
        private Guna2CheckBox chkPiers;
        private Guna2TrackBar sliderBrightness;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private Guna2CheckBox MakeLayerCheck(string text, int y, bool @checked)
        {
            return new Guna2CheckBox
            {
                Text = text,
                Checked = @checked,
                Location = new System.Drawing.Point(8, y),
                Size = new System.Drawing.Size(270, 22),
                Font = ThemeFonts.BodySm,
                CheckedState = { BorderColor = ThemeColors.Primary, FillColor = ThemeColors.Primary }
            };
        }

        private void InitializeComponent()
        {
            this.panelHeader = new Guna2Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnClose = new Guna2Button();
            this.panelBody = new Guna2Panel();
            this.sliderBrightness = new Guna2TrackBar();
            this.SuspendLayout();

            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.panelHeader.FillColor = ThemeColors.SurfaceContainer;
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 32;

            this.lblTitle.Text = "Danh Mục Địa Điểm";
            this.lblTitle.Font = ThemeFonts.LabelMd;
            this.lblTitle.ForeColor = ThemeColors.OnSurface;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(8, 8);

            this.btnClose.Text = "✕";
            this.btnClose.Size = new System.Drawing.Size(20, 20);
            this.btnClose.Location = new System.Drawing.Point(272, 6);
            this.btnClose.FillColor = ThemeColors.SurfaceContainer;
            this.btnClose.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnClose.Click += (s, e) => CloseRequested?.Invoke(this, System.EventArgs.Empty);

            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.btnClose);

            this.panelBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBody.FillColor = ThemeColors.SurfaceContainerLowest;

            this.chkHeritage = MakeLayerCheck("Di tích lịch sử & Điểm tham quan (38 điểm)", 8, true);
            this.chkFood = MakeLayerCheck("Món ngon & Quán đặc sản Huế (64 quán)", 36, true);
            this.chkStay = MakeLayerCheck("Khách sạn & Nơi lưu trú (52 nơi)", 64, true);
            this.chkRoads = MakeLayerCheck("Tuyến đường chính & Cầu qua sông", 92, true);
            this.chkRiver = MakeLayerCheck("Sông Hương & Các hồ cảnh quan", 120, true);
            this.chkPiers = MakeLayerCheck("Bến thuyền rồng & Điểm xe buýt", 148, false);

            this.sliderBrightness.Location = new System.Drawing.Point(8, 190);
            this.sliderBrightness.Size = new System.Drawing.Size(270, 24);
            this.sliderBrightness.Minimum = 20;
            this.sliderBrightness.Maximum = 100;
            this.sliderBrightness.Value = 85;

            this.panelBody.Controls.Add(this.chkHeritage);
            this.panelBody.Controls.Add(this.chkFood);
            this.panelBody.Controls.Add(this.chkStay);
            this.panelBody.Controls.Add(this.chkRoads);
            this.panelBody.Controls.Add(this.chkRiver);
            this.panelBody.Controls.Add(this.chkPiers);
            this.panelBody.Controls.Add(this.sliderBrightness);

            this.Controls.Add(this.panelBody);
            this.Controls.Add(this.panelHeader);
            this.Name = "LayerControlPanel";
            this.Size = new System.Drawing.Size(300, 300);
            this.ResumeLayout(false);
        }
    }
}
```

- [ ] **Step 2: Create `LayerControlPanel.cs`**

```csharp
using System;
using System.Windows.Forms;

namespace Hue_travel.Panels.LayerControl
{
    public partial class LayerControlPanel : UserControl
    {
        public event EventHandler CloseRequested;

        public LayerControlPanel()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 3: Create `BaseMapSwitcherPopup.Designer.cs`**

```csharp
using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.BaseMapSwitcher
{
    partial class BaseMapSwitcherPopup
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Button btnStandard;
        private Guna2Button btnSatellite;
        private Guna2Button btnNight;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnStandard = new Guna2Button();
            this.btnSatellite = new Guna2Button();
            this.btnNight = new Guna2Button();
            this.SuspendLayout();

            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.btnStandard.Text = "Chuẩn sáng";
            this.btnStandard.Location = new System.Drawing.Point(8, 8);
            this.btnStandard.Size = new System.Drawing.Size(80, 48);
            this.btnStandard.FillColor = ThemeColors.SurfaceContainer;
            this.btnStandard.ForeColor = ThemeColors.Primary;
            this.btnStandard.Click += (s, e) => BaseMapSelected?.Invoke(this, "standard");

            this.btnSatellite.Text = "Vệ tinh";
            this.btnSatellite.Location = new System.Drawing.Point(96, 8);
            this.btnSatellite.Size = new System.Drawing.Size(80, 48);
            this.btnSatellite.FillColor = ThemeColors.SurfaceContainerLowest;
            this.btnSatellite.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnSatellite.Click += (s, e) => BaseMapSelected?.Invoke(this, "satellite");

            this.btnNight.Text = "Ngắm đêm";
            this.btnNight.Location = new System.Drawing.Point(184, 8);
            this.btnNight.Size = new System.Drawing.Size(80, 48);
            this.btnNight.FillColor = ThemeColors.SurfaceContainerLowest;
            this.btnNight.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnNight.Click += (s, e) => BaseMapSelected?.Invoke(this, "night");

            this.Controls.Add(this.btnStandard);
            this.Controls.Add(this.btnSatellite);
            this.Controls.Add(this.btnNight);
            this.Name = "BaseMapSwitcherPopup";
            this.Size = new System.Drawing.Size(272, 64);
            this.ResumeLayout(false);
        }
    }
}
```

- [ ] **Step 4: Create `BaseMapSwitcherPopup.cs`**

```csharp
using System;
using System.Windows.Forms;

namespace Hue_travel.Panels.BaseMapSwitcher
{
    public partial class BaseMapSwitcherPopup : UserControl
    {
        public event EventHandler<string> BaseMapSelected;

        public BaseMapSwitcherPopup()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 5: Register files, build, commit**

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Build
git add Hue_travel/Panels/LayerControl Hue_travel/Panels/BaseMapSwitcher Hue_travel/Hue_travel.csproj
git commit -m "feat: add LayerControlPanel and BaseMapSwitcherPopup"
```

---

## Task 9: Panels — SearchPoiPanel

**Files:**
- Create: `Hue_travel\Panels\SearchPoi\PoiSearchFilter.cs`
- Create: `Hue_travel\Panels\SearchPoi\SearchPoiPanel.cs`
- Create: `Hue_travel\Panels\SearchPoi\SearchPoiPanel.Designer.cs`
- Test: `Hue_travel.Tests\Panels\SearchPoi\PoiSearchFilterTests.cs`

**Interfaces:**
- Consumes: `Poi`, `PoiCategory` (Task 3), `SamplePois` (Task 4).
- Produces: `PoiSearchFilter.Apply(IEnumerable<Poi> source, string keyword, PoiCategory? category)` → `IReadOnlyList<Poi>` (pure, no UI dependency) — consumed by `SearchPoiPanel` in this task and directly unit tested. `SearchPoiPanel.PoiSelected` (`EventHandler<Poi>`) — consumed by Task 13.

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Collections.Generic;
using FluentAssertions;
using Hue_travel.Models;
using Hue_travel.Panels.SearchPoi;
using Xunit;

namespace Hue_travel.Tests.Panels.SearchPoi
{
    public class PoiSearchFilterTests
    {
        private static List<Poi> SamplePois() => new List<Poi>
        {
            new Poi { Id = "a", Name = "Đại Nội - Hoàng Thành Huế", Category = PoiCategory.Heritage },
            new Poi { Id = "b", Name = "Chùa Thiên Mụ", Category = PoiCategory.Heritage },
            new Poi { Id = "c", Name = "Bún Bò Huế Bà Tuyết", Category = PoiCategory.Food },
        };

        [Fact]
        public void Apply_EmptyKeywordAndNullCategory_ReturnsAllItems()
        {
            var result = PoiSearchFilter.Apply(SamplePois(), "", null);
            result.Should().HaveCount(3);
        }

        [Fact]
        public void Apply_KeywordMatchesCaseInsensitiveSubstring()
        {
            var result = PoiSearchFilter.Apply(SamplePois(), "bún bò", null);
            result.Should().ContainSingle(p => p.Id == "c");
        }

        [Fact]
        public void Apply_CategoryFilterAloneNarrowsResults()
        {
            var result = PoiSearchFilter.Apply(SamplePois(), "", PoiCategory.Heritage);
            result.Should().HaveCount(2);
        }

        [Fact]
        public void Apply_KeywordWithNoMatches_ReturnsEmpty_NotAll()
        {
            var result = PoiSearchFilter.Apply(SamplePois(), "không tồn tại", null);
            result.Should().BeEmpty();
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Hue_travel.Tests --filter PoiSearchFilterTests`
Expected: FAIL — `PoiSearchFilter` does not exist yet.

- [ ] **Step 3: Implement `PoiSearchFilter.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Hue_travel.Models;

namespace Hue_travel.Panels.SearchPoi
{
    /// <summary>Pure filtering logic, deliberately separated from the UserControl so it is unit-testable without a WinForms message loop.</summary>
    public static class PoiSearchFilter
    {
        public static IReadOnlyList<Poi> Apply(IEnumerable<Poi> source, string keyword, PoiCategory? category)
        {
            var query = source ?? Enumerable.Empty<Poi>();

            if (category.HasValue)
            {
                query = query.Where(p => p.Category == category.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return query.ToList();
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Hue_travel.Tests --filter PoiSearchFilterTests`
Expected: PASS (all 4 tests).

- [ ] **Step 5: Create `SearchPoiPanel.Designer.cs`**

```csharp
using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.SearchPoi
{
    partial class SearchPoiPanel
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private Guna2Button btnClose;
        private Guna2TextBox txtKeyword;
        private System.Windows.Forms.ListBox lstResults;
        private System.Windows.Forms.Label lblResultCount;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelHeader = new Guna2Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnClose = new Guna2Button();
            this.txtKeyword = new Guna2TextBox();
            this.lstResults = new System.Windows.Forms.ListBox();
            this.lblResultCount = new System.Windows.Forms.Label();
            this.SuspendLayout();

            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 32;
            this.panelHeader.FillColor = ThemeColors.Primary;

            this.lblTitle.Text = "Tra Cứu Địa Điểm Du Lịch & Ẩm Thực Huế";
            this.lblTitle.Font = ThemeFonts.LabelMd;
            this.lblTitle.ForeColor = ThemeColors.OnPrimary;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(8, 8);

            this.btnClose.Text = "✕";
            this.btnClose.Size = new System.Drawing.Size(20, 20);
            this.btnClose.Location = new System.Drawing.Point(356, 6);
            this.btnClose.FillColor = ThemeColors.Primary;
            this.btnClose.ForeColor = ThemeColors.OnPrimary;
            this.btnClose.Click += (s, e) => this.Hide();

            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.btnClose);

            this.txtKeyword.PlaceholderText = "Tìm kiếm: Bún bò, Chùa, Lăng tẩm, Khách sạn...";
            this.txtKeyword.Location = new System.Drawing.Point(8, 40);
            this.txtKeyword.Size = new System.Drawing.Size(368, 32);
            this.txtKeyword.Font = ThemeFonts.BodySm;
            this.txtKeyword.TextChanged += (s, e) => RefreshResults();

            this.lblResultCount.Font = ThemeFonts.BodySm;
            this.lblResultCount.ForeColor = ThemeColors.OnSurfaceVariant;
            this.lblResultCount.AutoSize = true;
            this.lblResultCount.Location = new System.Drawing.Point(8, 78);

            this.lstResults.Font = ThemeFonts.BodySm;
            this.lstResults.Location = new System.Drawing.Point(8, 100);
            this.lstResults.Size = new System.Drawing.Size(368, 220);
            this.lstResults.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstResults.SelectedIndexChanged += (s, e) => OnResultSelected();

            this.Controls.Add(this.lstResults);
            this.Controls.Add(this.lblResultCount);
            this.Controls.Add(this.txtKeyword);
            this.Controls.Add(this.panelHeader);
            this.Name = "SearchPoiPanel";
            this.Size = new System.Drawing.Size(384, 340);
            this.ResumeLayout(false);
        }
    }
}
```

- [ ] **Step 6: Create `SearchPoiPanel.cs`**

```csharp
using System;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.SampleData;

namespace Hue_travel.Panels.SearchPoi
{
    public partial class SearchPoiPanel : UserControl
    {
        public event EventHandler<Poi> PoiSelected;

        private System.Collections.Generic.IReadOnlyList<Poi> currentResults;

        public SearchPoiPanel()
        {
            InitializeComponent();
            RefreshResults();
        }

        private void RefreshResults()
        {
            currentResults = PoiSearchFilter.Apply(SamplePois.All, txtKeyword.Text, null);

            lstResults.Items.Clear();
            foreach (var poi in currentResults)
            {
                lstResults.Items.Add(poi.Name);
            }

            lblResultCount.Text = $"Kết quả tìm kiếm: {currentResults.Count} địa điểm";
        }

        private void OnResultSelected()
        {
            if (lstResults.SelectedIndex < 0 || currentResults == null) return;
            PoiSelected?.Invoke(this, currentResults[lstResults.SelectedIndex]);
        }
    }
}
```

- [ ] **Step 7: Register files, build, commit**

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Build
dotnet test Hue_travel.Tests --filter PoiSearchFilterTests
git add Hue_travel/Panels/SearchPoi Hue_travel.Tests/Panels/SearchPoi Hue_travel/Hue_travel.csproj
git commit -m "feat: add SearchPoiPanel with tested filter logic"
```

---

## Task 10: Panels — RouteOptimizerPanel

**Files:**
- Create: `Hue_travel\Panels\RouteOptimizer\RouteSelection.cs`
- Create: `Hue_travel\Panels\RouteOptimizer\RouteOptimizerPanel.cs`
- Create: `Hue_travel\Panels\RouteOptimizer\RouteOptimizerPanel.Designer.cs`
- Test: `Hue_travel.Tests\Panels\RouteOptimizer\RouteSelectionTests.cs`

**Interfaces:**
- Consumes: `Poi` (Task 3), `SamplePois` (Task 4).
- Produces: `RouteSelection.BuildOrderedSummary(IReadOnlyList<Poi> selectedInClickOrder)` → `IReadOnlyList<(int Order, Poi Poi)>` (pure) — consumed by `RouteOptimizerPanel` and unit tested directly.

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Collections.Generic;
using FluentAssertions;
using Hue_travel.Models;
using Hue_travel.Panels.RouteOptimizer;
using Xunit;

namespace Hue_travel.Tests.Panels.RouteOptimizer
{
    public class RouteSelectionTests
    {
        private static Poi P(string id) => new Poi { Id = id, Name = id };

        [Fact]
        public void BuildOrderedSummary_NumbersSequentiallyFromOne()
        {
            var selected = new List<Poi> { P("a"), P("b"), P("c") };

            var summary = RouteSelection.BuildOrderedSummary(selected);

            summary.Should().HaveCount(3);
            summary[0].Order.Should().Be(1);
            summary[1].Order.Should().Be(2);
            summary[2].Order.Should().Be(3);
        }

        [Fact]
        public void BuildOrderedSummary_AfterRemovingMiddleItem_RenumbersWithoutGaps()
        {
            var selected = new List<Poi> { P("a"), P("c") }; // "b" was unchecked

            var summary = RouteSelection.BuildOrderedSummary(selected);

            summary[0].Order.Should().Be(1);
            summary[0].Poi.Id.Should().Be("a");
            summary[1].Order.Should().Be(2);
            summary[1].Poi.Id.Should().Be("c");
        }

        [Fact]
        public void BuildOrderedSummary_EmptySelection_ReturnsEmpty()
        {
            RouteSelection.BuildOrderedSummary(new List<Poi>()).Should().BeEmpty();
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Hue_travel.Tests --filter RouteSelectionTests`
Expected: FAIL — `RouteSelection` does not exist yet.

- [ ] **Step 3: Implement `RouteSelection.cs`**

```csharp
using System.Collections.Generic;
using System.Linq;
using Hue_travel.Models;

namespace Hue_travel.Panels.RouteOptimizer
{
    /// <summary>Pure renumbering logic, separated from the UserControl so it is unit-testable. "Order" always reflects current list position (1..N), never the original click order with gaps.</summary>
    public static class RouteSelection
    {
        public static IReadOnlyList<(int Order, Poi Poi)> BuildOrderedSummary(IReadOnlyList<Poi> selectedInClickOrder)
        {
            return selectedInClickOrder
                .Select((poi, index) => (Order: index + 1, Poi: poi))
                .ToList();
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Hue_travel.Tests --filter RouteSelectionTests`
Expected: PASS (all 3 tests).

- [ ] **Step 5: Create `RouteOptimizerPanel.Designer.cs`**

```csharp
using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.RouteOptimizer
{
    partial class RouteOptimizerPanel
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private Guna2Button btnClose;
        private System.Windows.Forms.ComboBox cboStartPoint;
        private System.Windows.Forms.CheckedListBox clbDestinations;
        private System.Windows.Forms.Label lblSelectedCount;
        private Guna2Button btnFindRoute;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelHeader = new Guna2Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnClose = new Guna2Button();
            this.cboStartPoint = new System.Windows.Forms.ComboBox();
            this.clbDestinations = new System.Windows.Forms.CheckedListBox();
            this.lblSelectedCount = new System.Windows.Forms.Label();
            this.btnFindRoute = new Guna2Button();
            this.SuspendLayout();

            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 32;
            this.panelHeader.FillColor = ThemeColors.Primary;

            this.lblTitle.Text = "Lên Lộ Trình Tham Quan Tự Động";
            this.lblTitle.Font = ThemeFonts.LabelMd;
            this.lblTitle.ForeColor = ThemeColors.OnPrimary;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(8, 8);

            this.btnClose.Text = "✕";
            this.btnClose.Size = new System.Drawing.Size(20, 20);
            this.btnClose.Location = new System.Drawing.Point(356, 6);
            this.btnClose.FillColor = ThemeColors.Primary;
            this.btnClose.ForeColor = ThemeColors.OnPrimary;
            this.btnClose.Click += (s, e) => this.Hide();

            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.btnClose);

            this.cboStartPoint.Location = new System.Drawing.Point(8, 40);
            this.cboStartPoint.Size = new System.Drawing.Size(368, 24);
            this.cboStartPoint.Font = ThemeFonts.BodySm;
            this.cboStartPoint.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            this.lblSelectedCount.Font = ThemeFonts.LabelSm;
            this.lblSelectedCount.ForeColor = ThemeColors.Primary;
            this.lblSelectedCount.AutoSize = true;
            this.lblSelectedCount.Location = new System.Drawing.Point(8, 72);

            this.clbDestinations.Font = ThemeFonts.BodySm;
            this.clbDestinations.Location = new System.Drawing.Point(8, 94);
            this.clbDestinations.Size = new System.Drawing.Size(368, 150);
            this.clbDestinations.CheckOnClick = true;
            this.clbDestinations.ItemCheck += (s, e) => BeginInvoke((System.Action)RefreshSelectionSummary);

            this.btnFindRoute.Text = "Bắt Đầu Tìm Đường Ngắn Nhất";
            this.btnFindRoute.Location = new System.Drawing.Point(8, 252);
            this.btnFindRoute.Size = new System.Drawing.Size(368, 32);
            this.btnFindRoute.FillColor = ThemeColors.Primary;
            this.btnFindRoute.ForeColor = ThemeColors.OnPrimary;
            this.btnFindRoute.Font = ThemeFonts.LabelSm;

            this.Controls.Add(this.btnFindRoute);
            this.Controls.Add(this.clbDestinations);
            this.Controls.Add(this.lblSelectedCount);
            this.Controls.Add(this.cboStartPoint);
            this.Controls.Add(this.panelHeader);
            this.Name = "RouteOptimizerPanel";
            this.Size = new System.Drawing.Size(384, 300);
            this.ResumeLayout(false);
        }
    }
}
```

- [ ] **Step 6: Create `RouteOptimizerPanel.cs`**

```csharp
using System.Linq;
using System.Windows.Forms;
using Hue_travel.SampleData;

namespace Hue_travel.Panels.RouteOptimizer
{
    public partial class RouteOptimizerPanel : UserControl
    {
        public RouteOptimizerPanel()
        {
            InitializeComponent();

            foreach (var poi in SamplePois.All)
            {
                cboStartPoint.Items.Add(poi.Name);
                clbDestinations.Items.Add(poi.Name);
            }
            if (cboStartPoint.Items.Count > 0) cboStartPoint.SelectedIndex = 0;

            RefreshSelectionSummary();
        }

        private void RefreshSelectionSummary()
        {
            var selected = SamplePois.All
                .Where((poi, index) => index < clbDestinations.CheckedIndices.Count &&
                                        clbDestinations.CheckedIndices.Contains(index))
                .ToList();

            var summary = RouteSelection.BuildOrderedSummary(selected);
            lblSelectedCount.Text = $"Đã chọn {summary.Count} điểm";
        }
    }
}
```

- [ ] **Step 7: Register files, build, commit**

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Build
dotnet test Hue_travel.Tests --filter RouteSelectionTests
git add Hue_travel/Panels/RouteOptimizer Hue_travel.Tests/Panels/RouteOptimizer Hue_travel/Hue_travel.csproj
git commit -m "feat: add RouteOptimizerPanel with tested selection ordering"
```

---

## Task 11: Panels — TravelGuidePanel + PoiIdentifyCard

**Files:**
- Create: `Hue_travel\Panels\TravelGuide\TravelGuidePanel.cs`
- Create: `Hue_travel\Panels\TravelGuide\TravelGuidePanel.Designer.cs`
- Create: `Hue_travel\Panels\PoiIdentify\PoiIdentifyCard.cs`
- Create: `Hue_travel\Panels\PoiIdentify\PoiIdentifyCard.Designer.cs`

**Interfaces:**
- Consumes: `Poi`, `TourSuggestion` (Task 3), `SampleTours` (Task 4).
- Produces: `PoiIdentifyCard.ShowPoi(Poi poi)` (public method) — consumed by Task 13 when a map marker or search result is clicked. `TravelGuidePanel.CloseRequested` (`EventHandler`).

- [ ] **Step 1: Create `TravelGuidePanel.Designer.cs`**

```csharp
using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.TravelGuide
{
    partial class TravelGuidePanel
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private Guna2Button btnClose;
        private System.Windows.Forms.FlowLayoutPanel flowTours;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelHeader = new Guna2Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnClose = new Guna2Button();
            this.flowTours = new System.Windows.Forms.FlowLayoutPanel();
            this.SuspendLayout();

            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 32;
            this.panelHeader.FillColor = ThemeColors.Primary;

            this.lblTitle.Text = "Cẩm Nang & Hướng Dẫn Du Lịch Huế";
            this.lblTitle.Font = ThemeFonts.LabelMd;
            this.lblTitle.ForeColor = ThemeColors.OnPrimary;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(8, 8);

            this.btnClose.Text = "✕";
            this.btnClose.Size = new System.Drawing.Size(20, 20);
            this.btnClose.Location = new System.Drawing.Point(356, 6);
            this.btnClose.FillColor = ThemeColors.Primary;
            this.btnClose.ForeColor = ThemeColors.OnPrimary;
            this.btnClose.Click += (s, e) => CloseRequested?.Invoke(this, System.EventArgs.Empty);

            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.btnClose);

            this.flowTours.Location = new System.Drawing.Point(8, 40);
            this.flowTours.Size = new System.Drawing.Size(368, 260);
            this.flowTours.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowTours.WrapContents = false;
            this.flowTours.AutoScroll = true;

            this.Controls.Add(this.flowTours);
            this.Controls.Add(this.panelHeader);
            this.Name = "TravelGuidePanel";
            this.Size = new System.Drawing.Size(384, 320);
            this.ResumeLayout(false);
        }
    }
}
```

- [ ] **Step 2: Create `TravelGuidePanel.cs`**

```csharp
using System;
using System.Windows.Forms;
using Hue_travel.SampleData;
using Hue_travel.Theme;

namespace Hue_travel.Panels.TravelGuide
{
    public partial class TravelGuidePanel : UserControl
    {
        public event EventHandler CloseRequested;

        public TravelGuidePanel()
        {
            InitializeComponent();
            PopulateTours();
        }

        private void PopulateTours()
        {
            foreach (var tour in SampleTours.All)
            {
                var card = new Panel
                {
                    Width = 352,
                    Height = 60,
                    Margin = new Padding(0, 0, 0, 8),
                    BackColor = ThemeColors.SurfaceContainerLow
                };

                var title = new Label
                {
                    Text = $"{tour.Title}  [{tour.Badge}]",
                    Font = ThemeFonts.LabelMd,
                    ForeColor = ThemeColors.OnSurface,
                    AutoSize = false,
                    Width = 340,
                    Location = new System.Drawing.Point(8, 6)
                };

                var desc = new Label
                {
                    Text = tour.Description,
                    Font = ThemeFonts.BodySm,
                    ForeColor = ThemeColors.OnSurfaceVariant,
                    AutoSize = false,
                    Width = 340,
                    Height = 32,
                    Location = new System.Drawing.Point(8, 26)
                };

                card.Controls.Add(title);
                card.Controls.Add(desc);
                flowTours.Controls.Add(card);
            }
        }
    }
}
```

- [ ] **Step 3: Create `PoiIdentifyCard.Designer.cs`**

```csharp
using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.PoiIdentify
{
    partial class PoiIdentifyCard
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelHeader;
        private System.Windows.Forms.Label lblBadge;
        private Guna2Button btnClose;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblAddress;
        private System.Windows.Forms.Label lblHours;
        private System.Windows.Forms.Label lblPrice;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelHeader = new Guna2Panel();
            this.lblBadge = new System.Windows.Forms.Label();
            this.btnClose = new Guna2Button();
            this.lblName = new System.Windows.Forms.Label();
            this.lblAddress = new System.Windows.Forms.Label();
            this.lblHours = new System.Windows.Forms.Label();
            this.lblPrice = new System.Windows.Forms.Label();
            this.SuspendLayout();

            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 32;
            this.panelHeader.FillColor = ThemeColors.SurfaceContainerLow;

            this.lblBadge.Font = ThemeFonts.LabelSm;
            this.lblBadge.ForeColor = ThemeColors.OnPrimary;
            this.lblBadge.BackColor = ThemeColors.BadgeAmber;
            this.lblBadge.AutoSize = true;
            this.lblBadge.Location = new System.Drawing.Point(8, 6);

            this.btnClose.Text = "✕";
            this.btnClose.Size = new System.Drawing.Size(20, 20);
            this.btnClose.Location = new System.Drawing.Point(512, 6);
            this.btnClose.FillColor = ThemeColors.SurfaceContainerLow;
            this.btnClose.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnClose.Click += (s, e) => this.Hide();

            this.panelHeader.Controls.Add(this.lblBadge);
            this.panelHeader.Controls.Add(this.btnClose);

            this.lblName.Font = ThemeFonts.HeadlineMd;
            this.lblName.ForeColor = ThemeColors.OnSurface;
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(8, 42);

            this.lblAddress.Font = ThemeFonts.BodySm;
            this.lblAddress.ForeColor = ThemeColors.OnSurfaceVariant;
            this.lblAddress.AutoSize = true;
            this.lblAddress.Location = new System.Drawing.Point(8, 64);

            this.lblHours.Font = ThemeFonts.BodySm;
            this.lblHours.ForeColor = ThemeColors.OnSurfaceVariant;
            this.lblHours.AutoSize = true;
            this.lblHours.Location = new System.Drawing.Point(8, 86);

            this.lblPrice.Font = ThemeFonts.BodySm;
            this.lblPrice.ForeColor = ThemeColors.Primary;
            this.lblPrice.AutoSize = true;
            this.lblPrice.Location = new System.Drawing.Point(8, 108);

            this.Controls.Add(this.lblPrice);
            this.Controls.Add(this.lblHours);
            this.Controls.Add(this.lblAddress);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.panelHeader);
            this.Name = "PoiIdentifyCard";
            this.Size = new System.Drawing.Size(540, 140);
            this.ResumeLayout(false);
        }
    }
}
```

- [ ] **Step 4: Create `PoiIdentifyCard.cs`**

```csharp
using System.Windows.Forms;
using Hue_travel.Models;

namespace Hue_travel.Panels.PoiIdentify
{
    public partial class PoiIdentifyCard : UserControl
    {
        public PoiIdentifyCard()
        {
            InitializeComponent();
        }

        public void ShowPoi(Poi poi)
        {
            lblBadge.Text = poi.Category == PoiCategory.Heritage ? "Di tích cấp Quốc gia đặc biệt" : poi.Category.ToString();
            lblName.Text = poi.Name;
            lblAddress.Text = poi.Address;
            lblHours.Text = $"Giờ mở cửa: {poi.OpeningHours}";
            lblPrice.Text = $"Giá vé: {poi.TicketPrice}";
            Visible = true;
            BringToFront();
        }
    }
}
```

- [ ] **Step 5: Register files, build, commit**

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Build
git add Hue_travel/Panels/TravelGuide Hue_travel/Panels/PoiIdentify Hue_travel/Hue_travel.csproj
git commit -m "feat: add TravelGuidePanel and PoiIdentifyCard"
```

---

## Task 12: Auth — AuthModal

**Files:**
- Create: `Hue_travel\Auth\AuthModal.cs`
- Create: `Hue_travel\Auth\AuthModal.Designer.cs`

**Interfaces:**
- Produces: `AuthModal.CloseRequested` (`EventHandler`) — consumed by Task 13. UI-only: login/register/forgot-password are three stacked panels switched by tab buttons; no fields actually submit anywhere (per spec §2 non-goals).

- [ ] **Step 1: Create `AuthModal.Designer.cs`**

```csharp
using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Auth
{
    partial class AuthModal
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelBackdrop;
        private Guna2Panel panelCard;
        private Guna2Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private Guna2Button btnClose;
        private Guna2TextBox txtEmail;
        private Guna2TextBox txtPassword;
        private Guna2Button btnSubmit;
        private System.Windows.Forms.LinkLabel lnkForgotPassword;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelBackdrop = new Guna2Panel();
            this.panelCard = new Guna2Panel();
            this.panelHeader = new Guna2Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnClose = new Guna2Button();
            this.txtEmail = new Guna2TextBox();
            this.txtPassword = new Guna2TextBox();
            this.btnSubmit = new Guna2Button();
            this.lnkForgotPassword = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();

            this.panelBackdrop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBackdrop.FillColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.panelBackdrop.Click += (s, e) => CloseRequested?.Invoke(this, System.EventArgs.Empty);

            this.panelCard.Size = new System.Drawing.Size(420, 360);
            this.panelCard.FillColor = ThemeColors.SurfaceContainerLowest;
            this.panelCard.BorderRadius = ThemeMetrics.RadiusLg;
            this.panelCard.Anchor = System.Windows.Forms.AnchorStyles.None;

            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 36;
            this.panelHeader.FillColor = ThemeColors.Primary;

            this.lblTitle.Text = "Đăng Nhập — Cố Đô GIS";
            this.lblTitle.Font = ThemeFonts.LabelMd;
            this.lblTitle.ForeColor = ThemeColors.OnPrimary;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(12, 10);

            this.btnClose.Text = "✕";
            this.btnClose.Size = new System.Drawing.Size(24, 24);
            this.btnClose.Location = new System.Drawing.Point(384, 6);
            this.btnClose.FillColor = ThemeColors.Primary;
            this.btnClose.ForeColor = ThemeColors.OnPrimary;
            this.btnClose.Click += (s, e) => CloseRequested?.Invoke(this, System.EventArgs.Empty);

            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.btnClose);

            this.txtEmail.PlaceholderText = "Email đã đăng ký";
            this.txtEmail.Location = new System.Drawing.Point(24, 64);
            this.txtEmail.Size = new System.Drawing.Size(372, 32);

            this.txtPassword.PlaceholderText = "Mật khẩu";
            this.txtPassword.PasswordChar = '•';
            this.txtPassword.Location = new System.Drawing.Point(24, 108);
            this.txtPassword.Size = new System.Drawing.Size(372, 32);

            this.lnkForgotPassword.Text = "Quên mật khẩu?";
            this.lnkForgotPassword.AutoSize = true;
            this.lnkForgotPassword.Location = new System.Drawing.Point(24, 148);

            this.btnSubmit.Text = "Đăng Nhập";
            this.btnSubmit.Location = new System.Drawing.Point(24, 180);
            this.btnSubmit.Size = new System.Drawing.Size(372, 36);
            this.btnSubmit.FillColor = ThemeColors.Primary;
            this.btnSubmit.ForeColor = ThemeColors.OnPrimary;
            this.btnSubmit.Font = ThemeFonts.LabelMd;

            this.panelCard.Controls.Add(this.btnSubmit);
            this.panelCard.Controls.Add(this.lnkForgotPassword);
            this.panelCard.Controls.Add(this.txtPassword);
            this.panelCard.Controls.Add(this.txtEmail);
            this.panelCard.Controls.Add(this.panelHeader);

            this.panelBackdrop.Controls.Add(this.panelCard);
            this.Controls.Add(this.panelBackdrop);
            this.Name = "AuthModal";
            this.Size = new System.Drawing.Size(1000, 700);
            this.ResumeLayout(false);
        }
    }
}
```

- [ ] **Step 2: Create `AuthModal.cs`**

```csharp
using System;
using System.Windows.Forms;

namespace Hue_travel.Auth
{
    public partial class AuthModal : UserControl
    {
        public event EventHandler CloseRequested;

        public AuthModal()
        {
            InitializeComponent();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            panelCard.Location = new System.Drawing.Point(
                (Width - panelCard.Width) / 2,
                (Height - panelCard.Height) / 2);
        }
    }
}
```

- [ ] **Step 3: Register files, build, commit**

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Build
git add Hue_travel/Auth Hue_travel/Hue_travel.csproj
git commit -m "feat: add AuthModal (UI-only login screen)"
```

---

## Task 13: MainForm Composition Root

**Files:**
- Create: `Hue_travel\Shell\MainForm.cs`
- Create: `Hue_travel\Shell\MainForm.Designer.cs`
- Modify: `Hue_travel\Program.cs`
- Delete: `Hue_travel\Form1.cs`, `Hue_travel\Form1.Designer.cs`

**Interfaces:**
- Consumes every public event/method produced by Tasks 5–12 (`MapHost`, `TitleBar`, `RibbonBar`, `CategoryRail`, `StatusBar`, `LayerControlPanel`, `BaseMapSwitcherPopup`, `SearchPoiPanel`, `RouteOptimizerPanel`, `TravelGuidePanel`, `PoiIdentifyCard`, `AuthModal`).
- Produces: the running application's entry window.

- [ ] **Step 1: Create `MainForm.Designer.cs`**

```csharp
using Hue_travel.MapEngine;
using Hue_travel.Panels.BaseMapSwitcher;
using Hue_travel.Panels.LayerControl;
using Hue_travel.Panels.PoiIdentify;
using Hue_travel.Panels.RouteOptimizer;
using Hue_travel.Panels.SearchPoi;
using Hue_travel.Panels.TravelGuide;
using Hue_travel.Auth;

namespace Hue_travel.Shell
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private TitleBar titleBar;
        private RibbonBar ribbonBar;
        private CategoryRail categoryRail;
        private StatusBar statusBar;
        private MapHost mapHost;

        private LayerControlPanel layerControlPanel;
        private RouteOptimizerPanel routeOptimizerPanel;
        private SearchPoiPanel searchPoiPanel;
        private TravelGuidePanel travelGuidePanel;
        private PoiIdentifyCard poiIdentifyCard;
        private BaseMapSwitcherPopup baseMapSwitcherPopup;
        private AuthModal authModal;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.titleBar = new TitleBar();
            this.ribbonBar = new RibbonBar();
            this.categoryRail = new CategoryRail();
            this.statusBar = new StatusBar();
            this.mapHost = new MapHost();
            this.layerControlPanel = new LayerControlPanel();
            this.routeOptimizerPanel = new RouteOptimizerPanel();
            this.searchPoiPanel = new SearchPoiPanel();
            this.travelGuidePanel = new TravelGuidePanel();
            this.poiIdentifyCard = new PoiIdentifyCard();
            this.baseMapSwitcherPopup = new BaseMapSwitcherPopup();
            this.authModal = new AuthModal();
            this.SuspendLayout();

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.ClientSize = new System.Drawing.Size(1280, 800);
            this.Text = "Cố Đô GIS";

            this.titleBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.titleBar.Size = new System.Drawing.Size(1280, 36);

            this.ribbonBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.ribbonBar.Size = new System.Drawing.Size(1280, 40);

            this.categoryRail.Dock = System.Windows.Forms.DockStyle.Left;
            this.categoryRail.Size = new System.Drawing.Size(48, 724);

            this.statusBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusBar.Size = new System.Drawing.Size(1280, 28);

            this.mapHost.Dock = System.Windows.Forms.DockStyle.Fill;

            this.layerControlPanel.Location = new System.Drawing.Point(64, 88);
            this.layerControlPanel.Visible = false;

            this.routeOptimizerPanel.Location = new System.Drawing.Point(888, 88);
            this.routeOptimizerPanel.Visible = false;

            this.searchPoiPanel.Location = new System.Drawing.Point(372, 88);
            this.searchPoiPanel.Visible = false;

            this.travelGuidePanel.Location = new System.Drawing.Point(888, 400);
            this.travelGuidePanel.Visible = false;

            this.poiIdentifyCard.Location = new System.Drawing.Point(370, 560);
            this.poiIdentifyCard.Visible = false;

            this.baseMapSwitcherPopup.Location = new System.Drawing.Point(1060, 84);
            this.baseMapSwitcherPopup.Visible = false;

            this.authModal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.authModal.Visible = false;

            this.Controls.Add(this.mapHost);
            this.Controls.Add(this.layerControlPanel);
            this.Controls.Add(this.routeOptimizerPanel);
            this.Controls.Add(this.searchPoiPanel);
            this.Controls.Add(this.travelGuidePanel);
            this.Controls.Add(this.poiIdentifyCard);
            this.Controls.Add(this.baseMapSwitcherPopup);
            this.Controls.Add(this.categoryRail);
            this.Controls.Add(this.ribbonBar);
            this.Controls.Add(this.titleBar);
            this.Controls.Add(this.authModal);
            this.Controls.Add(this.statusBar);

            this.ResumeLayout(false);
        }
    }
}
```

- [ ] **Step 2: Create `MainForm.cs`**

```csharp
using System.Windows.Forms;
using Hue_travel.Models;

namespace Hue_travel.Shell
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            WireEvents();
        }

        private void WireEvents()
        {
            titleBar.MinimizeClicked += (s, e) => WindowState = FormWindowState.Minimized;
            titleBar.MaximizeClicked += (s, e) =>
                WindowState = WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
            titleBar.CloseClicked += (s, e) => Close();
            titleBar.AuthClicked += (s, e) => authModal.Visible = !authModal.Visible;

            ribbonBar.LayerControlToggled += (s, e) => TogglePanel(layerControlPanel);
            ribbonBar.RouteOptimizerToggled += (s, e) => TogglePanel(routeOptimizerPanel);
            ribbonBar.SearchPoiToggled += (s, e) => TogglePanel(searchPoiPanel);
            ribbonBar.TravelGuideToggled += (s, e) => TogglePanel(travelGuidePanel);
            ribbonBar.ZoomInClicked += (s, e) => mapHost.ZoomIn();
            ribbonBar.ZoomOutClicked += (s, e) => mapHost.ZoomOut();
            ribbonBar.BaseMapSwitcherToggled += (s, e) => TogglePanel(baseMapSwitcherPopup);

            categoryRail.CategorySelected += (s, category) => TogglePanel(searchPoiPanel, forceVisible: true);

            layerControlPanel.CloseRequested += (s, e) => layerControlPanel.Visible = false;
            travelGuidePanel.CloseRequested += (s, e) => travelGuidePanel.Visible = false;
            authModal.CloseRequested += (s, e) => authModal.Visible = false;

            searchPoiPanel.PoiSelected += (s, poi) => ShowPoiCard(poi);
            baseMapSwitcherPopup.BaseMapSelected += (s, mapId) => baseMapSwitcherPopup.Visible = false;
        }

        private void TogglePanel(Control panel, bool forceVisible = false)
        {
            panel.Visible = forceVisible || !panel.Visible;
            if (panel.Visible) panel.BringToFront();
        }

        private void ShowPoiCard(Poi poi)
        {
            poiIdentifyCard.ShowPoi(poi);
        }
    }
}
```

- [ ] **Step 3: Update `Program.cs` to launch `MainForm`**

```csharp
using System;
using System.Windows.Forms;
using Hue_travel.Shell;

namespace Hue_travel
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
```

- [ ] **Step 4: Delete `Form1.cs` / `Form1.Designer.cs` and remove them from the csproj**

Remove the `<Compile Include="Form1.cs">` and `<Compile Include="Form1.Designer.cs">` entries from `Hue_travel.csproj`, then delete both files. Add the new `MainForm.cs`/`MainForm.Designer.cs` `<Compile>` entries in their place.

- [ ] **Step 5: Build**

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Build
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: wire MainForm composition root, retire Form1"
```

---

## Task 14: Final Build + Designer-Compatibility Verification

**Files:** none created — verification-only task.

- [ ] **Step 1: Full solution build**

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe' Hue_travel.sln /t:Rebuild /p:Configuration=Debug
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 2: Full test suite**

```powershell
dotnet test Hue_travel.Tests
```

Expected: all tests from Tasks 2, 4, 9, 10 pass (`ThemeColorsTests`, `SamplePoisTests`, `PoiSearchFilterTests`, `RouteSelectionTests`, plus `PlaceholderTests`).

- [ ] **Step 3: Manual Designer-compatibility checklist (user performs this in Visual Studio 2019)**

- [ ] Open `MainForm.cs`, switch to Designer view — loads without exceptions, matches `GiaoDien/screen.png` proportions (title bar, ribbon, left rail, map fill, status bar).
- [ ] Click into each floating panel's own `.cs` file (`LayerControlPanel`, `RouteOptimizerPanel`, `SearchPoiPanel`, `TravelGuidePanel`, `PoiIdentifyCard`, `BaseMapSwitcherPopup`, `AuthModal`) and confirm each opens in Designer view independently.
- [ ] On `MainForm`'s Designer surface, drag `layerControlPanel` to a new `Location` and confirm the position change persists after saving (proves layout is genuinely Designer-editable, not runtime-only).
- [ ] Press F5, confirm the app launches, ribbon buttons toggle each panel's visibility, zoom in/out buttons pan the (blank) `AxMap`, and the auth button opens `AuthModal`.

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "chore: verify full build and test suite for UI shell phase"
```
