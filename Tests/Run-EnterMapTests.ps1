# Use pwsh (.NET Core): the game's mapper calls CollectionExtensions, absent from Windows .NET Framework.
param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$managed=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
foreach($file in Get-ChildItem -LiteralPath $managed -Filter '*.dll'){try{[Reflection.Assembly]::LoadFrom($file.FullName)|Out-Null}catch{}}
$passed=0
function Check($value,[string]$label){if(!$value){throw ('FAIL '+$label)};$script:passed++;'PASS '+$label}
$enter=[SkyHook.SkyHookKeyMapper]::UnityKeyToSkyHookKey([UnityEngine.KeyCode]::Return)
$numpad=[SkyHook.SkyHookKeyMapper]::UnityKeyToSkyHookKey([UnityEngine.KeyCode]::KeypadEnter)
Check ($enter -eq [SkyHook.KeyLabel]::Enter) 'installed game maps main Enter to the correct SkyHook label'
Check ($numpad -eq [SkyHook.KeyLabel]::KeypadEnter) 'installed game keeps numpad Enter distinct'
Check ([SkyHook.SkyHookKeyMapper]::SkyHookKeyToUnityKey($enter) -eq [UnityEngine.KeyCode]::Return) 'main Enter capture converts back to the saved Unity binding'
Check ([SkyHook.SkyHookKeyMapper]::SkyHookKeyToUnityKey($numpad) -eq [UnityEngine.KeyCode]::KeypadEnter) 'numpad Enter capture converts back to its separate saved binding'
'Installed Enter mapping assertions passed: '+$passed
