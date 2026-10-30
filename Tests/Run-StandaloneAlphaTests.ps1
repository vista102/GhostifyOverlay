$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot
$taskManaged='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice\A Dance of Fire and Ice_Data\Managed'
foreach($taskDir in @($taskManaged,(Join-Path $taskManaged 'UnityModManager'))){foreach($taskFile in Get-ChildItem -LiteralPath $taskDir -Filter *.dll){try{[Reflection.Assembly]::LoadFrom($taskFile.FullName)|Out-Null}catch{}}}
$taskMod=[Reflection.Assembly]::LoadFrom((Join-Path $taskProject 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
$taskFlags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$taskPassed=0
function Check($okay,[string]$name){if(!$okay){throw ('FAIL '+$name)};$script:taskPassed++;'PASS '+$name}
$taskBuffer=[Collections.Generic.List[AnyKeyCode]]
$taskTarget=[RDInput].GetMethod('GetStateKeys',$taskFlags,$null,[type[]]@($taskBuffer,[ButtonState]),$null)
Check ($null -ne $taskTarget -and $taskTarget.ReturnType -eq [void]) 'installed alpha uses the void caller-buffer input API'
$taskPostfix=$taskMod.GetType('DonQuixoteOverlay.MenuBlockGetStateKeysPatch',$true).GetMethod('Postfix',$taskFlags)
Check ($taskPostfix.GetParameters().Count -eq 1 -and $taskPostfix.GetParameters()[0].Name -eq '__0' -and $taskPostfix.GetParameters()[0].ParameterType -eq $taskBuffer) 'input patch consumes argument zero instead of a nonexistent result'
foreach($taskType in @('UmmSettingsHost','UmmSettingsInput','UmmSettingsRaycaster','UmmSettingsGeometry','UmmEscapeClosePatch')){Check ($null -eq $taskMod.GetType('DonQuixoteOverlay.'+$taskType)) ('standalone build excludes '+$taskType)}
Check ($taskMod.GetReferencedAssemblies().Name -notcontains 'UnityEngine.IMGUIModule') 'standalone settings have no IMGUI image bridge dependency'
Add-Type -Path (Join-Path $PSScriptRoot 'ILReader.cs') -ReferencedAssemblies 'System.Core'
$taskToggle=$taskMod.GetType('DonQuixoteOverlay.SettingsWindow',$true).GetMethod('Toggle',$taskFlags,$null,[type[]]@(),$null)
$taskCalls=[LifecycleTests].GetMethod('Calls',$taskFlags).Invoke($null,@($taskToggle))
Check (@($taskCalls|Where-Object {$_.Name -eq 'SetVisible'}).Count -eq 1) 'Alt+D toggles the original independent settings window'
Check ($null -eq $taskMod.GetType('DonQuixoteOverlay.AttemptTracker',$true).GetMethod('ValidateCurrentRun',$taskFlags)) '0.4.9 counted-attempt preservation remains in place'
$taskCompatibility=Get-Content -LiteralPath (Join-Path $taskProject 'GameCompatibility.json') -Raw|ConvertFrom-Json
Check ($taskCompatibility.SteamBuild -eq '26311090' -and $taskCompatibility.AssemblySha256 -eq (Get-FileHash -LiteralPath (Join-Path $taskManaged 'Assembly-CSharp.dll')).Hash) 'compatibility manifest matches the updated installed alpha bytes'
'Standalone alpha SDK assertions passed: '+$taskPassed
$taskFixture=Join-Path $taskProject ('Backups\Feedback-0.4.10\Input-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskFixture -Force|Out-Null
$taskHarness=Join-Path $taskFixture 'UiInputBlockHarness.dll'
$taskCsc='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$taskInputSources=Get-ChildItem -LiteralPath (Join-Path $taskProject 'src/Mod/Integration/Adofai/Input') -Filter 'Menu*.cs'|ForEach-Object FullName
& $taskCsc /nologo /target:library /langversion:latest /r:System.Core.dll ('/r:'+(Join-Path $taskManaged 'UnityModManager\0Harmony.dll')) "/out:$taskHarness" $taskInputSources (Join-Path $PSScriptRoot 'UiInputBlockHarness.cs')
if($LASTEXITCODE -ne 0){throw 'Standalone input harness compilation failed'}
$taskAssembly=[Reflection.Assembly]::LoadFrom($taskHarness)
$taskResults=$taskAssembly.GetType('DonQuixoteOverlay.UiInputBlockHarness',$true).GetMethod('Run').Invoke($null,@())
$taskResults
'Standalone input regression assertions passed: '+$taskResults.Count
