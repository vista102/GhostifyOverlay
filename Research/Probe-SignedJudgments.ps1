$ErrorActionPreference='Stop'
$taskManaged='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice\A Dance of Fire and Ice_Data\Managed'
foreach($taskFile in Get-ChildItem -LiteralPath $taskManaged -Filter *.dll){try{[Reflection.Assembly]::LoadFrom($taskFile.FullName)|Out-Null}catch{}}
$taskFlags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$taskBound=[scrMisc].GetMethod('GetAdjustedTimeBoundaries',$taskFlags)
foreach($taskDifficulty in [Enum]::GetValues([Difficulty])) {
 $taskValues=$taskBound.Invoke($null,@($taskDifficulty,[double]180,[float]1,[double]1))
 $taskDelta=([double]$taskValues.XPerfect+[double]$taskValues.Pure)*.5
 $taskEarly=[scrMisc]::GetHitMarginInSec($taskDifficulty,-$taskDelta,[float]180,[float]1,[double]1)
 $taskLate=[scrMisc]::GetHitMarginInSec($taskDifficulty,$taskDelta,[float]180,[float]1,[double]1)
 "Native $taskDifficulty : early delta=$(-$taskDelta) => $taskEarly ; late delta=$taskDelta => $taskLate"
}
'HitMarginHelper methods:'
[HitMarginHelper].GetMethods($taskFlags)|Where-Object {$_.Name -match 'Color|Colour|Text|Early|Late'}|ForEach-Object {$_.ToString()}
'Colour scheme:'
[ColourSchemeHitMargin].GetMethods($taskFlags)|Where-Object {$_.Name -match 'Select'}|ForEach-Object {$_.ToString()}
'Native results methods:'
[DetailedResults].GetMethods($taskFlags)|Where-Object {$_.Name -match 'Result|Generate'}|ForEach-Object {$_.ToString()}
'Native sign preset values:'
[Enum]::GetNames([HitMarginPerfectTextPreset])
'WorldData fields:'
[WorldData].GetFields($taskFlags)|ForEach-Object {$_.ToString()}
'WorldData properties:'
[WorldData].GetProperties($taskFlags)|ForEach-Object {$_.ToString()}
