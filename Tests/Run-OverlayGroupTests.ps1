param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$managed=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
foreach($dir in @($managed,(Join-Path $managed 'UnityModManager'))){foreach($file in Get-ChildItem -LiteralPath $dir -Filter *.dll){try{[Reflection.Assembly]::LoadFrom($file.FullName)|Out-Null}catch{}}}
$mod=[Reflection.Assembly]::LoadFrom((Join-Path $project 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$passed=0
function T([string]$name){$mod.GetType('DonQuixoteOverlay.'+$name,$true)}
function Call($target,[string]$name,[object[]]$values){$type=if($target -is [Type]){$target}else{$target.GetType()};$instance=if($target -is [Type]){$null}else{$target};for($i=0;$i -lt $values.Count;$i++){if($null -ne $values[$i]){$values[$i]=$values[$i].PSObject.BaseObject}};$type.GetMethod($name,$flags).Invoke($instance,$values)}
function Check($okay,[string]$name){if(!$okay){throw ('FAIL '+$name)};$script:passed++;'PASS '+$name}
function F($value,[string]$name){$value.GetType().GetField($name,$flags).GetValue($value)}
$settings=[Activator]::CreateInstance((T 'DonQuixoteSettings'))
$catalog=@((T 'OverlayGroups').GetField('All',$flags).GetValue($null))
Check ($catalog.Count -eq 5) 'exactly five overlay groups'
$names=@();$parts=@()
foreach($group in $catalog){$fields=@(F $group 'Fields');$labels=@(F $group 'Labels');Check ($fields.Length -eq $labels.Length) ('labels match members '+(F $group 'Title'));foreach($field in @((F $group 'EnabledField'))+$fields){Check ($null -ne (T 'OverlaySettings').GetField($field)) ('group references an existing setting '+$field)};$names+=$fields;$parts+=F $group 'Parts'}
Check (($names|Sort-Object -Unique).Count -eq 16 -and $names.Count -eq 16) 'each overlay component belongs to exactly one group'
Check (($parts|ForEach-Object ToString|Sort-Object -Unique).Count -eq 9) 'all nine independent layout parts have controls'
$map=@(@('Progress','Accuracy','XAccuracy','MusicTime','MapTime'),@('Bpm','Pitch','Kps','Fps'),@('ProgressBar','SongInfo','Combo'),@('Judgments','Detailed','Timing'),@('Attempts'))
foreach($field in $names){Call (T 'OverlayGroups') 'Set' @($settings.Overlay,$field,$true)|Out-Null}
for($i=0;$i -lt 5;$i++){
    $groupField='Group'+($i+1)+'Enabled';Call (T 'OverlayGroups') 'Set' @($settings.Overlay,$groupField,$false)|Out-Null
    $show=[Activator]::CreateInstance((T 'OverlayVisibility'),$flags,$null,[object[]]@($settings.Overlay),$null)
    for($j=0;$j -lt 5;$j++){foreach($member in $map[$j]){Check ((F $show $member) -eq ($j -ne $i)) ('master affects only its group '+($i+1)+'/'+$member)}}
    foreach($field in F $catalog[$i] 'Fields'){Check (Call (T 'OverlayGroups') 'Get' @($settings.Overlay,$field)) ('master preserves child preference '+$field)}
    Call (T 'OverlayGroups') 'Set' @($settings.Overlay,$groupField,$true)|Out-Null
}
$settings.Overlay.ShowBpm=$false;$settings.Overlay.ShowPitch=$true;$settings.Overlay.ShowJudgmentCounts=$false;$settings.Overlay.ShowDetailedJudgments=$true
$show=[Activator]::CreateInstance((T 'OverlayVisibility'),$flags,$null,[object[]]@($settings.Overlay),$null)
Check (!(F $show 'Bpm') -and (F $show 'Pitch') -and !(F $show 'Judgments') -and (F $show 'Detailed')) 'Pitch and detailed judgments have independent toggles'
$layout=[Activator]::CreateInstance((T 'LayoutData'));$layout.TopLeftX=37;$layout.DetailedPerfectX=66
foreach($part in $parts){foreach($axis in @('X','Y','Scale')){Check (Call (T 'OverlayTransforms') 'Set' @($layout,$part,$axis,[float]$(if($axis -eq 'Scale'){1.4}else{123}))) ('layout input exists '+$part+'/'+$axis)}}
$before=[Newtonsoft.Json.JsonConvert]::SerializeObject($layout)
Check (!(Call (T 'OverlayTransforms') 'Set' @($layout,$parts[0],'Scale',[float]::NaN)) -and [Newtonsoft.Json.JsonConvert]::SerializeObject($layout) -eq $before) 'invalid scale cannot change saved layout'
Call (T 'OverlayTransforms') 'Set' @($layout,$parts[7],'X',[float]200)|Out-Null
Check ($layout.DetailedPerfectX -eq 123 -and $layout.TimingRangesX -eq 200) 'timing edit cannot move detailed judgments'
Call (T 'OverlayTransforms') 'Reset' @($layout,$parts[2])|Out-Null
Check ($layout.ProgressBarX -eq 0 -and $layout.ProgressBarScale -eq 1 -and $layout.TimingRangesX -eq 200) 'part reset cannot reset another group'
$fixture=Join-Path $project ('Backups\Feedback-0.4.6\Groups-'+[Guid]::NewGuid().ToString('N'));Call (T 'SettingsStore') 'Initialize' @($fixture)|Out-Null
$path=Join-Path $fixture 'UserData\settings.json'
[IO.File]::WriteAllText($path,'{"SchemaVersion":3,"Effects":{"Enabled":true,"DisableBloom":true},"Overlay":{"ShowJudgmentCounts":false,"ShowBpm":false,"FullAttempts":{"saved":12}}}')
$loaded=Call (T 'SettingsStore') 'Load' @();Check ($loaded.SchemaVersion -eq 4 -and !$loaded.Overlay.ShowDetailedJudgments -and !$loaded.Overlay.ShowBpm -and $loaded.Overlay.ShowPitch -and $loaded.Overlay.Group1Enabled -and $loaded.Overlay.FullAttempts['saved'] -eq 12) 'legacy prefs and attempts survive group migration'
Call (T 'SettingsStore') 'Save' @($loaded)|Out-Null;Check ([IO.File]::ReadAllText($path) -notmatch 'Effects|DisableBloom') 'obsolete effect settings no longer save'
$loaded.Overlay.ShowDetailedJudgments=$true;$loaded.Overlay.Group4Enabled=$false
Call (T 'SettingsStore') 'Save' @($loaded)|Out-Null;$reloaded=Call (T 'SettingsStore') 'Load' @()
Check (!$reloaded.Overlay.ShowJudgmentCounts -and $reloaded.Overlay.ShowDetailedJudgments -and !$reloaded.Overlay.Group4Enabled) 'new master and independent detail choices survive reload'
(T 'LayoutStore').GetField('Current',$flags).SetValue($null,$layout)
Call (T 'LayoutStore') 'Save' @()|Out-Null
Call (T 'SettingsStore') 'Initialize' @($fixture)|Out-Null
$restored=(T 'LayoutStore').GetField('Current',$flags).GetValue($null)
Check ($restored.TimingRangesX -eq 200 -and $restored.DetailedPerfectX -eq 123 -and $restored.ProgressBarX -eq 0) 'independent placements survive layout save and reload'
[IO.File]::WriteAllText($path,'{"Overlay":{"ShowJudgmentCounts":false,"FullAttempts":{"saved":12}}}')
$missingSchema=Call (T 'SettingsStore') 'Load' @()
Check (!$missingSchema.Overlay.ShowDetailedJudgments -and $missingSchema.Overlay.FullAttempts['saved'] -eq 12) 'schema-less legacy files preserve hidden details and attempt records'
$keys=Call (T 'KeyViewerContents.KeyViewerStore') 'Deserialize' @('{"SchemaVersion":5,"ShowTotalKpsKey16":false,"KeyGap":12}')
Check ($null -eq $keys.GetType().GetField('ShowTotalKpsKey16') -and $keys.KeyGap -eq 12) 'old footer OFF value is ignored without resetting key spacing'
foreach($name in @('EffectsSettings','EffectGate','EffectLifetime','EffectRestoration','DisableBloomPatch','DisableInitialFiltersPatch','LimitMoveTrackPatch')){Check ($null -eq $mod.GetType('DonQuixoteOverlay.'+$name)) ('removed runtime type '+$name)}
Check ($null -ne $mod.GetType('DonQuixoteOverlay.LayoutEditorController')) 'screen layout editing is restored'
Check (@(Call (T 'PatchRegistry') 'Catalog' @()).Count -eq 5) 'standalone overlay and input patch groups are registered'
$typedCatalog=(T 'PatchRegistry').GetMethod('Catalog',$flags).Invoke($null,@())
Call (T 'PatchRegistry') 'ValidateCatalog' @($mod,$typedCatalog)|Out-Null
Check ($true) 'every remaining Harmony class is classified'
'Overlay group assertions passed: '+$passed
