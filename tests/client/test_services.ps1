$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$binDir = Join-Path $repoRoot 'Hue_travel\bin\Debug'
[void][System.Reflection.Assembly]::LoadFrom((Join-Path $binDir 'Hue_travel.exe'))
$asm = [AppDomain]::CurrentDomain.GetAssemblies() | ? { $_.GetName().Name -eq 'Hue_travel' }
$script:fail = 0
$script:pass = 0

function Check($name, $actual, $expected) {
    if ("$actual" -ceq "$expected") { $script:pass++ }
    else { $script:fail++; Write-Host "FAIL $name`n   expected: [$expected]`n   actual:   [$actual]" }
}
function GetT($n) { $t = $asm.GetType($n); if (-not $t) { throw "type missing: $n" }; $t }

# ---- MapScale
$ms = GetT 'Hue_travel.Services.MapScale'
$den = $ms.GetMethod('Denominator')
$fmt = $ms.GetMethod('Format')
Check 'denominator 1000m over 1920px@96dpi' ([math]::Round($den.Invoke($null, @([double]1000, [int]1920, [double]96)), 1)) '1968.5'
Check 'denominator zero width is NaN' ([double]::IsNaN($den.Invoke($null, @([double]0, [int]1920, [double]96)))) 'True'
Check 'format 15372' $fmt.Invoke($null, @([double]15372)) '1 : 15,000'
Check 'format 987' $fmt.Invoke($null, @([double]987)) '1 : 990'
Check 'format 123456' $fmt.Invoke($null, @([double]123456)) '1 : 120,000'
Check 'format 50' $fmt.Invoke($null, @([double]50)) '1 : 50'
Check 'format NaN' $fmt.Invoke($null, @([double]::NaN)) '1 : --'

# ---- WeatherDescriber (advice uses feels-like, rain, wind, UV and day / night)
$snapType = GetT 'Hue_travel.Services.Weather.WeatherSnapshot'
function Snap($temp, $feels, $code, $isDay, $rain, $wind, $uv) {
    [Activator]::CreateInstance($snapType, [object[]]@([double]$temp, [double]$feels, [int]$code, [bool]$isDay, [double]$rain, [double]$wind, [double]$uv, [datetime]::UtcNow))
}
$desc = (GetT 'Hue_travel.Services.Weather.WeatherDescriber').GetMethod('Describe')
function Say($s) { $desc.Invoke($null, @($s)) }
Check 'clear day, comfortable' (Say (Snap 26.4 26.4 0 $true 0 5 3)) 'Nắng đẹp, 26°C - Rất thuận tiện tham quan'
Check 'night, overcast, humid' (Say (Snap 23.9 27.3 3 $false 0 12.3 0)) 'Nhiều mây, 24°C (cảm giác 27°C) - Trời tối, phù hợp dạo phố và ngắm sông Hương'
Check 'hot and humid asks for water and hat' ((Say (Snap 30 36 0 $true 0 5 6)).Contains('mũ')) 'True'
Check 'rain code asks for raincoat' ((Say (Snap 24 24 61 $true 2 5 1)).Contains('áo mưa')) 'True'
Check 'measured precipitation asks for raincoat' ((Say (Snap 24 24 3 $true 0.5 5 1)).Contains('áo mưa')) 'True'
Check 'thunderstorm warns first' ((Say (Snap 24 24 95 $true 5 5 1)).StartsWith('Dông')) 'True'
Check 'strong wind warns' ((Say (Snap 26 26 1 $true 0 45 3)).Contains('Gió mạnh')) 'True'
Check 'high UV warns by day' ((Say (Snap 30 31 0 $true 0 5 9)).Contains('UV')) 'True'
Check 'high UV is ignored at night' ((Say (Snap 26 26 0 $false 0 5 9)).Contains('UV')) 'False'
Check 'cold asks for a jacket' ((Say (Snap 12 12 3 $true 0 5 1)).Contains('áo khoác')) 'True'
Check 'small feels-like gap is not shown' ((Say (Snap 26 27 0 $true 0 5 3)).Contains('cảm giác')) 'False'

# ---- OpenMeteoParser
$pr = GetT 'Hue_travel.Services.Weather.OpenMeteoParser'
$json = '{"latitude":16.4,"current_units":{"temperature_2m":"C"},"current":{"time":"2026-10-08T20:45","interval":900,"temperature_2m":24.1,"weather_code":3,"is_day":0}}'
$args1 = New-Object object[] 2
$args1[0] = $json
$ok = $pr.GetMethod('TryParse').Invoke($null, $args1)
Check 'parse ok' $ok 'True'
$snap = $args1[1]
Check 'parse temperature' $snap.TemperatureC '24.1'
Check 'parse code' $snap.WeatherCode '3'
Check 'parse is_day false' $snap.IsDay 'False'
Check 'parse missing feels-like falls back to temperature' $snap.ApparentC '24.1'
$json2 = '{"current":{"time":"2026-10-08T23:15","temperature_2m":23.9,"apparent_temperature":27.3,"is_day":0,"precipitation":0.4,"weather_code":3,"wind_speed_10m":12.3,"uv_index":0.5}}'
$args4 = New-Object object[] 2
$args4[0] = $json2
Check 'parse extended ok' $pr.GetMethod('TryParse').Invoke($null, $args4) 'True'
Check 'parse feels-like' $args4[1].ApparentC '27.3'
Check 'parse precipitation' $args4[1].PrecipitationMm '0.4'
Check 'parse wind' $args4[1].WindKmh '12.3'
Check 'parse uv' $args4[1].UvIndex '0.5'
$args2 = New-Object object[] 2
$args2[0] = '{}'
Check 'parse missing current fails' $pr.GetMethod('TryParse').Invoke($null, $args2) 'False'
$args3 = New-Object object[] 2
$args3[0] = 'not json'
Check 'parse garbage fails' $pr.GetMethod('TryParse').Invoke($null, $args3) 'False'

# ---- SceneGeoreference
$sg = GetT 'Hue_travel.Services.Area.SceneGeoreference'
$gx = $sg.GetMethod('X'); $gy = $sg.GetMethod('Y')
Check 'anchor easting' $gx.Invoke($null, @([double]620)) '775116.47'
Check 'anchor northing' $gy.Invoke($null, @([double]320)) '1822770.6'
Check '100 canvas units east = 200 m' ($gx.Invoke($null, @([double]720)) - $gx.Invoke($null, @([double]620))) '200'
Check 'canvas y grows downwards, northing falls' ($gy.Invoke($null, @([double]420)) - $gy.Invoke($null, @([double]320))) '-200'

# ---- CoordinateFormatter
$cf = GetT 'Hue_travel.Services.CoordinateFormatter'
$deg = [string][char]0xB0
$dot = [string][char]0xB7
Check 'utm' $cf.GetMethod('FormatUtm').Invoke($null, @([double]775116.47, [double]1822770.6)) ('E 775,116 ' + $dot + ' N 1,822,771')
Check 'degrees north-east' $cf.GetMethod('FormatDegrees').Invoke($null, @([double]107.5786, [double]16.4698)) ('16.4698' + $deg + 'N, 107.5786' + $deg + 'E')
Check 'degrees south-west' $cf.GetMethod('FormatDegrees').Invoke($null, @([double]-70.25, [double]-33.5)) ('33.5000' + $deg + 'S, 70.2500' + $deg + 'W')

# ---- BaseMapCatalog (all base maps are tile maps; the drawn light/night maps are gone)
$bc = GetT 'Hue_travel.MapEngine.BaseMapCatalog'
$find = $bc.GetMethod('Find')
Check 'catalog has four base maps' ($bc.GetProperty('All').GetValue($null).Count) '4'
Check 'default base map is the roadmap' $bc.GetField('DefaultId').GetValue($null) 'roadmap'
foreach ($id in 'roadmap','satellite','hybrid','terrain') { Check "$id has a tile url" ($find.Invoke($null, @($id)).TileUrl -ne $null) 'True' }
Check 'standard map is gone' ($find.Invoke($null, @('standard')) -eq $null) 'True'
Check 'night map is gone' ($find.Invoke($null, @('night')) -eq $null) 'True'
Check 'hybrid url uses lyrs=y' ($find.Invoke($null, @('hybrid')).TileUrl.Contains('lyrs=y')) 'True'
Check 'satellite url uses lyrs=s' ($find.Invoke($null, @('satellite')).TileUrl.Contains('lyrs=s')) 'True'
Check 'url uses MapWinGIS placeholders' ($find.Invoke($null, @('terrain')).TileUrl.Contains('{x}') -and $find.Invoke($null, @('terrain')).TileUrl.Contains('{zoom}')) 'True'
Check 'tile layers credit Google' ($find.Invoke($null, @('satellite')).Attribution.Contains('Google')) 'True'
Check 'unknown id gives null' ($find.Invoke($null, @('nope')) -eq $null) 'True'
$pid1 = $find.Invoke($null, @('satellite')).ProviderId
$pid2 = $find.Invoke($null, @('hybrid')).ProviderId
Check 'provider ids are distinct' ($pid1 -ne $pid2) 'True'
Check 'provider ids avoid the reserved range' ($pid1 -gt 10000) 'True'

# ---- PoiLayerFilter (left rail: show only one kind of object)
$catType = GetT 'Hue_travel.Models.PoiCategory'
$heritage = [Enum]::Parse($catType, 'Heritage'); $food = [Enum]::Parse($catType, 'Food'); $stay = [Enum]::Parse($catType, 'Stay')
$flt = New-Object (GetT 'Hue_travel.MapEngine.PoiLayerFilter')
$flt.Add($heritage, 11); $flt.Add($food, 12); $flt.Add($stay, 13)
Check 'all layers visible at first' (($flt.IsVisible(11)) -and ($flt.IsVisible(12)) -and ($flt.IsVisible(13))) 'True'
$flt.ShowOnly($food)
Check 'only food visible: heritage hidden' $flt.IsVisible(11) 'False'
Check 'only food visible: food shown' $flt.IsVisible(12) 'True'
Check 'only food visible: stay hidden' $flt.IsVisible(13) 'False'
$flt.ShowOnly($null)
Check 'all again after "Tat ca"' (($flt.IsVisible(11)) -and ($flt.IsVisible(12)) -and ($flt.IsVisible(13))) 'True'
Check 'unknown layer is not managed' $flt.IsVisible(99) 'False'
Check 'handles are listed' ($flt.Handles.Count) '3'

# ---- PoiCategory: the six Hue groups
Check 'six Hue categories in rail order' (([Enum]::GetNames((GetT 'Hue_travel.Models.PoiCategory'))) -join ',') 'Heritage,Scenery,Food,Stay,Service,Transport'

# ---- GeoMath / RoutePlanner (shortest visiting order, straight line x road detour)
$geoType = GetT 'Hue_travel.Services.Routing.GeoPoint'
function Pt($lon, $lat) { [Activator]::CreateInstance($geoType, [object[]]@([double]$lon, [double]$lat)) }
function Pts($list) { $a = [Array]::CreateInstance($geoType, $list.Count); for ($i = 0; $i -lt $list.Count; $i++) { $a.SetValue($list[$i], $i) }; ,$a }
$gm = GetT 'Hue_travel.Services.Routing.GeoMath'
Check 'one degree of latitude is 111.19 km' ([math]::Round($gm.GetMethod('DistanceKm').Invoke($null, @([double]0, [double]0, [double]0, [double]1)), 2)) '111.19'
$planner = (GetT 'Hue_travel.Services.Routing.RoutePlanner').GetMethod('Plan')
$A = Pt 0 0; $B = Pt 0 0.1; $C = Pt 0 0.2
$p1 = $planner.Invoke($null, @($A, (Pts @($C, $B))))
Check 'visits the nearer stop first' (($p1.Order) -join ',') '1,0'
Check 'total km with detour factor' ([math]::Round($p1.TotalKm, 1)) '28.9'
Check 'travel minutes at city speed' $p1.TravelMinutes '69'
Check 'minutes saved against the listed order' $p1.SavedMinutes '35'
Check 'one leg per hop from the start' ($p1.LegsKm.Count) '2'
Check 'first leg km' ([math]::Round($p1.LegsKm[0], 1)) '14.5'
$p2 = $planner.Invoke($null, @($null, (Pts @($C, $A, $B))))
Check 'without a start the path runs along the line' ((($p2.Order) -join ',') -in @('1,2,0','0,2,1')) 'True'
Check 'without a start the total is the same' ([math]::Round($p2.TotalKm, 1)) '28.9'
Check 'without a start there is one leg fewer' ($p2.LegsKm.Count) '2'
$p3 = $planner.Invoke($null, @($A, (Pts @($B))))
Check 'single stop' (($p3.Order) -join ',') '0'
$p4 = $planner.Invoke($null, @($A, (Pts @())))
Check 'no stops, no distance' $p4.TotalKm '0'

# ---- DemoPlaces (real coordinates, approximate) and the locator built on them
$places = GetT 'Hue_travel.Services.Area.DemoPlaces'
$tg = $places.GetMethod('TryGetPoi')
$g = New-Object object[] 2
$g[0] = 'dai-noi'
Check 'dai-noi is known' $tg.Invoke($null, $g) 'True'
Check 'dai-noi longitude' $g[1].Longitude '107.5786'
Check 'dai-noi latitude' $g[1].Latitude '16.4698'
$h = New-Object object[] 2
$h[0] = 'not-a-poi'
Check 'unknown poi' $tg.Invoke($null, $h) 'False'
$starts = $places.GetProperty('StartPoints').GetValue($null)
Check 'first start point has a location' ($starts[0].Location -ne $null) 'True'
Check 'hotel start point has no location yet' ($starts[$starts.Count - 1].Location -eq $null) 'True'
$loc = New-Object ($asm.GetType('Hue_travel.Services.Area.DemoAreaLocator'))
$find2 = $loc.GetType().GetMethod('Locate')
$near = $find2.Invoke($loc, @([double]107.5786, [double]16.4698))
Check 'near dai noi' ($near.Contains('Ho' + [char]0x00E0 + 'ng Th' + [char]0x00E0 + 'nh')) 'True'
Check 'far away falls back to default area' ($find2.Invoke($loc, @([double]0, [double]0))) ('Trung t' + [char]0x00E2 + 'm C' + [char]0x1ED1 + ' ' + [char]0x0111 + [char]0x00F4 + ' Hu' + [char]0x1EBF + ' & B' + [char]0x1EDD + ' s' + [char]0x00F4 + 'ng H' + [char]0x01B0 + [char]0x01A1 + 'ng')

# ---- PoiSearchFilter with several categories at once
$poiT = GetT 'Hue_travel.Models.Poi'
$catT = GetT 'Hue_travel.Models.PoiCategory'
$listT = [System.Collections.Generic.List`1].MakeGenericType($poiT)
$setT = [System.Collections.Generic.HashSet`1].MakeGenericType($catT)
function MakePoi($name, $category) { $x = [Activator]::CreateInstance($poiT); $x.Name = $name; $x.Category = [Enum]::Parse($catT, $category); $x }
$pois = [Activator]::CreateInstance($listT)
foreach ($pair in @(@('Đại Nội Huế', 'Heritage'), @('Chùa Thiên Mụ', 'Heritage'), @('Bún bò Mụ Rơi', 'Food'), @('Khách sạn Sao', 'Stay'))) { $pois.Add((MakePoi $pair[0] $pair[1])) }
$apply = (GetT 'Hue_travel.Panels.SearchPoi.PoiSearchFilter').GetMethod('Apply')
function Pick($keyword, [string[]]$cats) {
    $set = [Activator]::CreateInstance($setT)
    foreach ($c in $cats) { [void]$set.Add([Enum]::Parse($catT, $c)) }
    ($apply.Invoke($null, @($pois, $keyword, $set))).Count
}
Check 'no category chosen means all' (Pick $null @()) '4'
Check 'one category' (Pick $null @('Food')) '1'
Check 'two categories at once' (Pick $null @('Heritage','Food')) '3'
Check 'categories and keyword together' (Pick 'hue' @('Heritage','Food')) '1'
Check 'keyword ignores diacritics' (Pick 'bun bo' @()) '1'
Check 'null set means all' (($apply.Invoke($null, @($pois, $null, $null))).Count) '4'


# ---- Real tourism data (Phase 1): Shapefile reader, grouping, repository
$dataDir = Join-Path $repoRoot 'Hue_travel\Data'
$reader = GetT 'Hue_travel.Services.Data.ShapefilePointReader'
$stay = $reader.GetMethod('Read').Invoke($null, @([string](Join-Path $dataDir 'diemLuuTru.shp')))
Check 'stay layer record count' $stay.Count '38'
Check 'stay first name decodes Vietnamese' $stay[0].Attributes['TenCS'] 'Azerai La Residence Hue'
Check 'stay first geometry is UTM easting' ([int]($stay[0].X -gt 700000 -and $stay[0].X -lt 900000)) '1'
Check 'stay first Lon attribute' ([math]::Round($reader.GetMethod('ParseNumber').Invoke($null, @($stay[0].Attributes['Lon'])), 3)) '107.58'
$grp = (GetT 'Hue_travel.Services.Data.TourismGrouping').GetMethod('GroupOf')
function G($s) { "$($grp.Invoke($null, @($s)))" }
Check 'group lang tam' (G 'Di tích lịch sử - lăng tẩm') 'Heritage'
Check 'group bien' (G 'Biển - nghỉ dưỡng') 'Scenery'
Check 'group sinh thai' (G 'Sinh thái - hồ nước') 'Scenery'
Check 'group check-in' (G 'Cảnh quan - check-in') 'Scenery'
Check 'group suoi khoang' (G 'Nghỉ dưỡng - suối khoáng') 'Scenery'
Check 'group tam linh' (G 'Tâm linh - cảnh quan') 'Heritage'
Check 'group cho' (G 'Chợ - văn hóa - ẩm thực') 'Heritage'
Check 'group lang nghe' (G 'Làng nghề - trải nghiệm') 'Heritage'
Check 'group empty' (G '') 'Heritage'
$repo = (GetT 'Hue_travel.Services.Data.ShapefilePoiRepository').GetMethod('Load').Invoke($null, @([string]$dataDir))
function CountOf($c) { @($repo | ? { "$($_.Category)" -eq $c }).Count }
Check 'repo total' $repo.Count '109'
Check 'repo stay' (CountOf 'Stay') '38'
Check 'repo service' (CountOf 'Service') '12'
Check 'repo transport' (CountOf 'Transport') '9'
Check 'repo heritage+scenery' ((CountOf 'Heritage') + (CountOf 'Scenery')) '50'
Check 'repo no food yet' (CountOf 'Food') '0'
Check 'repo all have location' (@($repo | ? { -not $_.HasLocation }).Count) '0'
Check 'repo ids unique' (@($repo | % { $_.Id } | sort -Unique).Count) '109'
$hv = $repo | ? { $_.Id -eq 'DL001' }
Check 'repo DL001 name' $hv.Name 'Hải Vân Quan'
Check 'repo DL001 hours' $hv.OpeningHours '07:00-17:30'
Check 'repo DL001 utm x plausible' ([int]($hv.X -gt 700000 -and $hv.X -lt 900000)) '1'
Check 'repo DL001 lat' ([math]::Round($hv.Latitude, 4)) '16.1908'
$poiCat = (GetT 'Hue_travel.Services.Data.PoiCatalog').GetMethod('Load').Invoke($null, @([string]$dataDir))
Check 'catalog loads real data' $poiCat.Count '109'
$poiMissing = (GetT 'Hue_travel.Services.Data.PoiCatalog').GetMethod('Load').Invoke($null, @('Z:\missing\dir'))
Check 'catalog falls back to samples' ([int]($poiMissing.Count -gt 0 -and $poiMissing.Count -lt 109)) '1'

Write-Host "passed=$script:pass failed=$script:fail"
if ($script:fail -gt 0) { exit 1 }
