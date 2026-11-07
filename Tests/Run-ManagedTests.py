#!/usr/bin/env python3
"""Compile production and run managed regression harnesses; never launch/install the game."""
import argparse
import os
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--managed', required=True, type=Path, help='ADOFAI Data/Managed directory (read only)')
parser.add_argument('--compiler', required=True, type=Path, help='Roslyn csc.dll from a local .NET SDK')
parser.add_argument('--framework', default='/opt/homebrew/lib/mono/4.8-api', type=Path)
parser.add_argument('--dotnet', default='dotnet')
parser.add_argument('--mono', default='mono')
args = parser.parse_args()
work = ROOT / 'bin' / 'managed-checks'
work.mkdir(parents=True, exist_ok=True)
framework = [args.framework / name for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
json = args.managed / 'Newtonsoft.Json.dll'
netstandard = args.managed / 'netstandard.dll'
harmony = args.managed / 'UnityModManager' / '0Harmony.dll'
env = dict(os.environ, MONO_PATH=str(work), GHOSTIFY_TEST_MANAGED=str(args.managed))


def compile(name, sources, refs=(), executable=False):
    output = work / (name + ('.exe' if executable else '.dll'))
    subprocess.run([args.dotnet, str(args.compiler), '/nologo', '/nostdlib+', '/langversion:latest',
                    *([] if name == 'GhostifyOverlay' else ['/nowarn:0649,0414']),
                    '/target:' + ('exe' if executable else 'library'), '/out:' + str(output),
                    *('/r:' + str(p) for p in [*framework, *refs]),
                    *(str(ROOT / p) for p in sources)], check=True, cwd=ROOT)
    return output


def script_sources(name):
    # The Windows harness source lists are the authority for both runners.
    text = (ROOT / 'Tests' / name).read_text(encoding='utf-8-sig')
    return re.findall(r"'((?:src|Tests)/[^']+\.cs)'", text)


ns = {'m': 'http://schemas.microsoft.com/developer/msbuild/2003'}
project = ET.parse(ROOT / 'GhostifyOverlay.csproj')
production_refs = []
for hint in project.findall('.//m:Reference/m:HintPath', ns):
    path = hint.text.replace('$(ManagedDir)', str(args.managed)).replace('$(ModManagerDir)', str(harmony.parent)).replace('\\', '/')
    production_refs.append(Path(path))
production = compile('GhostifyOverlay', sorted(ROOT.glob('src/Mod/**/*.cs')), production_refs)
print('Production: compiled against local ADOFAI / Unity / UMM and Mono .NET Framework 4.8 references', flush=True)
runner = compile('ManagedHarnessRunner', ['Tests/ManagedHarnessRunner.cs'], executable=True)
state = 'src/Mod/Application/Replays/ReplayRunState.cs'
harnesses = [
    ('ReplayApiBridgeHarness', [state, 'src/Mod/Integration/TUFReplay/ReplayApiBridge.cs', 'Tests/ReplayApiBridgeHarness.cs'], [], None),
    ('KeyViewerReplayHarness', script_sources('Run-KeyViewerReplayTests.ps1'), [json, netstandard], None),
    ('DonQuixoteOverlay.AttemptTrackerHarness', list(dict.fromkeys(script_sources('Run-AttemptTrackerTests.ps1') + ['Tests/AttemptTrackerHarness.cs'])), [], None),
    ('FeatureIntegrationTests', script_sources('Run-FeatureIntegrationTests.ps1'), [harmony], None),
    ('KeyViewerTests', ['Tests/KeyViewerTests.cs'], [], str(production)),
    ('DonQuixoteOverlay.KeyViewerContents.RainRegressionHarness', [*ROOT.glob('src/Mod/Features/KeyViewer/Rain/*.cs'),
        'src/Mod/Features/KeyViewer/Layout/KeyViewerMetrics.cs', 'Tests/RainRegressionHarness.cs'], [], None),
    ('ArchitectureServiceTests', [*ROOT.glob('src/Mod/Application/**/*.cs'), *ROOT.glob('src/Mod/Domain/Presets/*.cs'),
        *(ROOT / ('src/Mod/Infrastructure/Persistence/' + name + '.cs') for name in ('IJsonStorage', 'AtomicJsonStorage', 'JsonSettingsRepository')),
        ROOT / 'Tests/ArchitectureServiceTests.cs'], [json, netstandard], str(work / 'architecture-fixture')),
]
for name, sources, refs, argument in harnesses:
    assembly = compile(name, sources, refs)
    subprocess.run([args.mono, str(runner), str(assembly), name, *([] if argument is None else [argument])], env=env, check=True, cwd=ROOT)
