param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$managed=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
foreach($dir in @($managed,(Join-Path $managed 'UnityModManager'))) {
    Get-ChildItem -LiteralPath $dir -Filter *.dll | ForEach-Object {try {[Reflection.Assembly]::LoadFrom($_.FullName)|Out-Null} catch {}}
}
$project=Split-Path $PSScriptRoot
$mod=[Reflection.Assembly]::LoadFrom((Join-Path $project 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
Add-Type -Path (Join-Path $PSScriptRoot 'KeyViewerTests.cs') -ReferencedAssemblies 'System.Core'
$results=[KeyViewerTests]::Run($mod)
$results
$passed=$results.Count
$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
function T($name) {$mod.GetType('DonQuixoteOverlay.KeyViewerContents.'+$name,$true)}
function Invoke-Test($type,$name,[object[]]$arguments) {for($i=0;$i -lt $arguments.Count;$i++){$arguments[$i]=$arguments[$i].PSObject.BaseObject};try {$type.GetMethod($name,$flags).Invoke($null,$arguments)} catch {throw $_.Exception.ToString()}}
function Assert($value,$name) {if(!$value){throw "FAIL $name"};$script:passed++;"PASS $name"}
$savedColors=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'UserSavedKeyColors-20261003.json') -Raw|ConvertFrom-Json
$savedColors.Background='#FFFFFFFF';$savedColors.Text='#292929FF'
foreach($name in @('BackgroundClicked','Outline','OutlineClicked','RainColor')){$savedColors.$name='#FFC939FF'}
$savedColors.PSObject.Properties.Remove('RainColor3')
$defaults=[Activator]::CreateInstance((T 'KeyViewerSetting'))
foreach($field in $savedColors.PSObject.Properties) {
    Assert ((Invoke-Test (T 'KeyViewerColorConverter') 'Format' @($defaults.GetType().GetField($field.Name).GetValue($defaults))) -eq $field.Value) ('new/reset key color matches saved in-game '+$field.Name)
}
$custom=[Activator]::CreateInstance((T 'KeyViewerSetting'))
$custom.SchemaVersion=2
$custom.Background=[UnityEngine.Color]::new(.1,.2,.3,.4);$custom.Outline=[UnityEngine.Color]::new(.5,.6,.7,.8);$custom.Text=[UnityEngine.Color]::new(.9,.8,.7,.6)
$beforeBackground=Invoke-Test (T 'KeyViewerColorConverter') 'Format' @($custom.Background)
$beforeOutline=Invoke-Test (T 'KeyViewerColorConverter') 'Format' @($custom.Outline)
$beforeText=Invoke-Test (T 'KeyViewerColorConverter') 'Format' @($custom.Text)
Invoke-Test (T 'KeyViewerStore') 'Normalize' @($custom)|Out-Null
Assert ((Invoke-Test (T 'KeyViewerColorConverter') 'Format' @($custom.Background)) -eq $beforeBackground -and (Invoke-Test (T 'KeyViewerColorConverter') 'Format' @($custom.Outline)) -eq $beforeOutline -and (Invoke-Test (T 'KeyViewerColorConverter') 'Format' @($custom.Text)) -eq $beforeText) 'default palette update preserves existing schema-two custom colors'
$fixture=Join-Path $PSScriptRoot ('keyviewer-fixture-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture | Out-Null
Invoke-Test (T 'KeyViewerStore') 'Initialize' @($fixture)|Out-Null
$settings=(T 'KeyViewerStore').GetField('Settings',$flags).GetValue($null)
$settings.YLocation=315;$settings.Size=1.4
$settings.key16Text[0]='한글 별칭'
$settings.key16[0]=[UnityEngine.KeyCode]::Return;$settings.key16[1]=[UnityEngine.KeyCode]::KeypadEnter
Invoke-Test (T 'KeyViewerStore') 'Save' @()|Out-Null
Invoke-Test (T 'KeyViewerStore') 'Initialize' @($fixture)|Out-Null
$loaded=(T 'KeyViewerStore').GetField('Settings',$flags).GetValue($null)
Assert ($loaded.YLocation -eq 315 -and [Math]::Abs($loaded.Size-1.4) -lt .001 -and $loaded.key16Text[0] -eq '한글 별칭') 'key viewer settings, layout and Unicode labels reload'
Assert ($loaded.key16[0] -eq [UnityEngine.KeyCode]::Return -and $loaded.key16[1] -eq [UnityEngine.KeyCode]::KeypadEnter) 'main Enter and numpad Enter persist as separate bindings'
$loaded.YLocation=400
Invoke-Test (T 'KeyViewerStore') 'Save' @()|Out-Null
[IO.File]::WriteAllText((Join-Path $fixture 'keyviewer.json'),'{broken')
Invoke-Test (T 'KeyViewerStore') 'Initialize' @($fixture)|Out-Null
$loaded=(T 'KeyViewerStore').GetField('Settings',$flags).GetValue($null)
Assert ($loaded.YLocation -eq 315 -and @(Get-ChildItem -LiteralPath $fixture -Filter 'keyviewer.json.corrupt-*').Count -eq 1) 'corrupt key viewer settings are backed up and recover the previous snapshot'
$json=Get-Content -LiteralPath (Join-Path $fixture 'keyviewer.json') -Encoding UTF8 -Raw|ConvertFrom-Json
$json.key16=@(100000);$json.key16Text=$null;$json.Size=999;$json.Background='invalid';$json|Add-Member -NotePropertyName AutoSetupKeyLimit -NotePropertyValue $true -Force
$json|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $fixture 'keyviewer.json') -Encoding UTF8
Invoke-Test (T 'KeyViewerStore') 'Initialize' @($fixture)|Out-Null
$loaded=(T 'KeyViewerStore').GetField('Settings',$flags).GetValue($null)
Assert ($loaded.key16.Count -eq 16 -and $loaded.key16Text.Count -eq 16 -and $loaded.Size -eq 2 -and $null -eq $loaded.GetType().GetField('AutoSetupKeyLimit')) 'partial settings repair array sizes, key IDs and finite limits after removing legacy limiter setup'
Assert ($loaded.YLocation -eq 315) 'invalid individual color does not reset unrelated layout settings'
$counts=(T 'KeyCountData').GetField('Instance',$flags).GetValue($null)
$counts.Count[0]=2147483648;$counts.TotalCount=2147483648
$counts.GetType().GetMethod('Save').Invoke($counts,@())|Out-Null
$counts.GetType().GetMethod('Flush',$flags).Invoke($counts,@([long]0,$true))|Out-Null
Invoke-Test (T 'KeyCountData') 'Load' @($fixture)|Out-Null
$restored=(T 'KeyCountData').GetField('Instance',$flags).GetValue($null)
Assert ($restored.Count[0] -eq 2147483648 -and $restored.TotalCount -eq 2147483648) 'cumulative counts above int32 persist safely'
$restored.Count[0]=77;$restored.TotalCount=77
$restored.GetType().GetMethod('Save').Invoke($restored,@())|Out-Null
$restored.GetType().GetMethod('Flush',$flags).Invoke($restored,@([long]0,$true))|Out-Null
[IO.File]::WriteAllText((Join-Path $fixture 'keyviewer-counts.json'),'{bad')
Invoke-Test (T 'KeyCountData') 'Load' @($fixture)|Out-Null
$restored=(T 'KeyCountData').GetField('Instance',$flags).GetValue($null)
Assert ($restored.Count[0] -eq 2147483648 -and @(Get-ChildItem -LiteralPath $fixture -Filter 'keyviewer-counts.json.corrupt-*').Count -eq 1) 'corrupt cumulative counts recover from the atomic backup'
$references=$mod.GetReferencedAssemblies().Name
Assert (!($references -contains 'JALib') -and !($references -contains 'UnityEngine.IMGUIModule') -and !($references -contains 'DOTween')) 'Release does not require JALib, IMGUI or DOTween'
Assert (Test-Path -LiteralPath (Join-Path $project 'bin\Release\GhostifyOverlay\Licenses\JipperResourcePack-BSD-3-Clause.txt')) 'upstream BSD license ships with Release'
"Key viewer assertions passed: $passed"
