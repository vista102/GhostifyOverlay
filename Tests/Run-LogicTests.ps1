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
$x=ModType 'XPerfectModule'
foreach($case in @(@(0,'XPerfect'),@(15,'XPerfect'),@(-15,'XPerfect'),@(15.001,'MinusPerfect'),@(-15.001,'PlusPerfect'))) {
    Equal $case[1] (Call $x 'ClassifySignedDelta' @([float]$case[0],[float]15)).ToString() "signed boundary $($case[0])"
}
Equal $true ((Call $x 'GetSignedDeltaDegrees' @([float]1,[float]0,$true)) -gt 0) 'clockwise sign'
Equal $true ((Call $x 'GetSignedDeltaDegrees' @([float]1,[float]0,$false)) -lt 0) 'counterclockwise sign'
foreach($bpm in @(60,120,360,1000)) {
    $actual=Call $x 'GetActualBoundaryDegrees' @([double]$bpm,[double]1,[float]1)
    $native=[scrMisc]::TimeToAngleInRad(.01667,$bpm,1,$false)*57.295780181884766
    Equal ([Math]::Max([double]15,[double]$native)) $actual "15 degrees / time boundary at $bpm BPM"
}
$input=ModType 'InputController'
foreach($case in @(@('Alpha3','3'),@('Left Shift','LEFTSHIFT'),@('MouseLeft','MOUSE1'),@('Forward Slash','SLASH'),@('RControl','RIGHTCONTROL'))) {
    Equal $case[1] (Call $input 'Normalize' @($case[0])) "key alias $($case[0])"
}
Equal 'MOUSE1' (Call $input 'Normalize' @([UnityEngine.KeyCode]::Mouse0)) 'typed mouse left'
Equal 'MOUSE2' (Call $input 'Normalize' @([UnityEngine.KeyCode]::Mouse1)) 'typed mouse right'
$settings=[Activator]::CreateInstance((ModType 'DonQuixoteSettings'))
Equal 'Google Sans' $settings.Overlay.Font 'GoogleSans default'
$settings.Overlay.Font='Galmuri'
Call (ModType 'SettingsNormalization') 'Normalize' @($settings)|Out-Null
Equal 'Google Sans' $settings.Overlay.Font 'previous font preference adopts requested GoogleSans'
$reader=[LifecycleTests].GetMethod('Calls',$flags)
$styleCalls=$reader.Invoke($null,@((ModType 'XPerfectModule').GetMethod('StyleText',$flags)))
Equal $false (@($styleCalls|Where-Object {$_.DeclaringType -eq (ModType 'FontAssetProvider')}).Count -gt 0) 'judgment text does not use mod font provider'
Equal $true (@($styleCalls|Where-Object {$_.Name -eq 'set_fontSharedMaterial'}).Count -gt 0) 'native judgment font material retained'
$uiCalls=$reader.Invoke($null,@((ModType 'DarkNeonTheme').GetMethod('FontFor',$flags)))
Equal $true (@($uiCalls|Where-Object {$_.Name -eq 'get_GoogleSans'}).Count -gt 0) 'all settings text uses GoogleSans entry point'
$overlayCalls=$reader.Invoke($null,@((ModType 'FontAssetProvider').GetProperty('OverlayFont',$flags).GetGetMethod($true)))
Equal $true (@($overlayCalls|Where-Object {$_.Name -eq 'get_GoogleSans'}).Count -gt 0) 'overlay uses GoogleSans entry point'
Add-Type -AssemblyName System.Drawing
$fontCollection=New-Object System.Drawing.Text.PrivateFontCollection
try {
    $fontCollection.AddFontFile((Join-Path $project 'Assets\GoogleSans-Regular.ttf'))
    Equal 'Google Sans' $fontCollection.Families[0].Name 'bundled static font is valid GoogleSans'
} finally { $fontCollection.Dispose() }
Equal $true (Test-Path -LiteralPath (Join-Path $project 'bin\Release\GhostifyOverlay\Assets\GoogleSans-OFL.txt')) 'font license included in Release'
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
Call $x 'Reset' @()|Out-Null
function Record([string]$judge) {
    $x.GetField('_lastJudge',$flags).SetValue($null,[Enum]::Parse((ModType 'DetailedJudge'),$judge))
    Call $x 'Record' @([HitMargin]::Perfect)|Out-Null
}
Record 'XPerfect'
Call $x 'MarkCheckpoint' @()|Out-Null
Record 'MinusPerfect';Record 'PlusPerfect'
Equal 1 $x.GetProperty('XCount').GetValue($null,$null) 'X count'
Equal 1 $x.GetProperty('MinusCount').GetValue($null,$null) 'minus count'
Equal 1 $x.GetProperty('PlusCount').GetValue($null,$null) 'plus count'
Call $x 'Revert' @()|Out-Null
Equal 1 $x.GetProperty('XCount').GetValue($null,$null) 'checkpoint keeps previous X'
Equal 0 $x.GetProperty('MinusCount').GetValue($null,$null) 'checkpoint removes minus'
Equal 0 $x.GetProperty('PlusCount').GetValue($null,$null) 'checkpoint removes plus'
$settings.Judgments.HideAll=$true
Record 'XPerfect'
Equal 2 $x.GetProperty('XCount').GetValue($null,$null) 'hidden judgment text still counts'
Call $x 'Reset' @()|Out-Null
Equal 0 $x.GetProperty('XCount').GetValue($null,$null) 'new run resets X'
$settings.Input.KeyLimiterEnabled=$true
Call (ModType 'SettingsNormalization') 'Normalize' @($settings)|Out-Null
Equal $false $settings.Input.KeyLimiterEnabled 'empty whitelist cannot enable limiter'
$settings.Input.AllowedKeys.Add('D')
$settings.Input.KeyLimiterEnabled=$true
Call (ModType 'SettingsNormalization') 'Normalize' @($settings)|Out-Null
Equal $true $settings.Input.KeyLimiterEnabled 'explicit whitelist supports limiter'
$keys=New-Object 'System.Collections.Generic.List[AnyKeyCode]'
foreach($code in @([UnityEngine.KeyCode]::D,[UnityEngine.KeyCode]::A,[UnityEngine.KeyCode]::D)) { $keys.Add([AnyKeyCode]::new($code)) }
Call $input 'FilterKeys' @($keys,$settings.Input)|Out-Null
Equal 2 $keys.Count 'limiter removes disallowed key and preserves repeated allowed hits'
Equal 'D' (Call $input 'Normalize' @($keys[0].value)) 'first allowed key retained'
$settings.Input.AllowedKeys.Clear();$settings.Input.AllowedKeys.Add('A')
Call $input 'FilterKeys' @($keys,$settings.Input)|Out-Null
Equal 0 $keys.Count 'whitelist update invalidates cache'
$settings.Input.AllowedKeys.Clear();$settings.Input.AllowedKeys.Add('D')
foreach($case in @(@(1,$false,$false),@(2,$false,$true),@(1,$true,$true))) {
    Equal $case[2] (Call (ModType 'AttemptTracker') 'IsProgressStart' @([int]$case[0],[bool]$case[1])) 'attempt split'
}
foreach($name in @('EditorModules','MusicLibraryController','RecordingController','InputEventDecisions','DmNoteController','PlanetSettings')) {
    Equal $null $assembly.GetType('DonQuixoteOverlay.'+$name) "excluded type $name"
}
Equal $true ($null -ne $assembly.GetType('DonQuixoteOverlay.EffectGate')) 'requested original effects are included'
Equal $null (ModType 'InputSettings').GetField('ChatterEnabled') 'chatter setting absent'
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
Equal $true $loaded.Judgments.HideAll 'text hiding reload'
Equal 'D' $loaded.Input.AllowedKeys[0] 'whitelist reload'
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
