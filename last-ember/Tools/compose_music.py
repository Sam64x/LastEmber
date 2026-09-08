"""Original Last Ember score. Deterministic additive/modal synthesis, no samples.

Requirements: Python 3 + numpy + ffmpeg (libvorbis). The game needs neither.
All stems within a cue have exactly the same 80 BPM, bar grid and sample count.
Events and reverb wrap at the loop boundary; no runtime synthesis or DSP cost.
"""
from pathlib import Path
import argparse
import json
import math
import subprocess
import numpy as np

SR = 32000
BPM = 80
BEAT = 60 / BPM
BAR = BEAT * 4
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Audio" / "Music"
PREVIEWS = ROOT / "TestResults" / "Audio"
TAU = 2 * np.pi
MOTIF = [57, 60, 64, 62]  # A3 C4 E4 D4. The same four-note identity in every cue.
CUES = {
    "menu": (32, "The Last Ember", ["ambient", "melody", "fire"]),
    "streets": (64, "Dead Streets / Feed the Fire", ["ambient", "melody", "strings", "percussion", "bass", "tension", "heartbeat", "breath", "fire", "fragments"]),
    "offering": (40, "The Offering / Bearer of the Torch", ["ambient", "melody", "choir", "strings", "percussion", "heartbeat", "breath", "fire", "fragments"]),
    "boss": (96, "The Extinguisher / One Spark Left", ["ambient", "melody", "strings", "percussion", "bass", "choir", "tension", "heartbeat", "breath", "fire", "fragments", "hope"]),
    "victory": (16, "The Furnace Breathes", ["ambient", "melody", "strings", "choir", "fire"]),
}
NOISE_CACHE = {}


def hz(midi):
    return 440 * 2 ** ((midi - 69) / 12)


def time_axis(seconds):
    return np.arange(int(round(seconds * SR)), dtype=np.float32) / SR


def shaped_noise(seconds, low, high, seed):
    """Periodic, band-limited noise synthesized in the frequency domain."""
    key = (seconds, low, high, seed)
    if key in NOISE_CACHE:
        return NOISE_CACHE[key]
    n = int(round(seconds * SR))
    rng = np.random.default_rng(seed)
    frequencies = np.fft.rfftfreq(n, 1 / SR)
    envelope = np.zeros_like(frequencies)
    active = (frequencies > low) & (frequencies < high)
    fraction = (frequencies[active] - low) / (high - low)
    envelope[active] = np.sin(np.pi * fraction) ** 2 / np.sqrt(np.maximum(frequencies[active], 1))
    spectrum = envelope * (rng.normal(size=len(frequencies)) + 1j * rng.normal(size=len(frequencies)))
    samples = np.fft.irfft(spectrum, n).astype(np.float32)
    samples /= max(float(np.std(samples)), 1e-5)
    if seconds == 24:
        NOISE_CACHE[key] = samples
    return samples


def note(midi, seconds, instrument="piano", seed=0):
    t = time_axis(seconds)
    f = hz(midi)
    sound = np.zeros(len(t), np.float32)
    if instrument == "piano":
        # Felt piano: fast hammer, soft inharmonic upper partials, long wooden decay.
        for partial in range(1, 8):
            detune = 1 + .00014 * partial * partial
            decay = np.exp(-t * (.53 + partial * .26))
            sound += np.sin(TAU * f * partial * detune * t) * decay / partial ** 1.65
        sound *= (1 - np.exp(-t * 180)) * .56
        sound += shaped_noise(seconds, 350, 2100, seed) * np.exp(-t * 95) * .018
    elif instrument in ("strings", "cello", "choir", "brass"):
        for partial in range(1, 9):
            weight = (1 / partial ** 1.8)
            if instrument == "choir":
                weight *= .6 + 1.1 * np.exp(-((f * partial - 620) / 240) ** 2)
            if instrument == "brass":
                weight = 1 / partial ** 1.35
            for detune in (-.0013, .0014):
                vibrato = .003 * np.sin(TAU * (4.5 + detune * 80) * t)
                sound += np.sin(TAU * f * partial * (1 + detune) * t + vibrato * partial) * weight * .22
        attack = .9 if instrument in ("cello", "strings") else 1.5
        envelope = np.minimum(t / attack, 1) * np.minimum((seconds - t) / 2.1, 1)
        envelope = np.maximum(envelope, 0)
        sound *= np.sin(envelope * np.pi / 2) ** 2
        sound *= .87 + .13 * np.sin(TAU * .14 * t)
    elif instrument == "bell":
        for multiple, volume in [(1, .5), (2.71, .23), (4.07, .11), (5.42, .04)]:
            sound += np.sin(TAU * f * multiple * t) * np.exp(-t * (.6 + multiple * .25)) * volume
        sound *= np.minimum(t / .007, 1)
    elif instrument == "tom":
        phase = TAU * (f * .55 * t + f * .09 * (1 - np.exp(-t * 12)))
        sound = np.sin(phase) * np.exp(-t * 6) * .65
        sound += shaped_noise(seconds, 60, 680, seed) * np.exp(-t * 30) * .05
        sound *= np.minimum(t / .002, 1)
    elif instrument == "pluck":
        for partial in range(1, 7):
            sound += np.sin(TAU * f * partial * t) * np.exp(-t * (1.1 + partial * .7)) / partial ** 1.6
        sound *= np.minimum(t / .009, 1) * .45
    elif instrument == "heart":
        sound = np.sin(TAU * (54 * t + 2 * (1 - np.exp(-t * 18)))) * np.exp(-t * 18)
        sound *= np.minimum(t / .012, 1) * .55
    sound *= np.minimum((seconds - t) / .08, 1)
    return sound.astype(np.float32)


class Stem:
    def __init__(self, bars, seed):
        self.length = int(round(bars * BAR * SR))
        self.seconds = bars * BAR
        self.audio = np.zeros((self.length, 2), dtype=np.float32)
        self.rng = np.random.default_rng(seed)

    def add(self, start, samples, gain=1, pan=0):
        # Equal-power panning; the tail of the final phrase belongs to the next loop.
        at = int(round(start * SR)) % self.length
        channels = [math.cos((pan + 1) * math.pi / 4), math.sin((pan + 1) * math.pi / 4)]
        consumed = 0
        while consumed < len(samples):
            take = min(len(samples) - consumed, self.length - at)
            for channel, value in enumerate(channels):
                self.audio[at:at + take, channel] += samples[consumed:consumed + take] * gain * value
            consumed += take
            at = 0

    def event(self, start, midi, seconds=4, instrument="piano", gain=.2, pan=0):
        self.add(start, note(midi, seconds, instrument, int(self.rng.integers(100000))), gain, pan)

    def reverb(self, wet=.35):
        # Sparse diffuse stereo room, circular so the loop tail never gets truncated.
        dry = self.audio.copy()
        for seconds, amount, swap in [(.137,.24,1),(.233,.21,0),(.419,.16,1),(.677,.13,0),(1.071,.09,1),(1.733,.07,0),(2.119,.035,1)]:
            self.audio += np.roll(dry[:, ::-1] if swap else dry, int(seconds * SR), axis=0) * wet * amount

    def finish(self):
        self.audio -= self.audio.mean(axis=0)
        # Catch the tiny remaining stochastic boundary discrepancy below audibility.
        count = int(.012 * SR)
        blend = np.linspace(0, 1, count, dtype=np.float32)[:, None]
        self.audio[-count:] = self.audio[-count:] * (1 - blend) + self.audio[0] * blend
        peak = float(np.max(np.abs(self.audio)))
        if peak > .65:
            self.audio *= .65 / peak
        return self.audio


def ambient(stem, cue):
    t = time_axis(stem.seconds)
    base = 45 if cue == "victory" else 33
    for midi, volume in [(base,.07),(base + 7,.024),(base + 12,.027)]:
        # Integer cycles over the complete cue: both waveform and modulation loop exactly.
        frequency = round(hz(midi) * stem.seconds) / stem.seconds
        wave = np.sin(TAU * frequency * t) * (.85 + .15 * np.sin(TAU * t / stem.seconds * 4))
        stem.audio[:, 0] += wave * volume
        stem.audio[:, 1] += np.sin(TAU * frequency * t + .05) * volume
    wind = np.tile(shaped_noise(24, 70, 780, 84), int(stem.seconds / 24))
    for channel in range(2):
        stem.audio[:, channel] += np.roll(wind, channel * 3400) * .014 * (.65 + .35 * np.sin(TAU * t / stem.seconds * 3 + channel))
    # An occasional furnace resonance, never a constant melody.
    for at in np.arange(14, stem.seconds, 24):
        stem.event(at, 33, 7, "bell", .055, -.3)


def melody(stem, cue):
    interval = 24 if cue == "streets" else 18 if cue == "menu" else 24 if cue == "offering" else 12
    instrument = "strings" if cue == "streets" else "pluck" if cue == "offering" else "brass" if cue == "boss" else "piano"
    for phrase, at in enumerate(np.arange(3, stem.seconds - 3, interval)):
        octave = -12 if cue in ("streets", "boss") else 0
        for index, midi in enumerate(MOTIF):
            stem.event(at + index * 1.5, midi + octave, 5.8, instrument, .22 if cue != "boss" else .27, -.22 + index * .13)
        if cue == "victory":
            # The previously unresolved D finally settles into a warm C-major cadence.
            for index, midi in enumerate([60, 64, 67, 72]):
                stem.event(at + 6 + index * .75, midi, 6, "piano", .18, .12)
        if cue == "menu" and phrase % 2 == 0:
            stem.event(at + 11.25, 45, 7, "piano", .10, -.4)


def strings(stem, cue, choir=False):
    progression = [[45,52,60],[41,48,57],[48,55,64],[43,50,59]]
    if cue == "boss":
        progression = [[33,40,48],[29,36,45],[36,43,52],[31,38,47]]
    if cue == "offering":
        progression = [[33,45,52],[38,45,53],[33,45,52],[40,47,55]]
    for index, at in enumerate(np.arange(0, stem.seconds, 12)):
        # Every fourth exploration chord is absent: leave space for footsteps and wind.
        if cue == "streets" and index % 4 in (1,3):
            continue
        chord = progression[index % len(progression)]
        for voice, midi in enumerate(chord):
            stem.event(at, midi + (12 if choir else 0), 13.5, "choir" if choir else "cello" if voice == 0 else "strings", .115 if cue != "boss" else .16, (voice - 1) * .45)


def percussion(stem, cue):
    for bar, at in enumerate(np.arange(0, stem.seconds, BAR)):
        if cue == "offering" and bar % 2:
            continue
        if cue == "streets" and bar % 8 in (5,6,7):
            continue
        stem.event(at, 38, 1, "tom", .27, -.12)
        if bar % 2 == 0:
            stem.event(at + BEAT * 2.5, 43, .9, "tom", .17, .25)
        if bar % 4 == 3:
            stem.event(at + BEAT * 3, 50, 2.4, "bell", .10, -.45)
        if cue == "boss" and bar % 4 != 2:
            stem.event(at + BEAT * 1.5, 42, .7, "tom", .19, .25)
            stem.event(at + BEAT * 3.5, 38, .7, "tom", .14, -.25)
    # Chain-like high modal rattles, with long gaps.
    for at in np.arange(8.25, stem.seconds, 12):
        for j in range(4):
            stem.event(at + j * .065, 79 + j, .27, "bell", .025, .5)


def bass(stem):
    for bar, at in enumerate(np.arange(0, stem.seconds, BAR)):
        root = [33,29,36,31][bar // 4 % 4]
        for step in [0, 1.5, 3]:
            stem.event(at + step * BEAT, root, 1.2, "pluck", .27, 0)


def fire(stem):
    rng = stem.rng
    for at in np.arange(.2, stem.seconds, .29):
        if rng.random() > .31:
            continue
        duration = float(rng.uniform(.035, .14))
        signal = shaped_noise(duration, 550, 5500, int(rng.integers(100000)))
        t = time_axis(duration)
        signal *= np.exp(-t * 65) * np.minimum(t / .0015, 1)
        stem.add(at + float(rng.uniform(0, .18)), signal, float(rng.uniform(.016,.055)), float(rng.uniform(-.6,.6)))


def tension(stem):
    for at in np.arange(7.5, stem.seconds, 15):
        signal = shaped_noise(3, 1700, 4800, int(stem.rng.integers(10000)))
        t = time_axis(3)
        signal *= np.sin(np.pi * t / 3) ** 3 * (.5 + .5 * np.sin(TAU * 11 * t))
        stem.add(at, signal, .023, float(stem.rng.uniform(-.8,.8)))
        stem.event(at + 2.25, 69, 5, "bell", .02, -.5)


def heartbeat(stem):
    for at in np.arange(0, stem.seconds, BEAT):
        stem.event(at, 33, .25, "heart", .27, 0)
        stem.event(at + .17, 33, .23, "heart", .15, .02)


def breath(stem):
    for at in np.arange(.75, stem.seconds, 6):
        signal = shaped_noise(2.4, 180, 1800, int(stem.rng.integers(10000)))
        t = time_axis(2.4)
        signal *= np.sin(np.pi * t / 2.4) ** 2
        stem.add(at, signal, .035, -.08)


def fragments(stem):
    for phrase, at in enumerate(np.arange(8.25, stem.seconds, 18)):
        # A / ... / C / silence: the complete identity is almost lost at low Flame.
        for offset, midi in [(0,57),(2.25,60 if phrase % 2 == 0 else 62)]:
            sample = note(midi, 5, "piano", phrase)
            t = time_axis(5)
            sample = np.tanh(sample * 2) * (.63 + .37 * np.sin(TAU * 2.9 * t))
            stem.add(at + offset, sample, .11, -.25)


def hope(stem):
    # Quarter-note entrances guarantee a return within 0.75 seconds of the HP trigger.
    for at in np.arange(0, stem.seconds, BAR):
        for index, midi in enumerate(MOTIF):
            stem.event(at + index * BEAT, midi, 3, "piano", .23, -.18 + .12 * index)


def encode(audio, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    command = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-f", "f32le", "-ar", str(SR), "-ac", "2", "-i", "pipe:0", "-c:a", "libvorbis", "-q:a", "4", str(path)]
    subprocess.run(command, input=audio.astype("<f4").tobytes(), check=True)


def compose(cue, layer, bars):
    seed = 7919 + sum((i + 1) * ord(char) for i, char in enumerate(cue + layer))
    stem = Stem(bars, seed)
    if layer == "ambient": ambient(stem, cue)
    elif layer == "melody": melody(stem, cue)
    elif layer == "strings": strings(stem, cue)
    elif layer == "choir": strings(stem, cue, choir=True)
    elif layer == "percussion": percussion(stem, cue)
    elif layer == "bass": bass(stem)
    elif layer == "fire": fire(stem)
    elif layer == "tension": tension(stem)
    elif layer == "heartbeat": heartbeat(stem)
    elif layer == "breath": breath(stem)
    elif layer == "fragments": fragments(stem)
    elif layer == "hope": hope(stem)
    if layer not in ("ambient", "heartbeat", "fire", "breath"):
        stem.reverb(.75 if layer in ("melody", "choir", "strings", "hope") else .22)
    return stem.finish()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", default="")
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    manifest_path = OUT / "score.json"
    manifest = json.loads(manifest_path.read_text()) if manifest_path.exists() else {"bpm": BPM, "sampleRate": SR, "motif": "A-C-E-D", "originalSynthesis": True, "cues": {}}
    analysis = []
    for cue, (bars, title, layers) in CUES.items():
        if args.only and cue != args.only:
            continue
        cue_data = {"title": title, "bars": bars, "seconds": bars * BAR, "stems": {}}
        for layer in layers:
            audio = compose(cue, layer, bars)
            path = OUT / f"{cue}_{layer}.ogg"
            encode(audio, path)
            cue_data["stems"][layer] = "res://" + path.relative_to(ROOT).as_posix()
            stats = {"cue": cue, "layer": layer, "frames": len(audio), "seconds": len(audio) / SR, "peak": float(np.abs(audio).max()), "rms": float(np.sqrt(np.mean(audio ** 2))), "loopStep": float(np.abs(audio[0] - audio[-1]).max()), "bytes": path.stat().st_size}
            analysis.append(stats)
            print(f"{cue:9} {layer:11} {stats['seconds']:5.0f}s  peak {stats['peak']:.3f}  {stats['bytes']//1024:5}KB", flush=True)
            del audio
        manifest["cues"][cue] = cue_data
        manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    (PREVIEWS / "stem-analysis.json").write_text(json.dumps(analysis, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
