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
    $method=@($type.GetMethods($taskFlags)|Where-Object {$_.Name -eq $name -and $_.GetParameters().Length -eq $values.Count})[0]
    $method.Invoke($instance,$values)
}
function Hex($color){Call (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($color)}
$taskNative=T 'NativeJudgments'
$taskBoundary=[scrMisc].GetMethod('GetAdjustedTimeBoundaries',$taskFlags)
foreach($taskDifficulty in [Enum]::GetValues([Difficulty])){foreach($taskBpm in @(90,180,600)){foreach($taskPitch in @([float]0.5,[float]1.5)){foreach($taskScale in @([double]0.5,[double]1,[double]2)){
    $taskLimits=$taskBoundary.Invoke($null,@($taskDifficulty,[double]$taskBpm,$taskPitch,$taskScale))
    $taskOffset=([double]$taskLimits.XPerfect+[double]$taskLimits.Pure)*.5
    $taskEarly=[scrMisc]::GetHitMarginInSec($taskDifficulty,-$taskOffset,[float]$taskBpm,$taskPitch,$taskScale)
    $taskLate=[scrMisc]::GetHitMarginInSec($taskDifficulty,$taskOffset,[float]$taskBpm,$taskPitch,$taskScale)
    $taskCounts=New-Object int[] 16;$taskCounts[[int]$taskEarly]=11;$taskCounts[[int]$taskLate]=47;$taskCounts[[int][HitMargin]::XPerfect]=3;$taskCounts[[int][HitMargin]::Auto]=2
    $taskDisplay=Call $taskNative 'FormatDetailedCounts' @($taskCounts,' | ')
    Check ($taskEarly -eq [HitMargin]::PerfectMinus -and $taskLate -eq [HitMargin]::PerfectPlus -and $taskDisplay -eq '<color=#60FF4E>11</color> | <color=#FFFFFF>5</color> | <color=#60FF4E>47</color>') ('native negative / positive timing feeds Early / X+Auto / Late: '+$taskDifficulty+'/'+$taskBpm+'/'+$taskPitch+'/'+$taskScale)
}}}}
$taskCounts=New-Object int[] 16
$taskCounts[[int][HitMargin]::FailOverload]=6;$taskCounts[[int][HitMargin]::FailMiss]=19;$taskCounts[[int][HitMargin]::Multipress]=83;$taskCounts[[int][HitMargin]::OverPress]=91
Check ((Call $taskNative 'JudgmentCount' @($taskCounts,0)) -eq 6 -and (Call $taskNative 'JudgmentCount' @($taskCounts,8)) -eq 19) 'distinct Overload / Miss totals render on left / right, excluding multipress'
Check ((Call $taskNative 'JudgmentCount' @($null,0)) -eq 0 -and (Call $taskNative 'JudgmentCount' @($null,8)) -eq 0) 'startup tracker absence is safe at both ends'
$taskScheme=[ColourSchemeHitMargin]::new();$taskScheme.colourFail=[UnityEngine.Color]::new(.9,.1,.2,1);$taskScheme.colourMultipress=[UnityEngine.Color]::new(.1,.7,.9,1)
Check ((Call (T 'OverlayPalette') 'SchemeJudgmentColor' @($taskScheme,0)) -eq $taskScheme.SelectByHitMargin([HitMargin]::FailOverload) -and (Call (T 'OverlayPalette') 'SchemeJudgmentColor' @($taskScheme,8)) -eq $taskScheme.SelectByHitMargin([HitMargin]::FailMiss)) 'fixed end-column colors follow their actual native judgment types'
foreach($taskCaption in @($null,'','Artist Title','Artist - Title',"Artist`nTitle",'<color=#00000000>Artist Title</color>','ScnGame')){
    Check ((Call (T 'OverlayMetadata') 'SongDisplay' @('Title','Artist',$taskCaption)) -eq 'Artist - Title') ('song separator stays canonical regardless of native caption: '+$taskCaption)
}
Check ((Call (T 'OverlayMetadata') 'SongDisplay' @('','Artist','ScnGame')) -eq 'Artist' -and (Call (T 'OverlayMetadata') 'SongDisplay' @('Title','','ScnGame')) -eq 'Title') 'partial metadata does not add a dangling hyphen or scene fallback'
$taskOverlayType=T 'OverlaySettings'
foreach($taskRemoved in @('TextColor','ComboColor','SongInfoColor','TimingScaleColor','FpsColor','DetailedPlusMinusColor','DetailedXColor','JudgmentColors')){Check ($null -eq $taskOverlayType.GetField($taskRemoved)) ('obsolete custom color field removed: '+$taskRemoved)}
Check (@($taskOverlayType.GetFields()|Where-Object {$_.FieldType -eq [UnityEngine.Color]}).Count -eq 3) 'only values / attempts / progress bar are configurable overlay colors'
Check ($null -eq (T 'KeyViewerContents.KeyViewerSetting').GetField('RainColor3')) 'third-rain color is absent from settings'
foreach($taskStyle in [Enum]::GetValues((T 'KeyViewerContents.KeyviewerStyle'))){
    $taskSlots=@(Call (T 'KeyViewerContents.KeyViewerGeometry') 'Hands' @($taskStyle,$true))
    Check (@($taskSlots|Where-Object { $_.GetType().GetField('RainRow',$taskFlags).GetValue($_) -gt 1 }).Count -eq 0) ('retained hand geometry cannot create a third rain row: '+$taskStyle)
}
$taskPool=[Activator]::CreateInstance((T 'KeyViewerContents.RainPool'),$taskFlags,$null,[object[]]@($null),$null)
Check ($taskPool.GetType().GetField('_layers',$taskFlags).GetValue($taskPool).Length -eq 4) 'rain pool holds only two ghost and two normal layers'
$taskFixture=Join-Path $taskProject ('Backups\Feedback-0.4.3\LegacyColors-'+[Guid]::NewGuid().ToString('N'))
Call (T 'SettingsStore') 'Initialize' @($taskFixture)|Out-Null
$taskOldJson='{"SchemaVersion":3,"Overlay":{"TextColor":"#12345680","ValueColor":"#ABCDEF80","AttemptsColor":"#887766FF","ProgressBarColor":"#FFC939FF","ComboColor":"#FF0000FF","SongInfoColor":"#FF0000FF","TimingScaleColor":"#FF0000FF","FpsColor":"#FF0000FF","DetailedPlusMinusColor":"#FF0000FF","DetailedXColor":"#FF0000FF","JudgmentColors":["#FF0000FF"],"FullAttempts":{"map":9},"FullProgressAttempts":{"map":3}}}'
$taskFile=Join-Path $taskFixture 'UserData\settings.json';[IO.File]::WriteAllText($taskFile,$taskOldJson,[Text.UTF8Encoding]::new($false))
$taskLoaded=Call (T 'SettingsStore') 'Load' @()
Check ((Hex $taskLoaded.Overlay.ValueColor) -eq '#ABCDEF80' -and (Hex $taskLoaded.Overlay.AttemptsColor) -eq '#887766FF') 'old independent colors are ignored while retained shared colors survive'
Check ($taskLoaded.Overlay.FullAttempts['map'] -eq 9 -and $taskLoaded.Overlay.FullProgressAttempts['map'] -eq 3) 'removing color features does not erase full attempt history'
Call (T 'SettingsStore') 'Save' @($taskLoaded)|Out-Null
$taskSaved=[IO.File]::ReadAllText($taskFile)
Check ($taskSaved -notmatch 'TextColor|JudgmentColors|ComboColor|SongInfoColor|TimingScaleColor|FpsColor|DetailedPlusMinusColor|DetailedXColor') 'saved settings no longer contain removed color fields'
$taskKeys=Call (T 'KeyViewerContents.KeyViewerStore') 'Deserialize' @('{"SchemaVersion":5,"RainColor":"#123456FF","RainColor2":"#ABCDEF80","RainColor3":"#FF0000FF","GhostRainColor":"#0EB4FCFF"}')
Check ((Hex $taskKeys.RainColor) -eq '#123456FF' -and (Hex $taskKeys.RainColor2) -eq '#ABCDEF80' -and (Hex $taskKeys.GhostRainColor) -eq '#0EB4FCFF') 'legacy third-rain settings cannot overwrite retained normal / ghost rain colors'
$taskCache=[Activator]::CreateInstance((T 'OverlayStatusTextCache'),$true)
$taskCache.GetType().GetField('ValueHex',$taskFlags).SetValue($taskCache,'ABCDEF80')
Call $taskCache 'Update' @(31,4000,9900,9800,12,90,30,120,$false,[float]0)|Out-Null
$taskText=$taskCache.GetType().GetProperty('Text',$taskFlags).GetValue($taskCache,$null)
Check ($taskText.Contains('Music Time | <color=#ABCDEF80>0:12-1:30</color>') -and $taskText.Contains('Map Time | <color=#ABCDEF80>0:30-2:00</color>')) 'both time rows use the same numeric color as progress and accuracy'
Add-Type -Path (Join-Path $PSScriptRoot 'ILReader.cs')
$taskInstructions=[LifecycleTests].GetMethod('Instructions',$taskFlags)
$taskApply=$taskInstructions.Invoke($null,@((T 'OverlayController').GetMethod('ApplySettings',$taskFlags)))
$taskFields=@($taskApply|ForEach-Object {$_.GetType().GetField('Member',$taskFlags).GetValue($_)}|Where-Object {$_ -is [Reflection.FieldInfo] -and $_.DeclaringType -eq $taskOverlayType})
Check (@($taskFields|Where-Object {$_.Name -eq 'TextColor'}).Count -eq 0 -and @($taskFields|Where-Object {$_.Name -eq 'AttemptsColor'}).Count -eq 1) 'actual renderer has no custom label color and keeps attempts separate'
$taskUpdate=$taskInstructions.Invoke($null,@((T 'OverlayController').GetMethod('Update',$taskFlags)))
Check (@($taskUpdate|ForEach-Object {$_.GetType().GetField('Member',$taskFlags).GetValue($_)}|Where-Object {$_ -is [Reflection.FieldInfo] -and $_.DeclaringType -eq $taskOverlayType -and $_.Name -eq 'ValueColor'}).Count -eq 1) 'actual combo number reads the shared value color'
'Feedback v5 assertions passed: '+$taskPassed
