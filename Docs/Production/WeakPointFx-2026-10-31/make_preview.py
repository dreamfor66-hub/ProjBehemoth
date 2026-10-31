"""Assemble actual Unity captures; never synthesize intermediate animation frames."""
import csv
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parent
captures = root / "Screenshots"
rows = list(csv.DictReader((root / "CaptureTimes.csv").open(encoding="utf-8")))
sequence = [(r["frame"], float(r["unscaledSeconds"])) for r in rows if r["frame"].startswith("BurstSequence-")]
crop = (650, 240, 1270, 800)

def frame(name):
    with Image.open(captures / (name + ".png")) as source:
        return source.convert("RGB").crop(crop)

frames = [frame("Final-Target")] + [frame(name) for name, _ in sequence]
durations = [700]
for index, (_, time) in enumerate(sequence):
    durations.append(max(10, round((sequence[index + 1][1] - time) * 1000 / 10) * 10) if index + 1 < len(sequence) else 500)
frames[0].save(root / "WeakPointFx-Live.gif", save_all=True, append_images=frames[1:], duration=durations, loop=0, disposal=2)
print(f"Saved {len(sequence)} real burst frames; interval {sequence[-1][1] - sequence[0][1]:.3f}s")
