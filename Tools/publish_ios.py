"""Publish this game's reviewed source/export and start its macOS IPA build.

Uses the already authenticated Git Credential Manager login through the helper.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
from IOSBuild import VERSION, IPA_NAME

ROOT = Path(__file__).resolve().parents[1]
REPO = 'yjmjeminq/angels-and-demons-ios'
TAG = f'v{VERSION}-ios.1'
STAGE = ROOT / f'Builds/PublicSource-v{VERSION}'
STATE = ROOT / f'.runtime/ios-build-state-v{VERSION}.json'
AUDIT = ROOT / f'.runtime/publication-audit-v{VERSION}.json'
ARCHIVE = ROOT / f'Artifacts/AngelsAndDemons-ios-export-v{VERSION}.zip'
ENV = dict(os.environ, GIT_TERMINAL_PROMPT='0', GCM_INTERACTIVE='never')


def sha(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def run(args, cwd=ROOT):
    result = subprocess.run(args, cwd=cwd, env=ENV, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=1800)
    if result.returncode:
        raise RuntimeError(result.stderr[-2000:] or result.stdout[-2000:])
    return result.stdout.strip()


def gh(*args):
    return run([sys.executable, '-X', 'utf8', str(ROOT / 'Tools/Cloud-GitCredential.py'), *map(str, args)])


def git(*args):
    return run(['git', '-c', 'credential.https://github.com.helper=', '-c', 'credential.https://github.com.helper=manager-core', '-c', 'credential.username=yjmjeminq', *args], STAGE)


def write(value):
    STATE.parent.mkdir(exist_ok=True)
    STATE.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')


def ensure_repo():
    assert gh('api', 'user', '--jq', '.login') == 'yjmjeminq'
    try:
        info = json.loads(gh('repo', 'view', REPO, '--json', 'nameWithOwner,visibility'))
        if info['visibility'] != 'PUBLIC':
            gh('repo', 'edit', REPO, '--visibility', 'public', '--accept-visibility-change-consequences')
    except Exception:
        gh('repo', 'create', REPO, '--public', '--description', 'Angels And Demons Unity source and iLoader iPhone test builds')


def prepare():
    ensure_repo()
    metadata = json.loads(Path(str(ARCHIVE) + '.json').read_text())
    assert sha(ARCHIVE) == metadata['sha256'] and metadata['export']['version'] == VERSION
    if STAGE.exists():
        shutil.rmtree(STAGE, ignore_errors=True)
    STAGE.mkdir(parents=True, exist_ok=True)
    git('init', '--initial-branch=main')
    for folder in ['Assets', 'Packages', 'ProjectSettings', 'Tools', 'Docs', '.github', 'ThirdPartyNotices']:
        src = ROOT / folder
        if src.exists():
            shutil.copytree(src, STAGE / folder, dirs_exist_ok=True, ignore=shutil.ignore_patterns('__pycache__', '*.pyc'))
    for name in ['README.md', '.gitignore']:
        if (ROOT / name).exists():
            shutil.copy2(ROOT / name, STAGE / name)

    forbidden = {'Library', 'Temp', 'Obj', 'Logs', 'UserSettings', 'Builds', 'BuildLogs', 'Artifacts', '.runtime'}
    patterns = [rb'\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,})\b',
                rb'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----']
    rows = []
    for path in sorted(STAGE.rglob('*')):
        if not path.is_file() or '.git' in path.relative_to(STAGE).parts:
            continue
        name = path.relative_to(STAGE).as_posix()
        assert not any(part in forbidden for part in path.relative_to(STAGE).parts), name
        assert path.suffix.lower() not in {'.p12', '.pfx', '.pem', '.key', '.mobileprovision', '.ipa', '.zip'}, name
        assert path.stat().st_size < 99 * 1024**2, name
        if path.suffix.lower() not in {'.png', '.jpg', '.jpeg', '.ttf', '.otf', '.wav', '.mp3'}:
            data = path.read_bytes()
            assert not any(re.search(p, data) for p in patterns), 'Sensitive data in ' + name
        rows.append({'path': name, 'bytes': path.stat().st_size, 'sha256': sha(path)})

    (ROOT / '.runtime').mkdir(exist_ok=True)
    AUDIT.write_text(json.dumps({'passed': True, 'files': rows, 'personalSavesIncluded': False, 'credentialsIncluded': False}, indent=2), encoding='utf-8')
    write({'repository': REPO, 'tag': TAG, 'version': VERSION, 'sourceSHA256': metadata['sha256'], 'stage': str(STAGE), 'status': 'prepared'})
    print(json.dumps({'passed': True, 'sourceFiles': len(rows), 'sourceBytes': sum(r['bytes'] for r in rows), 'sealedExportBytes': ARCHIVE.stat().st_size, 'stage': str(STAGE)}))


def publish():
    state = json.loads(STATE.read_text())
    assert state['status'] == 'prepared'
    audit = json.loads(AUDIT.read_text())
    assert audit['passed']
    for row in audit['files']:
        assert sha(STAGE / row['path']) == row['sha256'], 'Source changed after review'
    assert sha(ARCHIVE) == state['sourceSHA256']
    git('config', 'user.name', 'yjmjeminq')
    git('config', 'user.email', 'yjmjeminq@users.noreply.github.com')
    if git('status', '--porcelain'):
        git('add', '--all')
        git('commit', '-m', f'Angels And Demons {VERSION} iOS build')
    revision = git('rev-parse', 'HEAD')
    if not git('remote'):
        git('remote', 'add', 'origin', 'https://github.com/' + REPO + '.git')
    git('push', '-f', '-u', 'origin', 'main')
    notes = ROOT / f'.runtime/ios-release-notes-v{VERSION}.md'
    notes.write_text(f'천사와 악마 · 나루토 8등신 비키니 에디션 {VERSION} iPhone 무서명 IPA 빌드\n\n{IPA_NAME}를 iLoader에서 서명·설치하세요.\n', encoding='utf-8')
    try:
        gh('release', 'delete', TAG, '--repo', REPO, '--yes', '--cleanup-tag')
    except Exception:
        pass
    gh('release', 'create', TAG, str(ARCHIVE), '--repo', REPO, '--target', revision, '--title', f'Angels And Demons {VERSION} iPhone build', '--notes-file', str(notes), '--prerelease')
    state.update(revision=revision, status='published')
    write(state)
    gh('workflow', 'run', 'ios-build.yml', '--repo', REPO, '--ref', 'main', '-f', 'source_tag=' + TAG, '-f', 'source_sha256=' + state['sourceSHA256'])
    state['status'] = 'dispatched'
    write(state)
    print(json.dumps({'repository': 'https://github.com/' + REPO, 'release': 'https://github.com/' + REPO + '/releases/tag/' + TAG, 'revision': revision, 'status': state['status']}))


def status():
    state = json.loads(STATE.read_text())
    runs = json.loads(gh('api', 'repos/' + REPO + '/actions/workflows/ios-build.yml/runs?event=workflow_dispatch&per_page=10'))['workflow_runs']
    matching = [r for r in runs if r['head_sha'] == state.get('revision')]
    if not matching:
        print(json.dumps({'status': 'waiting_for_run'}))
        return
    job = matching[0]
    state.update(runId=job['id'], runURL=job['html_url'], status=job['status'], conclusion=job['conclusion'])
    write(state)
    jobs = json.loads(gh('api', 'repos/' + REPO + '/actions/runs/' + str(job['id']) + '/jobs'))['jobs']
    print(json.dumps({'runURL': state['runURL'], 'status': state['status'], 'conclusion': state['conclusion'], 'steps': [{'name': s['name'], 'status': s['status'], 'conclusion': s['conclusion']} for j in jobs for s in j['steps']]}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['prepare', 'publish', 'status'])
    args = parser.parse_args()
    {'prepare': prepare, 'publish': publish, 'status': status}[args.action]()
