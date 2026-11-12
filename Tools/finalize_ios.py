from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import sys
from IOSBuild import VERSION, IPA_NAME, verify_ipa
from publish_ios import STATE

root = Path(__file__).resolve().parents[1]
statepath = STATE
state = json.loads(statepath.read_text())
repo = state['repository']
tag = state['tag']


def gh(*args):
    p = subprocess.run([sys.executable, '-X', 'utf8', str(root / 'Tools/Cloud-GitCredential.py'), *args],
                       cwd=root, capture_output=True, text=True, encoding='utf-8', check=True)
    return p.stdout


def api(path):
    return json.loads(gh('api', path))


run = api(f'repos/{repo}/actions/runs/{state["runId"]}')
assert run['status'] == 'completed' and run['conclusion'] == 'success', 'Mac build has not completed successfully'
assert run['head_sha'] == state['revision']
release = api(f'repos/{repo}/releases/tags/{tag}')
name = IPA_NAME
asset = next(a for a in release['assets'] if a['name'] == name)
assert asset['state'] == 'uploaded' and asset['size'] > 1024 * 1024
download = root / f'.runtime/ios-download-v{VERSION}'
if download.exists():
    shutil.rmtree(download, ignore_errors=True)
download.mkdir(parents=True)
for pattern in [name, name + '.sha256', 'build-report.json', 'input-verification.json']:
    gh('release', 'download', tag, '--repo', repo, '--pattern', pattern, '--dir', str(download))

ipa = download / name
sha = hashlib.file_digest(ipa.open('rb'), 'sha256').hexdigest()
assert (download / (name + '.sha256')).read_text().split()[0] == sha
build = json.loads((download / 'build-report.json').read_text())
inputs = json.loads((download / 'input-verification.json').read_text())
assert build['passed'] and build['ipaCreated'] and build['runURL'] == state['runURL']
assert build['sourceRevision'] == state['revision'] and build['ipa']['sha256'] == sha
assert inputs['passed'] and inputs['archiveSHA256'] == state['sourceSHA256']
package_report = f'AngelsAndDemons-ios-package-verification-v{VERSION}.json'
verification = verify_ipa(ipa, root / 'Builds/iOS-Xcode', download / package_report)
assert verification['passed'] and verification['sha256'] == sha
assert verification['app']['gameFilesSHA256'] == inputs['gameFilesSHA256'] == build['app']['gameFilesSHA256']

artifacts = root / 'Artifacts'
artifacts.mkdir(exist_ok=True)
files = {
    name: ipa,
    name + '.sha256': download / (name + '.sha256'),
    f'AngelsAndDemons-ios-build-report-v{VERSION}.json': download / 'build-report.json',
    f'AngelsAndDemons-ios-input-verification-v{VERSION}.json': download / 'input-verification.json',
    package_report: download / package_report,
}
for filename, source in files.items():
    dst = artifacts / filename
    if dst.exists():
        dst.unlink()
    with source.open('rb') as reader, dst.open('xb') as writer:
        shutil.copyfileobj(reader, writer)

state.update(ipaSHA256=sha, ipaBytes=ipa.stat().st_size, ipaPath=str(artifacts / name), verified=True)
statepath.write_text(json.dumps(state, indent=2), encoding='utf-8')
print(json.dumps({
    'passed': True,
    'ipa': str(artifacts / name),
    'bytes': ipa.stat().st_size,
    'sha256': sha,
    'app': verification['app'],
    'runURL': state['runURL'],
    'physicalIPhoneTested': False
}, indent=2))
