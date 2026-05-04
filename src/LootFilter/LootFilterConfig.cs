using System.Collections.Generic;
using Newtonsoft.Json;

namespace LootFilter
{
    /// <summary>
    /// Pure data class holding a single player's loot-filter configuration.
    /// No API references, no events, no side effects.
    /// Serialized as-is to per-player JSON files on the server.
    /// </summary>
    public class LootFilterConfig
    {
        /// <summary>
        /// Exact item codes and wildcard patterns (using <c>*</c>) to filter.
        /// Wildcard example: <c>game:stone-*</c> matches all stone variants.
        /// </summary>
        [JsonProperty("filteredItemCodes")]
        public List<string> FilteredItemCodes { get; set; } = new List<string>();

        /// <summary>
        /// Case-insensitive substrings matched against the item's display name.
        /// </summary>
        [JsonProperty("filteredKeywords")]
        public List<string> FilteredKeywords { get; set; } = new List<string>();

        /// <summary>
        /// When true the server auto-drops matching items from the player's
        /// inventory every tick (Trash-on-Sight).
        /// </summary>
        [JsonProperty("autoDropFiltered")]
        public bool AutoDropFiltered { get; set; }

        /// <summary>
        /// Inverts the filter: only items ON the list are picked up;
        /// everything else is blocked.
        /// </summary>
        [JsonProperty("allowlistMode")]
        public bool AllowlistMode { get; set; }

        /// <summary>
        /// When true, holding Sneak bypasses the filter and allows all pickups
        /// (and suppresses Trash-on-Sight for that tick).
        /// </summary>
        [JsonProperty("crouchBypassEnabled")]
        public bool CrouchBypassEnabled { get; set; } = true;
    }
}
