param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$managed=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
foreach($dir in @($managed,(Join-Path $managed 'UnityModManager'))){foreach($dll in Get-ChildItem -LiteralPath $dir -Filter '*.dll'){try{[Reflection.Assembly]::LoadFrom($dll.FullName)|Out-Null}catch{}}}
$mod=[Reflection.Assembly]::LoadFrom((Join-Path $project 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$script:passed=0
function T([string]$name){$mod.GetType('DonQuixoteOverlay.'+$name,$true)}
function Call($type,[string]$name,[object[]]$values){
    for($i=0;$i -lt $values.Length;$i++){if($null -ne $values[$i]){$values[$i]=$values[$i].PSObject.BaseObject}}
    try{$type.GetMethod($name,$flags).Invoke($null,$values)}catch{throw $_.Exception.ToString()}
}
function Check($ok,[string]$name){if(!$ok){throw ('FAIL '+$name)};$script:passed++;'PASS '+$name}
$native=T 'NativeJudgments'
foreach($case in @(@($true,$false,$false,$false,$false,$true),@($false,$false,$false,$false,$false,$false),@($true,$true,$false,$false,$false,$false),@($true,$false,$true,$false,$false,$false),@($true,$false,$false,$true,$false,$false),@($true,$false,$false,$false,$true,$false))){
    Check ((Call (T 'AttemptTracker') 'CountsAttempts' @([bool]$case[0],[bool]$case[1],[bool]$case[2],[bool]$case[3],[bool]$case[4])) -eq $case[5]) ('attempt eligibility: '+([string]::Join(',', $case)))
}
Check ([Enum]::GetValues([HitMargin]).Count -eq 16 -and [int][HitMargin]::XPerfect -eq 4 -and [int][HitMargin]::PerfectPlus -eq 5) 'installed alpha has the expected native judgment model'
foreach($removed in @('XPerfectModule','DetailedJudge','JudgmentSettings','InputSettings','InputController','KeyLimiterPatch','XPerfectCalculatePatch')){
    Check ($null -eq $mod.GetType('DonQuixoteOverlay.'+$removed)) ('obsolete module removed: '+$removed)
}
$counts=New-Object int[] 16
for($i=0;$i -lt $counts.Length;$i++){$counts[$i]=$i+1}
$expected=@(12,1,2,3,28,7,8,9,11)
for($i=0;$i -lt $expected.Length;$i++){
    Check ((Call $native 'JudgmentCount' @($counts,[int]$i)) -eq $expected[$i]) ('native ordinary judgment bucket '+$i)
}
Check ((Call $native 'ExactCount' (,$counts)) -eq 18) 'native X plus Auto appear in the detailed exact bucket once'
Check ((Call $native 'Count' @($null,[HitMargin]::XPerfect)) -eq 0 -and (Call $native 'PerfectCount' (,(New-Object int[] 2))) -eq 0) 'missing and short tracker arrays are safe'
$counts[4]=-12;Check ((Call $native 'Count' @($counts,[HitMargin]::XPerfect)) -eq 0) 'invalid negative counters do not enter the HUD'
$counts[4]=[int]::MaxValue;$counts[12]=[int]::MaxValue
Check ((Call $native 'ExactCount' (,$counts)) -eq [int]::MaxValue) 'large aggregate counters saturate without wrapping'
foreach($preset in [Enum]::GetValues([HitMarginPerfectTextPreset])){
    Check ((Call $native 'ShouldShowDetails' @($true,$preset)) -eq ([int]$preset -ne 0)) ('native detail visibility follows '+$preset)
    Check (!(Call $native 'ShouldShowDetails' @($false,$preset))) ('overlay judgment toggle gates '+$preset)
}
foreach($hit in @([HitMargin]::PerfectMinus,[HitMargin]::XPerfect,[HitMargin]::PerfectPlus,[HitMargin]::Auto)){
    Check ((Call $native 'AdvanceCombo' @([int]7,$hit,[int]3)) -eq 10) ('batch hit combo includes '+$hit)
}
foreach($hit in @([HitMargin]::Midspin,[HitMargin]::Multipress)){
    Check ((Call $native 'AdvanceCombo' @([int]7,$hit,[int]4)) -eq 7) ('neutral tile keeps combo: '+$hit)
}
foreach($hit in @([HitMargin]::EarlyPerfect,[HitMargin]::TooLate,[HitMargin]::FailMiss,[HitMargin]::FailOverload,[HitMargin]::OverPress,[HitMargin]::FailedFloor)){
    Check ((Call $native 'AdvanceCombo' @([int]7,$hit,[int]1)) -eq 0) ('imperfect hit breaks combo: '+$hit)
}
Check ((Call $native 'AdvanceCombo' @([int]7,[HitMargin]::FailMiss,[int]0)) -eq 7) 'empty native hit batches cannot break combo'
Check ((Call $native 'AdvanceCombo' @([int]::MaxValue,[HitMargin]::Auto,[int]2)) -eq [int]::MaxValue) 'combo cannot overflow'
$history=New-Object 'System.Collections.Generic.List[HitMargin]'
foreach($hit in @([HitMargin]::XPerfect,[HitMargin]::FailMiss,[HitMargin]::PerfectPlus,[HitMargin]::Midspin,[HitMargin]::Auto,[HitMargin]::Multipress,[HitMargin]::PerfectMinus)){$history.Add($hit)}
Check ((Call $native 'ComboFromHistory' (,$history)) -eq 3) 'checkpoint combo rebuild uses the retained native history'
$history.RemoveRange(3,4)
Check ((Call $native 'ComboFromHistory' (,$history)) -eq 1) 'reverted checkpoint cannot retain future combo hits'
Check ((Call $native 'ComboFromHistory' @($null)) -eq 0) 'missing history begins at zero'
$first=[Runtime.Serialization.FormatterServices]::GetUninitializedObject([scrMarginTracker])
$second=[Runtime.Serialization.FormatterServices]::GetUninitializedObject([scrMarginTracker])
[scrMistakesManager]::marginTrackers=[scrMarginTracker[]]@($first,$second)
Check ((Call $native 'IsDisplayedTracker' @($first)) -and !(Call $native 'IsDisplayedTracker' @($second))) 'native counters and combo consistently use player one'
[scrMistakesManager]::marginTrackers=$null
Check ($null -eq $native.GetProperty('DisplayedTracker',$flags).GetValue($null,$null)) 'tracker discovery tolerates startup before player allocation'
$format=Call $native 'FormatCounts' @([int]2,[int]7,[int]3,' / ')
Check ($format -eq '<color=#60FF4E>2</color> / <color=#FFFFFF>7</color> / <color=#60FF4E>3</color>') 'detail HUD retains Early / X / Late order and fixed DonQuixote colors'
$colors=T 'NativeJudgmentColors'
$original=[UnityEngine.Color]::new(.1,.2,.3,1)
$applied=[UnityEngine.Color]::new(.3,.8,1,1)
$faded=[UnityEngine.Color]::new(.3,.8,1,.25)
$restored=Call $colors 'RestoreTint' @($faded,$original,$applied)
Check ($restored.r -eq $original.r -and $restored.g -eq $original.g -and $restored.b -eq $original.b -and $restored.a -eq $faded.a) 'owned native tint restoration keeps the game fade alpha'
$foreign=[UnityEngine.Color]::new(.9,.7,.2,.4)
$untouched=Call $colors 'RestoreTint' @($foreign,$original,$applied)
Check ($untouched.r -eq $foreign.r -and $untouched.g -eq $foreign.g -and $untouched.b -eq $foreign.b -and $untouched.a -eq $foreign.a) 'a later foreign/native color change is never overwritten'
foreach($preset in @([HitMarginPerfectTextPreset]::Default,[HitMarginPerfectTextPreset]::ShowXPerfectAndSignedPerfects)){
    $before=[int[]]@(1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16)
    $snapshot=$before.Clone()
    Call $native 'ShouldShowDetails' @($true,$preset)|Out-Null
    Call $native 'ExactCount' (,$before)|Out-Null
    Check (([string]::Join(',', $before)) -eq ([string]::Join(',', $snapshot))) ('display changes never modify native statistics: '+$preset)
}
Add-Type -Path (Join-Path $PSScriptRoot 'ILReader.cs') -ReferencedAssemblies 'System.Core'
$reader=[LifecycleTests].GetMethod('Calls',$flags)
foreach($method in @('Update','SetTexts')){
    $calls=$reader.Invoke($null,@((T 'OverlayController').GetMethod($method,$flags)))
    Check (@($calls|Where-Object {$_.DeclaringType -eq $native -and $_.Name -eq 'DetailedTextVisible'}).Count -eq 1) ('HUD display path uses native setting gate: '+$method)
}
foreach($preset in [Enum]::GetValues([HitMarginPerfectTextPreset])){
    foreach($hit in @([HitMargin]::XPerfect,[HitMargin]::PerfectPlus,[HitMargin]::PerfectMinus,[HitMargin]::EarlyPerfect)){
        $colorArgs=[object[]]@($hit,$preset,[UnityEngine.Color]::white)
        $actual=$colors.GetMethod('TryColor',$flags).Invoke($null,$colorArgs)
        $expectedColor=if($hit -eq [HitMargin]::XPerfect){[HitMarginHelper]::IsShowXPerfect($preset,$false)}elseif($hit -eq [HitMargin]::EarlyPerfect){$false}else{[HitMarginHelper]::IsShowSignedPerfects($preset)}
        Check ($actual -eq $expectedColor) ('native color scope '+$preset+' / '+$hit)
        if($actual){$hex=Call (T 'KeyViewerContents.KeyViewerColorConverter') 'Format' @($colorArgs[2]);$expectedHex=if($hit -eq [HitMargin]::XPerfect){'#FFFFFFFF'}else{'#60FF4EFF'};Check ($hex -eq $expectedHex) ('native tint '+$preset+' / '+$hit)}
    }
}
$results=T 'NativeResultsPresentation'
foreach($locale in @(@('Perfect-','Perfect+'),@('정확-','정확+'),@('Perfekt-','Perfekt+'))){
    $minus=$locale[0];$plus=$locale[1]
    $nativeText="Header`r`n<color=#0f0>${plus}: 7</color>     XPerfect: 2     <color=#fff>${minus}: 13</color>`r`nAuto: 20     XScore: 44`r`n"
    $expectedText="Header`r`n<color=#fff>${minus}: 13</color>     XPerfect: 2     <color=#0f0>${plus}: 7</color>`r`nAuto: 20     XScore: 44`r`n"
    $ordered=Call $results 'Reorder' @($nativeText,$minus,$plus)
    Check ($ordered -ceq $expectedText) ('native result cells preserve all data and CRLF: '+$minus)
    Check ((Call $results 'Reorder' @($ordered,$minus,$plus)) -ceq $ordered) ('result ordering is idempotent: '+$minus)
}
Check ((Call $results 'Reorder' @("Perfect+: 2`nPerfect-: 9",'Perfect-','Perfect+')) -ceq "Perfect-: 9`nPerfect+: 2") 'signed-only native results place Early before Late across separate rows'
foreach($text in @('no signed cells','Perfect-: 2','Perfect-: 2     Perfect-: 3     Perfect+: 1','Perfect- and Perfect+ in one cell')){
    Check ((Call $results 'Reorder' @($text,'Perfect-','Perfect+')) -ceq $text) ('ambiguous or absent native result cells are preserved: '+$text)
}
Check ((Call $results 'Reorder' @($null,'Perfect-','Perfect+')) -eq $null) 'null results remain null'
'Alpha migration assertions passed: '+$script:passed
