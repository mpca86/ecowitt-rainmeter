"""Create or validate private local cloud configuration. Does not call Ecowitt."""
import argparse
import getpass
import json
import os
from pathlib import Path
import re

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--check', action='store_true', help='Validate without displaying credentials')
args = parser.parse_args()
base = Path(os.environ.get('LOCALAPPDATA', str(Path.home() / '.config')))
path = base / 'EcowittWeatherDisplay' / 'cloud.json'

def validate(config):
    for key in ('application_key', 'api_key'):
        if not isinstance(config.get(key), str) or not config[key].strip():
            raise ValueError(f'{key} is required')
    if not re.fullmatch(r'(?:[0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}', config.get('mac', '')):
        raise ValueError('MAC must have format AA:BB:CC:DD:EE:FF')
    if config.get('timezone') != 'Europe/Bratislava':
        raise ValueError('Expected timezone Europe/Bratislava')
    if config.get('poll_seconds') != 60:
        raise ValueError('Expected polling interval 60 seconds')

try:
    if args.check:
        validate(json.loads(path.read_text(encoding='utf-8')))
        print('Configuration valid. Credentials were not displayed. No API request was made.')
    else:
        if path.exists():
            if input('Configuration exists. Replace it? [y/N]: ').strip().lower() != 'y':
                raise SystemExit('Existing configuration retained.')
        print('Enter credentials locally. Keys are hidden while typing.')
        config = {
            'application_key': getpass.getpass('Application Key: ').strip(),
            'api_key': getpass.getpass('API Key: ').strip(),
            'mac': input('Gateway MAC (AA:BB:CC:DD:EE:FF): ').strip().upper(),
            'station_name': input('Station name [Poprad · Veľká]: ').strip() or 'Poprad · Veľká',
            'timezone': 'Europe/Bratislava',
            'poll_seconds': 60,
        }
        validate(config)
        path.parent.mkdir(parents=True, exist_ok=True)
        temp = path.with_suffix('.tmp')
        fd = os.open(temp, os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
        with os.fdopen(fd, 'w', encoding='utf-8') as f:
            json.dump(config, f, ensure_ascii=False, indent=2)
        os.replace(temp, path)
        print(f'Saved: {path}')
        print('This prepares configuration only. Display remains in DEMO mode.')
except (OSError, ValueError, TypeError) as error:
    # Do not print exception contents: malformed input could contain credentials.
    parser.exit(1, 'Configuration missing, unreadable or invalid. Run setup again.\n')
