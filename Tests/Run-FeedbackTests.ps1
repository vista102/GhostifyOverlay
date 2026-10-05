# Run in pwsh: native SkyHook's mapper requires .NET CollectionExtensions.
param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot
$taskManaged=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
$taskSkyHook=[Runtime.InteropServices.NativeLibrary]::Load((Join-Path $GameDir 'A Dance of Fire and Ice_Data\Plugins\x86_64\skyhook.dll'))
foreach($taskDir in @($taskManaged,(Join-Path $taskManaged 'UnityModManager'))){
    foreach($taskFile in Get-ChildItem -LiteralPath $taskDir -Filter *.dll){try{[Reflection.Assembly]::LoadFrom($taskFile.FullName)|Out-Null}catch{}}
}
$taskMod=[Reflection.Assembly]::LoadFrom((Join-Path $taskProject 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
$taskFlags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$taskPassed=0
function Check($ok,[string]$name){if(!$ok){throw ('FAIL '+$name)};$script:taskPassed++;'PASS '+$name}
function ModType([string]$name){$taskMod.GetType('DonQuixoteOverlay.'+$name,$true)}
function Call($type,[string]$name,[object[]]$values){
    for($i=0;$i -lt $values.Length;$i++){if($null -ne $values[$i]){$values[$i]=$values[$i].PSObject.BaseObject}}
    $type.GetMethod($name,$taskFlags).Invoke($null,$values)
}
$visibility=ModType 'KeyViewerVisibility'
foreach($world in @($false,$true)){foreach($editor in @($false,$true)){foreach($playing in @($false,$true)){
    $expected=if($editor){$playing}else{$world}
    Check ((Call $visibility 'IsPlaying' @($world,$editor,$playing)) -eq $expected) ('visibility follows native world / editor play mode: '+$world+'/'+$editor+'/'+$playing)
}}}
$keys=Call (ModType 'NativeInputSync') 'RegisteredKeys' @([UnityEngine.KeyCode[]]@([UnityEngine.KeyCode]::None,[UnityEngine.KeyCode]::A,[UnityEngine.KeyCode]::Return),[UnityEngine.KeyCode[]]@([UnityEngine.KeyCode]::A,[UnityEngine.KeyCode]::F8,[UnityEngine.KeyCode]::KeypadEnter))
Check ($keys.Count -eq 4 -and !$keys.Contains([UnityEngine.KeyCode]::None)) 'native allowlist removes unbound slots and duplicates across hand/foot keys'
Check ($keys.Contains([UnityEngine.KeyCode]::Return) -and $keys.Contains([UnityEngine.KeyCode]::KeypadEnter)) 'native allowlist retains both Enter bindings'
foreach($key in @([UnityEngine.KeyCode]::Return,[UnityEngine.KeyCode]::KeypadEnter,[UnityEngine.KeyCode]::A,[UnityEngine.KeyCode]::F8,[UnityEngine.KeyCode]::LeftShift,[UnityEngine.KeyCode]::RightShift)){
    $label=[SkyHook.SkyHookKeyMapper]::UnityKeyToSkyHookKey($key)
    $native=Call (ModType 'NativeInputSync') 'NativeKey' @($key)
    $expectedUnity=if($key -eq [UnityEngine.KeyCode]::KeypadEnter){[UnityEngine.KeyCode]::Return}else{$key}
    Check ($native -ne [ushort]::MaxValue -and [SkyHook.SkyHookKeyMapper]::SkyHookKeyToUnityKey([SkyHook.SkyHookKeyMapper]::NativeKeyCodeToKeyLabel($native)) -eq $expectedUnity) ('native allowlist preserves the supported Windows key code: '+$key)
}
foreach($name in @('unityKeys','asyncKeys')){
    $setter=[KeysSetting].GetProperty($name,$taskFlags).GetSetMethod($true)
    Check ($null -ne $setter) ('installed game provides the saved native '+$name+' setter')
}
$metadata=ModType 'OverlayMetadata'
Check ((Call $metadata 'SongText' @('Song name','Artist')) -eq 'Artist - Song name') 'composer and title use the same hyphen-separated line'
Check ((Call $metadata 'SongText' @('  title  ','')) -eq 'title') 'missing composer does not create an empty row'
Check ((Call $metadata 'SongText' @($null,' artist ')) -eq 'artist') 'missing title retains available composer'
Check ((Call $metadata 'SongText' @("line1`nline2","name`r`nnext")) -eq 'name next - line1 line2') 'metadata normalizes line breaks and preserves its separating hyphen'
$text=Call $metadata 'TimingText' @([double]0.75)
Check ($text -eq 'Timing Scale | 75%') 'overlay shows native tile timing scale as a percentage'
foreach($value in @([double]::NaN,[double]::PositiveInfinity,[double]::NegativeInfinity,[double]-1)){
    Check ((Call $metadata 'TimingText' @($value)) -eq '') 'invalid timing scale never appears as NaN, infinity or a negative value'
}
$nativeHelper=[scrMisc].GetMethod('GetAdjustedTimeBoundaries',$taskFlags)
Check ($nativeHelper.GetParameters().Count -eq 4 -and $nativeHelper.ReturnType.GetField('XPerfect') -and $nativeHelper.ReturnType.GetField('Pure') -and $nativeHelper.ReturnType.GetField('Perfect') -and $nativeHelper.ReturnType.GetField('Counted')) 'alpha native range API has all four inspected boundaries'
foreach($difficulty in [Enum]::GetValues([Difficulty])){
    foreach($bpm in @(90,180,600,1200)){
        foreach($pitch in @([float]0.5,[float]1.5)){
            foreach($scale in @([double]0.5,[double]1,[double]2)){
                $limits=$nativeHelper.Invoke($null,@($difficulty,[double]$bpm,$pitch,$scale))
                $xp=[double]$limits.XPerfect;$pure=[double]$limits.Pure;$perfect=[double]$limits.Perfect;$counted=[double]$limits.Counted
                $center=[scrMisc]::GetHitMarginInSec($difficulty,[double]0,[float]$bpm,$pitch,$scale)
                $edge=[scrMisc]::GetHitMarginInSec($difficulty,$xp*0.99,[float]$bpm,$pitch,$scale)
                $outside=[scrMisc]::GetHitMarginInSec($difficulty,$xp+[double]0.0000001,[float]$bpm,$pitch,$scale)
                Check ($xp -gt 0 -and $xp -le $pure -and $pure -le $perfect -and $perfect -le $counted -and $center -eq [HitMargin]::XPerfect -and $edge -eq [HitMargin]::XPerfect -and $outside -ne [HitMargin]::XPerfect) ('displayed native limits agree with native judging: '+$difficulty+', BPM '+$bpm+', pitch '+$pitch+', margin '+$scale)
            }
        }
    }
}
$defaults=[Activator]::CreateInstance((ModType 'KeyViewerContents.KeyViewerSetting'))
Check $defaults.SyncNativeInputKeys 'native input synchronization is enabled by default'
Check ($null -ne $defaults.GetType().GetField('GhostRainColor')) 'ghost rain has an independently saved color'
$overlay=[Activator]::CreateInstance((ModType 'OverlaySettings'))
Check ($overlay.ShowSongInfo -and $overlay.ShowTimingRanges -and $overlay.Font -eq 'Gmarket Sans') 'requested overlays and Gmarket Sans are the defaults'
Check ($null -eq (ModType 'OverlayController').GetMethod('AdjustAutoPlayText',$taskFlags) -and $null -eq $taskMod.GetType('DonQuixoteOverlay.OverlayAutoScanSchedule')) 'native autoplay labels are not scanned or relocated'
$taskLayout=[Activator]::CreateInstance((ModType 'LayoutData'))
$taskLayout.SongInfoX=42;$taskLayout.SongInfoY=-19;$taskLayout.TimingRangesX=-51;$taskLayout.TimingRangesY=31
$taskLayout.SongInfoScale=[float]::NaN;$taskLayout.TimingRangesScale=99
$normalize=(ModType 'SettingsNormalization').GetMethods($taskFlags)|Where-Object {$_.Name -eq 'Normalize' -and $_.GetParameters()[0].ParameterType.Name -eq 'LayoutData'}
$normalize.Invoke($null,@($taskLayout))|Out-Null
Check ($taskLayout.SongInfoX -eq 42 -and $taskLayout.SongInfoY -eq -19 -and $taskLayout.TimingRangesX -eq -51 -and $taskLayout.TimingRangesY -eq 31 -and $taskLayout.SongInfoScale -eq 1 -and $taskLayout.TimingRangesScale -eq 2) 'new layout sections preserve coordinates and normalize invalid scale'
'Feedback assertions passed: '+$taskPassed
