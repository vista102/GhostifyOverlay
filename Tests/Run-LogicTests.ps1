param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$managed=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
foreach($dir in @($managed,(Join-Path $managed 'UnityModManager'))) {
    Get-ChildItem -LiteralPath $dir -Filter *.dll | ForEach-Object { try { [Reflection.Assembly]::LoadFrom($_.FullName)|Out-Null } catch {} }
}
$project=Split-Path $PSScriptRoot
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $project 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
Add-Type -Path @((Join-Path $PSScriptRoot 'ILReader.cs'),(Join-Path $PSScriptRoot 'OverlayPresentationTests.cs')) -ReferencedAssemblies 'System.Core'
$results=[OverlayPresentationTests]::Run($assembly)
$results
$script:passed=$results.Count
$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
function ModType([string]$name) { $assembly.GetType('DonQuixoteOverlay.'+$name,$true) }
function Call($target,[string]$name,[object[]]$arguments) {
    for($i=0;$i -lt $arguments.Count;$i++) { $arguments[$i]=$arguments[$i].PSObject.BaseObject }
    $type=if($target -is [Type]){$target}else{$target.GetType()}
    $method=@($type.GetMethods($flags)|Where-Object {$_.Name -eq $name -and $_.GetParameters().Count -eq $arguments.Count})[0]
    $instance=if($target -is [Type]){$null}else{$target}
    try { $method.Invoke($instance,$arguments) } catch { throw $_.Exception.ToString() }
}
function Equal($expected,$actual,[string]$name) {
    if($expected -ne $actual) {throw "$name expected $expected, got $actual"}
    $script:passed++; "PASS $name"
}
$settings=[Activator]::CreateInstance((ModType 'DonQuixoteSettings'))
Equal 'Gmarket Sans' $settings.Overlay.Font 'GmarketSans default'
$settings.Overlay.Font='Galmuri'
Call (ModType 'SettingsNormalization') 'Normalize' @($settings)|Out-Null
Equal 'Gmarket Sans' $settings.Overlay.Font 'previous font preference adopts requested GmarketSans'
$reader=[LifecycleTests].GetMethod('Calls',$flags)
$styleCalls=$reader.Invoke($null,@((ModType 'NativeJudgmentColors').GetMethod('Apply',$flags)))
Equal $false (@($styleCalls|Where-Object {$_.Name -in @('set_font','set_fontSharedMaterial','set_fontSize','set_text','set_enableAutoSizing')}).Count -gt 0) 'native judgment fonts, scale and text are left to the game'
$uiCalls=$reader.Invoke($null,@((ModType 'DarkNeonTheme').GetMethod('FontFor',$flags)))
Equal $true (@($uiCalls|Where-Object {$_.Name -eq 'get_GmarketSans'}).Count -gt 0) 'all settings text uses GmarketSans entry point'
$overlayCalls=$reader.Invoke($null,@((ModType 'FontAssetProvider').GetProperty('OverlayFont',$flags).GetGetMethod($true)))
Equal $true (@($overlayCalls|Where-Object {$_.Name -eq 'get_GmarketSans'}).Count -gt 0) 'overlay uses GmarketSans entry point'
Add-Type -AssemblyName System.Drawing
$fontCollection=New-Object System.Drawing.Text.PrivateFontCollection
try {
    $fontCollection.AddFontFile((Join-Path $project 'Assets\GmarketSansTTFMedium.ttf'))
    Equal 'Gmarket Sans TTF Medium' $fontCollection.Families[0].Name 'bundled static font is valid GmarketSans'
} finally { $fontCollection.Dispose() }
Equal $true (Test-Path -LiteralPath (Join-Path $project 'bin\Release\GhostifyOverlay\Assets\GmarketSans-OFL.txt')) 'font license included in Release'
$notoFonts=New-Object System.Drawing.Text.PrivateFontCollection
try {
    $notoFonts.AddFontFile((Join-Path $project 'Assets\NotoSansKR-Regular.ttf'))
    Equal 'Noto Sans KR' $notoFonts.Families[0].Name 'bundled static Korean font is Noto Sans KR'
    Equal $true $notoFonts.Families[0].IsStyleAvailable([System.Drawing.FontStyle]::Regular) 'bundled Noto Sans KR provides a regular face'
} finally { $notoFonts.Dispose() }
$notoSource=Get-Content -LiteralPath (Join-Path $project 'Assets\NotoSansKR-source.json') -Raw|ConvertFrom-Json
Equal $notoSource.OutputSha256 (Get-FileHash -LiteralPath (Join-Path $project 'Assets\NotoSansKR-Regular.ttf') -Algorithm SHA256).Hash 'Noto font provenance hash matches bundled face'
Equal $notoSource.LicenseSha256 (Get-FileHash -LiteralPath (Join-Path $project 'bin\Release\GhostifyOverlay\Assets\NotoSansKR-OFL.txt') -Algorithm SHA256).Hash 'original Noto OFL ships unchanged'
Equal $false (Test-Path -LiteralPath (Join-Path $project 'bin\Release\GhostifyOverlay\Assets\BMDOHYEON_ttf.ttf')) 'old Korean font is excluded from the new Release'
$branding=Get-Content -LiteralPath (Join-Path $project 'Info.json') -Raw|ConvertFrom-Json
Equal 'Ghostify Overlay' $branding.DisplayName 'mod manager uses Ghostify Overlay branding'
Equal 'GhostifyOverlay.dll' $branding.AssemblyName 'mod manager loads renamed Ghostify assembly'
Equal 'GhostifyOverlay' $assembly.GetName().Name 'compiled assembly has the new project name'
Equal $true (Test-Path -LiteralPath (Join-Path $project 'GhostifyOverlay.csproj')) 'project file has the new Ghostify name'
(ModType 'Main').GetField('Settings',$flags).SetValue($null,$settings)
foreach($case in @(@(1,$false,$false),@(2,$false,$true),@(1,$true,$true))) {
    Equal $case[2] (Call (ModType 'AttemptTracker') 'IsProgressStart' @([int]$case[0],[bool]$case[1])) 'attempt split'
}
foreach($name in @('EditorModules','MusicLibraryController','RecordingController','InputEventDecisions','DmNoteController','PlanetSettings')) {
    Equal $null $assembly.GetType('DonQuixoteOverlay.'+$name) "excluded type $name"
}
Equal $true ($null -ne $assembly.GetType('DonQuixoteOverlay.EffectGate')) 'requested original effects are included'
Equal $null $assembly.GetType('DonQuixoteOverlay.InputSettings') 'key limiter and chatter settings are absent'
Equal $null (ModType 'Main').GetMethod('ToggleRecordingMode',$flags) 'recording shortcut absent'
Equal 'qkddn.DonQuixoteOverlay' (ModType 'Main').GetField('HarmonyId',$flags).GetRawConstantValue() 'independent Harmony ID'
Equal 'DonQuixoteOverlay' ((Get-Content -LiteralPath (Join-Path $project 'Info.json') -Raw|ConvertFrom-Json).Id) 'independent UMM ID'
# Storage tests use only a new fixture directory in this project.
$fixture=Join-Path $project ('Tests\fixture-'+[Guid]::NewGuid().ToString('N'))
Call (ModType 'SettingsStore') 'Initialize' @($fixture)|Out-Null
$settings.Overlay.FullAttempts['fixture']=17
$settings.Overlay.FullProgressAttempts['fixture']=23
Call (ModType 'SettingsStore') 'Save' @($settings)|Out-Null
$loaded=Call (ModType 'SettingsStore') 'Load' @()
Equal 17 $loaded.Overlay.FullAttempts['fixture'] 'full attempts reload'
Equal 23 $loaded.Overlay.FullProgressAttempts['fixture'] 'full progress attempts reload'
Equal 3 $loaded.SchemaVersion 'settings schema migrated to native judgments'
foreach($part in @('TopLeft','TopRight','Attempts','Judgments','Combo','DetailedPerfect')) {
    $layout=(ModType 'LayoutStore').GetField('Current').GetValue($null)
    $layout.GetType().GetField($part+'X').SetValue($layout,[float]45)
    $layout.GetType().GetField($part+'Scale').SetValue($layout,[float]1.4)
}
Call (ModType 'LayoutStore') 'Save' @()|Out-Null
Call (ModType 'SettingsStore') 'Initialize' @($fixture)|Out-Null
foreach($part in @('TopLeft','TopRight','Attempts','Judgments','Combo','DetailedPerfect')) {
    $layout=(ModType 'LayoutStore').GetField('Current').GetValue($null)
    Equal ([float]45) $layout.GetType().GetField($part+'X').GetValue($layout) "$part position reload"
    Equal ([float]1.4) $layout.GetType().GetField($part+'Scale').GetValue($layout) "$part scale reload"
}
$settingsPath=Join-Path $fixture 'UserData\settings.json'
[IO.File]::WriteAllText($settingsPath,'{broken')
$recovered=Call (ModType 'SettingsStore') 'Load' @()
Equal $true (@(Get-ChildItem -LiteralPath (Split-Path $settingsPath) -Filter 'settings.json.corrupt-*').Count -eq 1) 'corrupt settings backed up'
Equal $true ($recovered -ne $null) 'corrupt settings safely recovered'
"Logic/regression assertions passed: $script:passed"
# Verify every original file, including existing UserData and build output.
$baselinePath=Join-Path $project 'SOURCE_BASELINE.json'
if(Test-Path -LiteralPath $baselinePath){
    $baseline=Get-Content -LiteralPath $baselinePath -Encoding UTF8 -Raw|ConvertFrom-Json
    foreach($file in $baseline) {
        if(!(Test-Path -LiteralPath $file.Path) -or (Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash -ne $file.Hash) {throw "Original file changed: $($file.Path)"}
    }
    "Original file hashes preserved: $($baseline.Count)"
}else{
    'Original-mod hash comparison skipped: this checkout has no local baseline.'
}
