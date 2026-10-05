param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$managed=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
foreach($dir in @($managed,(Join-Path $managed 'UnityModManager'))){foreach($file in Get-ChildItem -LiteralPath $dir -Filter '*.dll'){try{[Reflection.Assembly]::LoadFrom($file.FullName)|Out-Null}catch{}}}
$project=Split-Path $PSScriptRoot
$mod=[Reflection.Assembly]::LoadFrom((Join-Path $project 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$passed=0
function T([string]$name){$mod.GetType('DonQuixoteOverlay.'+$name,$true)}
function Invoke-Method($type,[string]$name,[object[]]$values){for($i=0;$i -lt $values.Count;$i++){$values[$i]=$values[$i].PSObject.BaseObject};try{$type.GetMethod($name,$flags).Invoke($null,$values)}catch{throw $_.Exception.ToString()}}
function Check($value,[string]$label){if(!$value){throw ('FAIL '+$label)};$script:passed++;'PASS '+$label}
$counts=New-Object int[] 16
$counts[[int][HitMargin]::XPerfect]=2;$counts[[int][HitMargin]::Auto]=3
Check ((Invoke-Method (T 'OverlayController') 'JudgmentCount' @($counts,[int]4)) -eq 5) 'ordinary Perfect includes three Auto tiles exactly once'
$counts[[int][HitMargin]::TooEarly]=4
Check ((Invoke-Method (T 'OverlayController') 'JudgmentCount' @($counts,[int]1)) -eq 4) 'Auto inclusion does not alter the early judgment bucket'
Check ((Invoke-Method (T 'OverlayController') 'JudgmentCount' @((New-Object int[] 2),[int]4)) -eq 0) 'short or absent trackers safely return zero'
$settings=[Activator]::CreateInstance((T 'DonQuixoteSettings'))
(T 'Main').GetField('Settings',$flags).SetValue($null,$settings)
$metrics=T 'KeyViewerContents.KeyViewerMetrics'
$side=$metrics.GetField('HandSide').GetRawConstantValue()
$step=$metrics.GetField('HandStep').GetRawConstantValue()
Check ($side -eq 50) 'sixteen input frames keep the upstream 50x50 size'
Check ($step-$side -eq 4) 'fixed square grid retains four-unit gaps'
Check ((Invoke-Method $metrics 'RainWidth' @([int]1)) -eq $side) 'front rain width exactly matches the fixed square key'
Check ($metrics.GetField('FooterWidth').GetRawConstantValue() -eq 212) 'KPS and Total retain upstream footer widths'
Check ((T 'KeyViewerContents.KeyViewer').GetMethod('ArrangeSquareHands',$flags) -eq $null) 'long labels and Total counts cannot enlarge the sixteen-key grid'
$keySettings=[Activator]::CreateInstance((T 'KeyViewerContents.KeyViewerSetting'))
$keySettings.SchemaVersion=1;$keySettings.XLocation=375;$keySettings.YLocation=420;$keySettings.Size=1.4;$keySettings.key16Text[0]='alias'
$normalized=Invoke-Method (T 'KeyViewerContents.KeyViewerStore') 'Normalize' @($keySettings)
$pressed=Invoke-Method (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($normalized.BackgroundClicked)
Check ($pressed -eq '#FFC939FF' -and $normalized.SchemaVersion -eq 3) 'old orange key theme migrates to the attached yellow'
Check ($normalized.XLocation -eq 375 -and $normalized.YLocation -eq 420 -and [Math]::Abs($normalized.Size-1.4) -lt .001 -and $normalized.key16Text[0] -eq 'alias') 'theme migration preserves key position, scale and aliases'
$layout=[Activator]::CreateInstance((T 'LayoutData'))
$saved=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'UserSavedLayout-20261002.json') -Raw|ConvertFrom-Json
$matches=$true
foreach($property in $saved.PSObject.Properties){$actual=$layout.GetType().GetField($property.Name).GetValue($layout);if($property.Name -eq 'SchemaVersion'){$matches=$matches -and $actual -eq $property.Value}else{$matches=$matches -and $actual -eq [float]$property.Value}}
Check $matches 'fresh defaults match every saved overlay position and scale'
$accuracy=T 'OverlayAccuracy'
foreach($invalid in @([float]::NaN,[float]::PositiveInfinity,[float]::NegativeInfinity)) {
    Check ((Invoke-Method $accuracy 'DisplayKey' @($invalid)) -eq 10000) ('uninitialized accuracy has a finite 100.00% display: '+$invalid)
}
Check ((Invoke-Method $accuracy 'DisplayKey' @([float]100.92)) -eq 10092) 'real Perfect bonuses above 100% remain visible'
Check ((Invoke-Method $accuracy 'DisplayKey' @([float]0)) -eq 0) 'a real zero accuracy is preserved'
Check ((Invoke-Method $accuracy 'DisplayKey' @([float]-1)) -eq 0) 'negative accuracy cannot enter the display'
Check ((Invoke-Method $accuracy 'DisplayKey' @([float]98.125)) -eq 9812) 'accuracy uses the existing midpoint-to-even rounding'
Check ((Invoke-Method $accuracy 'DisplayKey' @([float]::MaxValue)) -eq [int]::MaxValue) 'extreme finite accuracy cannot overflow into a negative integer'
Add-Type -Path (Join-Path $PSScriptRoot 'ILReader.cs') -ReferencedAssemblies 'System.Core'
$calls=[LifecycleTests].GetMethod('Calls',$flags)
function Calls($type,[string]$method){$calls.Invoke($null,@($type.GetMethod($method,$flags)))}
Check (@(Calls (T 'OverlayController') 'RefreshStatusText'|Where-Object {$_.Name -eq 'DisplayKey'}).Count -eq 2) 'both accuracy displays sanitize native values before integer conversion'
Check (@(Calls (T 'OverlayController') 'ApplyFont'|Where-Object {$_.Name -eq 'ForFont'}).Count -eq 1 -and @(Calls (T 'OverlayController') 'ApplyFont'|Where-Object {$_.Name -eq 'UpdateMeshPadding'}).Count -eq 1) 'overlay applies the owned shadow preset and updates SDF padding'
Check (@(Calls (T 'OverlayController') 'OnDisable'|Where-Object {$_.Name -eq 'Dispose' -and $_.DeclaringType -eq (T 'OverlayTextShadow')}).Count -eq 1) 'disabling the mod releases its shadow preset'
Check ((T 'DQColors').GetField('AccentHex',$flags).GetRawConstantValue() -eq 'FFC939' -and (T 'DQColors').GetField('KeyAccentHex',$flags).GetRawConstantValue() -eq 'FFC939') 'all UI and key highlights share the requested yellow'
Check (@(Calls (T 'SettingsWindow') 'BeginCapture'|Where-Object {$_.Name -eq 'Acquire' -and $_.DeclaringType -eq (T 'KeyCaptureUiLease')}).Count -eq 1) 'both binding flows start with the verified Enter-safe UI lease'
Check (@(Calls (T 'SettingsWindow') 'StopCapture'|Where-Object {$_.Name -eq 'Dispose' -and $_.DeclaringType -eq (T 'KeyCaptureUiLease')}).Count -eq 1 -and @(Calls (T 'SettingsWindow') 'OnDestroy'|Where-Object {$_.Name -eq 'Dispose' -and $_.DeclaringType -eq (T 'KeyCaptureUiLease')}).Count -eq 1) 'capture completion and UI teardown release shared navigation'
$kind=(T 'UiButtonKind')
$primaryColor=Invoke-Method (T 'DarkNeonUi') 'ButtonText' @([Enum]::Parse($kind,'Primary'))
Check ((Invoke-Method (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($primaryColor)) -eq '#292929FF') 'yellow buttons retain readable dark labels'
'User adjustment assertions passed: '+$passed
