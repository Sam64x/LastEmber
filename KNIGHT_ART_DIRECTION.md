# Рыцарь: смена художественного направления

Пользователь уточнил, что нужно изменить сам стиль, а не только исправить анатомию и анимацию. Реалистичная потёртая броня v1–v4 не является выбранным направлением. Следующие анимационные ассеты должны опираться на новый согласованный дизайн.

## Сравнение

`art/knight-style-directions-v5.png` — концептуальное сравнение трёх направлений. Источник визуального языка: игровой герой из `last-ember/Assets/Characters/ember-mask-atlas-v2.png`.

| Вариант | Основные признаки | Роль силуэта |
| --- | --- | --- |
| A — обсидиановый рыцарь | Угловатые крупные пластины, закрытая маска, короткая ткань, сдержанное свечение | Основной гуманоид ближнего боя |
| B — пепельный странник | Высокий узкий силуэт, капюшон и длинная ткань, тонкая броня | Более лёгкий и ритуальный образ |
| C — тяжёлый палач | Компактное массивное тело, крупные плечи, тяжёлое оружие | Медленная тяжёлая угроза |

Рекомендация для рыцаря — A. Выбор пользователю предложен; на момент подготовки документа окончательное направление не выбрано.

## Границы текущего результата

Это концепт-арт, не игровые спрайты и не production-ready модель. Камера на листе служит сравнению силуэтов; для выбранного направления требуется отдельный исходник в игровой перспективе. Изображение содержит общий фон и не предназначено для прямого рендеринга врага. Игровая модель этой итерацией не заменяется.

Дальнейший порядок: выбранный цельный персонаж в игровой камере → проверка рядом с героем в масштабе 90 px → согласованные ракурсы и экипировка → анимационный исходник → ходьба и один удар. До выбора направления не генерировать полный набор анимаций прежнего реалистичного рыцаря.

## Происхождение

Изображение создано встроенным imagegen (builtin mode). Единственный референс для нового сравнения — атлас героя; прежний реалистичный рыцарь не использован как стиль-референс. Сохранён исходный результат без ретуши.

## Точный промпт

ART DIRECTION COMPARISON BOARD for Last Ember humanoid knight enemies. Input is ONLY the visual language reference of our flame-mask hero: stylized sharp obsidian shapes, clean painterly/cel shading, strong silhouette, restrained simple surface detail. DO NOT copy the hero as a flame-headed humanoid. Make THREE clearly DISTINCT full-body humanoid ash knights arranged left-to-right on a plain dark charcoal studio backdrop, with small clear labels A, B, C at top. Same elevated three-quarter 2D gameplay camera looking down at 40-45 degrees, all facing lower-right in relaxed combat stance. Each carries a sword and small shield, feet anatomically planted. Designs should feel deliberately authored for a premium stylized dark-fantasy action game, NOT photorealistic medieval armor. Avoid scratch noise, realistic steel reflections, tiny rivets, busy texture, chibi proportions, skeleton/gore, glowing flame heads, disconnected limbs. Large readable color/value shapes with charcoal-black, muted gray-violet, ash cream and very small burnt-red accents, the hero keeps the brightest orange glow. A: OBSIDIAN WARDEN, medium-height broad imposing 5.5-head figure, large angular faceted black armor plates, short buried neck, pointed closed mask, asymmetrical broad pauldron, short torn ash tabard, heavy straight sword, geometric dark shield. Clean graphic hard planes, subtle edge highlights, strong triangular silhouette. B: ASH PENITENT, tall lean 6.5-head figure with long pointed hood/closed mask, flowing layered ash cloth over slim black armor, long legs and narrow shoulders, slightly curved long sword and narrow shield, elegant threatening vertical silhouette. More expressive hand-painted brush shapes, limited hard metal. C: CINDER EXECUTIONER, squat powerful 4.5-head adult humanoid, compact helmet sunk into massive blocky shoulders, oversized forearms and heavy boots, very broad cleaver sword, thick small shield, burnt cloth sash, menacing weight and strongly exaggerated chunky silhouette, bold cel-shaded planes. All three must look like DIFFERENT stylistic choices, not the same knight with color swaps. Professional readable character design, coherent anatomy and construction, no panels of detached body parts. Leave room between figures. No extra text except A B C.
