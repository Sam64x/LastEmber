# Ash Knight — articulated rig v2

Историческая версия, отклонённая пользователем. В игре заменена на [v3](KNIGHT_V3.md); описание ниже относится к прежней реализации.

## Изменение

Рыцарь использует раздельные детали вместо смешивания шести цельных изображений. Охотник и жрец продолжают использовать прежние атласы. Портрет рыцаря в коллекции сохраняет прежнюю иллюстрацию.

`KnightRigVisual` — аналитический 2D-риг: положения суставов вычисляются непрерывно, локти и колени решаются двухзвенной IK, жёсткие пластины привязаны к реальным центрам суставов нарисованных деталей. Это собственный код рига на Node2D, а не импорт из Spine или ресурс Skeleton2D. Материал сохраняет обычное освещение сцены.

- Idle: дыхание корпуса, небольшое движение ткани.
- Ходьба: фаза зависит от пройденного расстояния (64 единицы на цикл), опорная часть занимает 62% шага, перенос — 38%. Во время опоры локальная стопа компенсирует скорость тела. Ограничение длины ноги предотвращает растяжение; на предельных шагах и при разворотах фиксация стопы приблизительная.
- Вес: смещение таза и встречный поворот корпуса; звук шага вызывается при переходе стопы в опору.
- Удар: подготовка остаётся 0.65 с; первая часть поднимает меч, последние 20% подготовки проводят его к контакту. В момент завершения подготовки остаётся существующая проверка попадания. Затем меч проходит доведение и возвращается в стойку за 0.48 с; движение AI заблокировано на этот период.
- Щит отстаёт от корпуса, ткань деформируется полосами текстуры и пружиной с ограниченным шагом интеграции.
- Попадание: импульс корпуса в направлении от источника, краткая вспышка материала. Оглушение отменяет подготовку и сопровождение удара.
- Смерть: опускание корпуса и исчезновение; длительность около 0.7 с.
- Направление: сглаженный угол, зеркальная левая сторона, отдельные изображения головы и торса со спины, изменение ширины корпуса и порядка слоёв. Боковые направления пока используют проекционное сжатие — это не полный комплект восьми нарисованных ракурсов.

## Файлы и происхождение

Новый атлас: `last-ember/Assets/Enemies/ash-knight-parts-v2.png` (1254×1254, RGBA, 16 деталей). Создан встроенным imagegen, builtin mode, по прежнему `ash-knight-atlas-v1.png` как референсу персонажа. Исходник сохранён без обработки альфа-канала. Области и точки крепления заданы в риге вручную после визуальной проверки; скрытые области суставов дорисованы при генерации.

Превью: `art/knight-rig-v2-60fps.webp`. Слева прежние позы, в центре новый production-риг в увеличении 2.8×, справа игровой масштаб 1×. Эффекты удара специально отсутствуют, чтобы было видно само движение.

Изолированный рендер после C# build:

```text
Godot --path last-ember --rendering-method gl_compatibility --script res://Tools/render_knight_rig_preview.gd
```

Не использовать headless для рендера кадров. Скрипт создаёт только визуальные компоненты и SubViewport, не создаёт Enemy, RunManager или бой. Кадры: `.tools/knight-rig-preview`.

Проверка: C# build, импорт, изолированный рендер idle/ходьбы/замаха/удара/разворота/получения урона, Windows export. Игра и геймплейные тесты не запускались. Баланс, контакт со стенами и читаемость при реальном освещении остаются для ручной проверки. Это первая итерация рига, не финальная постановка всех анимаций.

## Точный промпт imagegen

Use case: stylized-concept. Production 2D cutout animation parts atlas, not a pose sheet. Reference image is identity/material reference ONLY: preserve this Ash Pilgrim knight's charred steel, closed pointed helmet, battered armor, pale tattered tabard. Create exactly 16 SEPARATED individual body/equipment parts in a precise 4 COLUMNS x 4 ROWS square grid on actual transparent alpha. Each part centered in its own equal cell, 15% empty padding on every edge, no touching adjacent cells. No whole characters. All frontal parts viewed from elevated three-quarter top-down game camera, slight turn to screen right. Neutral limbs point vertically DOWN, proximal joint at TOP; reconstruct hidden joint ends as complete rounded armor/chainmail so rig can rotate without holes. Objects can be different scales per cell for legibility. ROW 1 left-to-right: closed helmet front view with neck collar; armored chest torso only from neck to waist NO arms/head/legs; armored pelvis belt and short hip plates only; narrow long torn pale ash tabard cloth strip isolated. ROW 2: upper sword arm including shoulder pauldron and bicep NO forearm; upper shield arm including shoulder pauldron and bicep NO forearm; sword forearm with closed gauntlet at bottom NO weapon; shield forearm with closed gauntlet at bottom NO shield. ROW 3: left armored thigh from hip to knee; right armored thigh from hip to knee; left shin with attached boot at bottom; right shin with attached boot at bottom. ROW 4: long battered straight sword, hilt on TOP, point DOWN, no hand; broken small kite shield exterior, top UP; SAME helmet from BACK; SAME torso from BACK without arms/head/legs. Hand-painted dark fantasy detailed surfaces, coherent material and lighting, strong readable highlights. No lettering, labels, grid lines, environment, floor, cast shadows or glow clouds. Transparent background.
