"""Offline asset contact sheet; does not start Godot or gameplay."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

project = Path(__file__).resolve().parents[1]
atlas = Image.open(project / 'Assets/Characters/ember-spirit-atlas.png').convert('RGBA')
assert atlas.size == (1536, 1024), 'Unexpected atlas grid'
assert atlas.getchannel('A').getextrema()[0] == 0, 'Atlas needs transparency'
preview = Image.new('RGBA', (1200, 480), '#11151c')
draw = ImageDraw.Draw(preview)
font_path = Path('C:/Windows/Fonts/segoeui.ttf')
font = ImageFont.truetype(str(font_path), 17) if font_path.exists() else ImageFont.load_default()
draw.text((28, 18), 'LAST EMBER / CHARACTER ART / DIRECTIONAL POSES', font=font, fill='#e6d8bc')
labels = ['WARM / FRONT', 'WARM / 3/4', 'WARM / BACK',
          'BLUE / FRONT', 'BLUE / 3/4', 'BLUE / BACK']
for frame, label in enumerate(labels):
    x, y = frame % 3 * 512, frame // 3 * 512
    sprite = atlas.crop((x, y, x + 512, y + 512))
    left = 24 + frame * 194
    draw.text((left, 65), label, font=font, fill='#97a2b0')
    preview.alpha_composite(sprite.resize((184, 184), Image.Resampling.LANCZOS), (left, 100))
    preview.alpha_composite(sprite.resize((76, 76), Image.Resampling.LANCZOS), (left + 54, 330))
draw.text((28, 440), 'BOTTOM ROW: 76px production cell size. Offline asset preview; not a gameplay capture.', font=font, fill='#97a2b0')
output = project.parent / 'art/ember-spirit-preview.png'
output.parent.mkdir(parents=True, exist_ok=True)
preview.convert('RGB').save(output)
print(output)
