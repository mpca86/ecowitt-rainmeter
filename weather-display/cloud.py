"""Server-side Ecowitt real-time adapter; credentials never leave this module."""
import hashlib
import json
import math
import os
from pathlib import Path
import sqlite3
import threading
import time
from urllib.parse import urlencode
from urllib.request import urlopen

BASE = Path(os.environ.get('LOCALAPPDATA', str(Path.home() / '.config'))) / 'EcowittWeatherDisplay'

def number(item, kind):
    if not isinstance(item, dict): return None
    try: value = float(item['value'])
    except (KeyError, ValueError, TypeError): return None
    if not math.isfinite(value): return None
    unit = str(item.get('unit', '')).lower().replace(' ', '').replace('º', '°')
    if kind == 'temp':
        if unit in ('°f', 'f'): return (value-32)*5/9
        if unit in ('°c', 'c'): return value
    elif kind == 'wind':
        if unit == 'm/s': return value*3.6
        if unit in ('mph', 'mi/h'): return value*1.609344
        if unit in ('km/h', 'kph'): return value
    elif kind == 'pressure':
        if unit == 'inhg': return value*33.8638866667
        if unit in ('hpa', 'mb'): return value
    elif kind == 'rain':
        if unit in ('in', 'inch'): return value*25.4
        if unit == 'mm': return value
    elif kind == 'rate':
        if unit in ('in/hr', 'in/h'): return value*25.4
        if unit in ('mm/hr', 'mm/h'): return value
    elif kind == 'humidity' and unit == '%': return value if 0 <= value <= 100 else None
    elif kind == 'direction' and unit in ('°', 'deg', ''): return value % 360
    elif kind == 'uv' and unit in ('', 'index'): return value if value >= 0 else None
    elif kind == 'solar' and unit in ('w/m²', 'w/m2'): return value if value >= 0 else None
    return None

def normalize(payload, now):
    if str(payload.get('code')) != '0' or not isinstance(payload.get('data'), dict):
        raise ValueError('Invalid API response')
    data = payload['data']
    def item(group, field): return data.get(group, {}).get(field, {})
    temp = item('outdoor', 'temperature')
    ts = float(temp.get('time', 0))
    if not math.isfinite(ts) or ts <= 0 or ts > now+120: raise ValueError('Invalid measurement time')
    fields = {
        'temp': ('outdoor','temperature','temp'), 'feelslike': ('outdoor','feels_like','temp'),
        'humidity': ('outdoor','humidity','humidity'), 'wind': ('wind','wind_speed','wind'),
        'gust': ('wind','wind_gust','wind'), 'direction': ('wind','wind_direction','direction'),
        'rain': ('rainfall','daily','rain'), 'rate': ('rainfall','rain_rate','rate'),
        'hourrain': ('rainfall','hourly','rain'), 'pressure': ('pressure','relative','pressure'),
        'uv': ('solar_and_uvi','uvi','uv'), 'solar': ('solar_and_uvi','solar','solar'),
    }
    values = {name: number(item(group,field),kind) for name,(group,field,kind) in fields.items()}
    if values['temp'] is None: raise ValueError('Outdoor temperature missing or unsupported unit')
    # Reject secondary measurements with missing, future or old timestamps.
    for name,(group,field,kind) in fields.items():
        if name == 'temp': continue
        try: field_ts = float(item(group,field).get('time',0))
        except (TypeError,ValueError): field_ts = 0
        if not math.isfinite(field_ts) or field_ts <= 0 or abs(field_ts-ts)>180: values[name] = None
    return {'ts': int(ts*1000), **values}

class Cloud:
    def __init__(self):
        self.config = json.loads((BASE/'cloud.json').read_text(encoding='utf-8'))
        for key in ('application_key','api_key','mac'):
            if not self.config.get(key): raise ValueError('Configuration incomplete')
        self.lock = threading.Lock()
        self.error = None
        self.stop = threading.Event()
        station_id = hashlib.sha256(self.config['mac'].upper().encode()).hexdigest()[:16]
        self.db = BASE/f'history-{station_id}.sqlite3'
        with sqlite3.connect(self.db) as db:
            db.execute('CREATE TABLE IF NOT EXISTS samples (ts INTEGER PRIMARY KEY, value TEXT NOT NULL)')
        self.thread = threading.Thread(target=self.run, daemon=True)
        self.thread.start()

    def run(self):
        while not self.stop.is_set():
            try:
                query = urlencode({k:self.config[k] for k in ('application_key','api_key','mac')} | {'call_back':'outdoor,wind,rainfall,pressure,solar_and_uvi'})
                with urlopen('https://api.ecowitt.net/api/v3/device/real_time?'+query, timeout=15) as r:
                    payload = json.loads(r.read(2_000_000))
                sample = normalize(payload, time.time())
                with sqlite3.connect(self.db) as db:
                    db.execute('INSERT OR IGNORE INTO samples VALUES (?,?)',(sample['ts'],json.dumps(sample)))
                    db.execute('DELETE FROM samples WHERE ts < ?', (int((time.time()-7*86400)*1000),))
                with self.lock: self.error = None
            except Exception:
                # Never expose exception text: urllib errors can contain credential-bearing URLs.
                with self.lock: self.error = 'Cloud API nedostupné alebo odpoveď neplatná'
            self.stop.wait(60)

    def snapshot(self):
        now = int(time.time()*1000)
        with sqlite3.connect(self.db) as db:
            rows = db.execute('SELECT value FROM samples WHERE ts >= ? ORDER BY ts',(now-48*3600000,)).fetchall()
        samples = [json.loads(row[0]) for row in rows]
        with self.lock: error = self.error
        return {'mode':'live','station':self.config.get('station_name','Ecowitt'),
                'latest':samples[-1] if samples else None, 'history':samples, 'error':error}
