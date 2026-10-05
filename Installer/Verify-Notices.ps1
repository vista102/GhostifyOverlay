param([string]$Root=(Split-Path $PSScriptRoot))
$ErrorActionPreference='Stop'

# Reviewed license texts. Normalize line endings so a Windows/Linux checkout
# preserves the same check while retaining all copyright, conditions and disclaimers.
# Jipper: ac6d6410571ee7d585aedce7f5085de34f615af2/LICENSE.
# Noto: google/fonts 4efc2774c63917927efe769ca845def6bd6debae/ofl/notosanskr/OFL.txt.
# Gmarket: the vendor license retained with the original Light/Medium fonts.
$notices=@{
    'Licenses/JipperResourcePack-BSD-3-Clause.txt'='B85C066B532EA07471B8575D0495DB987C811186805DE2266062529CF0A81226'
    'Assets/GmarketSans-OFL.txt'='730DB2B2DD4C86E67F8EB48E293979448976A6E4709AA6A0FB4608B004CA8C39'
    'Assets/NotoSansKR-OFL.txt'='1C05C68C34F9708415AADA51F17E1B0092D2CEA709BF4A94CD38114F9E73D7D9'
}
foreach($entry in $notices.GetEnumerator()){
    $path=Join-Path $Root $entry.Key
    if(!(Test-Path -LiteralPath $path -PathType Leaf)){throw ('Required third-party notice is missing: '+$entry.Key)}
    $text=[IO.File]::ReadAllText($path).Replace("`r",'').TrimEnd([char]10)+"`n"
    $sha=[Security.Cryptography.SHA256]::Create()
    try{$hash=[BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text))).Replace('-','')}finally{$sha.Dispose()}
    if($hash -ne $entry.Value){throw ('Third-party notice differs from its reviewed full text: '+$entry.Key)}
    'PASS complete third-party notice '+$entry.Key
}
