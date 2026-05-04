# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-05-03

### Added
- **Server-authoritative architecture** — all filter config is now owned and
  enforced by the server. The client GUI sends update packets; the server
  validates, persists, and echoes the authoritative state back. Multiplayer
  is now fully supported.
- **Allowlist Mode** — invert the filter so only items on your list are picked
  up; everything else is blocked. Toggle via GUI Settings tab or
  `/lootfilter allowlist on|off`.
- **Crouch Bypass** — hold Sneak to temporarily bypass the filter and pick up
  anything, including items suppressed by Trash-on-Sight. Enabled by default;
  toggle via GUI or `/lootfilter crouch on|off`.
- **Expanded auto-drop coverage** — Trash-on-Sight now scans hotbar, backpack,
  character, and crafting inventories (previously only hotbar and backpack).
- **Keyword filtering** — filter items by case-insensitive display-name
  substring in addition to item codes. Manage via the Keywords tab or
  `/lootfilter keyword add|remove <word>`.
- **Regex search in GUI** — wrap your search in `/slashes/` to use a regular
  expression against item display names.
- **Export to Chat** — dump the current filter config as JSON to the chat
  window from the Settings tab.
- `/lootfilter list` command — prints all codes, keywords, and toggle states.
- Network packets (`FilterUpdatePacket`, `FilterSyncPacket`) with full
  round-trip defensive copying.

### Changed
- Complete rewrite of mod architecture from client-local to server-authoritative.
- GUI search is debounced (300 ms) and runs on a background thread; the dialog
  is no longer recomposed on every keystroke.
- Item toggle sends are batched via a 300 ms debounce to reduce network traffic
  when rapidly toggling multiple items.
- Per-player configs are stored under `ModConfig/LootFilter/players/{uid}.json`
  (previously a single flat file).
- Chat commands now sync the authoritative config back to the calling player's
  client after every mutation.
- Project restructured: `src/LootFilter/` with `Client/` and `Server/`
  subdirectories, `tests/LootFilter.Tests/`, `build/` for packaging.

### Removed
- Client-side `lootfilterconfig.json` — the client no longer reads or writes
  config files.
- `config.OnConfigChanged` event — replaced by explicit packet-based sync.
- `/lootfilter reloadconfig` command — server syncs on join; no manual reload
  needed.
- "Refresh" button in the GUI — replaced by automatic sync on dialog open.
- `LootFilterConfig(ICoreAPI api)` constructor — config is now a pure data
  class with no API dependency.

## [1.0.0] - 2025-10-27

### Added
- Initial release with item code filtering and auto-drop.

[1.1.0]: https://github.com/Elocrypt/LootFilter/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Elocrypt/LootFilter/releases/tag/v1.0.0
