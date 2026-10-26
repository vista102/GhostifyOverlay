param([switch]$InstallerOnly)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
if($InstallerOnly){
    $scripts=@('Installer/Run-InstallerTests.ps1','Installer/Run-InstallerUiTests.ps1','Installer/Verify-Shipping.ps1')
}else{
    $scripts=@('Tests/Run-RepositoryTests.ps1','Tests/Run-ModManagerCompatibilityTests.ps1','Tests/Run-FactoryDefaultsTests.ps1','Tests/Run-ArchitectureTests.ps1','Tests/Run-FeatureIntegrationTests.ps1','Tests/Run-StandaloneAlphaTests.ps1','Tests/Run-PatchTests.ps1','Tests/Run-AlphaTests.ps1','Tests/Run-KeyViewerTests.ps1','Tests/Run-AttemptTrackerTests.ps1','Tests/Run-OverlayGroupTests.ps1','Tests/Run-OverlayGroupUiTests.ps1','Tests/Run-LogicTests.ps1','Tests/Run-RainRegressionTests.ps1','Tests/Run-UiFixTests.ps1','Tests/Run-FontTests.ps1','Tests/Run-ColorRowTests.ps1','Tests/Run-WindowSurfaceTests.ps1')
}
foreach($script in $scripts){
    $arguments=@('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $project $script))
    if($script -eq 'Installer/Run-InstallerTests.ps1' -and (Get-Process -Name 'A Dance of Fire and Ice' -ErrorAction SilentlyContinue)){$arguments+='-SkipShippingInstall'}
    & powershell.exe @arguments
    if($LASTEXITCODE -ne 0){throw ('Test suite failed: '+$script)}
}
'All selected test suites passed: '+$scripts.Count
