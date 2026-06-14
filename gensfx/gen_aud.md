# gen_aud — генерация SFX через ElevenLabs (gensfx)

CLI для [elevenlabs.io/sound-effects](https://elevenlabs.io/sound-effects). API-ключ ElevenLabs **не нужен** — используется тот же HTTP-запрос, что и на лендинге.

Прокси: [proxy6.net](https://proxy6.net/ru) — скрипт покупает **IPv4 Shared** (version `3`, дешевле обычных) и при ошибке квоты/прокси покупает новый.

Промпты игры — в [`AUDIO.md`](../AUDIO.md).

---

## Установка

```bash
cd gensfx
pip install -r requirements.txt
```

Создайте `proxy6.local.json` из примера (или задайте `PROXY6_API_KEY`):

```bash
copy proxy6.local.json.example proxy6.local.json
# вписать api_key с https://proxy6.net/ru/user/developers
```

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `country` | `us` | Страна прокси (iso2) |
| `version` | `3` | **IPv4 Shared** (`4` = обычный IPv4, дороже) |
| `period` | `7` | Срок аренды (дней) |
| `descr` | `gensfx` | Метка для поиска своих прокси |

На балансе Proxy6 должны быть средства (~8 ₽ за 1 Shared / 7 дней).

### Переиспользование прокси

Да — **один прокси живёт до ошибки**:

1. При старте читается `.proxy_state.json` или ищется активный прокси с `descr=gensfx` на Proxy6.
2. Все запросы идут через **тот же** прокси (fingerprint каждый раз новый).
3. Новый прокси покупается только при:
   - `401 quota_exceeded` от ElevenLabs;
   - сетевой ошибке прокси (`ProxyError`, таймаут и т.п.);
   - отсутствии активного прокси в аккаунте.
4. Старый прокси удаляется через API Proxy6 при смене.

---

## Быстрый старт

```bash
# тест (4 opus + trace JSON)
python generate_sfx.py probe -v

# один SFX → WAV
python generate_sfx.py generate "Short UI click, soft wooden button tap, fantasy menu, 0.1 sec" \
  -f output/click.wav

# loop ambient
python generate_sfx.py generate "rain on leaves, seamless loop, 15 sec" --loop \
  -f output/rain.wav

# 4 варианта
python generate_sfx.py generate "wooden chest open" --all-variants -f output/chest.wav
```

---

## Прокси и отпечаток

1. При первом запросе — покупка Shared-прокси (если нет активного с меткой `gensfx`).
2. Дальше **тот же** прокси на все генерации, пока не придёт ошибка.
3. Каждый запрос — **новый браузерный отпечаток** (UA, Referer, Accept-Language).
4. При ошибке квоты/прокси — покупка нового, старый удаляется, повтор (до 8 раз).

```bash
python generate_sfx.py generate "click" -f out.wav -v    # показать proxy + fingerprint
python generate_sfx.py generate "click" -f out.wav --no-proxy   # без прокси (для отладки)
```

---

## Команды

| Опция | Описание |
|-------|----------|
| `-f`, `--file` | Выходной файл (`.wav` `.opus` `.ogg`) |
| `-o`, `--output` | Папка (без `-f`) |
| `-n`, `--name` | Имя без расширения |
| `--wav` | WAV через ffmpeg (режим `-o`) |
| `--loop` / `--no-loop` | Бесшовный loop |
| `--all-variants` | Все 4 варианта (`_01`…`_04`) |
| `--no-proxy` | Без Proxy6 |
| `--jitter SEC` | Случайная пауза 0…SEC |
| `-v`, `--verbose` | Proxy + fingerprint в stderr |

Длительность и loop задаются **в тексте промпта** (`0.1 sec`, `seamless loop`) + флаг `--loop`.

---

## Пакетная генерация

```bash
python generate_all_sfx.py --skip-existing --jitter 2 --only UI/sfx_ui_button
```

Рекомендуется `-j 1` или `-j 2` — каждый воркер тратит квоту лендинга.

---

## Как это работает

```
POST https://api.elevenlabs.io/sound-generation
```

```json
{
  "text": "описание звука",
  "loop": false,
  "output_format": "opus_48000_128"
}
```

Ответ: JSON, 4 варианта в `waveform_base_64` (Ogg Opus).

Proxy6: `GET https://px6.link/api/{key}/buy?count=1&period=7&country=us&version=4&descr=gensfx`

Спека: [`captured/web_api_spec.json`](captured/web_api_spec.json)

---

## Устранение неполадок

| Симптом | Решение |
|---------|---------|
| `Set PROXY6_API_KEY` | Создать `proxy6.local.json` |
| `Error no money` (400) | Пополнить баланс на proxy6.net |
| `Failed after N proxy rotations` | Проверить баланс / страну / лимит ElevenLabs |
| `ffmpeg not found` | `pip install imageio-ffmpeg` |

**Не коммитьте** `proxy6.local.json` — файл в `.gitignore`.
