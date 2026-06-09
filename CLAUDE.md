# CLAUDE.md

Этот файл содержит инструкции для Claude Code при работе с проектом Forest Alchemist.

## Обзор проекта

**Forest Alchemist** — 2D top-down роглайт на Unity 6000.3.10f1 (URP).

Игрок управляет алхимиком Элианом, который ходит в процедурно генерируемый лес за ресурсами, сражается с орками, выполняет квесты, возвращается домой и развивает базу. Смерть = потеря походного инвентаря.

**Статус:** Pre-Alpha (играбельный прототип с полным core-циклом).

Подробное описание систем — в `PRD.md`. Состояние реализации — в `AUDIT.md`.

## Технический стек

- **Движок:** Unity 6000.3.10f1
- **Render Pipeline:** URP 2D
- **Платформа:** StandaloneWindows64
- **Input:** New Input System
- **Камера:** Cinemachine 3
- **Скриптинг:** C#, .NET Standard 2.0, Mono2x
- **Сохранения:** JSON (`menu_save.json` в persistentDataPath)

## Структура проекта

```
Assets/
├── Scenes/
│   ├── MainMenu.unity      — главное меню, авторизация
│   ├── Home.unity          — база (крафт, магазин, грядка, подготовка)
│   ├── Level.unity         — экспедиция (процедурный лес)
│   └── Instructions.unity  — НЕ в билде
├── Scripts/
│   ├── GameCore.cs                — синглтон-ядро, аккаунты, сохранения
│   ├── ExpeditionManager.cs       — жизненный цикл экспедиции
│   ├── HomeManager.cs, LevelManager.cs, MainMenuController.cs
│   ├── *SceneBootstrap.cs         — инициализация сцен
│   ├── Crafting/                  — система крафта
│   ├── Enemy/                     — враги, AI (FSM)
│   │   └── AI/                    — состояния (Patrol, Chase, Attack, Death)
│   ├── Garden/                    — грядка
│   ├── Gathering/                 — сбор ресурсов
│   ├── HomeInteraction/           — взаимодействия на Home
│   ├── Hotbar/                    — хотбар 10 слотов
│   ├── Inventory/                 — инвентарь
│   ├── Items/                     — каталог предметов
│   ├── Magic/                     — заклинания
│   ├── Player/                    — игрок (Health, Combat, Score)
│   ├── Quests/                    — квесты
│   ├── UI/                        — все UI-панели
│   └── World/                     — погода, видимость, алтари, порталы
├── Resources/
│   ├── QuestData.json             — 14 квестов
│   ├── Game/
│   │   ├── Character/             — спрайт-стрипы игрока (Idle/Walk/Run/Attack)
│   │   ├── Items/                 — 68 PNG-спрайтов предметов
│   │   ├── Orc/                   — конфиги врагов (.asset) + спрайты
│   │   └── World/                 — текстуры окружения
│   └── Menu/                      — UI-ассеты главного меню
```

## Ключевые архитектурные паттерны

### Синглтоны (DontDestroyOnLoad, ленивое создание через Instance)
Все основные сервисы — синглтоны, создаваемые через `new GameObject` + `AddComponent`:
- `GameCore` — ядро, аккаунты, сохранения
- `ExpeditionManager` — экспедиции, инвентарь похода
- `InventoryService` — домашнее хранилище
- `QuestManager` — прогресс квестов
- `CraftingManager`, `CraftingProgressionService`
- `ShopService`, `GardenService`, `OrcEvolutionService`
- `HotbarManager`

### Поток сцен
```
MainMenu → Home → Level → Home → MainMenu
```
Переходы через `SceneManager.LoadScene` (Addressables НЕ используются, несмотря на наличие `AddressableSceneLoader.cs`).

### Сохранения
Автоматически при: выходе, отключении GameCore, завершении экспедиции, крафте, покупке, изменении хотбара, сборе грядки. Ручное — через паузу.

Структура: `MenuSaveRoot` → `accounts[]` → `MenuAccountData` → `GameProgressData`.

## Реализованные системы

| Система | Файлы | Статус |
|---------|-------|--------|
| Аккаунты + SHA256-пароли | MenuAccountRepository, MenuDomainServices | ✅ |
| Экспедиции (Start/End, успех/смерть) | ExpeditionManager | ✅ |
| Игрок (WASD + мышь, стамина) | PlayerTopDownController | ✅ |
| Здоровье + реген | PlayerHealth | ✅ |
| Заклинания (4 шт, 4 стихии) | PlayerSpellCaster, SpellDefinition | ✅ |
| Враги + FSM AI | EnemyController, EnemyStateMachine | ✅ |
| Волновой спавн с эволюцией | EnemyBaseController, OrcEvolutionService | ✅ |
| Процедурный мир | InfiniteWorldStreamer, WorldChunkTemplate | ✅ |
| Сбор ресурсов | ResourceGatherer, GatherableResourceInteraction | ✅ |
| Крафт (рецепты + заклинания) | CraftingManager, CraftingUI | ✅ |
| Магазин | ShopService, ShopUI | ✅ |
| Грядка (3 стадии) | GardenService, GardenHarvestInteraction | ✅ |
| Квесты (5 типов, 14 шт) | QuestManager, PlayerQuestService | ✅ |
| Хотбар (10 слотов) | HotbarManager, HotbarDisplay | ✅ |
| Погода (5 типов) | WeatherSystem | ✅ |
| Система заметности | VisibilitySystem | ✅ |
| Полный HUD и UI | UI/* | ✅ |

## Не реализовано (приоритет Phase 2 Alpha)

- 🔴 **Аудио** — ни музыки, ни SFX (настройки громкости есть, но ни на что не влияют)
- 🔴 **Визуальные эффекты заклинаний** — firebolt невидимый raycast, dash без анимации
- 🟡 **Биомы** — все чанки выглядят одинаково
- 🟡 **Время суток** — не реализовано
- 🟡 **Particle System** — не используется

## Известный мёртвый код

НЕ удалять без обсуждения, но не трогать без причины:
- `AddressableSceneLoader.cs`, `ISceneLoader.cs`, `ILoadingView.cs`, `LoadingPanelView.cs`, `SceneLoadTrigger.cs` — система загрузки через Addressables не используется
- `DeskBoardInteraction.cs`, `PlayerSetupIntegration.cs`, `SpriteDepthSorter.cs`, `ItemActions.cs` — не прикреплены ни к одному объекту
- `Assets/Enemy.prefab` — враги создаются динамически
- Папки `Resources/Type 1/`, `Resources/Новая папка/`, `Resources/Game/Spells/` — пустые/неиспользуемые

## Работа с Unity MCP

В проекте подключён Unity MCP (порт 7890, instance `ForestAlchemist`):
- **Всегда** передавай `port: 7890` в каждом `unity_*` вызове
- При первом обращении вызывай `unity_list_instances` → `unity_select_instance`
- Иерархия сцен может быть большой — используй `maxDepth: 3-4` или `parentPath`
- После переключения сцен возвращайся к исходной (по умолчанию активна MainMenu)

## Правила работы с кодом

### Стиль
- Sealed классы по умолчанию (большинство классов в проекте `sealed`)
- Синглтоны через `Instance` getter с ленивым созданием
- Поля с `[SerializeField] private` — для Inspector
- События — стандартный C# `event Action<T>`
- Локализация: русский в строковых литералах UI и Debug.Log

### Идентификаторы предметов
Использовать константы из `ItemCatalog`:
```csharp
ItemCatalog.OrcBlood, ItemCatalog.SakuraSapling, ItemCatalog.HealthPotion и т.д.
```
Никогда не хардкодить строки типа `"orc_blood"` напрямую.

### Сохранения
Любое изменение прогресса → `GameCore.Instance.SaveProgress()`. Уже встроено в большинство сервисов через события.

### Сцены
- Никогда не вызывать `SceneManager.LoadScene` напрямую из произвольного места — есть `ExpeditionManager.StartExpedition()`, `GameCore.ReturnToMainMenu()`
- Bootstrap-скрипты сами создают недостающие компоненты при загрузке сцены

### Враги
- Создаются через `EnemyBaseController` динамически (не через префабы)
- Конфигурируются через `EnemyConfig` ScriptableObject в `Resources/Game/Orc/`
- При добавлении нового типа врага: создать .asset + добавить в `EnemyBaseController.LoadConfigsIfNeeded()`

### Анимации
- Спрайты игрока загружаются как `Resources.LoadAll<Sprite>("Game/Character/Idle/Idle_Strip_North")` и т.д.
- Анимации врагов — из `Resources/Game/Orc/{1,2,3}/` через `EnemyAnimationController`

## Команды разработки

### Сборка
Через Unity Editor: File → Build Settings → Build (Windows64).

### Запуск
Через Unity Editor: открыть `Assets/Scenes/MainMenu.unity` → Play.

### Очистка сохранений
Удалить `%AppData%/LocalLow/DefaultCompany/ForestAlchemist/menu_save.json` или через UI: «Новая игра» → сброс прогресса.

## Документация

- `PRD.md` — Product Requirements Document, полное описание всех систем и roadmap
- `AUDIT.md` — детальный аудит реализации, dead code, оценка готовности
- `PROMPT.md` — постановка задачи и пошаговая инструкция по продолжению миграции UI на App UI (для новой сессии)

## Git

- **Основная ветка:** main
- **Коммиты:** только по явному запросу пользователя
- **Никогда:** force push, --no-verify, изменение git config

## Важные предупреждения

1. **Не путать спрайты врагов** — они ЕСТЬ (загружаются из `Resources/Game/Orc/{1,2,3}/`). Tint накладывается поверх для различения типов.
2. **Старый PRD удалён** — текущий `PRD.md` отражает реальную реализацию, а не первоначальную концепцию (которая была про жуков и tower-defense).
3. **Синглтоны создаются лениво** — при первом обращении к `Instance`. Не пытаться найти их через `FindObjectsByType` до первого обращения.
4. **InfiniteWorldStreamer** запускается на Level, но шаблон `Objects` находится в иерархии Level в неактивном состоянии — он клонируется в чанки.


## Миграция UI на App UI (UI Toolkit)

Главное меню переведено с uGUI (Canvas) на **Unity App UI** (`com.unity.dt.app-ui`, UI Toolkit). Идёт постепенный перенос остальных панелей.

### Где что лежит
- `Assets/UI/MainMenu/` — меню: `MainMenuView.uxml`, `MainMenuStyles.uss`, `MainMenuAppUIController.cs`, `ForestAlchemistTheme.tss`, `MainMenuPanelSettings.asset`.
- `Assets/UI/HomePanels/` — внутриигровые панели Home: общий `HomePanelsStyles.uss` (стиль "доска объявлений"), `HomePanelsSettings.asset`, плюс по панели UXML+контроллер (начато с `ExpeditionResult`).

### Правила
- Каждая App UI-вёрстка обёрнута в `<appui:Panel>` (обязательный корень). Namespace: `xmlns:appui="Unity.AppUI.UI"`, `xmlns:ui="UnityEngine.UIElements"`.
- Фон панели прозрачный: `.root > .appui-panel__main { background-color: rgba(0,0,0,0); }`.
- Деревянный/лесной вид воспроизводится спрайтами из `Resources/Menu/` через `resource("Menu/...")` + 9-slice.
- Шрифт: `arturito.ttf` через `-unity-font` / `-unity-font-definition` (НЕ использовать `arturito SDF.asset` — это TMP, несовместим с UI Toolkit).
- Базовый стиль внутриигровых окон — **"доска объявлений"** (`Resources/Menu/DoskaUI.png` или сгенерированная `Resources/Menu/Generated/doska_clean.png`).
- Контроллеры повторяют логику и сервисы оригинальных uGUI-скриптов (те же `GameCore`, `ExpeditionManager` и т.д.). Старый Canvas-объект отключается, App UI добавляется как отдельный `UIDocument`.
- Dropdown (App UI): читать через `.selectedIndex` (int), писать `SetValueWithoutNotify(new[]{ idx })`.

### Скриншоты UI
- **Только** `unity_screenshot_game` (ScreenCapture) ловит UI Toolkit-оверлей.
- `unity_graphics_game_capture` (рендер камеры) UI Toolkit НЕ видит — покажет пустой фон.
- Оригиналы Canvas-панелей сняты в `Assets/Screenshots/canvas/`.

### Подводные камни Unity MCP при работе с UI
- `AssetDatabase.Refresh()` и вход в Play Mode вызывают domain reload → бридж временно недоступен, **порт смещается** (7890→7891→7892...). После — `unity_list_instances` + `unity_select_instance`.
- Refresh в Play Mode выбрасывает из Play Mode; рантайм-объекты `UIDocument`, созданные скриптом, при этом теряются (пересоздавать).
- Живой reload USS: `AssetDatabase.Refresh()` + `host.SetActive(false/true)` для пересборки дерева.
- В `unity_execute_code` нельзя `using`-директивы — только полные имена (`UnityEngine.UIElements.X`). Extension-метод `.Q<T>()` недоступен — звать `UnityEngine.UIElements.UQueryExtensions.Q<T>(root, "name", (string)null)`.

## Генерация изображений (gpt-image-2 / Codex Sale)

Реальные текстуры/иконки можно генерировать и редактировать через OpenAI-совместимый endpoint Codex Sale.

- **Generation:** `POST https://codex.sale/v1/images/generations`
- **Edit:** `POST https://codex.sale/v1/images/edits` (multipart, поле `image`)
- **Заголовок:** `Authorization: Bearer <API_KEY>`
- **Параметры:** `model: "gpt-image-2"` (обяз.), `prompt`, `size` (`1024x1024`|`1536x1024`|`1024x1536`), `response_format` (`b64_json`|`url`).
- Ответ b64 — это **C2PA-помеченный PNG**; b64-строку парсить аккуратно (искать `"b64_json":"` и резать до следующей `"`).

### Как генерировать надёжно в этом окружении
- `curl` через Bash работает (самый быстрый путь), но в текущей сессии инструмент Bash периодически "срезает" параметр — тогда генерировать **прямо из Unity** через `unity_execute_code`:
  - `System.Net.Http.HttpClient` внутри `System.Threading.Tasks.Task.Run(...)` (фоново, чтобы не блокировать главный поток и не упереться в 30s-таймаут MCP);
  - писать `.png` в `Assets/Resources/Menu/Generated/`, лог/ошибку — в соседний `.log`/`.err`;
  - проверять результат отдельными быстрыми вызовами (Task пишет молча, с задержкой 20-60с);
  - валидировать PNG по сигнатуре `89 50 4E 47`, затем `AssetDatabase.Refresh()`.
- Сгенерированные ассеты складывать в `Assets/Resources/Menu/Generated/`.


## ⚠️ Причина зависаний Unity при работе с App UI (ВАЖНО)

**Симптом:** Unity фризит/перестаёт отвечать в Play Mode при показе App UI с деревянными кнопками/панелями.

**Причина:** несоответствие **border спрайта** (Sprite Editor) и **`-unity-slice-*`** в USS. UI Toolkit на КАЖДЫЙ кадр перерисовки КАЖДОГО элемента логирует warning "Sprite X borders (...) are overridden by style slices (...)" с полным stack trace (дорогой `ExtractStackTrace`). В Play Mode при анимации/hover это сотни логов в секунду → захлёбывается главный поток.

**Подтверждено:** в зависшей сессии было 137 таких warning через `UIElementsRuntimeUtilityNative:RepaintPanels`. Виновники — `btn.png` (border 211,101,211,110 vs slice 170,150,170,150) и `Panel.png` (217,178,171,164 vs 60,50,60,50).

**Правила:**
1. **Border спрайта ОБЯЗАН совпадать с `-unity-slice-*` в USS.** Проверять/чинить через `TextureImporter.spriteBorder = new Vector4(L,B,R,T)` + `SaveAndReimport()`. (Vector4 порядок: x=Left, y=Bottom, z=Right, w=Top.)
2. **НЕ вызывать `AssetDatabase.Refresh()` / `SaveAndReimport()` в Play Mode** — ронял import-воркеров (`transport error ... code=10054`, `IPC stream failed`). Сначала `unity_play_mode stop`, потом Refresh.
3. После правок USS со slice — сверять консоль: `unity_console_log type=warning`. Спам спрайт-border = повод чинить border.


## ⚠️ Зависание Unity на импорте сгенерированных PNG (РЕШЕНО)

**Симптом:** после `AssetDatabase.Refresh()` Unity застревает на "Importing assets" (busy 2+ мин), main thread заблокирован, бридж отдаёт "Timed out waiting for main thread", иногда полный фриз.

**Причина:** большие сгенерированные PNG (1024x1536, 2.5-2.8 МБ) импортировались как Sprite с `maxTextureSize=2048` + `Compressed` (DXT). Компрессия нескольких больших RGBA-текстур разом вешает главный поток; усугубляют падающие import-воркеры.

**Правила для сгенерированных через gpt-image-2 ассетов:**
1. Генерировать размером **1024x1024** (не 1536) — меньше вес, быстрее импорт.
2. После записи PNG задавать импортёру: `maxTextureSize=1024`, `textureCompression=Uncompressed`, `mipmapEnabled=false`, `alphaIsTransparency=true`. Uncompressed для UI-спрайтов = нет артефактов на прозрачных краях + быстрый импорт.
3. **Не плодить дубли** — удалять устаревшие версии (`AssetDatabase.DeleteAsset`), чтобы не переимпортировались.
4. Импорт делать в **edit mode**, не в Play Mode.

## Удаление белого фона у сгенерированных картинок

gpt-image-2 НЕ делает прозрачный фон даже по запросу — отдаёт заливку. "Шахматка" во вьюере = это и есть непрозрачный фон.

**Решение:**
1. В промпте требовать **явный белый фон**: "fully centered on a completely flat solid pure white background (#FFFFFF), plenty of white margin, isolated on white". Без этого фон пёстрый/градиентный и плохо чистится.
2. Очистка: **flood-fill прозрачности от 4 краёв** по порогу белизны (r,g,b >= 240 → alpha=0). Flood-fill (а не "все белые пиксели") сохраняет белые детали ВНУТРИ рисунка.
3. Проверять результат пиксельно: доля alpha==0 должна быть 30-70%; остаточные near-white непрозрачные пиксели ~0.
4. **ОБЯЗАТЕЛЬНО обрезать прозрачные поля по краям (auto-trim).** После flood-fill вокруг рисунка остаётся широкая прозрачная рамка (gpt-image-2 центрирует объект с большими полями). Если её НЕ срезать, картинка в UI Toolkit ведёт себя плохо:
   - при `-unity-background-scale-mode: scale-to-fit` объект занимает лишь часть элемента → контент (заголовки, поля, иконки) рассинхронен с границами рисунка и вылезает за рамку;
   - 9-slice border нельзя задать корректно, т.к. реальные края рисунка не совпадают с краями текстуры.
   **Как:** найти непрозрачные границы (alpha>16) по 4 сторонам через `GetPixels32`, вырезать этот прямоугольник в новый `Texture2D`, сохранить как `name_trim.png` (или перезаписать `name.png`). После обрезки картинка плотно занимает холст → можно задавать `spriteBorder` = `-unity-slice-*` и использовать 9-slice (см. раздел про зависания: border ОБЯЗАН совпадать со slice).
5. Сохранять как `name.png`, временный `name_raw.png` удалять.


## Текущее состояние миграции UI на App UI (актуально)

См. **`PROMPT.md`** — полная постановка задачи и инструкция для продолжения.

**Готово:** главное меню (`MainMenu_AppUI`), модалки меню переписаны под новые ассеты.
**Актуальные ассеты** в `Assets/Resources/Menu/Generated/` (все 1024², Uncompressed, фон удалён):
`modal2` (рамка диалога), `ribbon2` (лента-плашка), `close_btn`, `settings_gear`, `account_icon`, `doska_clean`.
Старые `modal_frame`/`title_ribbon`/`wood_button` УДАЛЕНЫ (заменены на modal2/ribbon2).

**В работе / осталось:** довести модалки меню визуально → подключить ExpeditionResult вместо Canvas →
перенести Home-панели (Shop, Storage, Crafting, ExpeditionPreparation, Pause) → HUD Level.

**Чеклист против зависаний (перед каждым Refresh):**
1. Не в Play Mode. 2. PNG = 1024², Uncompressed, без mip. 3. spriteBorder == USS slice.
4. После — проверить `unity_console_log type=warning` на spam про borders.
