# Jellyfin-Roulette for Jellyfin 10.11

> Fork of [ztffn/Jellyfin-Roulette](https://github.com/ztffn/Jellyfin-Roulette) ported to **Jellyfin 10.11.x** (.NET 9). It fixes "Surprise Me" finding nothing in playlists that contain episodes, series or other non-movie items. See the [changelog](CHANGELOG.md#110---2026-10-06).
 ![plugin banner](https://github.com/ztffn/Jellyfin-Roulette/blob/main/screenshots/playlistRouletteIconHeader.png) 
 
![Version](https://img.shields.io/badge/version-1.1.0-blue)
![Jellyfin](https://img.shields.io/badge/jellyfin-10.11-purple)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)
![License](https://img.shields.io/badge/license-MIT-green)

**A Jellyfin plugin that plays a random item from your playlists.**

Click a playlist, hit "Surprise Me," and watch as movie posters spin past before landing on your next unwatched item!


## 📦 Features

- **Slot machine animation** - 3-second spin with dynamic speed and blur effects
- **Smart filtering** - Only shows unwatched items from your playlists (movies, episodes, videos and music)
- **Audio feedback** - Synthesized tick sounds during animation (no external files)
- **Confetti celebration** - Visual effects when selection completes
- **Dynamic resize** - Container morphs from landscape (playlist) to portrait (movie poster)
- **Keyboard navigation** - Full support for Tab, Enter, Escape, and arrow keys (←/→ for remote controls)
- **Configuration panel** - Customize animation timing, button text, audio volume, and more from Jellyfin dashboard
- **Responsive design** - Works on desktop and mobile
- **Non-invasive** - JavaScript injection pattern, doesn't modify Jellyfin core files

## 🎥 Preview
https://github.com/user-attachments/assets/7842878e-5216-4fef-ade5-87691595680a


 
## 🛠️ Installation

### Option 1: Install via Plugin Repository (Recommended)

1. Open Jellyfin Dashboard → **Plugins** → **Repositories**
2. Click **Add** and enter:
   - Repository Name: `Jellyfin-Roulette 10.11`
   - Repository URL: `https://raw.githubusercontent.com/ivanzheludkov/jellyfin-roulette-1011/main/manifest.json`
3. Go to **Catalog** tab
4. Find **Jellyfin-Roulette** and click **Install**
5. Restart Jellyfin server
6. Hard refresh your browser: `Ctrl + Shift + R` (Windows/Linux) or `Cmd + Shift + R` (Mac)

### Option 2: Manual Installation

1. Download `Jellyfin.Plugin.Roulette.zip` from [Releases](https://github.com/ivanzheludkov/jellyfin-roulette-1011/releases) and extract `Jellyfin.Plugin.Roulette.dll`
2. Create plugin directory:
   ```bash
   mkdir -p /path/to/jellyfin/config/plugins/Roulette_1.1.0.0/
   ```
3. Copy the DLL to the directory
4. Restart Jellyfin
5. Hard refresh your browser

Need help? See the [Troubleshooting guide](TROUBLESHOOTING.md). Or open an [Issue](https://github.com/ivanzheludkov/jellyfin-roulette-1011/issues)

## 🎰 Usage

1. Navigate to any playlist in Jellyfin
2. A modal appears with two buttons:
   - **🍿 Surprise Me!** - Triggers slot animation and selects random unwatched item
   - **🎞️ Show List** - Opens normal playlist view
3. Click "Surprise Me!" to watch the animation
4. After selection:
   - **🎬 Play it!** - Navigate to the selected item
   - **🎲 Reroll** - Try again for a different selection
   - **🎞️ Show List** - View the full playlist

### ⚙️ Configuration

Access configuration from **Dashboard → Plugins → Roulette**:
- Customize animation timing and effects
- Change button text (localization support)
- Adjust audio volume
- Configure confetti count
- Enable/disable focus trap

**Tip:** Enable browser sound to hear the satisfying audio ticks!

## 🔄 Compatibility

- **Jellyfin**: 10.11.x (built and tested against 10.11.11). For 10.8–10.10 use the original [1.0.2](https://github.com/ztffn/Jellyfin-Roulette/releases).
- **Upgrading from 1.0.x**: the old injected script is replaced automatically on the first start of 1.1.0. Do a hard browser refresh afterwards.
- **Platform**: Docker (Linux), Native Linux, Windows, macOS
- **Browsers**: Chrome, Firefox, Safari, Edge
- **Mobile**: Responsive design for all screen sizes

## 📄 License

MIT License - See [LICENSE](LICENSE) file for details

## 🙏 Acknowledgments

Built with 🎲 for the Jellyfin Community using the [jellyfin-plugin-template](https://github.com/jellyfin/jellyfin-plugin-template) and inspired by [jellyfin-plugin-custom-javascript](https://github.com/johnpc/jellyfin-plugin-custom-javascript) injection pattern.

---

## 🧪 Development

```bash
dotnet build -c Release            # .NET 9 SDK; output: Jellyfin.Plugin.Roulette/bin/Release/net9.0/
```

`tests/e2e/e2e_roulette.py` drives a real Jellyfin web client in headless Chromium (Playwright): log in, open Playlists, click a playlist, "Surprise Me", "Play it!", and print console errors, dialogs and the final route.

```bash
python3 -m venv .venv && .venv/bin/pip install playwright && .venv/bin/playwright install --with-deps chromium
.venv/bin/python tests/e2e/e2e_roulette.py http://localhost:8096 <user> <password> "<playlist name>"
```

**Repository:** https://github.com/ivanzheludkov/jellyfin-roulette-1011 · original: https://github.com/ztffn/Jellyfin-Roulette
