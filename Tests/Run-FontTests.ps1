$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$source=Get-Content -LiteralPath (Join-Path $project 'OverlayController.cs') -Encoding UTF8 -Raw
$start=$source.IndexOf('    internal static class FontAssetProvider')
$end=$source.IndexOf('    internal static class AttemptTracker')
if($start -lt 0 -or $end -le $start) {throw 'Font provider source boundaries are missing'}
$provider='using System; using System.IO; using System.Collections.Generic; using TMPro; using UnityEngine; using UnityEngine.TextCore.LowLevel; namespace DonQuixoteOverlay {'+$source.Substring($start,$end-$start)+'}'
$harness=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'FontProviderHarness.cs') -Raw
# Compile the production provider against stubs of the inspected native API contracts.
Add-Type -TypeDefinition ($provider+$harness.Replace('using System;','').Replace('using System.Collections.Generic;','')) -ReferencedAssemblies 'System.Core'
$results=[DonQuixoteOverlay.FontProviderHarness]::Run($project)
$results
'Font startup assertions passed: '+$results.Count
