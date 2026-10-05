param(
    [string]$Before=(Join-Path $PSScriptRoot 'GameApi-public-24397494.json'),
    [string]$After=(Join-Path $PSScriptRoot 'GameApi-alpha-25590222.json'),
    [string]$OutputFile=(Join-Path $PSScriptRoot 'ApiDiff-current-patches-to-alpha-25590222.json'),
    [string[]]$SourceFiles=@('UiInputBlock.cs','NativeJudgments.cs','GameplayPatches.cs')
)
$ErrorActionPreference='Stop'
$taskBefore=Get-Content -LiteralPath $Before -Raw|ConvertFrom-Json
$taskAfter=Get-Content -LiteralPath $After -Raw|ConvertFrom-Json
$taskChanges=@(foreach($taskOldType in $taskBefore.Types){
    $taskNewType=$taskAfter.Types|Where-Object Name -EQ $taskOldType.Name
    $taskMethodRemoved=@($taskOldType.Methods|Where-Object {$_ -notin $taskNewType.Methods})
    $taskMethodAdded=@($taskNewType.Methods|Where-Object {$_ -notin $taskOldType.Methods})
    $taskFieldRemoved=@($taskOldType.Fields|Where-Object {$_ -notin $taskNewType.Fields})
    $taskFieldAdded=@($taskNewType.Fields|Where-Object {$_ -notin $taskOldType.Fields})
    $taskPropertyRemoved=@($taskOldType.Properties|Where-Object {$_ -notin $taskNewType.Properties})
    $taskPropertyAdded=@($taskNewType.Properties|Where-Object {$_ -notin $taskOldType.Properties})
    if($taskMethodRemoved.Count -or $taskMethodAdded.Count -or $taskFieldRemoved.Count -or $taskFieldAdded.Count -or $taskPropertyRemoved.Count -or $taskPropertyAdded.Count){
        [pscustomobject]@{
            Name=$taskOldType.Name;RemovedMethods=$taskMethodRemoved;AddedMethods=$taskMethodAdded
            RemovedFields=$taskFieldRemoved;AddedFields=$taskFieldAdded
            RemovedProperties=$taskPropertyRemoved;AddedProperties=$taskPropertyAdded
        }
    }
})
$taskPatchTargets=@(foreach($taskSource in $SourceFiles){
    $taskCode=Get-Content -LiteralPath (Join-Path (Split-Path $PSScriptRoot) $taskSource) -Raw
    foreach($taskMatch in [regex]::Matches($taskCode,'\[HarmonyPatch\(typeof\((\w+)\),\s*"([^"]+)"')){
        $taskTypeName=$taskMatch.Groups[1].Value
        $taskMethodName=$taskMatch.Groups[2].Value
        $taskPattern='\s'+[regex]::Escape($taskMethodName)+'\('
        $taskOldType=$taskBefore.Types|Where-Object Name -EQ $taskTypeName
        $taskNewType=$taskAfter.Types|Where-Object Name -EQ $taskTypeName
        $taskOldMethods=@($taskOldType.Methods|Where-Object {$_ -match $taskPattern})
        $taskNewMethods=@($taskNewType.Methods|Where-Object {$_ -match $taskPattern})
        [pscustomobject]@{
            Source=$taskSource;Type=$taskTypeName;Method=$taskMethodName
            OldSignatures=$taskOldMethods;AlphaSignatures=$taskNewMethods
            Status=if(!$taskNewMethods.Count){'Removed'}elseif(($taskOldMethods -join '|') -eq ($taskNewMethods -join '|')){'SignatureUnchanged'}else{'SignatureChanged'}
        }
    }
})
$taskResult=[pscustomobject]@{
    Compared=(Get-Date -Format o)
    BeforeBuild=$taskBefore.SteamBuild;AlphaBuild=$taskAfter.SteamBuild
    ComparedExistingTypeCount=$taskBefore.Types.Count
    ChangedTypes=$taskChanges
    AddedCapturedTypes=@($taskAfter.Types|Where-Object {$_.Name -notin $taskBefore.Types.Name}|ForEach-Object {$_.Name})
    PatchAttributeCount=$taskPatchTargets.Count
    DistinctPatchTargetCount=@($taskPatchTargets|ForEach-Object {$_.Type+'.'+$_.Method}|Sort-Object -Unique).Count
    PatchTargets=$taskPatchTargets
    Limitation='Metadata signatures only; no Harmony patch applied and no game behavior invoked.'
}
[IO.File]::WriteAllText($OutputFile,($taskResult|ConvertTo-Json -Depth 7),[Text.UTF8Encoding]::new($false))
$taskResult|Select-Object BeforeBuild,AlphaBuild,ComparedExistingTypeCount,PatchAttributeCount,DistinctPatchTargetCount|ConvertTo-Json
$taskPatchTargets|Where-Object Status -NE 'SignatureUnchanged'|ConvertTo-Json -Depth 5
