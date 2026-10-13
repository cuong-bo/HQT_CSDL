# Third-party assets in `Resources/`

| File | Source | License |
|---|---|---|
| `PlusJakartaSans-{Regular,Medium,SemiBold,Bold}.ttf` | https://github.com/tokotype/PlusJakartaSans | SIL Open Font License 1.1 |
| `MaterialSymbolsSubset.ttf` | Subset (about 60 glyphs, instanced at FILL 0 / wght 400 / opsz 20) of *Material Symbols Outlined* from https://github.com/google/material-design-icons | Apache License 2.0 |
| `logo.jpg` | Logo image referenced by `GiaoDien/code.html` (the project owner's mockup) | As supplied by the project owner |
| Weather data (runtime, not a file) | https://open-meteo.com via `Services/Weather/OpenMeteoWeatherService.cs` | CC BY 4.0, free for non-commercial use; keep the attribution |
| Base map (data downloaded from the server, drawn locally): `Data/shp` shapefiles (`NenBien`, `rgXa`, `song_1n`, `song_2n`, `MangGiaoThong`, `DiaPhan_Tinh_2025`, ...) | Project data; river and road geometry © OpenStreetMap contributors | ODbL 1.0 (keep the attribution). Google tile addresses remain in `MapEngine/BaseMapCatalog.cs` but are switched off (`UseLocalData = true`) |
| `icons.map.txt` | Icon name to codepoint list for the subset above (mirrors `Theme/IconFont.cs`) | Apache License 2.0 |
