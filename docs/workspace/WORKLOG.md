# Журнал работ: Jellyfin-Roulette → Jellyfin 10.11

Краткая карта окружения и команды — в [CLAUDE.md](CLAUDE.md).

## Сессия 2026-10-06

### Задача
Плагин [ztffn/Jellyfin-Roulette](https://github.com/ztffn/Jellyfin-Roulette) v1.0.2 не работал на Jellyfin 10.11.11:
нажимаешь «Surprise Me» — плагин «не находит ни одного файла». Нужно найти причину, починить и выложить в GitHub
пользователя как `jellyfin-roulette-1011`.

### Как устроен плагин
- **C# (`Plugin.cs`)**: при старте сервера ищет `index.html` веб-клиента и вставляет перед `</body>` блок:
  маркер `<!-- Roulette Plugin Script -->`, `<script>window.RouletteConfig = {…}</script>` (настройки плагина)
  и `<script>` с содержимым `Web/roulette.js` (embedded resource).
- **JS (`Web/roulette.js`)**: в capture-фазе перехватывает клик по карточке плейлиста (`[data-type="Playlist"]` и др.),
  показывает модалку → «Surprise Me» грузит элементы плейлиста через `window.ApiClient`, фильтрует непросмотренные,
  крутит «слот-машину», «Play it!» открывает страницу деталей выбранного элемента.
- **API (`Api/RouletteController.cs`)**: `GET /Roulette/Random/{playlistId}` — JS его **не использует**.
- **Конфиг (`Configuration/`)**: тайминги анимации, тексты кнопок, громкость, конфетти, focus trap.

### Диагностика (что выяснили и как)
1. Сравнили исходники Jellyfin 10.10.7 и 10.11.11 (`ref/`): логика `Folder.QueryRecursive` / `Playlist.GetPlayableItems`
   почти не менялась; легаси-маршрут `Users/{userId}/Items` в 10.11 сохранён.
2. Поставили реальный Jellyfin 10.11.11, создали библиотеки и плейлисты, повторили запросы плагина curl'ом
   и прогнали сценарий в headless Chromium (Playwright).
3. **Найденные причины:**

| # | Проблема | Симптом | Доказательство |
|---|---|---|---|
| 1 | JS запрашивал `Users/{u}/Items?ParentId=<playlist>&Recursive=true&IncludeItemTypes=Movie` — только фильмы | плейлист из эпизодов/сериала → `alert('This playlist is empty!')`; смешанный плейлист теряет эпизоды | 0 элементов против 2 у `Playlists/{id}/Items`; E2E показал alert |
| 2 | Серверная часть собрана против 10.9.11 / net8 | `/Roulette/Random` → HTTP 500 `MissingMethodException: IUserManager.GetUserById` | лог jellyfin |
| 3 | Навигация на `/web/index.html#!/details?...` | полная перезагрузка страницы, `[BangRedirect] deprecated URL format` | консоль браузера |
| 4 | `handleKeydown` объявлен локально в `showRouletteModal`, а вызывается в функции показа результата | `ReferenceError: handleKeydown is not defined` после каждой прокрутки | pageerror в E2E |
| 5 | Инъекция пропускалась, если маркер уже есть | после апгрейда плагина в `index.html` оставался старый скрипт → фикс бы не применился | код `InjectJavaScript` |
| 6 | Перехват кликов по всей карточке | не открывалось меню «⋮» карточки плейлиста | код init() |

Проверено и **не является** проблемой: `window.ApiClient` в 10.11 есть; перехват клика по карточке работает
(`data-type="Playlist"` + `data-id`); смешанная библиотека даёт тип `Movie`; публичный плейлист (`OpenAccess: true`)
доступен другим пользователям — так же ведёт себя и официальный `/Playlists/{id}/Items`.

### Что изменено (коммит `2b0a43b`, версия 1.1.0)
- `roulette.js`
  - `fetchPlaylistItems` → `Playlists/{id}/Items?UserId=…&EnableUserData=true` (все воспроизводимые типы; сериал раскрывается в эпизоды).
  - Картинка: для эпизода — постер сериала (`SeriesPrimaryImageTag`), иначе Primary, иначе обложка альбома.
  - `getDisplayName()`: «Сериал — S01E02 · Название», «Исполнитель — Трек».
  - `navigateToDetails()`: `window.location.hash = '#/details?id=…&serverId=…'` (без перезагрузки) — используется в «Play it!» и «Show List».
  - Удалён лишний `addEventListener('keydown', handleKeydown)` (обработчик модалки и так активен, фокусируемые элементы пересчитываются на каждое нажатие).
  - Клики по `[data-action="menu"]`, `[data-action="multiselect"]`, `.chkItemSelect` не перехватываются.
  - Версия клиента 1.1.0.
- `Plugin.cs`
  - Блок обрамлён маркерами `<!-- Roulette Plugin Script -->` … `<!-- /Roulette Plugin Script -->`.
  - `RemoveInjectedBlock()` вырезает и новый блок, и легаси-блок 1.0.x (маркер + два `</script>`); затем вставка перед последним `</body>`. Если результат совпадает с файлом — не пишет («already up to date»).
  - `UpdateConfiguration` override → переинъекция (настройки применяются без рестарта).
  - `OnUninstalling` override → очистка `index.html`.
  - `lock` на инъекцию.
- `RouletteController.cs`: через `IPlaylistManager.GetPlaylistForUser` + `GetManageableItems()`; права как у официального эндпоинта (владелец / Shares / OpenAccess); `userId` необязателен (берётся из claim `Jellyfin-UserId`), чужой `userId` — только роль Administrator (иначе 403); фильтр `!IsFolder && IsVisible && !IsPlayed(user, null)`; `Random.Shared`.
- `.csproj`: `net9.0`, `Jellyfin.Controller`/`Jellyfin.Model` **10.11.11**; версии 1.1.0.0 (`Directory.Build.props`, `.csproj`).
- `build.yaml` (были заглушки шаблона), `manifest.json` (owner, imageUrl, версия 1.1.0.0 targetAbi 10.11.0.0), `README.md`, `CHANGELOG.md`, `TROUBLESHOOTING.md`, `.gitignore`, `tests/e2e/e2e_roulette.py`.

### Проверки (все пройдены на 10.11.11)
- Сборка: 0 warnings / 0 errors.
- E2E (Chromium): «Movie Night» (3 фильма + эпизод), «Episodes only», «Whole series» — модалка, прокрутка, «Play it!» → `#/details`, страница рендерится, нет pageerror, нет BangRedirect, нет перезагрузки.
- API `/Roulette/Random`: 200 для своих/публичных плейлистов; viewer с `userId` админа → 403; без токена → 401; всё просмотрено → 404 «No unwatched items»; при одном непросмотренном выбирается именно он.
- Апгрейд с 1.0.2: легаси-блок заменён, в `index.html` один блок v1.1.0.
- Сохранение конфига из API → блок обновлён (проверено `ConfettiCount`/`SurpriseMeText`), без дублей; настройки возвращены к дефолтным.
- Uninstall → `index.html` идентичен оригиналу (`ref/index.html.orig`, без учёта переводов строк).
- Повторный рестарт → «Injected JavaScript is already up to date», файл не меняется.
- Установка из каталога через опубликованный `manifest.json` → `Jellyfin-Roulette_1.1.0.0`, MD5 ассета совпадает, E2E ок.

### Публикация
- Репо: https://github.com/ivanzheludkov/jellyfin-roulette-1011 (public), ветка `main` = история ztffn + коммит порта.
- Релиз: https://github.com/ivanzheludkov/jellyfin-roulette-1011/releases/tag/v1.1.0 (`Jellyfin.Plugin.Roulette.zip`, MD5 `d8cca680d0e8a472c87bbfaca2bdef4d`).
- Manifest для Jellyfin: `https://raw.githubusercontent.com/ivanzheludkov/jellyfin-roulette-1011/main/manifest.json`.

### Тестовые данные (ID в `.jf-test.env`)
| Переменная | Что |
|---|---|
| `TOK`, `USERID` | токен и id админа `admin` |
| `VTOK`, `VID` | токен и id пользователя `viewer` |
| `PL` | «Movie Night» — 3 фильма + 1 эпизод, публичный |
| `P_EP` | «Episodes only» — 2 эпизода |
| `P_SER` | «Whole series» — добавлен сериал (раскрыт в 2 эпизода) |
| `VPL` | «Viewer list» — плейлист viewer'а (фильм + эпизод) |

Плагин на тестовом сервере сейчас установлен **из опубликованного manifest** (`/var/lib/jellyfin/plugins/Jellyfin-Roulette_1.1.0.0`).
Репозиторий плагинов «Jellyfin-Roulette 10.11» добавлен в настройки сервера.

## Идеи / что можно сделать дальше
- [ ] «Play it!» сейчас открывает страницу деталей; можно запускать воспроизведение сразу (нужен доступ к playbackManager веб-клиента — в 1.0 это не удалось, стоит проверить в 10.11 или использовать `Sessions/{id}/Playing`). Пользователь пока не решил.
- [ ] Для Docker/read-only веб-клиента инъекция в `index.html` не работает — альтернатива: интеграция с плагинами File Transformation / JavaScript Injector.
- [ ] Почистить шаблонный мусор: enum `SomeOptions` в `PluginConfiguration.cs`, неиспользуемая настройка `AutoUpdateCheck`, `Jellyfin.Plugin.PlaylistModal.zip` в корне репо, устаревший `design.md`.
- [ ] JS мог бы использовать серверный `/Roulette/Random` вместо загрузки всего плейлиста (сейчас все элементы нужны для анимации постеров).
- [ ] GitHub Actions: сборка + релиз по тегу.
- [ ] Unit-тесты для `Plugin.RemoveInjectedBlock` (метод `internal static`, удобно тестировать).
