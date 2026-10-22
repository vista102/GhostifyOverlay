$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
function Read-Model([string]$name){
    $text=[IO.File]::ReadAllText((Join-Path $project ('src/Mod/Domain/Configuration/'+$name+'.cs')))
    $start=$text.IndexOf('{')+1
    $text.Substring($start,$text.LastIndexOf('}')-$start).Replace('[JsonConverter(','[Newtonsoft.Json.JsonConverterAttribute(')
}
$overlay=Read-Model 'OverlaySettings'
$layout=Read-Model 'LayoutData'
$fixture=Join-Path $project ('Backups\Feedback-0.4.6\Ui-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture -Force|Out-Null
$model=Join-Path $fixture 'Model.cs'
[IO.File]::WriteAllText($model,'using System;using System.Collections.Generic;using UnityEngine;namespace DonQuixoteOverlay { public sealed class DonQuixoteSettings { public OverlaySettings Overlay=new OverlaySettings(); }'+$overlay+$layout+'}',[Text.UTF8Encoding]::new($false))
$csc='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$json='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice\A Dance of Fire and Ice_Data\Managed\Newtonsoft.Json.dll'
$harness=Join-Path $fixture 'GroupUi.dll'
$groupSources=@('OverlayPart','OverlayGroup','OverlayGroups','OverlayVisibility','OverlayTransforms')|ForEach-Object{Join-Path $project ('src/Mod/Features/Overlay/'+$_+'.cs')}
$layoutSources=Get-ChildItem -LiteralPath (Join-Path $project 'src/Mod/Features/LayoutEditing') -Filter *.cs|Where-Object Name -NE 'LayoutEditorController.cs'|ForEach-Object FullName
& $csc /nologo /target:library /langversion:latest /r:System.Core.dll ('/r:'+(Join-Path (Split-Path $json) 'netstandard.dll')) "/r:$json" "/out:$harness" $model $groupSources (Join-Path $project 'src/Mod/Presentation/Settings/SettingsWindow.Overlay.cs') (Join-Path $project 'src/Mod/Features/LayoutEditing/LayoutEditorController.cs') $layoutSources (Join-Path $PSScriptRoot 'OverlayGroupUiHarness.cs')
if($LASTEXITCODE -ne 0){throw 'Group UI harness compilation failed'}
[Reflection.Assembly]::LoadFrom($json)|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom($harness)
$results=$assembly.GetType('DonQuixoteOverlay.SettingsWindow',$true).GetMethod('Run').Invoke($null,@())
$results
'Overlay group UI assertions passed: '+$results.Count
