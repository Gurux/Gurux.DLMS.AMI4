#!/usr/bin/env python3
"""Initialize systemd updater JSON from the installation's existing AMI settings."""
import argparse
import copy
import json
import os
import re
import stat
import tempfile
from pathlib import Path, PurePosixPath
from urllib.parse import urlsplit


def read_json(path):
    with path.open(encoding='utf-8-sig') as source:
        value = json.load(source)
    if not isinstance(value, dict):
        raise TypeError('Configuration must be a JSON object.')
    return value


def address(value):
    if value is None:
        raise ValueError('Supply --public-url or set Server.Address in the existing AMI settings.')
    if not isinstance(value, str):
        raise TypeError('Public URL must be a string.')
    parsed = urlsplit(value)
    _ = parsed.port
    if (not parsed.hostname or parsed.username or parsed.password or parsed.query or parsed.fragment
            or '?' in value or '#' in value or any(c.isspace() for c in parsed.netloc)
            or parsed.hostname == 'ami.example.com'
            or parsed.scheme != 'https' and not (parsed.scheme == 'http'
                                                and parsed.hostname in {'localhost', '127.0.0.1', '::1'})):
        raise ValueError('Use an actual HTTPS public URL, or HTTP loopback for a local installation.')
    return value.rstrip('/') + '/'


def prepare(settings, existing, *, public_url=None, root=None, service=None):
    ami = copy.deepcopy(settings)
    defaults = read_json(Path(__file__).with_name('updater.defaults.json'))
    updater = {**defaults, **copy.deepcopy(existing)}
    section = ami.setdefault('Updater', {})
    deployment = section.setdefault('Deployment', {})
    if (updater['mode'] != 'static' or deployment.get('Mode', 'static') != 'static'
            or updater['productId'] != 'Gurux.DLMS.AMI' or updater['includeBackend'] is not True
            or deployment.get('ProductId', 'Gurux.DLMS.AMI') != 'Gurux.DLMS.AMI'
            or deployment.get('IncludeBackend', True) is not True):
        raise ValueError('This initializer is for full AMI systemd installations only.')
    updater['root'] = root or existing.get('root') or deployment.get('Root') or defaults['root']
    updater['catalogUrl'] = existing.get('catalogUrl') or section.get('CatalogUrl') or defaults['catalogUrl']
    updater['serviceName'] = service or updater['serviceName']
    updater['publicUrl'] = address(public_url or existing.get('publicUrl') or ami.get('Server', {}).get('Address'))
    updater['apiVersionUrl'] = (existing.get('apiVersionUrl') if not public_url else None) or updater['publicUrl'] + 'api/Configuration/Version'
    address(updater['apiVersionUrl'])
    address(updater['catalogUrl'])
    install_root = updater['root']
    if (not isinstance(install_root, str) or not install_root.startswith('/') or install_root == '/'
            or '..' in PurePosixPath(install_root).parts or '\\' in install_root or '\x00' in install_root):
        raise ValueError('Deployment root must be an absolute Linux installation directory.')
    if not isinstance(updater['serviceName'], str) or not re.fullmatch(r'[A-Za-z0-9_@][A-Za-z0-9_.@-]*', updater['serviceName']):
        raise ValueError('Invalid systemd service name.')
    if updater['backendAssembly'] != 'Gurux.DLMS.AMI.dll' or type(updater['checkTimeoutSeconds']) is not int or updater['checkTimeoutSeconds'] < 1:
        raise ValueError('Invalid backend assembly or verification timeout.')
    section['CatalogUrl'] = updater['catalogUrl']
    deployment.update(Root=install_root, ProductId=updater['productId'], IncludeBackend=True, Mode='static')
    return ami, updater


def write_json(path, value, group):
    content = (json.dumps(value, ensure_ascii=False, indent=2) + '\n').encode('utf-8')
    if path.exists() and path.read_bytes() == content:
        return
    metadata = path.stat() if path.exists() else None
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary = tempfile.mkstemp(prefix='.' + path.name + '-', dir=path.parent)
    try:
        with os.fdopen(descriptor, 'wb') as target:
            target.write(content)
            target.flush()
            os.fsync(target.fileno())
        if os.name == 'posix':
            os.chown(temporary, metadata.st_uid if metadata else os.geteuid(), metadata.st_gid if metadata else group)
        # Preserve existing service access; only newly created updater settings
        # receive the private default. Ownership/group are retained above.
        os.chmod(temporary, stat.S_IMODE(metadata.st_mode) if metadata else 0o640)
        os.replace(temporary, path)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


def initialize(ami_path, updater_path, **options):
    if ami_path.is_symlink() or updater_path.is_symlink() or ami_path.resolve() == updater_path.resolve():
        raise ValueError('Use distinct regular configuration files.')
    settings = read_json(ami_path)
    existing = read_json(updater_path) if updater_path.exists() else {}
    ami, updater = prepare(settings, existing, **options)
    group = ami_path.stat().st_gid
    write_json(updater_path, updater, group)
    write_json(ami_path, ami, group)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ami-settings', type=Path, default=Path('/etc/gurux-ami/ami-settings.json'))
    parser.add_argument('--updater-settings', type=Path, default=Path('/etc/gurux-ami/updater.json'))
    parser.add_argument('--public-url', help='Use when the existing settings do not contain Server.Address.')
    parser.add_argument('--root', help='Override the standard /srv/gurux-ami deployment root.')
    parser.add_argument('--service', help='Override the standard gurux-ami.service unit name.')
    args = parser.parse_args()
    try:
        initialize(args.ami_settings, args.updater_settings, public_url=args.public_url, root=args.root, service=args.service)
    except (OSError, ValueError, TypeError, AttributeError):
        parser.exit(1, 'Settings were not initialized. Check file permissions and valid JSON objects; supply --public-url if Server.Address is missing or still an example. Existing AMI settings are required.\n')
    print('AMI and updater settings initialized. Database and other application settings were retained.')


if __name__ == '__main__':
    main()
