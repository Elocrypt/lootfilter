# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-05-03

### Added
- **Attribute filtering** — filter items based on live `ItemStack` properties.
  Add rules in the new **Attributes** tab using four built-in synthetic fields
  plus any raw attribute key:
  - `durability` — remaining durability (absolute integer).
  - `durability%` — remaining durability as a 0–1 fraction (e.g. `≤ 0.25` = below 25%).
  - `freshness` — spoilage progress 0–1 (0 = fresh, 1 = fully spoiled).
  - `stacksize` — current stack count.
  - Any other key is resolved via `stack.Attributes.TryGetDecimal(key)`.
  Rules use operators `<`, `≤`, `=`, `≥`, `>` and an optional display label.
  An item is blocked when it satisfies **any** rule (OR semantics, consistent
  with codes and keywords).  Attribute rules participate in Allowlist Mode
  inversion the same as codes and keywords.
- **Import from Chat** — "Import…" button in the Settings tab opens a modal
  where the player can paste a JSON filter config (e.g. one copied from
  Export to Chat). Two confirmation modes:
  - **Merge** — union-merges codes, keywords, and attribute rules into the
    current filter (duplicates skipped); bool toggles are preserved.
  - **Replace All** — replaces the working config wholesale.
  Invalid JSON shows an inline error without closing the modal.
- `FilterImportDialog` — new standalone `GuiDialog` for the paste/parse UI.
- `AttributeRule` data class and `AttributeOperator` enum (shared layer,
  no API dependency).
- `AttributeRulePacket` protobuf DTO carrying attribute rules over the wire.
- `LootFilterMatchLogic.MatchesFilter(cfg, code, name, stack)` overload —
  accepts a nullable `ItemStack` to enable attribute evaluation. The old two-
  argument signature is preserved as a passthrough for the GUI item browser.

### Changed
- GUI now has **four tabs**: Items, Keywords, Attributes, Settings (Settings
  moved from index 2 to index 3).
- `LootFilterPatch` passes the full `ItemStack` to `MatchesFilter` so
  attribute rules are evaluated at pickup time.
- Auto-drop tick passes the full `ItemStack` to `MatchesFilter` so attribute
  rules are also enforced by Trash-on-Sight.
- `FilterUpdatePacket` and `FilterSyncPacket` carry `FilteredAttributes`
  as `ProtoMember(6)`. Old packets (v1.1.0) remain wire-compatible — the
  field defaults to an empty list when missing.
- `LootFilterConfig` gains `FilteredAttributes` (JSON key `filteredAttributes`,
  defaults to empty list). Old config files without the key deserialize cleanly.
- `CloneConfig` in `FilterGuiDialog` deep-copies attribute rules.
- Server-side log message now reports attribute rule count alongside codes and keywords.

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

[1.2.0]: https://github.com/Elocrypt/LootFilter/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/Elocrypt/LootFilter/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Elocrypt/LootFilter/releases/tag/v1.0.0
