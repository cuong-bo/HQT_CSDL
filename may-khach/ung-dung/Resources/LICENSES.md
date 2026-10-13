# Third-party assets in `Resources/`

| File | Source | License |
|---|---|---|
| `PlusJakartaSans-{Regular,Medium,SemiBold,Bold}.ttf` | https://github.com/tokotype/PlusJakartaSans | SIL Open Font License 1.1 |
| `MaterialSymbolsSubset.ttf` | Subset (about 60 glyphs, instanced at FILL 0 / wght 400 / opsz 20) of *Material Symbols Outlined* from https://github.com/google/material-design-icons | Apache License 2.0 |
| `logo.jpg` | Logo image referenced by `GiaoDien/code.html` (the project owner's mockup) | As supplied by the project owner |
| Weather data (runtime, not a file) | https://open-meteo.com via `Services/Weather/OpenMeteoWeatherService.cs` | CC BY 4.0, free for non-commercial use; keep the attribution |
| Base map tiles (runtime): Google roads / satellite / hybrid / terrain (the only base maps) via `MapEngine/BaseMapCatalog.cs` | https://mt1.google.com tile addresses | TEMPORARY. Google's terms do not allow this in a distributed product: replace with Esri, MapTiler or an own tile server before release. No rebuild needed: set `TILE_URL_*` in `deploy/.env` and the app takes the addresses from `GET /v1/config` |
| `icons.map.txt` | Icon name to codepoint list for the subset above (mirrors `Theme/IconFont.cs`) | Apache License 2.0 |
