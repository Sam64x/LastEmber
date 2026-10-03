# Ash Knight v3 — исправление силуэта

Предыдущая сборка v2 была отклонена пользователем. Раздельные голова, торс, пояс и фрагменты конечностей давали несогласованные пропорции; слишком согнутые руки поднимали локти, а изменение ширины корпуса усиливало ощущение бумажной куклы.

## Что заменено

- Новая связная основа: шлем, шея, торс и пояс нарисованы вместе. Масштаб фиксирован, сжатие корпуса по горизонтали убрано.
- Цельные слои рук и ног вместо отдельных фрагментов брони. Polygon2D деформирует их через плавный переход весов около локтя/колена; изображение в суставе непрерывно.
- Уточнены крепления плеч, опущены кисти в покое, расширена стойка. Дальность руки ограничена; колени имеют небольшой изгиб, чтобы не разъезжаться в стороны.
- При ударе корпус переносит вес, меч проходит перед щитом, затем возвращается в стойку. Тайминги боя прежние: подготовка 0.65 с, восстановление 0.48 с.
- Дыхание, ходьба по скорости, звук постановки стоп, ткань, щит, реакция на попадание и исчезновение после смерти сохранены.

Это двухракурсный 2D-риг. Спереди и сзади отдельный корпус; конечности пока общие. Полного бокового ракурса и корректной смены перспективы всех деталей нет. Ходьба использует ограниченный стилизованный шаг, не точную фиксацию стоп в мировых координатах. Перспектива и постановка ещё требуют художественной доработки.

## Ассеты и просмотр

- Новый атлас: `last-ember/Assets/Enemies/ash-knight-layers-v3.png`, 1536×1024 RGBA, 9 частей.
- Исходный дизайн: `last-ember/Assets/Enemies/ash-knight-atlas-v1.png`.
- Превью: `art/knight-v3-60fps.webp`, 8 секунд, 60 fps. Слева исходный дизайн, в центре v3, справа v3 в игровом масштабе.
- Код: `last-ember/Scripts/Enemies/KnightRigVisual.cs`.

Атлас создан встроенным imagegen (builtin mode) с исходным рыцарем как референсом. Альфа и изображение сохранены без ретуши; области и суставы размечены в коде. Старые ассеты не перезаписаны.

Проверены C# build, импорт, изолированный рендер production-компонента через PilgrimVisual, Windows export. Рендер охватывает idle, ходьбу, замах, удар, поворот, попадание и удаление после смерти. Игра и геймплейные тесты не запускались.

Изолированный рендер: Godot с `--path last-ember --rendering-method gl_compatibility --script res://Tools/render_knight_rig_preview.gd`. Кадры сохраняются в `.tools/knight-v3-preview`. Сцена не создаёт RunManager или Enemy.

## Точный промпт imagegen

Production animation layers for the EXACT original Ash Knight in the reference. Preserve original grounded realistic heroic anatomy, short neck buried between broad shoulders, angular battered charcoal steel helmet and plate armor, pale ash ragged tabard. No redesign, no tiny head, no elongated torso, no exaggerated joints, no glowing lava. Create a precisely aligned 3 COLUMNS x 3 ROWS atlas of NINE isolated pieces on true transparent alpha. Each cell has generous transparent padding, no parts crossing borders. Elevated three-quarter top-down game camera, front pieces face slightly toward screen right. Cohesive painterly dark fantasy style, broad readable value groups. ROW1: (1) connected head + neck + armored torso + belt + short hip plates, one continuous cohesive central body, NO arms or shoulder pauldrons, NO legs, NO long hanging tabard; (2) the exact same connected central body from BACK, same scale and proportion, NO arms or legs; (3) COMPLETE sword arm from shoulder pauldron through elbow to closed gauntlet, natural almost straight relaxed hang, hand below shoulder, NO sword. ROW2: (4) COMPLETE shield arm from shoulder pauldron through elbow to gauntlet, natural almost straight relaxed hang, NO shield; (5) complete screen-left armored LEG from covered hip joint through knee to boot, natural almost straight standing leg; (6) complete screen-right armored LEG from covered hip joint through knee to boot. ROW3: (7) straight battered steel sword vertical hilt TOP tip DOWN, no hand; (8) broken battered small kite shield top UP; (9) single long narrow ragged pale-gray tabard cloth strip, attachment top, torn ends bottom. Keep complete rounded hidden limb roots behind shoulder/hip overlaps; do NOT show open hollow sockets, severed tissue or cut cross sections. The limbs are continuous painted pieces, do NOT split at elbows or knees. Actual transparent background. No text, grids, scenery, cast shadows, labels, glow or gradients.
