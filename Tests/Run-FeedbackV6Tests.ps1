param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot
$taskManaged=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
foreach($taskDir in @($taskManaged,(Join-Path $taskManaged 'UnityModManager'))){foreach($taskFile in Get-ChildItem -LiteralPath $taskDir -Filter *.dll){try{[Reflection.Assembly]::LoadFrom($taskFile.FullName)|Out-Null}catch{}}}
$taskMod=[Reflection.Assembly]::LoadFrom((Join-Path $taskProject 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
$taskFlags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$taskPassed=0
function Check($okay,[string]$name){if(!$okay){throw ('FAIL '+$name)};$script:taskPassed++;'PASS '+$name}
function T([string]$name){$taskMod.GetType('DonQuixoteOverlay.'+$name,$true)}
function Call($target,[string]$name,[object[]]$values){$type=if($target -is [Type]){$target}else{$target.GetType()};$instance=if($target -is [Type]){$null}else{$target};for($i=0;$i -lt $values.Count;$i++){if($null -ne $values[$i]){$values[$i]=$values[$i].PSObject.BaseObject}};$type.GetMethod($name,$taskFlags).Invoke($instance,$values)}
function F($object,[string]$name){$object.GetType().GetField($name,$taskFlags).GetValue($object)}
$fps=[Activator]::CreateInstance((T 'OverlayFpsCounter'),$true)
Call $fps 'Tick' @([double]0)|Out-Null
$updates=0
for($i=1;$i -le 250;$i++){if(Call $fps 'Tick' @([double]($i/250))){$updates++;Check ($fps.GetType().GetProperty('Value',$taskFlags).GetValue($fps,$null) -eq 250) ('FPS measurement stays correct at sample '+$updates)}}
Check ($updates -eq 10) 'FPS publishes ten samples per second at 250 FPS'
Check (!(Call $fps 'Tick' @([double]::NaN)) -and !(Call $fps 'Tick' @([double]::PositiveInfinity))) 'invalid clocks do not publish'
foreach($item in @(@(5000,'50%'),@(10000,'100%'),@(15000,'150%'),@(12345,'123.45%'))){Check ((Call (T 'OverlayController') 'FormatPitch' @([int]$item[0])) -eq $item[1]) ('Pitch percentage '+$item[1])}
Check ((Call (T 'OverlayController') 'FormatAttempts' @(2,3,4,5)) -eq "Attempt 2`nPrac Attempt 3`nFull Attempt 4`nFull Prac Attempt 5") 'both practice labels change while counts stay independent'
$geometry=T 'KeyViewerContents.KeyViewerGeometry'
foreach($style in [Enum]::GetValues((T 'KeyViewerContents.KeyviewerStyle'))){foreach($footer in @($false,$true)){foreach($gap in @([float]0,[float]4,[float]12,[float]30)){
    $slots=@(Call $geometry 'HandsWithGap' @($style,$footer,$gap));$keys=@($slots|Where-Object {(F $_ 'Index') -ge 0});$top=@($keys|Where-Object {(F $_ 'RainRow') -eq 0}|Sort-Object {F $_ 'X'})
    Check (@($keys|Where-Object {(F $_ 'Width') -ne 50 -or (F $_ 'Height') -ne 50}).Count -eq 0) ('hand sizes remain fixed '+$style+'/'+$footer+'/'+$gap)
    Check (@(1..7|Where-Object {[Math]::Abs((F $top[$_] 'X')-(F $top[$_-1] 'X')-50-$gap) -gt .0001}).Count -eq 0) ('horizontal gaps match '+$style+'/'+$footer+'/'+$gap)
    foreach($slot in $keys){$source=F $slot 'RainSource';if($source -ge 0){$upper=@($top|Where-Object {(F $_ 'Index') -eq $source})[0];Check ((F $slot 'X') -eq (F $upper 'X')) ('lower key remains aligned to its shared rain '+$style+'/'+$gap+'/'+(F $slot 'Index'))}}
    if($gap -eq 4){$original=@(Call $geometry 'Hands' @($style,$footer));$actual=($slots|ForEach-Object {"$(F $_ 'Index')/$(F $_ 'X')/$(F $_ 'Y')/$(F $_ 'Width')"}) -join ',';$expected=($original|ForEach-Object {"$(F $_ 'Index')/$(F $_ 'X')/$(F $_ 'Y')/$(F $_ 'Width')"}) -join ',';Check ($actual -eq $expected) ('default geometry unchanged '+$style+'/'+$footer)}
}}}
foreach($count in @(2,4,8,16)){foreach($gap in @([float]0,[float]4,[float]12,[float]30)){
    $feet=@(Call $geometry 'FeetWithGap' @($count,$gap))
    Check (@($feet|Where-Object {(F $_ 'Width') -ne 30 -or (F $_ 'Height') -ne 30}).Count -eq 0) ('foot sizes stay fixed '+$count+'/'+$gap)
    Check ((($feet|ForEach-Object {F $_ 'X'}|Measure-Object -Minimum).Minimum) -eq 400+8*$gap) ('foot bank follows the hand bank '+$count+'/'+$gap)
}}
Check ((Call (T 'KeyViewerContents.KeyViewerMetrics') 'RainWidth' @(1)) -eq 50 -and (Call (T 'KeyViewerContents.KeyViewerMetrics') 'RainWidth' @(2)) -eq 40) 'normal and secondary rain widths remain unchanged'
$keys=Call (T 'KeyViewerContents.KeyViewerStore') 'Deserialize' @('{"SchemaVersion":5,"rainHeight":333,"rainSpeed":155,"key16Text":["saved name"]}')
Check ($keys.KeyGap -eq 4 -and $keys.rainHeight -eq 333 -and $keys.rainSpeed -eq 155 -and $keys.key16Text[0] -eq 'saved name') 'old keyviewer files receive only the default gap and retain rain settings and aliases'
foreach($item in @(@([float]::NaN,[float]4),@([float]::PositiveInfinity,[float]4),@([float]-1,[float]0),@([float]90,[float]30))){$keys.KeyGap=$item[0];Call (T 'KeyViewerContents.KeyViewerStore') 'Normalize' @($keys)|Out-Null;Check ($keys.KeyGap -eq $item[1]) ('gap safely normalizes '+$item[0])}
$keys.KeyGap=12.5
$json=[Newtonsoft.Json.JsonConvert]::SerializeObject($keys)
Check ((Call (T 'KeyViewerContents.KeyViewerStore') 'Deserialize' @($json)).KeyGap -eq 12.5) 'fractional key gap survives JSON save and reload'
Check ($null -eq (T 'OverlaySettings').GetField('TextColor')) 'custom label color has been removed'
Add-Type -Path (Join-Path $PSScriptRoot 'ILReader.cs')
$instructions=[LifecycleTests].GetMethod('Instructions',$taskFlags).Invoke($null,@((T 'OverlayController').GetMethod('ApplySettings',$taskFlags)))
$members=@($instructions|ForEach-Object {$_.GetType().GetField('Member',$taskFlags).GetValue($_)})
Check (@($members|Where-Object {$_ -is [Reflection.MethodInfo] -and $_.DeclaringType -eq [UnityEngine.Color] -and $_.Name -eq 'get_white'}).Count -eq 5) 'actual renderer fixes left, right, combo label, song and timing to opaque white'
'Feedback v6 assertions passed: '+$taskPassed
