# PROMPT.md — Задача: миграция UI Forest Alchemist на Unity App UI

> Этот файл — постановка задачи и инструкция для НОВОЙ сессии Claude Code.
> Предыдущая сессия деградировала (инструменты Read/Edit/Bash периодически теряли
> параметры, Unity зависал на импорте). Здесь зафиксировано ЧТО уже сделано,
> ЧТО осталось и КАК это делать без зависаний.

---

## 1. ЦЕЛЬ

Перевести игровой UI с uGUI (Canvas) на **Unity App UI (UI Toolkit)**,
сохранив внешний вид оригинала и улучшив его. Главное меню уже переведено.
Нужно довести **модалки меню** и перенести **внутриигровые панели** (Home/Level).

Визуальный стиль: фэнтези-лес, деревянные рамки, пергамент, шрифт `arturito.ttf`.

---

## 2. ЧТО УЖЕ СДЕЛАНО

### Главное меню (App UI) — ГОТОВО
- `Assets/UI/MainMenu/`: `MainMenuView.uxml`, `MainMenuStyles.uss`,
  `MainMenuAppUIController.cs`, `ForestAlchemistTheme.tss`, `MainMenuPanelSettings.asset`.
- В сцене `MainMenu.unity`: объект `MainMenu_AppUI` (UIDocument) — работает,
  старый `Canvas` отключается в рантайме.
- Кнопки, лого, плашка ника, оверлеи Login/Settings/Records/Load/NewGame.

### Модалки меню — В ПРОЦЕССЕ
- Переписаны под сгенерированную рамку `modal2` + плашку `ribbon2`.
- USS-классы: `.wood-frame` (фон `Menu/Generated/modal2`, scale-to-fit),
  `.account-chip` (фон `Menu/Generated/ribbon2`), `.frame-close` (`Menu/Generated/close_btn`).
- Текст на пергаменте сделан ТЁМНЫМ (`#4a3018` и т.п.).
- НЕ ПРОВЕРЕНО визуально в игре до конца (Unity завис на последнем скриншоте).
  Последний скрин: `Assets/Screenshots/appui/login_v4.png`.

### Прототип ExpeditionResult (App UI) — ЕСТЬ, не подключён вместо Canvas
- `Assets/UI/HomePanels/`: `ExpeditionResultView.uxml`, `ExpeditionResultAppUI.cs`,
  `HomePanelsStyles.uss` (Doska-стиль), `HomePanelsSettings.asset`.

### Сгенерированные ассеты (gpt-image-2) — в `Assets/Resources/Menu/Generated/`
- `modal2.png` — вертикальная рамка с пергаментом и тёмной шапкой (для диалогов)
- `ribbon2.png` — горизонтальная деревянная лента (плашка заголовка/ника)
- `close_btn.png` — круглая кнопка с красным X
- `settings_gear.png` — шестерёнка дерево+бронза
- `account_icon.png` — портрет алхимика
- `doska_clean.png` — деревянная доска объявлений (фон in-game панелей)
- У ВСЕХ фон удалён (flood-fill), импорт: maxSize=1024, Uncompressed, без mip.

### Референс-скриншоты оригинала Canvas — `Assets/Screenshots/canvas/`
- Меню: `menu_main`, `menu_login`, `menu_settings`, `menu_records`, `menu_newgame`
- Home: `home_desk`, `home_crafting`, `home_expprep`, `home_pause_settings`
  (а также ранее: shop, storage, expresult, pause, hud)

---

## 3. ЧТО ОСТАЛОСЬ (приоритет сверху вниз)

1. **Доверить модалки меню** — открыть каждую (Login/Settings/Records/NewGame/Load)
   в Play Mode, заскринить, сравнить с `Screenshots/canvas/menu_*`, поправить отступы/цвета.
2. **Подключить ExpeditionResult** вместо Canvas-версии (отключить старый `ExpeditionResultUI`).
3. **Перенести in-game панели Home** (uGUI → App UI), используя Doska-стиль:
   - Shop (`ShopUI.cs`), Storage (`HomeStorageUI.cs`), Crafting (`CraftingUI.cs`),
     ExpeditionPreparation (`ExpeditionPreparationUI.cs`), Pause (`PauseMenuController.cs`).
4. **HUD Level**: Hotbar, Mana, Shield, ActiveQuest, Weather, ExpeditionStats.

---

## 4. КАК РАБОТАТЬ — КРИТИЧЕСКИ ВАЖНО (иначе Unity зависает)

### Подключение к Unity
- Порт МЕНЯЕТСЯ при каждом Play/Refresh (7890↔7891↔7892).
  Всегда: `unity_list_instances {refresh:true}` → `unity_select_instance` → передавать `port:` в КАЖДЫЙ вызов.

### Импорт PNG (главная причина зависаний!)
- Генерировать ассеты строго **1024x1024** (НЕ 1536).
- После записи PNG ставить импортёру: `maxTextureSize=1024`, `textureCompression=Uncompressed`,
  `mipmapEnabled=false`, `alphaIsTransparency=true`.
- Удалять устаревшие дубли (`AssetDatabase.DeleteAsset`), не копить большие PNG.
- `AssetDatabase.Refresh()` / `SaveAndReimport()` — ТОЛЬКО в edit mode, НИКОГДА в Play Mode.

### Sprite border vs USS slice (вторая причина зависаний!)
- Если у спрайта в Sprite Editor border (X,Y,Z,W) НЕ совпадает с `-unity-slice-*` в USS,
  UI Toolkit спамит warning со стектрейсом КАЖДЫЙ кадр → фриз главного потока.
- Чинить: `importer.spriteBorder = new Vector4(L,B,R,T)` = значениям slice в USS.
- После правок slice — проверять `unity_console_log {type:warning}` на 'borders ... overridden by style slices'.

### Скриншоты App UI
- ТОЛЬКО `unity_screenshot_game` (ScreenCapture) ловит UI Toolkit.
- `unity_graphics_game_capture` (камера) НЕ видит UI Toolkit — покажет пустой фон.

### Генерация картинок (gpt-image-2 / Codex Sale)
- Endpoint: `POST https://codex.sale/v1/images/generations`, заголовок `Authorization: Bearer <KEY>`.
- В промпте ОБЯЗАТЕЛЬНО требовать ЯВНЫЙ белый фон:
  'fully centered on a completely flat solid pure white background (#FFFFFF), plenty of white margin, isolated on white'.
- Ответ b64 — C2PA PNG. Парсить по маркеру '\"b64_json\":\"'.
- Надёжнее всего генерировать ИЗ Unity через `unity_execute_code`:
  `System.Net.Http.HttpClient` внутри `Task.Run(...)`, писать `name_raw.png`, потом очистка.
- Удаление фона: **flood-fill прозрачности от 4 краёв** по порогу белизны (r,g,b>=240 → alpha=0).
  Проверять: доля alpha==0 = 30-70%, остаточных near-white непрозрачных ~0. Затем удалить `_raw.png`.

### unity_execute_code — ограничения
- НЕЛЬЗЯ `using`-директивы. Только полные имена (`UnityEngine.UIElements.X`).
- `.Q<T>()` недоступен → `UnityEngine.UIElements.UQueryExtensions.Q<T>(root, \"name\", (string)null)`.
- Писать файлы можно через `System.IO.File.WriteAllText/WriteAllBytes` (надёжнее Edit/Write,
  если те деградировали).

### Если инструменты Read/Edit/Write/Bash начали терять параметры
- Это session-level баг. Read обычно восстанавливается ретраями.
- Запись файлов делать через `unity_execute_code` + `System.IO.File`.
- Скриншоты смотреть через Read с явным `file_path` (повторять до успеха).

---

## 5. ПОРЯДОК ДЕЙСТВИЙ ДЛЯ КАЖДОЙ ПАНЕЛИ

1. Открыть оригинал в Canvas, заскринить → `Screenshots/canvas/<panel>.png`, изучить макет.
2. Создать `Assets/UI/HomePanels/<Panel>View.uxml` + USS-классы (переиспользовать `HomePanelsStyles.uss`).
3. Создать `<Panel>AppUI.cs` — повторить логику оригинального uGUI-скрипта,
   те же сервисы (`GameCore`, `ExpeditionManager`, `ShopService`, и т.д.).
4. Добавить в сцену `UIDocument` с `HomePanelsSettings.asset` + UXML + контроллер.
   Отключить старый Canvas-объект панели.
5. Войти в Play Mode, показать панель, `unity_screenshot_game`, прочитать, сравнить с оригиналом.
6. Поправить, проверить консоль на sprite-border warnings. Повторять до аккуратного вида.

---

## 6. КЛЮЧЕВЫЕ ФАЙЛЫ ПРОЕКТА
- Оригинальные uGUI-контроллеры: `Assets/Scripts/UI/*.cs`, `Assets/Scripts/Crafting/CraftingUI.cs`.
- Сцены: `Assets/Scenes/MainMenu.unity`, `Home.unity`, `Level.unity`.
- Шрифт: `Assets/Resources/Menu/Fonts/arturito.ttf` (НЕ SDF — он TMP, несовместим с UI Toolkit).
- Подробности систем — `PRD.md`, `AUDIT.md`, правила — `CLAUDE.md`.
