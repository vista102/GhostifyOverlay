param(
    [string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice',
    [string[]]$TypeNames,
    [string]$NamePattern='HitMargin|Judgment|Gameplay|SavedSettings|XPerfect|ErrorMeterTick|^Difficulty$'
)
$ErrorActionPreference='Stop'
$taskManaged=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
foreach($taskDir in @($taskManaged,(Join-Path $taskManaged 'UnityModManager'))){
    Get-ChildItem -LiteralPath $taskDir -Filter *.dll | ForEach-Object {
        try { [Reflection.Assembly]::LoadFrom($_.FullName)|Out-Null } catch {}
    }
}
$taskAssembly=[Reflection.Assembly]::LoadFrom((Join-Path $taskManaged 'Assembly-CSharp.dll'))
$taskFlags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance,DeclaredOnly'
try { $taskTypes=$taskAssembly.GetTypes() }
catch [Reflection.ReflectionTypeLoadException] { $taskTypes=@($_.Exception.Types|Where-Object { $null -ne $_ }) }
if(!$TypeNames){
    $taskTypes|Where-Object { $_.FullName -match $NamePattern }|ForEach-Object {$_.FullName}
    exit
}
foreach($taskTypeName in @($TypeNames|ForEach-Object {$_ -split ','})){
    $taskType=$taskAssembly.GetType($taskTypeName,$false)
    if(!$taskType){ [pscustomobject]@{Name=$taskTypeName;Exists=$false}|ConvertTo-Json;continue }
    [pscustomobject]@{
        Name=$taskType.FullName
        Enum=if($taskType.IsEnum){ @([Enum]::GetNames($taskType)|ForEach-Object {
            [pscustomobject]@{Name=$_;Value=[int][Enum]::Parse($taskType,$_)}
        }) }else{@()}
        Fields=@($taskType.GetFields($taskFlags)|ForEach-Object{$_.ToString()})
        Properties=@($taskType.GetProperties($taskFlags)|ForEach-Object{$_.ToString()})
        Methods=@($taskType.GetMethods($taskFlags)|ForEach-Object{$_.ToString()})
    }|ConvertTo-Json -Depth 5
}
# Metadata only: no game method, property getter or field initializer is invoked.
