param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot
$taskManaged=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
foreach($taskDir in @($taskManaged,(Join-Path $taskManaged 'UnityModManager'))){foreach($taskFile in Get-ChildItem -LiteralPath $taskDir -Filter *.dll){try{[Reflection.Assembly]::LoadFrom($taskFile.FullName)|Out-Null}catch{}}}
$taskMod=[Reflection.Assembly]::LoadFrom((Join-Path $taskProject 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
$taskFlags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$taskPassed=0
function T([string]$name){$taskMod.GetType('DonQuixoteOverlay.'+$name,$true)}
function Invoke-Member($target,[string]$name,[object[]]$values){for($i=0;$i -lt $values.Count;$i++){if($null -ne $values[$i]){$values[$i]=$values[$i].PSObject.BaseObject}};$target.GetType().GetMethod($name,$taskFlags).Invoke($target,$values)}
function Check($condition,[string]$label){if(!$condition){throw ('FAIL '+$label)};$script:taskPassed++;'PASS '+$label}
function New-Repeat{[Activator]::CreateInstance((T 'LayoutKeyRepeat'),$true)}
$taskRepeat=New-Repeat
Check ((Invoke-Member $taskRepeat 'Poll' @([int]-1,[double]0)) -eq -1) 'first press is immediate'
Check ((Invoke-Member $taskRepeat 'Poll' @([int]-1,[double]0.2)) -eq 0) 'short hold waits before repeating'
Check ((Invoke-Member $taskRepeat 'Poll' @([int]-1,[double]0.28)) -eq -1) 'repeat begins after 280ms'
Check ((Invoke-Member $taskRepeat 'Poll' @([int]-1,[double]0.5)) -lt -1) 'slow frames catch up more than one repeat'
Check ((Invoke-Member $taskRepeat 'Poll' @([int]-1,[double]50)) -eq -8) 'stalls cap catch-up to prevent large jumps'
Check ((Invoke-Member $taskRepeat 'Poll' @([int]1,[double]50.01)) -eq 1) 'direction change responds immediately'
Check ((Invoke-Member $taskRepeat 'Poll' @([int]0,[double]50.02)) -eq 0) 'release or opposite arrows cancel motion'
Check ((Invoke-Member $taskRepeat 'Poll' @([int]1,[double]50.03)) -eq 1) 'a fresh press moves immediately after release'
foreach($taskInvalid in @([double]::NaN,[double]::PositiveInfinity,[double]::NegativeInfinity)){Check ((Invoke-Member $taskRepeat 'Poll' @([int]1,$taskInvalid)) -eq 0) 'invalid clock resets repeat safely'}
$taskCounts=@()
foreach($taskRate in @(30,60,144)){
    $taskRepeat=New-Repeat;$taskTotal=0
    for($taskFrame=0;$taskFrame -le $taskRate;$taskFrame++){$taskTotal+=Invoke-Member $taskRepeat 'Poll' @([int]1,([double]$taskFrame/$taskRate))}
    $taskCounts+=@($taskTotal)
    Check ($taskTotal -ge 19 -and $taskTotal -le 20) ('consistent held movement at '+$taskRate+' FPS')
}
Check ((($taskCounts|Measure-Object -Maximum).Maximum-($taskCounts|Measure-Object -Minimum).Minimum) -le 1) 'repeat speed is independent of rendering frame rate'
$taskLayout=[Activator]::CreateInstance((T 'LayoutData'))
$taskKeys=[Activator]::CreateInstance((T 'KeyViewerContents.KeyViewerSetting'))
$taskFields=@($taskLayout.GetType().GetFields([Reflection.BindingFlags]'Public,Instance'))
$taskBefore=@{}
foreach($taskField in $taskFields){$taskBefore[$taskField.Name]=$taskField.GetValue($taskLayout)}
$taskKeys.XLocation=37;$taskKeys.YLocation=281;$taskKeys.Size=1.3
$taskCtor=(T 'LayoutEditSnapshot').GetConstructors($taskFlags)[0]
$taskSnapshot=$taskCtor.Invoke(@($taskLayout,$taskKeys))
foreach($taskField in $taskFields){if($taskField.FieldType -eq [single]){$taskField.SetValue($taskLayout,[single]99)}else{$taskField.SetValue($taskLayout,[int]1)}}
$taskKeys.XLocation=200;$taskKeys.YLocation=-200;$taskKeys.Size=2
Invoke-Member $taskSnapshot 'Restore' @($taskLayout,$taskKeys)|Out-Null
foreach($taskField in $taskFields){Check ($taskField.GetValue($taskLayout) -eq $taskBefore[$taskField.Name]) ('cancel restores '+$taskField.Name)}
Check ($taskKeys.XLocation -eq 37 -and $taskKeys.YLocation -eq 281 -and [Math]::Abs($taskKeys.Size-1.3) -lt .0001) 'cancel restores keyviewer translation and scale'
$taskUi=T 'DarkNeonUi'
$taskWidth=$taskUi.GetField('SwitchWidth',$taskFlags).GetRawConstantValue()
$taskHeight=$taskUi.GetField('SwitchHeight',$taskFlags).GetRawConstantValue()
$taskThumb=$taskUi.GetField('SwitchThumbSize',$taskFlags).GetRawConstantValue()
$taskInset=$taskUi.GetField('SwitchInset',$taskFlags).GetRawConstantValue()
$taskTravel=$taskUi.GetField('SwitchTravel',$taskFlags).GetRawConstantValue()
Check ($taskWidth -eq 46 -and $taskHeight -eq 26 -and $taskThumb -eq 20) 'compiled UI uses smaller cylindrical toggle dimensions'
Check ((44-$taskHeight)/2 -eq 9) '44px cards have nine pixel vertical margins around switch'
Check (($taskHeight-$taskThumb)/2 -eq $taskInset) 'circular thumb has equal vertical insets'
Check ($taskTravel+$taskThumb/2 -eq $taskWidth/2-$taskInset) 'thumb stays inside track at both toggle positions'
$taskSource=[IO.File]::ReadAllText((Join-Path $taskProject 'SettingsWindow.cs'))
Check ($taskSource -notmatch 'Numeric\("위치 [XY]"|Numeric\("전체 배율') 'settings window has no remaining placement numeric controls'
Check ($taskSource -match 'LayoutEditorController>\(\)\.Begin\(6\)') 'keyviewer page opens editing with keyviewer selected'
'Layout editor assertions passed: '+$taskPassed
