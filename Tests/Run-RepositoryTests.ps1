$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$passed=0
function Check($okay,[string]$label){if(!$okay){throw ('FAIL '+$label)};$script:passed++;'PASS '+$label}
Check (@(Get-ChildItem -LiteralPath $project -Filter *.cs -File).Count -eq 0) 'production C# files are organized under src'
Check (@(Get-ChildItem -LiteralPath (Join-Path $project 'Installer') -Filter *.cs -File).Count -eq 0) 'installer C# files and tests have dedicated source folders'
[xml]$definition=Get-Content -LiteralPath (Join-Path $project 'GhostifyOverlay.csproj') -Raw
$includes=@($definition.Project.ItemGroup.Compile|Where-Object {$null -ne $_}|ForEach-Object Include)
Check ($includes.Count -eq 1 -and $includes[0] -eq 'src\Mod\**\*.cs') 'mod build excludes installer and test sources by construction'
foreach($folder in @('Bootstrap','Application','Domain','Features','Integration','Infrastructure','Presentation')){Check (Test-Path -LiteralPath (Join-Path $project ('src/Mod/'+$folder)) -PathType Container) ('mod responsibility folder exists: '+$folder)}
foreach($folder in @('Bootstrap','Application','Domain','Infrastructure','Presentation')){Check (Test-Path -LiteralPath (Join-Path $project ('src/Installer/'+$folder)) -PathType Container) ('installer responsibility folder exists: '+$folder)}
Check (@(Get-ChildItem -LiteralPath $project -Filter *.md -File|Where-Object Name -NotIn @('README.md','THIRD-PARTY-NOTICES.md','AI_ATTRIBUTION.md')).Count -eq 0) 'root markdown is limited to usage, licenses and AI provenance'
foreach($file in @('docs/README.md','docs/architecture/ARCHITECTURE.md','docs/development/BUILD_AND_TEST.md','docs/validation/REFACTOR_0.4.11_VALIDATION.md')){Check (Test-Path -LiteralPath (Join-Path $project $file)) ('developer documentation exists: '+$file)}
$documents=@((Join-Path $project 'README.md'),(Join-Path $project 'THIRD-PARTY-NOTICES.md'),(Join-Path $project 'AI_ATTRIBUTION.md'),(Join-Path $project 'Installer/README.md'))
$documents+=Get-ChildItem -LiteralPath (Join-Path $project 'docs') -Filter *.md -Recurse -File|Where-Object {$_.FullName -notlike '*\docs\local\*'}|ForEach-Object FullName
foreach($file in $documents){
    $text=[IO.File]::ReadAllText($file)
    foreach($match in [regex]::Matches($text,'!?\[[^\]]*\]\(([^\)]+)\)')){
        $target=$match.Groups[1].Value.Trim('<','>')
        if($target -match '^[a-zA-Z][a-zA-Z0-9+.-]*:' -or $target.StartsWith('#')){continue}
        $target=[Uri]::UnescapeDataString($target.Split('#',2)[0])
        if(!$target){continue}
        Check (Test-Path -LiteralPath (Join-Path (Split-Path $file) $target)) ('local markdown link resolves: '+[IO.Path]::GetFileName($file)+' -> '+$target)
    }
}
'Repository structure / link assertions passed: '+$passed
