using System.Collections.Generic;
using Newtonsoft.Json;

namespace LootFilter
{
    /// <summary>
    /// Comparison operator used by <see cref="AttributeRule"/>.
    /// </summary>
    public enum AttributeOperator
    {
        /// <summary>Attribute value must be less than the threshold.</summary>
        LessThan,
        /// <summary>Attribute value must be less than or equal to the threshold.</summary>
        LessThanOrEqual,
        /// <summary>Attribute value must equal the threshold (within float epsilon).</summary>
        Equal,
        /// <summary>Attribute value must be greater than or equal to the threshold.</summary>
        GreaterThanOrEqual,
        /// <summary>Attribute value must be greater than the threshold.</summary>
        GreaterThan
    }

    /// <summary>
    /// A single attribute-based filter rule evaluated against an
    /// <see cref="Vintagestory.API.Common.ItemStack"/>.
    /// <para>
    /// The <see cref="Field"/> is a dot-separated path into the stack's
    /// <c>Attributes</c> tree plus two synthetic shortcuts:
    /// <list type="bullet">
    ///   <item><c>durability</c> — remaining durability (0 .. MaxDurability).</item>
    ///   <item><c>durability%</c> — remaining durability as a percentage (0.0 .. 1.0).</item>
    ///   <item><c>freshness</c> — perish rate from TransitionableProperties (0.0 .. 1.0), where 0 = fresh.</item>
    ///   <item><c>stacksize</c> — current stack count.</item>
    ///   <item>Anything else is resolved via <c>stack.Attributes.TryGetDecimal(field)</c>.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class AttributeRule
    {
        /// <summary>Attribute field path (e.g. <c>durability%</c>, <c>freshness</c>, <c>stacksize</c>).</summary>
        [JsonProperty("field")]
        public string Field { get; set; } = "";

        /// <summary>Comparison operator applied to the resolved value.</summary>
        [JsonProperty("op")]
        public AttributeOperator Op { get; set; } = AttributeOperator.LessThanOrEqual;

        /// <summary>Threshold value for the comparison.</summary>
        [JsonProperty("threshold")]
        public double Threshold { get; set; }

        /// <summary>Human-readable label shown in the GUI (e.g. "Durability ≤ 25%").</summary>
        [JsonProperty("label")]
        public string Label { get; set; } = "";
    }

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
        /// ItemStack attribute rules.  An item is matched when its stack satisfies
        /// ANY rule in the list (OR semantics, consistent with codes and keywords).
        /// </summary>
        [JsonProperty("filteredAttributes")]
        public List<AttributeRule> FilteredAttributes { get; set; } = new List<AttributeRule>();

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
