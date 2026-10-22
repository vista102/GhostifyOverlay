$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot
$taskManaged='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice\A Dance of Fire and Ice_Data\Managed'
foreach($taskDir in @($taskManaged,(Join-Path $taskManaged 'UnityModManager'))){foreach($taskFile in Get-ChildItem -LiteralPath $taskDir -Filter *.dll){try{[Reflection.Assembly]::LoadFrom($taskFile.FullName)|Out-Null}catch{}}}
$taskMod=[Reflection.Assembly]::LoadFrom((Join-Path $taskProject 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
$taskFlags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$taskPixels=$taskMod.GetType('DonQuixoteOverlay.DarkNeonUi',$true).GetMethod('RoundedPixels',$taskFlags)
$taskPassed=0
function Check($okay,[string]$name){if(!$okay){throw ('FAIL '+$name)};$script:taskPassed++;'PASS '+$name}
Check ($taskMod.GetType('DonQuixoteOverlay.DQRadii',$true).GetField('Window',$taskFlags).GetRawConstantValue() -eq 0) 'settings and palette windows have square outer corners'
foreach($taskSize in @(16,64)){
    $taskFill=$taskPixels.Invoke($null,@([int]$taskSize,[single]0,[bool]$false))
    Check (@($taskFill|Where-Object {$_.a -ne 1}).Count -eq 0) 'square fill remains fully opaque including its corners'
    $taskStroke=$taskPixels.Invoke($null,@([int]$taskSize,[single]0,[bool]$true))
    Check (@($taskStroke|Where-Object {$_.a -gt 0}).Count -eq (4*$taskSize-4)) 'square stroke has one-pixel edges without a filled center'
    foreach($taskIndex in @(0,($taskSize-1),($taskSize*($taskSize-1)),($taskSize*$taskSize-1))){Check ($taskStroke[$taskIndex].a -eq 1) 'square border meets exactly at each corner'}
}
foreach($taskRadius in @(9,11,15)){
    $taskFill=$taskPixels.Invoke($null,@([int]64,[single]$taskRadius,[bool]$false))
    $taskStroke=$taskPixels.Invoke($null,@([int]64,[single]$taskRadius,[bool]$true))
    Check ($taskFill[0].a -eq 0 -and $taskFill[32*64+32].a -eq 1) 'inner controls retain rounded corners and solid fills'
    Check ($taskStroke[32*64+32].a -eq 0 -and @($taskStroke|Where-Object {$_.a -lt 0 -or $_.a -gt 1}).Count -eq 0) 'rounded control borders keep transparent centers and valid alpha'
}
'Window surface assertions passed: '+$taskPassed
