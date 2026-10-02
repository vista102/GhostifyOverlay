"""Prepare a static Regular face from the pinned official Google Fonts source."""
import hashlib
import json
import shutil
import sys
from pathlib import Path

project = Path(__file__).resolve().parent.parent
reference = project.parent / "_references" / "NotoSansKR"
sys.path.insert(0, str(reference / "fonttools"))
import fontTools
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

manifest = json.loads((reference / "source.json").read_text(encoding="utf-8-sig"))
source = reference / "NotoSansKR-VF.ttf"
if hashlib.sha256(source.read_bytes()).hexdigest().upper() != manifest["FontSha256"]:
    raise RuntimeError("Official source hash mismatch")
font = TTFont(source, recalcTimestamp=False)
font = instantiateVariableFont(font, {"wght": 400}, inplace=True, updateFontNames=True)
if "fvar" in font or font["OS/2"].usWeightClass != 400:
    raise RuntimeError("Regular instantiation failed")
family = font["name"].getDebugName(16) or font["name"].getDebugName(1)
style = font["name"].getDebugName(17) or font["name"].getDebugName(2)
if family != "Noto Sans KR" or style != "Regular":
    raise RuntimeError(f"Unexpected font names: {family}, {style}")
missing = [cp for cp in range(0xAC00, 0xD7A4) if cp not in font.getBestCmap()]
if missing:
    raise RuntimeError("Missing Hangul syllables")
destination = project / "Assets" / "NotoSansKR-Regular.ttf"
font.save(destination)
font.close()
shutil.copyfile(reference / "OFL.txt", project / "Assets" / "NotoSansKR-OFL.txt")
manifest.update({
    "Output": destination.name,
    "OutputSha256": hashlib.sha256(destination.read_bytes()).hexdigest().upper(),
    "Family": family,
    "Style": style,
    "Weight": 400,
    "FontToolsVersion": fontTools.__version__,
    "Conversion": "instantiateVariableFont(wght=400, updateFontNames=True); no glyph subsetting",
    "HangulSyllables": 11172,
})
(project / "Assets" / "NotoSansKR-source.json").write_text(
    json.dumps(manifest, indent=2) + "\n", encoding="utf-8"
)
print(json.dumps(manifest, indent=2))
