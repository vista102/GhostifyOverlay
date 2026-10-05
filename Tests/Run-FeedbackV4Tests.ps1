# Run in pwsh against the shipping mod assembly; never writes to the real game.
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
function Field($object,[string]$name){$object.GetType().GetField($name,$taskFlags).GetValue($object)}
function Hex($color){Call (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($color)}
function NormalizeLayout($layout){
    $method=(T 'SettingsNormalization').GetMethods($taskFlags)|Where-Object {$_.Name -eq 'Normalize' -and $_.GetParameters()[0].ParameterType.Name -eq 'LayoutData'}
    $method.Invoke($null,@($layout))|Out-Null
}
$taskMetadata=T 'OverlayMetadata'
Check ((Call $taskMetadata 'CleanLine' @('<color=#00000000>かめりあ (Camellia)' + "`n" + 'ΩΩPARTS</color>')) -eq 'かめりあ (Camellia) ΩΩPARTS') 'reported transparent color tags are removed while Japanese and title remain visible'
Check ((Call $taskMetadata 'SongText' @('<b><size=40>곡명</size></b>','<color=red>작곡가</color>')) -eq '작곡가 - 곡명') 'nested metadata formatting cannot appear as literal tags'
Check ((Call $taskMetadata 'CleanLine' @('one<br>two<BR />three')) -eq 'one two three') 'line break tags become single-line spaces'
Check ((Call $taskMetadata 'CleanLine' @('A <3 B / 1 < 2 > 0 / <unknown>literal</unknown>')) -eq 'A <3 B / 1 < 2 > 0 / <unknown>literal</unknown>') 'ordinary angle brackets and unknown literal text are preserved'
Check ((Call $taskMetadata 'SongText' @('<color=#00000000></color>','')) -eq '') 'formatting-only metadata has no visible song label'
$taskLong='긴 곡명 LONG TITLE '*300
Check ((Call $taskMetadata 'SongText' @($taskLong,'Composer')) -eq ('Composer - '+$taskLong.Trim())) 'long metadata is retained without ellipsis or truncation'
Check ((Call $taskMetadata 'SongDisplay' @('title','artist',"<color=#00000000>native artist`nnative title</color>")) -eq 'artist - title') 'a differently formatted native caption cannot replace canonical metadata'
Check ((Call $taskMetadata 'SongDisplay' @('title','artist','ScnGame')) -eq 'artist - title') 'a scene-name caption falls back to real metadata'
Check ((Call $taskMetadata 'SongDisplay' @('','', 'ScnGame')) -eq '') 'missing metadata cannot reintroduce a scene label'
Check ((Call $taskMetadata 'SongDisplay' @('title','artist','')) -eq 'artist - title') 'metadata remains visible before the native caption is initialized'
$taskLayout=[Activator]::CreateInstance((T 'LayoutData'))
$taskAnchor=[UnityEngine.Vector2]::new(27,118)
$taskPlacement=T 'OverlayPlacement'
$taskBefore=Call $taskPlacement 'Timing' @($taskAnchor,$taskLayout)
$taskLayout.DetailedPerfectX=123;$taskLayout.DetailedPerfectY=-71;$taskLayout.DetailedPerfectScale=1.7
$taskAfter=Call $taskPlacement 'Timing' @($taskAnchor,$taskLayout)
Check ($taskBefore.x -eq $taskAfter.x -and $taskBefore.y -eq $taskAfter.y) 'moving and scaling detailed judgments cannot move timing scale'
$taskDetail=Call $taskPlacement 'Detailed' @($taskAnchor,$taskLayout)
$taskLayout.TimingRangesX=-83;$taskLayout.TimingRangesY=47;$taskLayout.TimingRangesScale=1.3
$taskDetailAfter=Call $taskPlacement 'Detailed' @($taskAnchor,$taskLayout)
Check ($taskDetail.x -eq $taskDetailAfter.x -and $taskDetail.y -eq $taskDetailAfter.y) 'timing movement and scaling cannot move detailed judgments'
$taskOld=[Activator]::CreateInstance((T 'LayoutData'));$taskOld.SchemaVersion=4
$taskOld.DetailedPerfectX=124;$taskOld.DetailedPerfectY=-61;$taskOld.DetailedPerfectScale=1.2
$taskOld.TimingRangesX=-13;$taskOld.TimingRangesY=27;$taskOld.ComboY=-90;$taskOld.TopLeftX=25
$taskExpectedX=$taskAnchor.x+$taskOld.DetailedPerfectX+$taskOld.TimingRangesX
$taskExpectedY=$taskAnchor.y+$taskOld.DetailedPerfectY+38*$taskOld.DetailedPerfectScale+8+$taskOld.TimingRangesY
NormalizeLayout $taskOld
$taskMigrated=Call $taskPlacement 'Timing' @($taskAnchor,$taskOld)
Check ([Math]::Abs($taskMigrated.x-$taskExpectedX) -lt .0001 -and [Math]::Abs($taskMigrated.y-$taskExpectedY) -lt .0001) 'saved timing screen position survives detaching its former parent'
Check ($taskOld.ComboY -eq -90 -and $taskOld.TopLeftX -eq 25 -and $taskOld.SchemaVersion -eq 5) 'layout migration preserves other sections and updates schema'
$taskOnce=$taskOld.TimingRangesY;NormalizeLayout $taskOld
Check ($taskOld.TimingRangesY -eq $taskOnce) 'timing migration runs once across repeated normalization'
$taskStore=T 'KeyViewerContents.KeyViewerStore'
foreach($taskStyle in @('2','"Key20"')){
    $taskJson='{"SchemaVersion":4,"KeyViewerStyle":'+$taskStyle+',"key20":[97,98],"key20Text":["legacy hand"],"GhostKey20":[103],"FootKeyViewerStyle":"Key6","footkey6":[102,103,104,105,106,107],"footkey6Text":["legacy foot"],"Background":"#123456FF","Text":"#876543FF","XLocation":83,"YLocation":278}'
    $taskSettings=Call $taskStore 'Deserialize' @($taskJson)
    Check ($taskSettings.KeyViewerStyle.ToString() -eq 'Key16' -and $taskSettings.key16.Length -eq 16 -and [int]$taskSettings.key16[0] -eq 97 -and $taskSettings.key16Text[0] -eq 'legacy hand' -and [int]$taskSettings.GhostKey16[0] -eq 103) ('removed hand style migrates with bindings / names / ghosts: '+$taskStyle)
    Check ($taskSettings.FootKeyViewerStyle.ToString() -eq 'Key4' -and $taskSettings.footkey4.Length -eq 4 -and [int]$taskSettings.footkey4[3] -eq 105 -and $taskSettings.footkey4Text[0] -eq 'legacy foot') ('removed foot style migrates its first four bindings: '+$taskStyle)
    Check ((Hex $taskSettings.KpsColors.Background) -eq '#123456FF' -and (Hex $taskSettings.TotalColors.Value) -eq '#876543FF' -and $taskSettings.XLocation -eq 83 -and $taskSettings.YLocation -eq 278) ('legacy palette and location survive the split: '+$taskStyle)
    Check (![object]::ReferenceEquals($taskSettings.KpsColors,$taskSettings.TotalColors)) ('KPS and Total palette objects are independent: '+$taskStyle)
}
foreach($taskFootStyle in @('3','"Key6"')) {
    $taskSettings=Call $taskStore 'Deserialize' @('{"SchemaVersion":4,"KeyViewerStyle":3,"FootKeyViewerStyle":'+$taskFootStyle+'}')
    Check ($taskSettings.KeyViewerStyle.ToString() -eq 'Key10' -and $taskSettings.FootKeyViewerStyle.ToString() -eq 'Key4') ('existing 10-key numeric value survives legacy foot migration: '+$taskFootStyle)
}
foreach($taskPair in @(@('KeyViewerContents.KeyviewerStyle',3,'Key10'),@('KeyViewerContents.FootKeyviewerStyle',4,'Key8'),@('KeyViewerContents.FootKeyviewerStyle',5,'Key16'))){
    Check ([Enum]::ToObject((T $taskPair[0]),$taskPair[1]).ToString() -eq $taskPair[2]) ('unchanged saved enum number: '+$taskPair[2])
}
Check ([Enum]::GetNames((T 'KeyViewerContents.KeyviewerStyle')) -join ',' -eq 'Key12,Key16,Key10') 'only 10 / 12 / 16 hand layouts remain available'
Check ([Enum]::GetNames((T 'KeyViewerContents.FootKeyviewerStyle')) -join ',' -eq 'None,Key2,Key4,Key8,Key16') '6-foot-key layout is absent from the available styles'
foreach($taskType in @((T 'KeyViewerContents.KeyviewerStyle'),(T 'KeyViewerContents.FootKeyviewerStyle'))){
    $taskNext=(T 'SettingsWindow').GetMethod('NextStyle',$taskFlags).MakeGenericMethod($taskType)
    $taskValues=[Enum]::GetValues($taskType)
    for($taskI=0;$taskI -lt $taskValues.Count;$taskI++){
        Check ($taskNext.Invoke($null,@($taskValues[$taskI])) -eq $taskValues[($taskI+1)%$taskValues.Count]) ('settings cycle skips removed enum values: '+$taskValues[$taskI])
    }
}
$taskSettings=[Activator]::CreateInstance((T 'KeyViewerContents.KeyViewerSetting'))
$taskSettings.Background=[UnityEngine.Color]::red;$taskSettings.KpsColors.Background=[UnityEngine.Color]::blue;$taskSettings.TotalColors.Background=[UnityEngine.Color]::green
$taskSettings.KpsColors.Text=[UnityEngine.Color]::black;$taskSettings.KpsColors.Value=[UnityEngine.Color]::white
$taskSettings.TotalColors.Text=[UnityEngine.Color]::new(1,1,0,1);$taskSettings.TotalColors.Value=[UnityEngine.Color]::cyan
foreach($taskPressed in @($false,$true)){
    $taskKps=Call (T 'KeyViewerContents.KeyViewerColors') 'Resolve' @($taskSettings,-1,$taskPressed)
    $taskTotal=Call (T 'KeyViewerContents.KeyViewerColors') 'Resolve' @($taskSettings,-2,$taskPressed)
    Check ((Hex (Field $taskKps 'Background')) -eq '#0000FFFF' -and (Hex (Field $taskKps 'Text')) -eq '#000000FF' -and (Hex (Field $taskKps 'Value')) -eq '#FFFFFFFF') ('KPS rendering uses its own background / label / number colors: '+$taskPressed)
    Check ((Hex (Field $taskTotal 'Background')) -eq '#00FF00FF' -and (Hex (Field $taskTotal 'Text')) -eq '#FFFF00FF' -and (Hex (Field $taskTotal 'Value')) -eq '#00FFFFFF') ('Total rendering uses its own background / label / number colors: '+$taskPressed)
}
$taskNormal=Call (T 'KeyViewerContents.KeyViewerColors') 'Resolve' @($taskSettings,0,$false)
Check ((Hex (Field $taskNormal 'Background')) -eq '#FF0000FF') 'counter palettes cannot recolor ordinary keys'
$taskFixture=Join-Path $taskProject ('Backups\Feedback-0.4.2\ColorFixture-'+[Guid]::NewGuid().ToString('N'))
Call $taskStore 'Initialize' @($taskFixture)|Out-Null
$taskStore.GetField('Settings',$taskFlags).SetValue($null,$taskSettings)
Call $taskStore 'Save' @()|Out-Null;Call $taskStore 'Initialize' @($taskFixture)|Out-Null
$taskRestored=$taskStore.GetField('Settings',$taskFlags).GetValue($null)
Check ((Hex $taskRestored.Background) -eq '#FF0000FF' -and (Hex $taskRestored.KpsColors.Background) -eq '#0000FFFF' -and (Hex $taskRestored.TotalColors.Background) -eq '#00FF00FF') 'all three independent palettes round-trip through actual save and load'
Check ((Hex $taskRestored.KpsColors.Value) -eq '#FFFFFFFF' -and (Hex $taskRestored.TotalColors.Value) -eq '#00FFFFFF') 'independent number colors persist after reopening'
$taskSettings.KpsColors=$null;$taskSettings.TotalColors.Value=[UnityEngine.Color]::new([float]::NaN,2,-1,[float]::PositiveInfinity)
Call $taskStore 'Normalize' @($taskSettings)|Out-Null
Check ($null -ne $taskSettings.KpsColors -and $taskSettings.TotalColors.Value.g -eq 1 -and $taskSettings.TotalColors.Value.b -eq 0 -and $taskSettings.TotalColors.Value.a -eq 1) 'missing palettes and invalid color channels recover safely'
Add-Type -Path (Join-Path $PSScriptRoot 'ILReader.cs')
$taskCalls=[LifecycleTests].GetMethod('Calls',$taskFlags)
$taskUpdateCalls=$taskCalls.Invoke($null,@((T 'OverlayController').GetMethod('Update',$taskFlags)))
Check (@($taskUpdateCalls|Where-Object {$_.DeclaringType.Name -eq 'OverlayPlacement' -and $_.Name -eq 'Detailed'}).Count -eq 1 -and @($taskUpdateCalls|Where-Object {$_.DeclaringType.Name -eq 'OverlayPlacement' -and $_.Name -eq 'Timing'}).Count -eq 1) 'actual overlay update uses both independently tested position calculations'
$taskKeyCalls=$taskCalls.Invoke($null,@((T 'KeyViewerContents.Key').GetMethod('UpdateKey',$taskFlags)))
Check (@($taskKeyCalls|Where-Object {$_.DeclaringType.Name -eq 'KeyViewerColors' -and $_.Name -eq 'Resolve'}).Count -eq 1) 'actual key renderer uses the independently tested palette selection'
'Feedback v4 assertions passed: '+$taskPassed
