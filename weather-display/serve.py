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
args = parser.parse_args()
if not 1 <= args.port <= 65535:
    parser.error('Port must be between 1 and 65535.')
class DisplayHandler(SimpleHTTPRequestHandler):
    # Only UI assets may be served; configuration, source helpers and data stay private.
    public = {'/', '/index.html', '/display.js', '/display.css', '/model.mjs'}

    def allowed(self):
        return unquote(urlsplit(self.path).path) in self.public

    def do_GET(self):
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
url = f'http://127.0.0.1:{args.port}/'
print(f'Ecowitt Weather Display DEMO: {url}\nKeep this window open. Ctrl+C stops the server.', flush=True)
if not args.no_browser:
    webbrowser.open(url)
try:
    server.serve_forever()
except KeyboardInterrupt:
    print('\nServer stopped.')
finally:
    server.server_close()
