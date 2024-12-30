# Loot Filter Mod for Vintage Story

## Description

The **Loot Filter** mod enhances the gameplay experience in [Vintage Story][VS] by allowing players to manage a custom loot filter. It provides tools to exclude unwanted items from being automatically picked up and introduces an intuitive user interface for configuring and managing these filters. This client-side mod is designed for seamless integration into your Vintage Story client.

---

## Features

- **Filter Items by Code:** Exclude specific items using their item codes.
- **Keyword Filtering:** Define keywords to automatically exclude items based on their names.
- **GUI Integration:** An in-game graphical user interface for easy management of filters.
- **Regex Support:** Advanced search functionality using regex patterns in the GUI.
- **Hotkey Controls:**
  - `~`: Open or close the loot filter GUI.
  - `End`: Reload the loot filter configuration.
- **Commands:** Manage filters through in-game commands (see the Commands section).

---

## Installation

1. Download the latest version of the mod from the [release page](#). OR You can find the latest version from the Vintage Story [ModDB page][MP].
2. Place the mod's `.zip` file into the `Mods` folder of your Vintage Story installation directory.
3. Start the game to enable the mod.

---

## Usage

### Graphical Interface
- **Access the GUI:** Press the `~` key (by default) to open the Loot Filter GUI.
- **Item Filtering:**
  - Navigate to the **Items** tab to browse and toggle item filters.
  - Use the search bar to find specific items using text or regex. _(Regex: /stone */ )_
- **Keyword Management:**
  - Switch to the **Keywords** tab to add or remove keywords that define item exclusions.

### Commands
Use the `/lootfilter` command to manage your filters. Available subcommands include:
- `/lootfilter add`: Add the currently held item to the filter.
- `/lootfilter remove`: Remove the currently held item from the filter.
- `/lootfilter keyword add [keyword]`: Add a keyword to the filter.
- `/lootfilter keyword remove [keyword]`: Remove a keyword from the filter.
- `/lootfilter reset`: Clear all filters.

---

## Configuration

The configuration file `lootfilterconfig.json` is located in the `ModConfig` folder. You can manually edit this file to customize the filters. Reload the configuration using the `End` hotkey or restart the game to apply changes.

---

## Development Details

- **Frameworks:** Utilizes Harmony for patching and Vintage Story's GUI API for creating a user-friendly interface.
- **Client-Side Only:** Does not require server-side installation.
- **Compatibility:** Tested with Vintage Story version **1.20.0-rc.6**.

---

## Support & Connect with Us

If you enjoy the Loot Filter mod and want to support its development or connect with us, here are some ways to do so:

- **Ko-fi**: Show your support by donating on [Ko-fi][KF]. Every contribution helps us improve and maintain the mod!
- **Twitch**: Catch live modding sessions or gameplay on [Twitch][TW]. Follow us to stay updated!
- **Discord**: Join our [Discord community][DC] to discuss the mod, share feedback, or just hang out with fellow players.
- **GitHub**: Stay updated with the latest development and submit bug reports on the [GitHub repository][GR].
_If you encounter any issues or have feature suggestions, feel free to create a ticket on the mod's [GitHub page][GP] or reach out via the [Vintage Story forums][VF]._

Your support fuels our passion for creating better mods and enhancing the Vintage Story experience. Thank you for being awesome! ❤️

---

## License

This project is licensed under the MIT License. See the [LICENSE] file for details.

Enjoy filtering your loot with ease! 🎉

[VS]: https://www.vintagestory.at/
[MP]: https://mods.vintagestory.at/lootfilter
[KF]: https://ko-fi.com/elocrypt
[TW]: https://www.twitch.tv/Elocrypt
[DC]: https://discord.gg/uyM2Np5hqw
[GR]: https://github.com/Elocrypt/lootfilter
[GP]: https://github.com/Elocrypt/lootfilter/issues
[VF]: https://www.vintagestory.at/forums/topic/13716-loot-filter/
