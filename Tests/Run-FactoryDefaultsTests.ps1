param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$managed=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
foreach($directory in @($managed,(Join-Path $managed 'UnityModManager'))){
    foreach($file in Get-ChildItem -LiteralPath $directory -Filter *.dll){try{[Reflection.Assembly]::LoadFrom($file.FullName)|Out-Null}catch{}}
}
$mod=[Reflection.Assembly]::LoadFrom((Join-Path $project 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$passed=0
function Check($okay,[string]$name){if(!$okay){throw ('FAIL '+$name)};$script:passed++;'PASS '+$name}
function T([string]$name){$mod.GetType('DonQuixoteOverlay.'+$name,$true)}
function Call([Type]$type,[string]$name,[object[]]$values){
    for($i=0;$i -lt $values.Count;$i++){if($null -ne $values[$i]){$values[$i]=$values[$i].PSObject.BaseObject}}
    $type.GetMethod($name,$flags).Invoke($null,$values)
}
function Json($value){,([Newtonsoft.Json.Linq.JObject]::FromObject($value.PSObject.BaseObject))}
function Compare-Profile($expected,$actual,[string]$path){
    if($expected.get_Type() -eq 'Object'){
        Check ($actual.get_Type() -eq 'Object') ($path+' object')
        $expectedNames=@($expected.Properties()|ForEach-Object Name)
        $actualNames=@($actual.Properties()|ForEach-Object Name)
        Check ((($expectedNames|Sort-Object)-join ',') -ceq (($actualNames|Sort-Object)-join ',')) ($path+' field set')
        foreach($name in $expectedNames){Compare-Profile $expected[$name] $actual[$name] ($path+'.'+$name)}
    }elseif($expected.get_Type() -eq 'Array'){
        Check ($actual.get_Type() -eq 'Array' -and $expected.get_Count() -eq $actual.get_Count()) ($path+' array length')
        for($i=0;$i -lt $expected.get_Count();$i++){Compare-Profile $expected[$i] $actual[$i] ($path+'['+$i+']')}
    }elseif($expected.get_Type() -in @('Integer','Float')){
        Check ($actual.get_Type() -in @('Integer','Float') -and [Math]::Abs([double]$expected.get_Value()-[double]$actual.get_Value()) -lt .00001) $path
    }else{
        Check ([Newtonsoft.Json.Linq.JToken]::DeepEquals($expected,$actual)) $path
    }
}
$expected=[Newtonsoft.Json.Linq.JObject]::Parse([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Data/FactoryDefaults.json')))
Check ($expected['Settings']['Overlay']['FullAttempts'].get_Count() -eq 0 -and $expected['Settings']['Overlay']['FullProgressAttempts'].get_Count() -eq 0) 'reference profile has no personal attempt records'
$fixture=Join-Path $project ('Backups/FactoryDefaults-20261027/Storage-'+[Guid]::NewGuid().ToString('N'))
Call (T 'SettingsStore') 'Initialize' @($fixture)|Out-Null
$settings=Call (T 'SettingsStore') 'Load' @()
$keys=(T 'KeyViewerContents.KeyViewerStore').GetField('Settings',$flags).GetValue($null)
$layout=(T 'LayoutStore').GetField('Current',$flags).GetValue($null)
Compare-Profile $expected['Settings'] (Json $settings) 'fresh.Settings'
Compare-Profile $expected['KeyViewer'] (Json $keys) 'fresh.KeyViewer'
Compare-Profile $expected['Layout'] (Json $layout) 'fresh.Layout'
$counts=(T 'KeyViewerContents.KeyCountData').GetField('Instance',$flags).GetValue($null)
Check ($counts.TotalCount -eq 0 -and @($counts.Count|Where-Object {$_ -ne 0}).Count -eq 0) 'fresh key counts start at zero'
Check (!(Test-Path -LiteralPath (Join-Path $fixture 'UserData/keyviewer-counts.json'))) 'personal key counts are never seeded into a new installation'

# Existing saved choices and records must survive an update to the factory profile.
$settings.Overlay.ShowAccuracy=$true
$settings.Overlay.ShowTheoreticalKps=$true
$settings.Overlay.ValueColor=[UnityEngine.Color]::new(.2,.3,.4,1)
$settings.Overlay.FullAttempts['test-record']=17
$settings.Overlay.FullProgressAttempts['test-record']=23
$keys.KeyGap=11
$keys.XLocation=77
$keys.Background=[UnityEngine.Color]::new(.2,.4,.6,1)
$keys.key10[0]=[UnityEngine.KeyCode]::Return
$keys.key10Text[0]='saved alias'
$layout.TopLeftY=123
$layout.ComboScale=1.4
$counts.Count[0]=42
$counts.TotalCount=77
Call (T 'SettingsStore') 'Save' @($settings)|Out-Null
Call (T 'LayoutStore') 'Save' @()|Out-Null
Call (T 'KeyViewerContents.KeyViewerStore') 'Save' @()|Out-Null
$counts.Save()
$flush=$counts.GetType().GetMethod('Flush',$flags)
$flush.Invoke($counts,@([long]0,$true))|Out-Null
$dataDirectory=Join-Path $fixture 'UserData'
$before=@(Get-ChildItem -LiteralPath $dataDirectory -File|ForEach-Object {@{Path=$_.FullName;Hash=(Get-FileHash -LiteralPath $_.FullName).Hash}})
Call (T 'SettingsStore') 'Initialize' @($fixture)|Out-Null
$settings=Call (T 'SettingsStore') 'Load' @()
$keys=(T 'KeyViewerContents.KeyViewerStore').GetField('Settings',$flags).GetValue($null)
$layout=(T 'LayoutStore').GetField('Current',$flags).GetValue($null)
$counts=(T 'KeyViewerContents.KeyCountData').GetField('Instance',$flags).GetValue($null)
foreach($file in $before){Check ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Hash) ('existing saved file unchanged: '+[IO.Path]::GetFileName($file.Path))}
Check ($settings.Overlay.ShowAccuracy -and $settings.Overlay.ShowTheoreticalKps -and [Math]::Abs($settings.Overlay.ValueColor.r-.2) -lt .00001) 'existing overlay choices and color survive'
Check ($keys.KeyGap -eq 11 -and $keys.XLocation -eq 77 -and [Math]::Abs($keys.Background.r-.2) -lt .00001 -and $keys.key10[0] -eq [UnityEngine.KeyCode]::Return -and $keys.key10Text[0] -eq 'saved alias') 'existing key colors, gap, position, bindings and aliases survive'
Check ($layout.TopLeftY -eq 123 -and [Math]::Abs($layout.ComboScale-1.4) -lt .00001) 'existing overlay layout survives'
Check ($settings.Overlay.FullAttempts['test-record'] -eq 17 -and $settings.Overlay.FullProgressAttempts['test-record'] -eq 23 -and $counts.Count[0] -eq 42 -and $counts.TotalCount -eq 77) 'existing attempts and key counts survive'

$reset=Call (T 'DonQuixoteSettings') 'CreateDefaultPreservingRecords' @($settings)
Check ($reset.Overlay.FullAttempts['test-record'] -eq 17 -and $reset.Overlay.FullProgressAttempts['test-record'] -eq 23) 'settings reset preserves cumulative attempts'
$resetJson=Json $reset
$resetJson['Overlay']['FullAttempts'].RemoveAll()
$resetJson['Overlay']['FullProgressAttempts'].RemoveAll()
Compare-Profile $expected['Settings'] $resetJson 'reset.Settings'
Call (T 'LayoutStore') 'Reset' @()|Out-Null
Compare-Profile $expected['Layout'] (Json ((T 'LayoutStore').GetField('Current',$flags).GetValue($null))) 'reset.Layout'
$newKeys=[Activator]::CreateInstance((T 'KeyViewerContents.KeyViewerSetting'))
Compare-Profile $expected['KeyViewer'] (Json $newKeys) 'reset.KeyViewer'
$newKeys.key10[0]=[UnityEngine.KeyCode]::Escape
$freshKeys=[Activator]::CreateInstance((T 'KeyViewerContents.KeyViewerSetting'))
Check ($freshKeys.key10[0] -eq [UnityEngine.KeyCode]::Tab) 'default binding arrays are independent between instances'
Check ($counts.Count[0] -eq 42 -and $counts.TotalCount -eq 77) 'appearance reset leaves existing key counts untouched'
'Factory default / existing-user preservation assertions passed: '+$passed
