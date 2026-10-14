# Jellyfin-Roulette для Jellyfin 10.11 — рабочее пространство

Порт плагина [ztffn/Jellyfin-Roulette](https://github.com/ztffn/Jellyfin-Roulette) под Jellyfin **10.11.11**.
Опубликован: **https://github.com/ivanzheludkov/jellyfin-roulette-1011** (public), релиз `v1.1.0`.
Подробная история, находки и проверки — в [WORKLOG.md](WORKLOG.md). Общаемся с пользователем по-русски.

## Структура папки

| Путь | Что это |
|---|---|
| `Jellyfin-Roulette/` | git-клон плагина. `origin` = ivanzheludkov/jellyfin-roulette-1011, `upstream` = ztffn/Jellyfin-Roulette. Рабочая ветка `main`; `upstream-main` = оригинал |
| `tools/e2e_roulette.py` | локальная копия E2E-скрипта (в репо: `tests/e2e/e2e_roulette.py`) |
| `e2e-shots/` | скриншоты прогонов E2E |
| `release/Jellyfin.Plugin.Roulette.zip` | собранный релиз 1.1.0 (MD5 `d8cca680d0e8a472c87bbfaca2bdef4d`) |
| `ref/jellyfin-v10.10.7`, `ref/jellyfin-v10.11.11` | исходники Jellyfin для сравнения API |
| `ref/index.html.orig` | чистый `index.html` веб-клиента 10.11.11 (до инъекции) |
| `.jf-test.env` | токены и ID тестового сервера (**не коммитить**) — `source` перед curl |

## Окружение на этой машине

- **.NET 9 SDK** 9.0.318 в `/usr/share/dotnet` (симлинк `/usr/local/bin/dotnet`, env в `/etc/profile.d/dotnet.sh`). Перед сборкой: `export DOTNET_ROOT=/usr/share/dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1`.
- **Jellyfin 10.11.11** нативно (apt, репо `repo.jellyfin.org/ubuntu noble`), версия закреплена `/etc/apt/preferences.d/jellyfin-pin` + `apt-mark hold` — в репо уже есть 12.x, не обновлять случайно.
  - http://localhost:8096, админ `admin`/`admin`, обычный пользователь `viewer`/`viewer`.
  - Данные `/var/lib/jellyfin`, плагины `/var/lib/jellyfin/plugins/`, логи `journalctl -u jellyfin`.
  - Веб-клиент `/usr/share/jellyfin/web` — `chown jellyfin:jellyfin` (нужно для инъекции).
  - Старт сервера ~2,5 мин (2 CPU / 2 ГБ RAM). `apt install` может перезапустить jellyfin через needrestart.
- Медиатека: `/srv/media/{movies,shows,mixed}`, тестовые ролики сгенерированы `/usr/lib/jellyfin-ffmpeg/ffmpeg`.
- **Playwright + Chromium**: venv `/opt/pwenv`. Node 18 (apt) — только для `node --check`.
- `gh` авторизован как `ivanzheludkov` (scopes repo, workflow). Git identity задана **локально в репо**: `ivanzheludkov <61048976+ivanzheludkov@users.noreply.github.com>` — gmail в коммиты не ставить.

## Частые команды

```bash
# сборка (0 warnings обязательно: TreatWarningsAsErrors + все анализаторы)
cd /root/projects/jelly/Jellyfin-Roulette && dotnet build -c Release
node --check Jellyfin.Plugin.Roulette/Web/roulette.js

# ручной деплой на тестовый сервер
systemctl stop jellyfin
rm -rf /var/lib/jellyfin/plugins/*Roulette*
D=/var/lib/jellyfin/plugins/Roulette_1.1.0.0; mkdir -p $D
cp Jellyfin.Plugin.Roulette/bin/Release/net9.0/Jellyfin.Plugin.Roulette.dll $D/ && chown -R jellyfin:jellyfin $D
systemctl start jellyfin   # ждать: curl -s localhost:8096/System/Info/Public | grep Version

# E2E (плейлисты: "Movie Night", "Episodes only", "Whole series")
cd /root/projects/jelly && /opt/pwenv/bin/python -I tools/e2e_roulette.py http://localhost:8096 admin admin "Episodes only"

# API-проверки
source /root/projects/jelly/.jf-test.env
curl -s "localhost:8096/Roulette/Random/$P_EP" -H "X-Emby-Token: $TOK"
```

## Выпуск новой версии (чек-лист)

1. Поднять версию: `Directory.Build.props`, `.csproj` (Assembly/FileVersion), `build.yaml`, `clientVersion` + баннер в `roulette.js`, `CHANGELOG.md`.
2. `dotnet build -c Release` → zip только с `Jellyfin.Plugin.Roulette.dll` → `md5sum`.
3. Добавить версию **в начало** `versions` в `manifest.json` (targetAbi `10.11.0.0`, sourceUrl `…/releases/download/vX.Y.Z/Jellyfin.Plugin.Roulette.zip`, checksum = MD5).
4. Commit (с trailer `Co-Authored-By`), `git push`, `gh release create vX.Y.Z <zip> --repo ivanzheludkov/jellyfin-roulette-1011`.
5. Проверить: MD5 скачанного ассета совпадает; установка из каталога на тестовом сервере; E2E.

## Правила

- Публикация (push, релизы, видимость репо) — только с явного согласия пользователя.
- Не коммитить `.jf-test.env`, `ref/`, `e2e-shots/`, токены.
- Плагин должен оставаться совместимым с 10.11.x: пакеты `Jellyfin.Controller`/`Jellyfin.Model` 10.11.11, `net9.0`.
