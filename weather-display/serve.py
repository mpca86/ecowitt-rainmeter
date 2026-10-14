"""Run the demo locally and open it in the default browser."""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import webbrowser
from urllib.parse import urlsplit, unquote

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--port', type=int, default=8080)
parser.add_argument('--no-browser', action='store_true')
parser.add_argument('--live', action='store_true', help='Read Ecowitt cloud using private local config')
args = parser.parse_args()
cloud = None
if args.live:
    try:
        from cloud import Cloud
        cloud = Cloud()
    except Exception:
        parser.exit(1, 'Cloud configuration unavailable or invalid. Run configure-cloud.cmd.\n')
if not 1 <= args.port <= 65535:
    parser.error('Port must be between 1 and 65535.')
class DisplayHandler(SimpleHTTPRequestHandler):
    # Only UI assets may be served; configuration, source helpers and data stay private.
    public = {'/', '/index.html', '/display.js', '/display.css', '/model.mjs', '/live.js'}

    def allowed(self):
        return unquote(urlsplit(self.path).path) in self.public

    def do_GET(self):
        if urlsplit(self.path).path == '/api/weather' and cloud:
            import json
            body = json.dumps(cloud.snapshot(), ensure_ascii=False).encode('utf-8')
            self.send_response(200)
            self.send_header('Content-Type','application/json; charset=utf-8')
            self.send_header('Cache-Control','no-store')
            self.send_header('Content-Length',str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        if not self.allowed():
            self.send_error(404)
            return
        super().do_GET()

    def do_HEAD(self):
        if not self.allowed():
            self.send_error(404)
            return
        super().do_HEAD()

    def log_message(self, format, *args):
        # Avoid logging arbitrary URL query strings.
        pass

handler = partial(DisplayHandler, directory=str(Path(__file__).resolve().parent))
try:
    server = ThreadingHTTPServer(('127.0.0.1', args.port), handler)
except OSError as error:
    parser.exit(1, f'Cannot start local server: {error}\nTry --port 8081.\n')
url = f'http://127.0.0.1:{args.port}/' + ('?mode=live' if args.live else '')
print(f'Ecowitt Weather Display DEMO: {url}\nKeep this window open. Ctrl+C stops the server.', flush=True)
if not args.no_browser:
    webbrowser.open(url)
try:
    server.serve_forever()
except KeyboardInterrupt:
    print('\nServer stopped.')
finally:
    server.server_close()
    if cloud: cloud.stop.set()
