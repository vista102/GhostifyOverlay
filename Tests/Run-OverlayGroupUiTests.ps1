$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$source=[IO.File]::ReadAllText((Join-Path $project 'Settings.cs'))
$start=$source.IndexOf('    public sealed class OverlaySettings')
$end=$source.IndexOf('    public static class SettingsStore')
$overlay=$source.Substring($start,$end-$start).Replace('[JsonConverter(','[Newtonsoft.Json.JsonConverterAttribute(')
$start=$source.IndexOf('    public sealed class LayoutData')
$end=$source.IndexOf('    public static class LayoutStore')
$layout=$source.Substring($start,$end-$start)
$fixture=Join-Path $project ('Backups\Feedback-0.4.6\Ui-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture -Force|Out-Null
$model=Join-Path $fixture 'Model.cs'
[IO.File]::WriteAllText($model,'using System;using System.Collections.Generic;using UnityEngine;namespace DonQuixoteOverlay { public sealed class DonQuixoteSettings { public OverlaySettings Overlay=new OverlaySettings(); }'+$overlay+$layout+'}',[Text.UTF8Encoding]::new($false))
$csc='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$json='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice\A Dance of Fire and Ice_Data\Managed\Newtonsoft.Json.dll'
$harness=Join-Path $fixture 'GroupUi.dll'
& $csc /nologo /target:library /langversion:latest /r:System.Core.dll ('/r:'+(Join-Path (Split-Path $json) 'netstandard.dll')) "/r:$json" "/out:$harness" $model (Join-Path $project 'OverlayConfiguration.cs') (Join-Path $project 'SettingsWindow.Overlay.cs') (Join-Path $project 'LayoutEditorController.cs') (Join-Path $project 'LayoutEditSession.cs') (Join-Path $PSScriptRoot 'OverlayGroupUiHarness.cs')
if($LASTEXITCODE -ne 0){throw 'Group UI harness compilation failed'}
[Reflection.Assembly]::LoadFrom($json)|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom($harness)
$results=$assembly.GetType('DonQuixoteOverlay.SettingsWindow',$true).GetMethod('Run').Invoke($null,@())
$results
'Overlay group UI assertions passed: '+$results.Count
