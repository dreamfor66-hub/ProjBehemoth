"""Loopback-only static art review server with restricted roots and file types."""
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import unquote, urlsplit

PROJECT = Path(__file__).resolve().parents[3]
ROOTS = [PROJECT / p for p in ("Docs/Production", "Assets/Art/World/Angular/Heroes", "Assets/Art/Trials/Angular01", "Assets/Art/Concepts", "Assets/Art/Characters/Selection", "Assets/Art/References")]
EXTENSIONS = {".html", ".png", ".jpg", ".jpeg", ".webp", ".json", ".md", ".txt", ".css", ".js"}

class ReviewHandler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(PROJECT), **kwargs)

    def send_head(self):
        path = (PROJECT / unquote(urlsplit(self.path).path).lstrip("/")).resolve()
        if path.suffix.lower() not in EXTENSIONS or not any(path.is_relative_to(root) for root in ROOTS) or not path.is_file():
            self.send_error(404)
            return None
        return super().send_head()

if __name__ == "__main__":
    ThreadingHTTPServer(("127.0.0.1", 8319), ReviewHandler).serve_forever()
