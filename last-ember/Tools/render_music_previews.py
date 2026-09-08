"""Render the real C# mix policy into listening copies and validate encoded stems."""
from pathlib import Path
import json
import subprocess
import numpy as np
from compose_music import SR, ROOT, OUT, PREVIEWS, encode


def decode(path, seconds=None):
    command = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-i", str(path)]
    if seconds:
        command += ["-t", str(seconds)]
    command += ["-f", "f32le", "-ar", str(SR), "-ac", "2", "pipe:1"]
    return np.frombuffer(subprocess.check_output(command), dtype="<f4").reshape(-1, 2)


def main():
    score = json.loads((OUT / "score.json").read_text())
    plan = json.loads((PREVIEWS / "mix-presets.json").read_text())
    results = []
    for preset in plan["presets"]:
        n = int(preset["seconds"] * SR)
        mix = np.zeros((n, 2), np.float32)
        controls = np.array(preset["frames"], np.float32)
        control_t = np.arange(len(controls)) / plan["controlRate"]
        sample_t = np.arange(n) / SR
        for layer, resource in score["cues"][preset["cue"]]["stems"].items():
            layer_index = plan["layers"].index(layer)
            if controls[:, layer_index].max() == 0:
                continue
            audio = decode(ROOT / resource.removeprefix("res://"), preset["seconds"])
            if len(audio) != n:
                raise ValueError(f"Length mismatch: {resource}: {len(audio)} != {n}")
            gain = np.interp(sample_t, control_t, controls[:, layer_index]).astype(np.float32)
            mix += audio * gain[:, None]
        peak = float(np.abs(mix).max())
        if peak >= .98:
            raise ValueError(f"Clipping in {preset['name']}: {peak}")
        # Fade listening copies at the end; the interactive source stems themselves loop.
        fade = int(1.2 * SR)
        mix[-fade:] *= np.linspace(1, 0, fade, dtype=np.float32)[:, None]
        path = PREVIEWS / (preset["name"] + ".ogg")
        encode(mix, path)
        probe = subprocess.run(["ffmpeg", "-hide_banner", "-i", str(path), "-af", "ebur128=peak=true", "-f", "null", "-"], capture_output=True, text=True)
        summary = probe.stderr[probe.stderr.rfind("Summary:"):]
        results.append({"file": path.name, "seconds": preset["seconds"], "peak": peak, "rms": float(np.sqrt(np.mean(mix**2))), "loudness": summary})
        print(f"{path.name}: {preset['seconds']}s peak={peak:.3f}\n{summary}", flush=True)
    # Validate every Ogg after compression, including muted layers.
    stems = []
    for cue, data in score["cues"].items():
        for layer, resource in data["stems"].items():
            audio = decode(ROOT / resource.removeprefix("res://"))
            if len(audio) != int(data["seconds"] * SR):
                raise ValueError("Stem sample count mismatch: " + resource)
            peak = float(np.abs(audio).max())
            step = float(np.abs(audio[0] - audio[-1]).max())
            if not np.isfinite(audio).all() or peak >= .98 or step > .015:
                raise ValueError(f"Unsafe audio or loop click: {resource}, peak={peak}, seam={step}")
            stems.append({"cue": cue, "layer": layer, "frames": len(audio), "peak": peak, "loopStep": step})
    (PREVIEWS / "audio-validation.json").write_text(json.dumps({"status": "PASS", "previews": results, "stems": stems}, indent=2), encoding="utf-8")
    print(f"PASS: {len(stems)} equal-length, finite, unclipped, seamless encoded stems.", flush=True)


if __name__ == "__main__":
    main()
