# Пепельные паломники / первый набор

Выбранная пользователем фракция: рыцарь, охотник, жрец. Общее направление — закрытые тёмные маски, обугленный металл, пепельная ткань и сдержанные угольные акценты. Широкий рыцарь, компактный арбалетчик и высокий жрец различаются силуэтом. Яркое живое пламя остаётся главным акцентом героя.

## Игровые роли

| Враг | Базовые HP / урон | Подготовка | Атака и ответ игрока |
| --- | --- | --- | --- |
| Рыцарь | 120 / 14 | 0.65 с | Зафиксированный сектор 120°, радиус 106. Обойти или прервать; восстановление 0.4 с, cooldown 1.55 с. |
| Охотник | 65 / 10 | 0.85 с | Фиксирует направление, выпускает снаряд со скоростью 360; держит около 260 px. Уклонение поперёк линии; cooldown 1.9 с. |
| Жрец | 80 / 12 | 1.15 с | Фиксированная область радиусом 64, проверка видимости перед уроном. Выйти из круга; cooldown 2.6 с. |

Cooldown отсчитывается с момента выпуска атаки и включает восстановление. Множитель урона подземелья применяется существующей системой. Оглушение отменяет подготовку, очищает разметку и завершает визуальное сопровождение уже выпущенного удара; выпущенные снаряды сохраняются. Завершение подготовки открывает стандартную слабую точку.

В обычных волнах Darkness первый слот занимает рыцарь, в охране тайника — охотник, в сопровождении элиты — жрец. В волнах от пяти противников добавляются охотник и жрец. Стаи, боссы, Frost и Inferno сохраняют прежний состав. Три открытые карточки с портретами добавлены в коллекцию.

## Ассеты и анимация

- `last-ember/Assets/Enemies/ash-knight-atlas-v1.png`
- `last-ember/Assets/Enemies/ash-hunter-atlas-v1.png`
- `last-ember/Assets/Enemies/ash-priest-atlas-v1.png`
- `art/humanoid-directions-v1.png` — первоначальное сравнение направлений.
- `art/ash-pilgrims-60fps.webp` — изолированное превью; верхний ряд 2.4×, нижний в игровом масштабе.

Созданы встроенным imagegen (builtin mode). Каждый прозрачный атлас 1536×1024 содержит сетку 3×2: idle, два шага, подготовка, выпуск атаки, получение урона. Это шесть опорных поз, а не 60 уникальных рисованных кадров. Шейдер смешивает позы с корректной альфой; дыхание, колебание ткани, наклон и отдача рассчитываются каждый кадр. При смерти поза получения урона наклоняется, сжимается и исчезает за 0.48 с. Поворот влево зеркальный; отдельных боковых и задних ракурсов пока нет.

У idle задана безопасная нижняя граница выборки, чтобы убрать части поднятого оружия из следующего ряда. Кадр подготовки захватывает дополнительный верхний край с поднятым оружием (11% у рыцаря, 2.5% у жреца). Для кадра выпуска рыцаря и жреца выборка шире на 12%, сохраняя край оружия. Портреты используют тот же безопасный crop idle.

## Проверка

C# build, импорт ассетов, изолированный рендер production-компонента PilgrimVisual и Windows export. Превью не создаёт Enemy, RunManager или боевую сцену. Игра и автоматические геймплейные тесты не запускались; баланс, навигацию в бою и читаемость при реальном освещении нужно проверить вручную.

Команда изолированного рендера после сборки C#: Godot с `--path last-ember --rendering-method gl_compatibility --script res://Tools/render_pilgrim_preview.gd`. Требуется графический рендерер, без `--headless`. Кадры сохраняются в `.tools/pilgrim-preview`.

## Точные промпты игровых атласов

### knight

Production transparent sprite animation atlas for a premium 2D top-down dark fantasy roguelite. Ash Pilgrim knight: broad armored humanoid with charred obsidian plate, closed cracked angular mask, ash-gray ragged tabard, heavy short sword in right hand and small broken shield in left. Tiny dim ember cracks, dark steel with clear pale ash edge highlights. Painterly stylized materials, clear value groups, strong silhouette, not photorealistic. Exactly SIX equal-sized cells in a perfectly regular 3 COLUMNS by 2 ROWS grid. Same single character identity, scale, camera and feet pivot in EVERY cell. Elevated three-quarter camera from above, character faces screen DOWN and slightly RIGHT. Entire character and weapon visible in every cell, generous 12% transparent padding, no parts crossing cell borders. Feet planted at local (50%,83%) in every cell. Frames left-to-right row-major: 1 relaxed guarded idle; 2 walking left foot forward; 3 walking right foot forward; 4 obvious attack preparation raising weapon; 5 committed attack follow-through with weapon forward; 6 recoiling from hit with bent torso. Small coherent pose changes, no extra VFX slash, no ground or painted shadows. No text, labels, grid, scenery, UI, gore, eyes glowing like hero. Actual transparent alpha background.

### hunter

Production transparent sprite animation atlas for a premium 2D top-down dark fantasy roguelite. Ash Pilgrim hunter: lean hooded humanoid in layered ash-gray leather and ragged short coat, closed narrow obsidian face mask, a compact heavy crossbow gripped with BOTH hands. Slim silhouette, quiver at belt, small rusty-red cloth accent, no bright fire. Hand-painted stylized detailed materials, clear pale ash edge highlights so body reads against darkness, consistent with charred iron knight enemies. Exactly SIX equal square cells, perfectly regular 3 COLUMNS by 2 ROWS. Same character identity, costume, scale and elevated three-quarter camera in EVERY cell, faces DOWN and slightly RIGHT. Feet anchored at local (50%,83%) in each cell. Full body and crossbow visible, generous transparent padding, nothing crosses cells. Frames row major: 1 guarded idle with crossbow lowered; 2 walk left foot forward; 3 walk right foot forward; 4 aiming crossbow precisely from shoulder; 5 firing with subtle recoil; 6 recoiling from a hit. No muzzle flash, no projectile, no text or grid, no environment. STRICT actual alpha transparency, no painted floor, no glow cloud, no background illumination, no gradients, no cast shadow behind character.

### priest

Production transparent sprite animation atlas for premium 2D top-down dark fantasy roguelite. Ash Pilgrim priest: tall lean humanoid in layered tattered ash-gray robes over black cloth, closed long angular obsidian mask, crooked iron ritual staff in one hand and small bronze ash censer in the other. Dim muted burgundy ritual glow in censer only, no bright fire head. Hand-painted stylized detailed materials, pale ash highlights and readable dark silhouette, belongs to same faction as charred knight and hooded crossbow hunter. Exactly SIX equal square cells, regular 3 COLUMNS by 2 ROWS. Same character identity, scale, elevated three-quarter camera, faces DOWN and slightly RIGHT. Feet anchored at local (50%,83%) in every cell, full body and staff/censer fit inside each cell with transparent padding. Frame order row major: 1 stooped idle staff planted; 2 walking left foot forward; 3 walking right foot forward; 4 casting preparation raising censer; 5 spell release reaching staff forward; 6 recoil from hit torso bent. No spell effect, no magic circles, no outward particles, no bright halo. STRICT actual alpha transparency, no background gradients, no scenery, no ground or cast shadow, no text, no labels, no grid.
