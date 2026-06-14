# AUDIO.md — Звуковой дизайн Forest Alchemist

Документ для саунд-дизайнера и для генерации ассетов через ИИ (Suno, Udio, ElevenLabs Sound Effects, AudioCraft и аналоги).

**Игра:** 2D top-down роглайт, фэнтезийный лес, алхимик Элиан, орки, стихии (огонь/вода/земля/воздух), уютная домашняя база и напряжённые экспедиции.

**Технические требования**

| Параметр | Музыка | SFX (UI) | SFX (мир/бой) |
|----------|--------|----------|----------------|
| Формат | `.ogg` (стрим) или `.wav` | `.wav` | `.wav` |
| Частота | 44.1 kHz | 44.1 kHz | 44.1 kHz |
| Каналы | Stereo | Stereo | Mono (панорама в Unity) |
| Громкость | −14 LUFS (мастер) | пик −3 dB | пик −3 dB |
| Петля | Seamless loop, 60–180 с | — | — |

**Группы микшера (уже есть в настройках):** `Music`, `SFX`.

**Структура папок (рекомендуемая)**

```
Assets/Audio/
├── Music/
├── SFX/
│   ├── UI/
│   ├── Player/
│   ├── Spells/
│   ├── Enemies/
│   ├── World/
│   ├── Items/
│   ├── Home/
│   └── Expedition/
```

**Именование:** `категория_событие_вариант.wav`, например `ui_button_click_01.wav`.

---

# 1. МУЗЫКА

Каждый пункт — отдельный трек или слой. В промпте указывай: *instrumental only, no vocals, seamless loop, fantasy forest alchemist game*.

---

## 1.1. Главное меню — `music_main_menu`

**Контекст:** сцена `MainMenu`, экран входа/регистрации, таблица рекордов, настройки. Игрок ещё не в мире — атмосфера «тёплого очага за пределами леса».

**Промпт для ИИ:**
> Seamless loop, 90–100 BPM, instrumental fantasy acoustic track for a indie roguelite main menu. Warm wooden cabin atmosphere outside an ancient enchanted forest. Soft plucked strings (harp, lute), gentle flute melody, distant owl hoots as subtle texture, very light pad. Cozy, mysterious, hopeful — not epic, not battle. No drums heavy, no choir, no vocals. Loop must end where it begins without click. Style reference: Stardew Valley menu meets Hollow Knight ambient menu. Duration 2–3 minutes loop.

**Длительность:** 120–180 с, loop.  
**Громкость относительно других треков:** базовая (0 dB).

---

## 1.2. Дом (база) — `music_home_base`

**Контекст:** сцена `Home`. Крафт, магазин, сундук, доска квестов, грядка, прокачка (U), подготовка к экспедиции. Безопасная зона.

**Промпт:**
> Seamless loop, 80–95 BPM, peaceful alchemist home base theme. Acoustic guitar or kalimba main motif, soft strings, bubbling cauldron and wind chimes as very subtle foley in the mix (barely audible). Garden breeze, birds far away. Feeling of safety, preparation, craftsmanship. Fantasy village workshop. No combat energy. Instrumental, no vocals. Smooth loop 2–4 min. Warm major key with modal folk touches.

**Длительность:** 150–240 с, loop.

---

## 1.3. Экспедиция — исследование леса — `music_expedition_explore`

**Контекст:** сцена `Level`, обычное исследование: сбор деревьев, блуждание по процедурному лесу, нет активного боя или бой далеко.

**Промпт:**
> Seamless loop, 100–110 BPM, exploration ambient track for top-down forest roguelite. Mysterious ancient woodland, light tension but not horror. Low pulsing synth bass very subtle, pizzicato strings, wooden percussion ticks, occasional distant tribal drums far in the mix. Leaves rustling texture. Player is an alchemist gathering resources before orcs find them. Instrumental, no vocals. Loop 2–3 min. Minor key with hopeful intervals.

**Длительность:** 120–180 с, loop.

---

## 1.4. Экспедиция — боевой слой (стем) — `music_expedition_combat_layer`

**Контекст:** накладывается поверх `music_expedition_explore`, когда игрок в бою (враг в состоянии Chase/Attack в радиусе ~12 м) или при `threatLevel ≥ 3`. Crossfade 1.5 с.

**Промпт:**
> Seamless loop stem layer, 120–130 BPM, combat intensity overlay for fantasy forest game. Driving tribal percussion (taiko-lite, frame drums), staccato strings, low brass stabs every 4 bars. Tension and urgency but still organic forest feel, not industrial EDM. Designed to mix ON TOP of exploration ambient — leave space in mids. No melody competing with explore track. Instrumental. 60–90 sec loop.

**Длительность:** 60–90 с, loop, стем (без баса explore-трека).

---

## 1.5. Босс — вождь орков — `music_boss_warchief`

**Контекст:** рядом с `BossOrc` или волна босса на вражеской базе. Заменяет combat layer полностью.

**Промпт:**
> Seamless loop, 135–145 BPM, boss battle theme for orc warchief in enchanted forest. Heavy war drums, aggressive brass, chanting-like wordless male choir pads (oh/ah only, no lyrics), distorted low strings. Primal, threatening, epic but grounded — orc tribe warlord not dragon. Short intense motif every 8 bars. Instrumental. 90–120 sec seamless loop. Dark minor, occasional dissonance.

**Длительность:** 90–120 с, loop.

---

## 1.6. Низкое здоровье — `music_low_health_stinger`

**Контекст:** HP игрока &lt; 25%, однократный fade-in пульсирующего слоя, fade-out при лечении.

**Промпт:**
> Short looping tension layer, 70 BPM heartbeat-like pulse, muted low strings tremolo, soft dissonant violin drone. Anxiety, near death in the forest. NOT jump-scare. 15–20 sec seamless micro-loop. Instrumental. Mix quietly — warning not punishment.

**Длительность:** 15–20 с, micro-loop.

---

## 1.7. Погода — дождь (музыкальный подслой) — `music_weather_rain_layer`

**Контекст:** `WeatherSystem.WeatherType.Rain`, crossfade 5 с с explore-треком.

**Промпт:**
> Ambient music layer, seamless loop 60 sec, soft melancholic piano notes sparse, rain rhythm on hi-hat and white noise bed very gentle. Exploration continues but mood turns reflective. Instrumental, 70 BPM feel. Mix under main explore track.

---

## 1.8. Погода — гроза (музыкальный подслой) — `music_weather_storm_layer`

**Контекст:** `WeatherType.Storm`.

**Промпт:**
> Tense ambient layer, 90 BPM implied, low rumbling drone, occasional distant thunder boom in rhythm (every 8–12 sec random), sparse metallic hits. Foreboding. 45–60 sec loop. Instrumental. Designed as overlay not standalone.

---

## 1.9. Погода — туман — `music_weather_fog_layer`

**Контекст:** `WeatherType.Fog`, сниженная видимость.

**Промпт:**
> Ethereal ambient pad layer, seamless 60 sec, very slow evolving choir pad wordless, hollow wind tones, lost in the woods feeling. Minimal percussion. Instrumental. Mysterious not scary.

---

## 1.10. Погода — жара — `music_weather_heatwave_layer`

**Контекст:** `WeatherType.Heatwave`.

**Промпт:**
> Dry ambient layer, subtle cicada-like rhythmic texture, shimmering heat haze synth pad, slow 60 BPM. Slight fatigue feeling. 45 sec loop. Instrumental.

---

## 1.11. Успешное возвращение — `music_expedition_success_stinger`

**Контекст:** `ExpeditionResult.Success`, панель `ExpeditionResultAppUI`, переход Home. Одноразовый стингер + короткий outro (не loop).

**Промпт:**
> 8–12 second victory stinger, relief and accomplishment. Ascending harp glissando, warm major chord resolution, soft flute melody, gentle chime. Alchemist returns home with loot alive. Not fanfare brass — intimate success. Instrumental, no loop, natural decay ending.

**Длительность:** 8–12 с, one-shot.

---

## 1.12. Смерть в экспедиции — `music_expedition_death`

**Контекст:** `ExpeditionResult.Death`, потеря всего походного инвентаря.

**Промпт:**
> 10–15 second defeat cue, somber not horror. Descending cello phrase, single low piano chord, wind gust fading out. Loss and roguelite failure. Instrumental. Ends with 2 sec silence tail. No loop.

**Длительность:** 10–15 с, one-shot.

---

## 1.13. Пауза — `music_pause_dim`

**Контекст:** `PauseAppUIController`, `timeScale = 0`. Приглушённый фильтр explore/home трека ИЛИ отдельный 30-сек loop.

**Промпт:**
> 30 sec seamless loop, same motif as home or explore but stripped to solo music box or kalimba, lowpassed, dreamy. «Time frozen» feeling. Very quiet. Instrumental.

---

## 1.14. Загрузка сцены — `music_loading_underscore`

**Контекст:** `LoadingOverlayController`, переход MainMenu → Home, Home → Level.

**Промпт:**
> 20–30 sec seamless loop, neutral fantasy underscore, soft pads and plucks, no strong hook. Background for loading bar. Calm anticipation. Instrumental.

---

## 1.15. Повышение угрозы орков — `music_threat_level_up`

**Контекст:** после экспедиции `OrcEvolutionService` увеличивает `threatLevel` — короткий стингер на Home при показе результата.

**Промпт:**
> 5–7 sec ominous stinger, deep war horn far away, tribal drum hit, low string stab. «The forest remembers you». Foreshadowing harder runs. Instrumental one-shot.

---

# 2. SFX — ЗВУКОВЫЕ ЭФФЕКТЫ

Каждый пункт — отдельный файл (или 2 вариации `_01`, `_02` для шагов). В промпте для ИИ-генераторов SFX указывай: *short game sound effect, clean, no reverb tail longer than 0.5s unless noted, fantasy, 2D game*.

**Консолидация (не отдельные файлы):**

| Вместо | Использовать |
|--------|--------------|
| Открытие/закрытие инвентаря, паузы, рекордов, прокачки, подготовки к походу | `sfx_ui_panel_open` / `sfx_ui_panel_close` |
| Вкладки Shop/Crafting, пустой хотбар, +1 стак предмета | `sfx_ui_button_click` |
| Стат на максимуме, пустой магазин | `sfx_ui_error_deny` |
| Награда кровью за квест | `sfx_currency_blood_gain` |
| Успех/смерть экспедиции (панель) | `music_expedition_success_stinger` / `music_expedition_death` |
| Сбор сакуры/дуба/яблони | `sfx_gather_complete` |
| Эмбиент дождя/тумана/жары | `music_weather_*_layer` (раздел 1.7–1.10) |
| Firebolt travel loop | не нужен (raycast, без видимого снаряда) |
| Waterspring aura loop | `sfx_spell_waterspring_cast` достаточно |
| HUD quest tick | `sfx_ui_notification_quest` |
| Промах ближней атаки | `sfx_player_attack_swing` без impact |

**Всего SFX в списке:** ~90 файлов (было ~138).

---

## 2.1. UI — общие (меню, App UI, пауза)

### `sfx_ui_button_click`
**Событие:** любой клик по деревянной кнопке (`AppUIClickRouter`, MainMenu, Pause, Home-панели).  
**Промпт:** Short UI click, soft wooden button tap on fantasy game menu, warm mid tone, 0.05–0.12 sec, single hit, no tail, cozy not plastic.

### `sfx_ui_panel_open`
**Событие:** открытие модальной панели (Pause, Settings, Shop, Crafting, Chest, Desk, Stat Upgrade, Expedition Inventory).  
**Промпт:** Panel open whoosh, parchment unfold mixed with soft wooden creak, 0.3–0.5 sec, fantasy game UI, satisfying not heavy.

### `sfx_ui_panel_close`
**Событие:** закрытие панели (✕, Esc).  
**Промпт:** Panel close thud, gentle book close, 0.2–0.4 sec, warm low-mid.

### `sfx_ui_toggle_on` / `sfx_ui_toggle_off`
**Событие:** чекбоксы музыки/SFX, оконный режим.  
**Промпт:** Soft switch click on / off, wooden latch, 0.06 sec each, slightly different pitch.

### `sfx_ui_error_deny`
**Событие:** нельзя купить, нельзя скрафтить, неверный логин, квест уже принят.  
**Промпт:** Deny buzz, muted low horn «nope», 0.25 sec, not annoying, fantasy game.

### `sfx_ui_success_confirm`
**Событие:** успешный логин, подтверждение «Новая игра», принятие квеста.  
**Промпт:** Positive confirm chime, crystal ding, 0.3 sec, bright major third.

### `sfx_ui_notification_quest`
**Событие:** `QuestManager.OnQuestProgressUpdated` — тихий тик прогресса (не каждый кадр, а при +1).  
**Промпт:** Small quest progress blip, parchment stamp, 0.1 sec, subtle.

### `sfx_ui_notification_quest_complete`
**Событие:** `QuestManager.OnQuestCompleted`, награда кровью в рюкзак.  
**Промпт:** Quest complete fanfare micro, harp + coin shimmer, 0.6–0.8 sec, rewarding.

---

## 2.2. Главное меню

### `sfx_menu_login_success`
**Событие:** успешный `TryLogin` / автологин.  
**Промпт:** Login success, magical door unlock, key turn + soft glow, 0.5 sec.

### `sfx_menu_login_fail`
**Событие:** неверный пароль.  
**Промпт:** Login fail, dull thud, 0.2 sec.

### `sfx_menu_register_success`
**Событие:** регистрация аккаунта.  
**Промпт:** Registration complete, quill write flourish, 0.4 sec.

### `sfx_menu_new_game_confirm`
**Событие:** подтверждение сброса прогресса.  
**Промпт:** Serious confirm gong, single low bell, 0.5 sec, slightly ominous.

### `sfx_menu_exit_game`
**Событие:** выход из игры.  
**Промпт:** Exit whoosh fade, fireplace dim, 0.6 sec.

---

## 2.3. Home — взаимодействия с миром

### `sfx_home_chest_open`
**Событие:** `ChestInteraction` → `HomeStorageUI.Open`.  
**Промпт:** Old wooden chest lid creak open, metal hinge, 0.6 sec, fantasy RPG.

### `sfx_home_chest_close`
**Событие:** закрытие сундука.  
**Промпт:** Chest lid close thud, latch, 0.4 sec.

### `sfx_home_shop_bell`
**Событие:** `ShopInteraction.Open`.  
**Промпт:** Merchant shop bell on door, brass ding, 0.4 sec, welcoming.

### `sfx_home_craft_station_open`
**Событие:** `CraftingStationInteraction` → крафт-панель.  
**Промпт:** Alchemy station open, glass clinks, burner hiss, 0.5 sec.

### `sfx_home_quest_board_rustle`
**Событие:** `DeskBoardInteraction` → доска квестов.  
**Промпт:** Bulletin board paper rustle, nails on wood, 0.35 sec.

### `sfx_home_quest_accept`
**Событие:** `PlayerQuestService.TryAcceptQuest`.  
**Промпт:** Quest accepted, wax seal stamp, 0.25 sec, decisive.

### `sfx_home_garden_harvest`
**Событие:** `GardenHarvestInteraction.Harvest` — сбор урожая с грядки.  
**Промпт:** Garden harvest, snip plants, basket fill, leaves rustle, 0.5 sec, satisfying.

### `sfx_home_expedition_start`
**Событие:** «В лес!» → `ExpeditionManager.StartExpedition` → загрузка Level.  
**Промпт:** Expedition departure, forest gate open, wind gust forward, boots on dirt, 1.0 sec, adventurous.

### `sfx_home_stat_upgrade_purchase`
**Событие:** `PlayerUpgradeService.TryUpgrade` за кровь орка.  
**Промпт:** Stat upgrade power surge, body empower thump + magic sparkle, 0.5 sec, satisfying RPG level-up feel.

---

## 2.4. Магазин и экономика

### `sfx_shop_buy_success`
**Событие:** `ShopService.TryBuy`, списание крови, предмет в HomeStorage.  
**Промпт:** Purchase success, coins or glass vials exchange, merchant handoff, 0.35 sec, positive.

### `sfx_shop_buy_fail`
**Событие:** недостаточно крови.  
**Промпт:** Empty pouch shake, 0.2 sec.

### `sfx_shop_sell_success`
**Событие:** `ShopService.TrySell`.  
**Промпт:** Sell item, coins received clink, 0.3 sec.

### `sfx_currency_blood_gain`
**Событие:** получение крови орка (лут, квест, продажа).  
**Промпт:** Blood vial clink, thick liquid slosh in glass, 0.15 sec, slightly wet.

---

## 2.5. Крафт и прогрессия

### `sfx_craft_success_potion`
**Событие:** `CraftingManager.TryCraft` рецепт зелья/свитка.  
**Промпт:** Alchemy craft complete, bubble boil pop, cork into bottle, 0.6 sec, satisfying.

### `sfx_craft_success_spell`
**Событие:** `CraftingManager.TryCraftSpell`, разблокировка навсегда.  
**Промпт:** Spell inscribed into grimoire, arcane whoosh + page glow, 0.8 sec, epic small moment.

### `sfx_craft_fail_resources`
**Событие:** `CanCraft` / `CanCraftSpell` false.  
**Промпт:** Craft fail, empty cauldron tap, 0.15 sec.

### `sfx_craft_level_up`
**Событие:** `CraftingProgressionService` новый уровень (6→7 и т.д.).  
**Промпт:** Crafting level up, anvil-lite ding + sparkle, 0.7 sec.

---

## 2.6. Игрок — передвижение и бой

### `sfx_player_footstep_grass_01` … `_02`
**Событие:** ходьба `PlayerTopDownController`, WASD. 2 вариации, случайный выбор.  
**Промпт:** Single footstep on forest grass and dirt, soft, top-down game, 0.08 sec, mono.

### `sfx_player_footstep_run_01`
**Событие:** бег (Shift), тратит стамину.  
**Промпт:** Faster running footstep grass, slightly heavier, 0.06 sec, mono.

### `sfx_player_attack_swing`
**Событие:** `PlayerCombatController` / `TryStartAttack`, ЛКМ ближняя атака.  
**Промпт:** Melee sword or staff swing whoosh, light fantasy alchemist weapon, 0.15 sec, airy not metallic heavy.

### `sfx_player_attack_hit_flesh`
**Событие:** попадание по `IDamageable` (орк).  
**Промпт:** Hit impact on creature flesh, wet thud, 0.1 sec, not gory.

### `sfx_player_take_damage`
**Событие:** `PlayerHealth.TakeDamage`.  
**Промпт:** Player hurt grunt implied without voice — body impact thud + cloth, 0.2 sec, painful.

### `sfx_player_heal`
**Событие:** `PlayerHealth.Heal`, зелья, Waterspring.  
**Промпт:** Healing sparkle wash, warm chime ascending, 0.4 sec.

### `sfx_player_death`
**Событие:** `PlayerHealth.OnDeath` → `ExpeditionResult.Death`.  
**Промпт:** Player death fall, body collapse on grass, spirit dissipate whisper, 1.0 sec.

### `sfx_player_mana_restore`
**Событие:** `PlayerSpellCaster.RestoreMana`, Mana Potion.  
**Промпт:** Mana refill, blue magic glug + chime, 0.35 sec.

---

## 2.7. Щит и баффы

### `sfx_shield_apply`
**Событие:** `PlayerBuffReceiver.ApplyShield`, Stoneskin, Shield Scroll, Earth Amulet.  
**Промпт:** Magic shield activate, stone or energy barrier form, low hum start, 0.5 sec.

### `sfx_shield_hit_absorb`
**Событие:** `AbsorbDamage` когда щит поглощает урон.  
**Промпт:** Shield absorb impact, dull energy deflect, 0.12 sec.

### `sfx_shield_break`
**Событие:** `OnShieldBroken`, щит истёк или сломан.  
**Промпт:** Shield shatter, glass-crystal crack, 0.35 sec.

### `sfx_buff_consumable_drink`
**Событие:** использование зелья с хотбара (`BuffConsumableAction`).  
**Промпт:** Potion drink gulp, cork pop before, 0.4 sec.

### `sfx_buff_scroll_unfurl`
**Событие:** Shield Scroll / Return Scroll.  
**Промпт:** Magic scroll unfurl, paper + arcane spark, 0.35 sec.

---

## 2.8. Заклинания

Для каждого: **cast** (каст), **travel** (полёт снаряда, loop), **impact** (попадание), где применимо.

### Firebolt — `spell_firebolt`
| ID | Промпт |
|----|--------|
| `sfx_spell_firebolt_cast` | Small fire spell cast, quick whoosh + flame puff, 0.25 sec, orange bright. |
| `sfx_spell_firebolt_impact` | Fire impact on enemy, flame burst pop, 0.2 sec. |

### Infernobolt — `spell_infernobolt`
| ID | Промпт |
|----|--------|
| `sfx_spell_infernobolt_cast` | Heavy fire spell cast, roaring flame charge, 0.4 sec, powerful. |
| `sfx_spell_infernobolt_travel_loop` | Large fireball travel, deeper rumble, 0.5 sec loop. |
| `sfx_spell_infernobolt_impact` | Explosive fire impact, AoE splash, 0.35 sec, bass thump. |

### Warchief Wrath — `spell_warchief_wrath`
| ID | Промпт |
|----|--------|
| `sfx_spell_warchief_wrath_cast` | Ultimate orc-fire spell cast, war cry energy + flame, 0.6 sec, epic. |
| `sfx_spell_warchief_wrath_impact` | Massive fire explosion, debris, 0.5 sec. |

### Waterspring — `spell_waterspring`
| ID | Промпт |
|----|--------|
| `sfx_spell_waterspring_cast` | Water healing spring, bubbling rise, gentle wave chime, 0.5 sec, soothing blue. |

### Stoneskin — `spell_stoneskin`
| ID | Промпт |
|----|--------|
| `sfx_spell_stoneskin_cast` | Earth armor spell, rocks grind up around body, stone clack, 0.5 sec. |

### Airdash — `spell_airdash`
| ID | Промпт |
|----|--------|
| `sfx_spell_airdash_cast` | Wind dash burst, air slash whoosh, quick teleport glide, 0.3 sec. |
| `sfx_spell_airdash_land` | Dash end skid on grass, 0.15 sec. |

### Общие магические
| ID | Промпт |
|----|--------|
| `sfx_spell_cast_fail_mana` | Not enough mana, dull fizzle, 0.2 sec. |
| `sfx_spell_cast_fail_cooldown` | On cooldown, soft clock tick, 0.1 sec. |
| `sfx_spell_unlocked` | First time spell added to hotbar after craft. |

---

## 2.9. Враги — орки

### Общие
| ID | Событие | Промпт |
|----|---------|--------|
| `sfx_enemy_orc_aggro` | Chase enter, игрок обнаружен | Orc growl alert, short aggressive grunt no words, 0.3 sec. |
| `sfx_enemy_orc_attack_melee` | Attack state | Orc melee swipe, claw/club whoosh, 0.2 sec. |
| `sfx_enemy_orc_hit` | `EnemyHealth.TakeDamage` | Orc pain grunt, flesh hit, 0.15 sec. |
| `sfx_enemy_orc_death` | Death state | Orc death fall, body drop, 0.5 sec. |
| `sfx_enemy_loot_drop` | После смерти, лут в инвентарь | Items drop jingle, coins and vials, 0.25 sec. |

### Шаман (`ShamanController`)
| ID | Промпт |
|----|--------|
| `sfx_enemy_shaman_cast` | Shaman curse cast, rattling bones + magic whisper, 0.4 sec. |
| `sfx_enemy_shaman_projectile` | Dark magic orb travel whoosh, 0.3 sec. |
| `sfx_enemy_shaman_projectile_hit` | Magic poison hit on player, splat, 0.2 sec. |

### Босс — вождь (`BossOrc`)
| ID | Промпт |
|----|--------|
| `sfx_enemy_boss_roar` | Появление / начало волны босса | Boss orc roar, deep and wide, 1.0 sec, intimidating. |
| `sfx_enemy_boss_stomp` | Атака босса | Heavy stomp attack, ground shake, 0.35 sec. |
| `sfx_enemy_boss_death` | Смерть босса | Boss collapse, earth rumble, 1.2 sec. |

### Вражеская база (`EnemyBaseController`)
| ID | Событие | Промпт |
|----|---------|--------|
| `sfx_enemy_base_wave_spawn` | `SpawnWave` | Distant war drums crescendo, orcs arriving, 0.8 sec. |
| `sfx_enemy_base_defeated` | Все волны убиты | Orc camp defeated, fire extinguish, victory thud, 1.0 sec. |

---

## 2.10. Мир — сбор ресурсов и объекты

### Сбор деревьев (`ResourceGatherer`, `GatherableResourceInteraction`)
| ID | Событие | Промпт |
|----|---------|--------|
| `sfx_gather_start` | Начало удержания ЛКМ / E | Gather start, hands on bark, tool ready, 0.2 sec. |
| `sfx_gather_loop` | Прогресс 0–100% | Tree chopping or picking loop, soft rhythm, 2 sec seamless, quiet. |
| `sfx_gather_complete` | Успех, +1 саженец/цветок | Gather success, branch snap, item pop into bag, 0.35 sec. |
| `sfx_gather_cancel` | Отмена, отход | Gather cancel, disappointed rustle, 0.15 sec. |

### По типу ресурса
| ID | Промпт |
|----|--------|
| `sfx_gather_flower_complete` | Rare flower pick, magical sparkle, 0.35 sec. |

*Сакура, дуб, яблоня — используй `sfx_gather_complete`.*

### Алтари (`AltarInteraction`)
| ID | Промпт |
|----|--------|
| `sfx_altar_fire_activate` | Огненный алтарь, flame pillar ignite, crackling rise, 1.0 sec, powerful. |
| `sfx_altar_water_activate` | Водный алтарь, spring burst, water rush up, 1.0 sec, serene. |

### Точки возврата
| ID | Событие | Промпт |
|----|---------|--------|
| `sfx_evacuation_point_activate` | `EvacuationPoint`, клик | Evacuation beacon activate, sci-fantasy flare whistle, 0.6 sec. |
| `sfx_portal_enter` | `PortalObject` | Magic portal enter, swirl vortex, 0.8 sec. |
| `sfx_return_scroll_teleport` | `ReturnScrollAction` | Return scroll teleport home, paper burn + warp, 0.7 sec. |
| `sfx_return_unlocked` | `TryUnlockReturn` | Return path unlocked, distant horn + chime, 0.5 sec. |

### Погода — SFX (не музыка)
| ID | `WeatherType` | Промпт |
|----|---------------|--------|
| `sfx_weather_storm_thunder` | Storm | Thunder crack one-shot, random, 1–2 sec, not every flash. |
| `sfx_weather_change_whoosh` | Смена погоды каждые 120 с | Weather shift whoosh, 0.5 sec. |

*Дождь, туман, жара — музыкальные подслои `music_weather_*_layer` (раздел 1.7–1.10), отдельные SFX-петли не нужны.*

---

## 2.11. Инвентарь и лут

| ID | Событие | Промпт |
|----|---------|--------|
| `sfx_item_pickup_generic` | `PlayerInventory.AddItem` | Item pickup blip, soft pop, 0.08 sec. |
| `sfx_item_pickup_rare` | Rare Flower, трофеи | Rare item pickup, shimmer chime, 0.25 sec. |

*Открытие/закрытие инвентаря — `sfx_ui_panel_open` / `sfx_ui_panel_close`.*

---

## 2.12. Экспедиция — поток игры

| ID | Событие | Промпт |
|----|---------|--------|
| `sfx_expedition_inventory_lost` | Смерть, очистка рюкзака | Items dissolving away, magic dispel downward, 0.6 sec, sad. |

*Успех/смерть экспедиции — музыкальные стингеры `music_expedition_success_stinger` / `music_expedition_death`. Пауза — `sfx_ui_panel_open` / `close`.*

---

## 2.13. Квесты — награды (геймплейные)

| ID | Событие | Промпт |
|----|---------|--------|
| `sfx_quest_boss_complete` | `defeat_boss_orc` | Boss quest complete, war horn + chime, 0.8 sec. |
| `sfx_quest_location_reached` | `reach_evacuation_point` | Location objective complete, flag plant, 0.4 sec. |

*Награда кровью — `sfx_currency_blood_gain`. Прогресс квеста в HUD — `sfx_ui_notification_quest`.*

---

# 3. СВОДНАЯ ТАБЛИЦА ПРИОРИТЕТОВ

Для **Alpha** (минимум играбельного звука):

| Приоритет | Категория | Кол-во |
|-----------|-----------|--------|
| P0 | UI click/open/close, error, confirm | ~8 |
| P0 | Player attack hit, take damage, death | ~4 |
| P0 | Orc hit, death, melee attack | ~4 |
| P0 | Music: menu, home, explore, success, death | 5 треков |
| P1 | Все заклинания cast+impact | ~15 |
| P1 | Gather start/complete, shop buy, craft success | ~8 |
| P1 | Music: combat layer, boss | 2 трека |
| P2 | Погода thunder, footstep вариации | остальное |

**Всего в документе:** ~16 музыкальных треков/слоёв + ~90 SFX.

---

# 4. ЗАМЕТКИ ДЛЯ ИНТЕГРАЦИИ В UNITY

1. Создать `AudioManager` singleton (или `AudioService`) с пулами `AudioSource`.
2. Подписаться на существующие события: `QuestManager.OnQuestCompleted`, `PlayerHealth.OnDeath`, `ExpeditionManager.OnExpeditionEnded`, `CraftingManager.OnSpellCrafted`, и т.д.
3. Учитывать `MenuSettingsData.musicEnabled`, `sfxEnabled`, `musicVolume`, `sfxVolume` — уже применяются через `UnityMenuSettingsApplier`.
4. Музыкальные стемы: `AudioMixer` с снэпшотами `Explore`, `Combat`, `Boss`, `Home`, `Menu`.
5. `HomeUIBlocker` (`timeScale=0`): SFX UI всё равно играют через `PlayClipAtPoint` с `ignoreListenerPause=true` ИЛИ unscaled time.
6. Не спамить `sfx_gather_loop` — один источник на активный сбор.

---

*Документ составлен по анализу кодовой базы Forest Alchemist (сцены MainMenu/Home/Level, все UI-панели App UI, бой, квесты, крафт, магазин, погода, враги, сбор, экспедиции). При добавлении новых систем — дополнять этот файл по тому же формату.*
