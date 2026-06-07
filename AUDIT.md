# Аудит проекта Forest Alchemist

**Дата:** 2026-06-05
**Движок:** Unity 6000.3.10f1, URP, 2D
**Платформа:** StandaloneWindows64
**Ветка:** main (clean)

---

## Резюме

Forest Alchemist — 2D top-down игра про алхимика, исследующего процедурно генерируемый лес, собирающего ресурсы, сражающегося с орками и выполняющего квесты. Проект прошёл эволюцию от изначальной концепции: вместо ручных уровней с жуками реализована процедурная генерация с орками и системой заклинаний.

**Текущий статус:** Pre-Alpha (играбельный прототип с полным core-циклом)

---

## Сцены и их наполнение

### MainMenu (build index 0)
| Элемент | Статус |
|---------|--------|
| Main Camera | ✅ |
| Global Light 2D | ✅ |
| Canvas / MainMenuController | ✅ |
| Панель MainMenu (Buttons, Account, SettingsBtn) | ✅ |
| Load (полоса загрузки) | ✅ |
| Settings (разрешение, качество, музыка, SFX, оконный режим) | ✅ |
| Records (таблица рекордов) | ✅ |
| Login (Username/Password, Login/Register) | ✅ |
| NewGame (диалог подтверждения) | ✅ |
| EventSystem | ✅ |

**Функционал:** регистрация, авторизация (SHA256), авто-логин, настройки с сохранением, новая игра/продолжить, таблица рекордов.

### Home (build index 1)
| Элемент | Статус |
|---------|--------|
| Main Camera + CinemachineBrain | ✅ |
| CinemachineCamera (Follow Player) | ✅ |
| Canvas / PauseMenuController | ✅ |
| Canvas/Main / ExpeditionResultUI | ✅ |
| PlayerInfo (HealthPanel, Player, Stamina, Mana, Shield) | ✅ |
| Abilities / HotbarDisplay (10 слотов) | ✅ |
| InventoryDisplay (15 слотов) | ✅ |
| CraftingUI (рецепты + спеллы) | ✅ |
| ShopUI | ✅ |
| ExpeditionPreparationUI | ✅ |
| Desk / QuestBoardGenerator | ✅ |
| Pause (Resume/Settings/Save/Exit) | ✅ |
| Player (TopDownController, SpriteRenderer, Animator) | ✅ |
| HomeSceneBootstrap | ✅ |
| Objects (25 шаблонов, inactive) | ✅ |
| EventSystem | ✅ |

### Level (build index 2)
| Элемент | Статус |
|---------|--------|
| CinemachineCamera (Follow Player) | ✅ |
| Main Camera + CinemachineBrain | ✅ |
| Canvas / PauseMenuController | ✅ |
| Сбор (панель прогресса) | ✅ |
| Main/PlayerInfo (HealthPanel 3 колбы, Stamina, Mana, Shield) | ✅ |
| Main/Abilities / HotbarDisplay (10 слотов Item1-10) | ✅ |
| Main/Tasks / LevelQuestHudDisplay | ✅ |
| Desk (панель заданий, неактивна) | ✅ |
| InventoryDisplay (15 слотов) | ✅ |
| Weather / WeatherDisplay | ✅ |
| ActiveQuestPanel | ✅ |
| Pause (полное меню с Settings) | ✅ |
| Player (TopDownController, CombatController, Health, SpellCaster, BuffReceiver, VisibilitySystem, ScoreProvider) | ✅ |
| World (WorldChunkTemplate, InfiniteWorldStreamer) | ✅ |
| Objects (25 шаблонов: камни 4 размеров, статуи, деревья 5 видов) | ✅ |
| Enemy (шаблоны OrcGreen, EnemyBase, OrcBlue, OrcBoss) | ✅ |
| LevelSceneBootstrap | ✅ |
| LevelManager | ✅ |
| WeatherSystem | ✅ |
| EventSystem | ✅ |

### Instructions (НЕ в билде)
Сцена существует, но не добавлена в Build Settings.

---

## Реализованные системы

### 1. Сохранения и аккаунты ✅
- **GameCore** — DontDestroyOnLoad синглтон, ядро
- JSON-сохранение `menu_save.json`
- SHA256-пароли с 8-байтной солью
- Авто-логин, структура GameProgressData (homeStorage, garden, orcs, crafting, hotbar, stats, loadout, expeditionInventory, quests, score)

### 2. Экспедиции ✅
- **ExpeditionManager** — полный цикл: StartExpedition → EndExpedition (Success/Death/Abandoned)
- При успехе: лут → HomeStorage, грядка растёт, орки эволюционируют (+0.05x)
- При смерти: инвентарь очищается, орки эволюционируют (+0.1x)
- Блокировка возврата: нужен портал, свиток или точка эвакуации
- Система заметности: вес инвентаря → штраф скорости + бонус радиуса обнаружения врагами

### 3. Игрок ✅
- **PlayerTopDownController** — WASD + Shift (бег), 4-направленная анимация (idle/walk/run/attack), стамина
- **PlayerHealth** — 100 HP, реген 2 HP/с после 3с без урона, щит через BuffReceiver
- **PlayerSpellCaster** — 100 маны, реген 5/с, 4 заклинания (firebolt/waterspring/stoneskin/airdash)
- **PlayerCombatController** — атака ЛКМ со стаминой
- **PlayerBuffReceiver** — щит с поглощением урона
- **VisibilitySystem** — штраф скорости и радиус обнаружения от загрузки инвентаря
- **ResourceGatherer** — сбор с деревьев удержанием ЛКМ с прогресс-баром

### 4. Враги ✅
- **EnemyController** — полный конвейер: SpriteRenderer, CapsuleCollider2D, Rigidbody2D, EnemyHealth, EnemyHPBar, EnemyStateMachine, EnemyAnimationController
- **EnemyStateMachine** — 4 состояния: Patrol, Chase, Attack, Death
- **EnemyBaseController** — волны: Green→Blue→Shaman→Boss, статуи по кругу, исключение safe-зоны
- **ShamanController** — ranged-атака снарядами (EnemyProjectile)
- **EnemyAnimationController** — загрузка спрайтов из `Resources/Game/Orc/{1,2,3}`, tint через ResolveTint (зелёный, синий, золотой босс, фиолетовый шаман)
- **EnemyConfig** (ScriptableObject) — все параметры, лут-таблица с шансами
- 3 конфига: `GreenOrc.asset`, `BlueOrc.asset`, `BossOrc.asset`
- Авто-дроп: шаман→талисман (75%), босс→трофей вождя (100%) + 4-6 крови, зелёные→drop+кровь
- **OrcBloodDropHandler** — дополнительная бутылка крови за убийство

### 5. Процедурный мир ✅
- **InfiniteWorldStreamer** — стриминг 2×2 чанков вокруг игрока
- Пул из 4 чанков, детерминированная хеш-функция
- 25 шаблонов объектов, 15% шанс базы врагов на чанк (>200 ед. между базами)
- 4 гарантированных объекта: эвакуация, портал, алтарь огня, алтарь воды
- F8 — регенерация

### 6. Погода ✅
- **WeatherSystem** — 5 типов (Clear, Rain, Storm, Fog, Heatwave), смена каждые 120с, переходы 5с
- WeatherDisplay UI

### 7. Крафт ✅
- **CraftingManager** — 4 fallback-рецепта + 4 fallback-заклинания
- **CraftingProgressionService** — 10 уровней с порогами XP
- **CraftingUI** — runtime-built UI
- Заклинания: Firebolt (огонь, projectile), Waterspring (вода, heal), Stoneskin (земля, щит), Airdash (воздух, рывок)

### 8. Магазин ✅
- **ShopService** — 4 товара за кровь орка (health/mana potion, shield/return scroll)
- **ShopUI** — покупка с проверкой баланса

### 9. Грядка ✅
- **GardenService** — 3 стадии роста от успешных экспедиций
- **GardenHarvestInteraction** — сбор по клику (наведение + дистанция)
- **GardenVisuals** — смена спрайта по стадии
- Сброс после сбора, защита от повтора

### 10. Квесты ✅
- 14 квестов в `Resources/QuestData.json`
- 5 типов: CollectItem, KillEnemy, ReachLocation, ActivateAltar, DefeatBoss
- **QuestManager** — отслеживание, авто-завершение, награда кровью
- **PlayerQuestService**, **LevelQuestHudDisplay**, **QuestBoardGenerator**

### 11. Хотбар ✅
- **HotbarManager** — 10 слотов, цифры 1-0, кулдауны
- Интеграция со SpellCaster и ItemActions
- Стартовое заполнение по умолчанию

### 12. UI (полный набор) ✅
MainMenuController, PauseMenuController, ExpeditionResultUI, CraftingUI, ShopUI, InventoryDisplay, HotbarDisplay, ExpeditionPreparationUI, HomeStorageUI, WeatherDisplay, ManaDisplay, ShieldDisplay, PlayerHealthDisplay, ExpeditionStatsDisplay, ActiveQuestPanel, LevelQuestHudDisplay, GatherProgressDisplay, EnemyHPBar

### 13. Ассеты предметов ✅
68 PNG-спрайтов (растения, минералы, трофеи, свитки, катализаторы, боссовые награды)

### 14. Персонаж ✅
Спрайт-стрипы Idle/Walk/Run/Attack (4 направления), загружаются из `Resources/Game/Character/`

---

## Неиспользуемый код

| Файл | Причина |
|------|---------|
| `AddressableSceneLoader.cs`, `ISceneLoader.cs`, `ILoadingView.cs`, `LoadingPanelView.cs`, `SceneLoadTrigger.cs` | Неиспользуемая система загрузки через Addressables |
| `DeskBoardInteraction.cs` | Не прикреплён к GameObject |
| `PlayerSetupIntegration.cs` | Функционал в LevelSceneBootstrap |
| `SpriteDepthSorter.cs` | Используется DepthSortingConfigurator |
| `ItemActions.cs` | Минимальная реализация, дублируется HotbarManager |
| `Enemy.prefab` | Враги создаются динамически через new GameObject |
| Папки `Resources/Type 1/`, `Resources/Новая папка/` | Неиспользуемые |
| Папка `Resources/Game/Spells/` | Ожидаются SpellDefinition asset, но спеллы создаются в коде |
| Пакеты: `ai.navigation`, `tilemap.extras`, `newtonsoft-json` | Не используются |

---

## Техническая архитектура

### Синглтоны (DontDestroyOnLoad, ленивое создание):
GameCore, ExpeditionManager, InventoryService, QuestManager, CraftingManager, CraftingProgressionService, ShopService, GardenService, OrcEvolutionService, HotbarManager

### Поток сцен:
MainMenu (логин) → Home (подготовка) → Level (экспедиция) → Home (результат) → MainMenu

### Сохранения:
`menu_save.json` — синхронизация при выходе, отключении GameCore, завершении экспедиции, покупке/крафте, изменении хотбара, сборе грядки, ручном сохранении

---

## Критические проблемы

| # | Проблема | Серьёзность |
|---|----------|-------------|
| 1 | Нет аудио (ни музыки, ни SFX) | 🔴 |
| 2 | Заклинания без визуальных эффектов (firebolt — невидимый raycast, dash — телепорт без анимации) | 🔴 |
| 3 | Нет экрана загрузки между сценами | 🔴 |
| 4 | SpellDefinition создаются в коде (нет ассетов, нет префабов снарядов) | 🟡 |
| 5 | Грядка даёт только базовые предметы (нет редких из спецификации) | 🟡 |
| 6 | Погода не имеет геймплейных эффектов | 🟡 |
| 7 | Нет визуальных эффектов (particle system) | 🟡 |
| 8 | 17 синглтонов создают GameObject'ы при первом обращении | 🟡 |
| 9 | Неиспользуемые пакеты в манифесте | 🟢 |

---

## Оценка по системам

| Система | Готовность |
|---------|-----------|
| Главное меню | 95% |
| Сохранения/аккаунты | 95% |
| Игрок (управление) | 90% |
| Игрок (боёвка) | 70% |
| Враги (AI) | 85% |
| Враги (визуал) | 75% |
| Процедурный мир | 90% |
| Крафт | 85% |
| Квесты | 80% |
| Грядка | 75% |
| Погода | 80% |
| UI/HUD | 95% |
| Аудио | 0% |
| Визуальные эффекты | 5% |

**Общая оценка: ~40% готовности к релизу** — играбельный прототип с полным core-циклом.
