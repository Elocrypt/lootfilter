using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;

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

        // ── Public entry points ───────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when the item identified by <paramref name="code"/>
        /// and <paramref name="displayName"/> should be blocked (or, in allowlist
        /// mode, is NOT on the list and should therefore be blocked).
        /// Attribute rules are not evaluated — use the <see cref="ItemStack"/>
        /// overload from the pickup patch and auto-drop tick.
        /// </summary>
        /// <param name="cfg">Player's current filter config. Must not be null.</param>
        /// <param name="code">Item collectible code (e.g. <c>game:stone-granite</c>).</param>
        /// <param name="displayName">Localised display name shown in the tooltip.</param>
        public static bool MatchesFilter(LootFilterConfig cfg, string code, string displayName)
            => MatchesFilter(cfg, code, displayName, stack: null);

        /// <summary>
        /// Returns <c>true</c> when <paramref name="stack"/> should be blocked.
        /// Code, keyword, and attribute rules are all evaluated.
        /// Pass <c>null</c> for <paramref name="stack"/> to skip attribute evaluation
        /// (used by the GUI item browser which has no live stack).
        /// </summary>
        /// <param name="cfg">Player's current filter config.</param>
        /// <param name="code">Collectible code extracted from the stack (avoids re-extraction).</param>
        /// <param name="displayName">Localised display name (avoids re-extraction).</param>
        /// <param name="stack">Live item stack, or <c>null</c> to skip attribute rules.</param>
        public static bool MatchesFilter(
            LootFilterConfig cfg, string code, string displayName, ItemStack? stack)
        {
            if (cfg == null) return false;

            bool matched = MatchesFilterCore(cfg, code, displayName, stack);

            // Allowlist mode inverts: items NOT on the list are blocked.
            return cfg.AllowlistMode ? !matched : matched;
        }

        // ── Core (non-inverted) match ─────────────────────────────────────

        private static bool MatchesFilterCore(
            LootFilterConfig cfg, string code, string displayName, ItemStack? stack)
        {
            // Nothing to match against.
            if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(displayName) && stack == null)
                return false;

            List<string> codes = cfg.FilteredItemCodes;
            List<string> keywords = cfg.FilteredKeywords;
            List<AttributeRule> attrRules = cfg.FilteredAttributes;

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
                if (!WildcardCache.TryGetValue(pattern, out Regex? rx))
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

            // --- Pass 3: attribute rules (requires a live stack) ---
            if (stack != null && attrRules != null && attrRules.Count > 0)
            {
                for (int i = 0; i < attrRules.Count; i++)
                {
                    AttributeRule rule = attrRules[i];
                    if (rule == null || string.IsNullOrEmpty(rule.Field)) continue;

                    if (EvaluateAttributeRule(rule, stack))
                        return true;
                }
            }

            return false;
        }

        // ── Attribute rule evaluation ─────────────────────────────────────

        /// <summary>
        /// Resolves <paramref name="rule"/>.Field against <paramref name="stack"/>
        /// and evaluates the operator/threshold comparison.
        /// Returns <c>false</c> (no match) on any resolution failure so a
        /// misconfigured rule is never a crash.
        /// </summary>
        private static bool EvaluateAttributeRule(AttributeRule rule, ItemStack stack)
        {
            double value;

            try
            {
                value = ResolveField(rule.Field, stack);
            }
            catch
            {
                // Resolution failure → no match; never crash.
                return false;
            }

            return Compare(value, rule.Op, rule.Threshold);
        }

        /// <summary>
        /// Resolves a field name to a numeric value from the given stack.
        /// Throws <see cref="ArgumentException"/> when the field is unrecognised
        /// so the caller can treat it as a no-match gracefully.
        /// </summary>
        private static double ResolveField(string field, ItemStack stack)
        {
            // Synthetic shortcut: remaining durability (absolute).
            if (string.Equals(field, "durability", StringComparison.OrdinalIgnoreCase))
            {
                int max = stack.Collectible?.GetMaxDurability(stack) ?? 0;
                if (max <= 0) return 0;
                int remaining = stack.Attributes?.GetInt("durability", max) ?? max;
                return remaining;
            }

            // Synthetic shortcut: remaining durability as 0..1 fraction.
            if (string.Equals(field, "durability%", StringComparison.OrdinalIgnoreCase))
            {
                int max = stack.Collectible?.GetMaxDurability(stack) ?? 0;
                if (max <= 0) return 1.0; // non-damageable item → treat as full
                int remaining = stack.Attributes?.GetInt("durability", max) ?? max;
                return (double)remaining / max;
            }

            // Synthetic shortcut: current stack count.
            if (string.Equals(field, "stacksize", StringComparison.OrdinalIgnoreCase))
                return stack.StackSize;

            // Synthetic shortcut: perish/freshness (0 = perfectly fresh, 1 = fully spoiled).
            // Reads the first TransitionableProperties entry whose TransitionedStack is food/rot.
            if (string.Equals(field, "freshness", StringComparison.OrdinalIgnoreCase))
            {
                var props = stack.Collectible?.GetTransitionableProperties(null, stack, null);
                if (props != null)
                {
                    for (int i = 0; i < props.Length; i++)
                    {
                        var tp = props[i];
                        if (tp == null) continue;
                        // FreshHours / TotalHours gives spoilage progress 0..1.
                        double total = tp.TransitionHours?.avg ?? 0;
                        if (total <= 0) continue;

                        double elapsed = stack.Attributes?.GetDouble("spoilstate", 0) ?? 0;
                        return Math.Clamp(elapsed / total, 0.0, 1.0);
                    }
                }
                return 0.0; // no perishable property → treat as fresh
            }

            // General path: resolve via Attributes tree.
            if (stack.Attributes == null)
                throw new ArgumentException($"Stack has no Attributes; cannot resolve '{field}'.");

            // Try the attribute as a double (covers int, float, and double attributes).
            if (stack.Attributes.HasAttribute(field))
                return stack.Attributes.GetDouble(field);

            throw new ArgumentException($"Attribute '{field}' not found on stack.");
        }

        /// <summary>Evaluates <c>value op threshold</c>.</summary>
        private static bool Compare(double value, AttributeOperator op, double threshold)
        {
            const double Epsilon = 1e-9;
            return op switch
            {
                AttributeOperator.LessThan           => value < threshold,
                AttributeOperator.LessThanOrEqual    => value <= threshold + Epsilon,
                AttributeOperator.Equal              => Math.Abs(value - threshold) <= Epsilon,
                AttributeOperator.GreaterThanOrEqual => value >= threshold - Epsilon,
                AttributeOperator.GreaterThan        => value > threshold,
                _                                    => false
            };
        }

        // ── Cache management ──────────────────────────────────────────────

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
