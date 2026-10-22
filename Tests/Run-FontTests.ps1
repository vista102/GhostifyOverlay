$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$provider=Get-Content -LiteralPath (Join-Path $project 'src/Mod/Infrastructure/Fonts/FontAssetProvider.cs') -Encoding UTF8 -Raw
$provider=$provider.Replace('using HarmonyLib;','').Replace('using UnityEngine.SceneManagement;','')
$harness=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'FontProviderHarness.cs') -Raw
# Compile the production provider against stubs of the inspected native API contracts.
Add-Type -TypeDefinition ('using System.Collections.Generic;'+$provider+$harness.Replace('using System;','').Replace('using System.Collections.Generic;','')) -ReferencedAssemblies 'System.Core'
$results=[DonQuixoteOverlay.FontProviderHarness]::Run($project)
$results
'Font startup assertions passed: '+$results.Count
