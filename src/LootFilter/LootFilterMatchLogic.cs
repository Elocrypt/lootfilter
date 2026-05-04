using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LootFilter
{
    /// <summary>
    /// Centralised filter-match logic shared by the Harmony pickup patch
    /// and the Trash-on-Sight auto-drop tick.  Stateless and allocation-light.
    /// </summary>
    public static class LootFilterMatchLogic
    {
        // Compiled-regex cache keyed by the raw wildcard pattern.
        // Patterns rarely change (only on config save), so caching is safe
        // and avoids re-compiling on every entity-collect or auto-drop check.
        private static readonly Dictionary<string, Regex> WildcardCache = new Dictionary<string, Regex>();

        /// <summary>
        /// Returns <c>true</c> when the item identified by <paramref name="code"/>
        /// and <paramref name="displayName"/> should be blocked (or, in allowlist
        /// mode, is NOT on the list and should therefore be blocked).
        /// </summary>
        /// <param name="cfg">Player's current filter config. Must not be null.</param>
        /// <param name="code">Item collectible code (e.g. <c>game:stone-granite</c>).</param>
        /// <param name="displayName">Localised display name shown in the tooltip.</param>
        public static bool MatchesFilter(LootFilterConfig cfg, string code, string displayName)
        {
            if (cfg == null) return false;

            bool matched = MatchesFilterCore(cfg, code, displayName);

            // Allowlist mode inverts: items NOT on the list are blocked.
            return cfg.AllowlistMode ? !matched : matched;
        }

        private static bool MatchesFilterCore(LootFilterConfig cfg, string code, string displayName)
        {
            // Nothing to match against.
            if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(displayName))
                return false;

            List<string> codes = cfg.FilteredItemCodes;
            List<string> keywords = cfg.FilteredKeywords;

            // --- Pass 1: exact code match & wildcard patterns ---
            for (int i = 0; i < codes.Count; i++)
            {
                string pattern = codes[i];
                if (string.IsNullOrEmpty(pattern)) continue;

                // Fast path: no wildcard → exact ordinal comparison.
                if (pattern.IndexOf('*') < 0)
                {
                    if (string.Equals(pattern, code, StringComparison.Ordinal))
                        return true;

                    continue;
                }

                // Wildcard path: compile once, cache for reuse.
                if (!WildcardCache.TryGetValue(pattern, out Regex rx))
                {
                    string escaped = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                    rx = new Regex(escaped, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    WildcardCache[pattern] = rx;
                }

                if (!string.IsNullOrEmpty(code) && rx.IsMatch(code))
                    return true;
            }

            // --- Pass 2: keyword substring match against display name ---
            if (!string.IsNullOrEmpty(displayName))
            {
                for (int i = 0; i < keywords.Count; i++)
                {
                    string kw = keywords[i];
                    if (string.IsNullOrEmpty(kw)) continue;

                    if (displayName.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Clears the compiled-regex cache.  Call when a player's
        /// <see cref="LootFilterConfig.FilteredItemCodes"/> list changes
        /// so stale patterns don't linger.
        /// </summary>
        public static void InvalidateCache()
        {
            WildcardCache.Clear();
        }
    }
}
