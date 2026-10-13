$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot
$taskManaged='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice\A Dance of Fire and Ice_Data\Managed'
foreach($taskDir in @($taskManaged,(Join-Path $taskManaged 'UnityModManager'))){foreach($taskFile in Get-ChildItem -LiteralPath $taskDir -Filter *.dll){try{[Reflection.Assembly]::LoadFrom($taskFile.FullName)|Out-Null}catch{}}}
$taskMod=[Reflection.Assembly]::LoadFrom((Join-Path $taskProject 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
$taskFlags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$taskPassed=0
function Check($okay,[string]$name){if(!$okay){throw ('FAIL '+$name)};$script:taskPassed++;'PASS '+$name}
$taskGeometry=$taskMod.GetType('DonQuixoteOverlay.UmmSettingsGeometry',$true)
function Call([string]$method,[object[]]$values){for($i=0;$i -lt $values.Count;$i++){if($null -ne $values[$i]){$values[$i]=$values[$i].PSObject.BaseObject}};$taskGeometry.GetMethod($method,$taskFlags).Invoke($null,$values)}
$taskArea=[UnityEngine.Rect]::new(120,180,960,720)
$taskClip=[UnityEngine.Rect]::new(120,180,960,360)
foreach($taskCase in @(@(600,720,$true),@(600,300,$false),@(119,720,$false),@(1080,720,$false))){
    Check ((Call 'Contains' @($taskArea,$taskClip,[UnityEngine.Vector2]::new($taskCase[0],$taskCase[1]),[single]1080)) -eq $taskCase[2]) 'actual DLL respects UMM clip and physical screen coordinates'
}
foreach($taskSize in @(240,480,960,1920)){
    $taskHeight=Call 'TextureHeight' @([int]$taskSize)
    $taskPointer=Call 'Pointer' @($taskArea,[UnityEngine.Vector2]::new(600,540),[single]1080,[int]$taskSize,[int]$taskHeight)
    Check ([Math]::Abs($taskPointer.x-$taskSize/2) -lt 0.001 -and [Math]::Abs($taskPointer.y-$taskHeight/2) -lt 0.001) 'pointer conversion stays correct across texture resolution changes'
}
$taskUI=[UnityModManagerNet.UnityModManager+UI]
foreach($taskName in @('ShowModSettings')){Check ($null -ne $taskUI.GetProperty($taskName,$taskFlags)) 'installed UMM exposes required settings selection metadata'}
Check ($null -ne $taskUI.GetField('mModFilter',$taskFlags)) 'installed UMM exposes filter metadata'
Check ($null -ne ([UnityEngine.GUI].Assembly.GetType('UnityEngine.GUIClip',$true)).GetProperty('visibleRect',$taskFlags)) 'installed Unity exposes current IMGUI scroll clip'
Check ($taskMod.GetType('DonQuixoteOverlay.UmmSettingsInput',$true).IsSubclassOf([UnityEngine.EventSystems.BaseInput])) 'pointer adapter remains a uGUI BaseInput'
Check ($taskMod.GetType('DonQuixoteOverlay.UmmSettingsRaycaster',$true).IsSubclassOf([UnityEngine.UI.GraphicRaycaster])) 'embedded controls use the actual uGUI raycaster'
Check ($taskMod.GetType('DonQuixoteOverlay.AttemptTracker',$true).GetMethod('ValidateCurrentRun',$taskFlags) -eq $null) 'UMM UI build includes the 0.4.9 no-subtraction fix'
Check ([version](Get-Content (Join-Path $taskProject 'Info.json') -Raw|ConvertFrom-Json).Version -ge [version]'0.5.0') 'UMM UI build carries version 0.5.0 or later'
'UMM SDK assertions passed: '+$taskPassed
