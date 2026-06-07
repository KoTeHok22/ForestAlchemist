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

## Git

- **Основная ветка:** main
- **Коммиты:** только по явному запросу пользователя
- **Никогда:** force push, --no-verify, изменение git config

## Важные предупреждения

1. **Не путать спрайты врагов** — они ЕСТЬ (загружаются из `Resources/Game/Orc/{1,2,3}/`). Tint накладывается поверх для различения типов.
2. **Старый PRD удалён** — текущий `PRD.md` отражает реальную реализацию, а не первоначальную концепцию (которая была про жуков и tower-defense).
3. **Синглтоны создаются лениво** — при первом обращении к `Instance`. Не пытаться найти их через `FindObjectsByType` до первого обращения.
4. **InfiniteWorldStreamer** запускается на Level, но шаблон `Objects` находится в иерархии Level в неактивном состоянии — он клонируется в чанки.
