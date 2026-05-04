<div align="center">

# Loot Filter

**A server-authoritative loot filter mod for [Vintage Story](https://www.vintagestory.at/).**

Block item pickups by code, wildcard pattern, or display-name keyword. Supports allowlist mode, crouch bypass, Trash-on-Sight auto-drop, and a searchable in-game GUI.

[![CI](https://github.com/Elocrypt/LootFilter/actions/workflows/ci.yml/badge.svg)](https://github.com/Elocrypt/LootFilter/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/Elocrypt/LootFilter?include_prereleases)](https://github.com/Elocrypt/LootFilter/releases)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)
[![VS 1.22.0](https://img.shields.io/badge/Vintage%20Story-1.22.0-purple)](https://www.vintagestory.at/)

</div>

---

> **1.1.0 is a complete rewrite.** The mod now uses a server-authoritative architecture and targets Vintage Story 1.22.0 on .NET 10. Filter configs from earlier versions will not carry over — reconfigure on first launch.

## Features

<table>
<tr>
<td width="50%" valign="top">

### Filtering
- **Item code filtering** — add exact codes (e.g. `game:stone-granite`) or wildcard patterns (e.g. `game:stone-*`) to block pickup of matching items
- **Keyword filtering** — case-insensitive substring matches against display names (e.g. "flint" blocks anything with "flint" in its name)
- **Allowlist mode** — invert the filter so only items on your list are picked up; everything else is blocked
- **Crouch bypass** — hold Sneak to temporarily pick up anything regardless of the filter, including items suppressed by Trash-on-Sight. Enabled by default

### Trash-on-Sight
- Automatically ejects filtered items from your inventory every server tick
- Covers **hotbar**, **backpack**, **character**, and **crafting** slots
- Drops items 0.5 blocks in front of the player at foot level
- Suppressed while crouching (when crouch bypass is enabled)
- Skips players in creative mode

</td>
<td width="50%" valign="top">

### GUI
- Open with <kbd>~</kbd> (Tilde, rebindable)
- **Items tab** — searchable, paginated list of every collectible in the game with item icons and per-item toggle switches
- **Keywords tab** — add and remove display-name keywords with instant sync
- **Settings tab** — toggle Trash-on-Sight, Allowlist Mode, and Crouch Bypass; export current config as JSON to chat
- Search supports plain text and **regex** (wrap in `/slashes/`)

### Multiplayer
- Server-authoritative — the server owns all config state and enforces pickup blocking. The client GUI sends update packets; it never reads or writes config files
- Per-player configs stored under `ModConfig/LootFilter/players/{uid}.json`
- Works in singleplayer and on dedicated servers
- Chat commands available for all filter operations

</td>
</tr>
</table>

## Install

1. Download the latest `LootFilter_<version>.zip` from the [Releases](https://github.com/Elocrypt/LootFilter/releases) page.
2. Drop the zip (don't extract it) into your Vintage Story `Mods/` folder:
   - **Windows:** `%AppData%\VintagestoryData\Mods`
   - **Linux:** `~/.config/VintagestoryData/Mods`
   - **macOS:** `~/Library/Application Support/VintagestoryData/Mods`
3. Launch Vintage Story. The mod loads on both client and server automatically.

Universal mod — install on both client and server for multiplayer, or just drop it in for singleplayer.

## Using it

### GUI

Press <kbd>~</kbd> (Tilde) to open the Loot Filter dialog. Three tabs:

- **Items** — browse or search every item in the game. Toggle the switch next to an item to add or remove it from your filter. Item icons are shown alongside names for easy identification.
- **Keywords** — type a keyword and click Add. Any item whose display name contains that keyword (case-insensitive) will be filtered. Click ✕ to remove.
- **Settings** — flip toggles for Trash-on-Sight, Allowlist Mode, and Crouch Bypass. Click "Export to Chat" to dump your current config as JSON.

All changes sync to the server immediately.

### Search

The search bar in the Items tab supports two modes:

| Mode | Syntax | Example | Matches |
|---|---|---|---|
| Plain text | Just type | `bread` | Any item with "bread" in its name |
| Regex | Wrap in `/slashes/` | `/^Iron.*sword$/i` | Names starting with "Iron" and ending with "sword" |

Regex examples:

- `/bread/` — items containing "bread"
- `/^Iron/` — items starting with "Iron"
- `/sword\|axe/` — items containing "sword" or "axe"
- `/plank.*oak/` — items with "plank" followed by "oak"
- `/(charcoal\|coke)\b/` — exact-word matches for fuel items

Search is debounced (300 ms) and runs on a background thread, so typing stays responsive even with thousands of items.

### Chat commands

All commands are available via `/lootfilter`:

| Command | Description |
|---|---|
| `/lootfilter add` | Add the held item's code to your filter |
| `/lootfilter remove` | Remove the held item's code from your filter |
| `/lootfilter keyword add <word>` | Add a display-name keyword |
| `/lootfilter keyword remove <word>` | Remove a keyword |
| `/lootfilter list` | Print your current filter (codes, keywords, toggles) |
| `/lootfilter reset` | Clear all codes and keywords |
| `/lootfilter trash on\|off` | Toggle Trash-on-Sight |
| `/lootfilter allowlist on\|off` | Toggle Allowlist Mode |
| `/lootfilter crouch on\|off` | Toggle Crouch Bypass |

Every command that modifies your config saves to disk and syncs the authoritative state back to your client immediately.

## Compatibility

- **Vintage Story 1.22.0** or later.
- Universal mod — works in singleplayer and multiplayer. Install on both client and server for dedicated servers.
- No known conflicts. The mod uses a Harmony prefix on `EntityBehaviorCollectEntities.OnFoundCollectible` to intercept pickups. If another mod patches the same method, standard Harmony priority rules apply.
- Per-player configs are isolated — each player's filter is independent and stored in its own JSON file on the server.

---

<details>
<summary><b>Building from source</b></summary>

### Requirements

- Vintage Story 1.22.0 or later (for the referenced game DLLs)
- .NET 10 SDK

### Setup

1. Install the Vintage Story client at a known location.
2. Set the `VINTAGE_STORY` environment variable to your install directory:

   ```powershell
   # Windows (PowerShell)
   [Environment]::SetEnvironmentVariable("VINTAGE_STORY", "F:\VintageStory\Client_v1.22.0\Vintagestory", "User")
   ```

   ```bash
   # Linux / macOS
   export VINTAGE_STORY="$HOME/.local/share/Vintagestory"
   ```

3. Optionally set `VINTAGE_STORY_DATA` if your data directory differs from the default:

   ```powershell
   [Environment]::SetEnvironmentVariable("VINTAGE_STORY_DATA", "F:\VintageStory\Client_v1.22.0\Vintagestory\v", "User")
   ```

4. Restart your IDE so it picks up the new variables.
5. Open `LootFilter.sln`.

If the variables are not set, `Directory.Build.props` falls back to `F:\VintageStory\Client_v1.22.1\Vintagestory` on Windows or `~/.local/share/Vintagestory` on Linux.

### Build

```powershell
dotnet build LootFilter.sln -c Release
```

Build output at `src/LootFilter/bin/Release/net10.0/` is a complete, loadable mod folder (DLL + `modinfo.json`). By default the build also deploys the mod to `$(VINTAGE_STORY_DATA)\Mods\LootFilter` so it's picked up by the game on next launch. Disable with `/p:DeployMod=false`.

### Test

```powershell
dotnet test LootFilter.sln -c Release
```

42 tests covering match logic (exact, wildcard, keyword, allowlist, edge cases), per-player config store (persistence, isolation, corruption recovery), and network packet round-trips.

### Package a release

```powershell
./build/package.ps1 -Configuration Release -Version 1.1.0
```

Produces `build/dist/LootFilter_1.1.0.zip`, ready to upload to the Vintage Story mod portal. On push of a tag matching `v*.*.*`, GitHub Actions runs the same script and publishes a release automatically — see `.github/workflows/release.yml`.

### Architecture

The codebase is split into three layers:

- **Shared** (`src/LootFilter/`) — `LootFilterConfig` (pure data class), `LootFilterMatchLogic` (static match helper), `LootFilterNetworkPackets` (protobuf DTOs). No API dependencies.
- **Server** (`src/LootFilter/Server/`) — `PerPlayerConfigStore` (file-backed cache), `LootFilterPatch` (Harmony prefix, server-gated), `LootFilterCommands` (chat commands). All mutations save to disk and sync back to the client.
- **Client** (`src/LootFilter/Client/`) — `FilterGuiDialog` (three-tab GUI), `GuiElementItemIcon` (custom element for rendering item icons in the GL phase). Reads from a server-synced mirror; never writes config files.

`LootFilterMod` is the `ModSystem` entry point that wires everything together.

</details>

## License

MIT — see [LICENSE.txt](LICENSE.txt).

## Credits

- [Elocrypt](https://github.com/Elocrypt) — author and maintainer.
