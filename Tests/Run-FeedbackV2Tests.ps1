param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot
$taskManaged=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
foreach($taskDir in @($taskManaged,(Join-Path $taskManaged 'UnityModManager'))){foreach($taskFile in Get-ChildItem -LiteralPath $taskDir -Filter *.dll){try{[Reflection.Assembly]::LoadFrom($taskFile.FullName)|Out-Null}catch{}}}
$taskMod=[Reflection.Assembly]::LoadFrom((Join-Path $taskProject 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
$taskFlags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$taskPassed=0
function Check($ok,[string]$name){if(!$ok){throw ('FAIL '+$name)};$script:taskPassed++;'PASS '+$name}
function T([string]$name){$taskMod.GetType('DonQuixoteOverlay.'+$name,$true)}
function Call($target,[string]$name,[object[]]$values){
    $type=if($target -is [Type]){$target}else{$target.GetType()};$instance=if($target -is [Type]){$null}else{$target}
    for($i=0;$i -lt $values.Count;$i++){if($null -ne $values[$i]){$values[$i]=$values[$i].PSObject.BaseObject}}
    $type.GetMethod($name,$taskFlags).Invoke($instance,$values)
}
function Field($object,[string]$name){$object.GetType().GetField($name,$taskFlags).GetValue($object)}
foreach($count in @(3,12,102,10002)){
    Check ((Call (T 'TileProgress') 'Fraction' @(1,$count)) -eq 0) ('first tile starts at zero: '+$count)
    Check ((Call (T 'TileProgress') 'Fraction' @(($count-1),$count)) -eq 1) ('last tile reaches one: '+$count)
    Check ((Call (T 'TileProgress') 'Fraction' @(-1,$count)) -eq 0 -and (Call (T 'TileProgress') 'Fraction' @(($count+9),$count)) -eq 1) ('tile progress clamps at both ends: '+$count)
}
Check ([Math]::Abs((Call (T 'TileProgress') 'Fraction' @(51,102))-.5) -lt .00001) 'half of the tile transitions equals half of the progress'
foreach($count in @(0,1,2)){Check ((Call (T 'TileProgress') 'Fraction' @(1,$count)) -eq 0) ('empty and degenerate maps have safe progress: '+$count)}
$fps=[Activator]::CreateInstance((T 'OverlayFpsCounter'),$true)
Check (!(Call $fps 'Tick' @([double]0))) 'FPS starts without a fabricated sample'
for($i=1;$i -lt 6;$i++){Check (!(Call $fps 'Tick' @([double]($i/60)))) ('FPS remains unchanged before 100 milliseconds: '+$i)}
Check (Call $fps 'Tick' @([double]0.1)) 'FPS publishes after 100 milliseconds'
Check ($fps.GetType().GetProperty('Value',$taskFlags).GetValue($fps,$null) -eq 60) 'sampled frame count reports 60 FPS'
Check (!(Call $fps 'Tick' @([double]::NaN))) 'invalid frame clock does not publish FPS'
for($i=1;$i -lt 12;$i++){Call $fps 'Tick' @([double](.1+$i/120))|Out-Null}
Check ((Call $fps 'Tick' @([double]0.2)) -and $fps.GetType().GetProperty('Value',$taskFlags).GetValue($fps,$null) -eq 120) 'next 100 milliseconds update independently to 120 FPS'
$geometry=T 'KeyViewerContents.KeyViewerGeometry'
foreach($style in [Enum]::GetValues((T 'KeyViewerContents.KeyviewerStyle'))){
    foreach($footer in @($false,$true)){
        $slots=@(Call $geometry 'Hands' @($style,$footer));$hands=@($slots|Where-Object {(Field $_ 'Index') -ge 0})
        $expected=switch($style.ToString()){'Key10'{10};'Key12'{12};'Key16'{16}}
        Check ($hands.Count -eq $expected -and @($hands|ForEach-Object {Field $_ 'Index'}|Sort-Object -Unique).Count -eq $expected) ('preview / runtime geometry contains each key once: '+$style+'/'+$footer)
        if($style.ToString() -eq 'Key16'){Check (@($hands|Where-Object {(Field $_ 'Width') -ne 50 -or (Field $_ 'Height') -ne 50}).Count -eq 0) ('all sixteen hand keys retain upstream 50 x 50 size: '+$footer)}
        foreach($slot in $hands){$source=Field $slot 'RainSource';if($source -ge 0){Check (@($hands|Where-Object {(Field $_ 'Index') -eq $source -and (Field $_ 'RainRow') -eq 0}).Count -eq 1) ('lower key shares an existing top rain lane: '+$style+'/'+(Field $slot 'Index'))}}
    }
}
$foot=@(Call $geometry 'Feet' @(16))
Check (($foot|ForEach-Object {Field $_ 'Index'}) -join ',' -eq '20,22,24,26,21,23,25,27,28,30,32,34,29,31,33,35') 'foot preview retains original alternating left / right ordering'
Check (@($foot|Where-Object {(Field $_ 'Width') -ne 30 -or (Field $_ 'Height') -ne 30}).Count -eq 0) 'foot keys remain square'
$settings=[Activator]::CreateInstance((T 'DonQuixoteSettings'))
(T 'Main').GetField('Settings',$taskFlags).SetValue($null,$settings)
$tracker=T 'AttemptTracker';Call $tracker 'EndSession' @()|Out-Null
Call $tracker 'ObserveLevel' @('path:map-A')|Out-Null
$sessions=$tracker.GetField('SessionAttempts',$taskFlags).GetValue($null);$progressSessions=$tracker.GetField('SessionProgressAttempts',$taskFlags).GetValue($null)
$sessions['path:map-A']=3;$progressSessions['path:map-A']=4
$settings.Overlay.FullAttempts['path:map-A']=7;$settings.Overlay.FullProgressAttempts['path:map-A']=9
Call $tracker 'ObserveLevel' @('PATH:MAP-A')|Out-Null
Check ($sessions['path:map-A'] -eq 3 -and $progressSessions['path:map-A'] -eq 4) 'same map restarts retain session attempts case insensitively'
Call $tracker 'EndSession' @()|Out-Null
Check ($sessions.Count -eq 0 -and $progressSessions.Count -eq 0 -and $settings.Overlay.FullAttempts['path:map-A'] -eq 7 -and $settings.Overlay.FullProgressAttempts['path:map-A'] -eq 9) 'leaving a map resets both session counters while preserving both full counters'
Call $tracker 'ObserveLevel' @('path:map-A')|Out-Null
Check ($tracker.GetProperty('Attempts',$taskFlags).GetValue($null,$null) -eq 0) 'reopening the same map begins with zero session attempts'
$sessions['path:map-A']=2;Call $tracker 'ObserveLevel' @('path:map-B')|Out-Null;Call $tracker 'ObserveLevel' @('path:map-A')|Out-Null
Check ($sessions.Count -eq 0) 'switching maps does not retain an old session'
$settings.Overlay.ValueColor=[UnityEngine.Color]::new(1,0,0,128/255);$settings.Overlay.ShowFps=$false
Call (T 'OverlayPalette') 'Normalize' @($settings.Overlay)|Out-Null
Check ($null -eq (T 'OverlaySettings').GetField('JudgmentColors')) 'accumulated judgment custom colors are absent'
$red=$settings.Overlay.ValueColor
Check ((Call (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($red)) -eq '#FF000080') 'numeric color retains opacity'
Call (T 'OverlayPalette') 'Reset' @($settings.Overlay)|Out-Null
Check (!$settings.Overlay.ShowFps -and $settings.Overlay.FullAttempts['path:map-A'] -eq 7) 'color reset preserves visibility and full attempt records'
$fixedX=(T 'DQColors').GetField('XPerfect',$taskFlags).GetValue($null)
Check ((Call (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($fixedX)) -eq '#FFFFFFFF') 'fixed center X remains white'
$palette=[Activator]::CreateInstance((T 'ColorPaletteState'),$taskFlags,$null,@($red),$null)
Check ((Call (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($palette.GetType().GetProperty('Color',$taskFlags).GetValue($palette,$null))) -eq '#FF000080') 'opening HSV palette preserves its original HEX and alpha'
Check (!(Call $palette 'TryHex' @('not-a-color'))) 'invalid palette HEX does not replace draft color'
Check ((Call $palette 'TryHex' @('#00C8FFFF'))) 'valid sky blue HEX updates palette draft'
Check ((Call (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($palette.GetType().GetProperty('Color',$taskFlags).GetValue($palette,$null))) -eq '#00C8FFFF') 'HEX and HSV palette round-trip saturated sky blue'
Check ((Call (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($settings.Overlay.ValueColor)) -eq '#FFC939FF') 'draft palette edits leave numeric color unchanged until application'
$keySettings=[Activator]::CreateInstance((T 'KeyViewerContents.KeyViewerSetting'))
Check ((Call (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($keySettings.GhostRainColor)) -eq '#0EB4FCFF') 'default ghost rain matches requested auxiliary sky blue'
$keySettings.SchemaVersion=3;$keySettings.GhostRainColor=[UnityEngine.Color]::white;Call (T 'KeyViewerContents.KeyViewerStore') 'Normalize' @($keySettings)|Out-Null
Check ((Call (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($keySettings.GhostRainColor)) -eq '#0EB4FCFF') 'previous white ghost default migrates to requested sky blue'
$keySettings.SchemaVersion=3;$keySettings.GhostRainColor=$red;Call (T 'KeyViewerContents.KeyViewerStore') 'Normalize' @($keySettings)|Out-Null
Check ((Call (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($keySettings.GhostRainColor)) -eq '#FF000080') 'custom ghost color survives migration'
$fixture=Join-Path $PSScriptRoot ('feedback-v2-'+[Guid]::NewGuid().ToString('N'))
Call (T 'SettingsStore') 'Initialize' @($fixture)|Out-Null
$settings.Overlay.AttemptsColor=$red;$settings.Overlay.ValueColor=[UnityEngine.Color]::new(0,200/255,1,128/255);Call (T 'SettingsStore') 'Save' @($settings)|Out-Null
$loaded=Call (T 'SettingsStore') 'Load' @()
Check ((Call (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($loaded.Overlay.AttemptsColor)) -eq '#FF000080' -and (Call (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($loaded.Overlay.ValueColor)) -eq '#00C8FF80') 'attempt / value colors reload from disk with opacity intact'
Call (T 'KeyViewerContents.KeyViewerStore') 'Initialize' @($fixture)|Out-Null
$keys=(T 'KeyViewerContents.KeyViewerStore').GetField('Settings',$taskFlags).GetValue($null);$keys.footkey4Text[0]='발 별칭';Call (T 'KeyViewerContents.KeyViewerStore') 'Save' @()|Out-Null
Call (T 'KeyViewerContents.KeyViewerStore') 'Initialize' @($fixture)|Out-Null
Check ((T 'KeyViewerContents.KeyViewerStore').GetField('Settings',$taskFlags).GetValue($null).footkey4Text[0] -eq '발 별칭') 'foot key display names persist alongside hand aliases'
$cache=[Activator]::CreateInstance((T 'OverlayStatusTextCache'),$true)
$cacheArgs=@(7,5000,10000,9950,0,0,0,0,$false,[float]0)
Call $cache 'Update' $cacheArgs|Out-Null
Check (!(Call $cache 'Update' $cacheArgs)) 'unchanged overlay values reuse cached text'
$cache.GetType().GetField('ValueHex',$taskFlags).SetValue($cache,'00C8FF80');Call $cache 'Invalidate' @()|Out-Null
Check ((Call $cache 'Update' $cacheArgs) -and $cache.GetType().GetProperty('Text',$taskFlags).GetValue($cache,$null).Contains('<color=#00C8FF80>')) 'applying a color refreshes cached values even when statistics stay unchanged'
$layout=[Activator]::CreateInstance((T 'LayoutData'));$layout.SchemaVersion=3;$layout.TopLeftX=23;$layout.DetailedPerfectY=-45;$layout.TimingRangesX=-32;$layout.TimingRangesY=65
$normalize=(T 'SettingsNormalization').GetMethods($taskFlags)|Where-Object {$_.Name -eq 'Normalize' -and $_.GetParameters()[0].ParameterType.Name -eq 'LayoutData'}
$normalize.Invoke($null,@($layout))|Out-Null
Check ($layout.SchemaVersion -eq 5 -and $layout.TimingRangesX -eq 0 -and [Math]::Abs($layout.TimingRangesY+30) -lt .0001 -and $layout.TopLeftX -eq 23 -and $layout.DetailedPerfectY -eq -45) 'timing migration keeps its rendered position when the former parent becomes independent'
Call $tracker 'EndSession' @()|Out-Null
'Feedback v2 assertions passed: '+$taskPassed
