namespace LootFilter.Tests;

public class MatchLogicTests
{
    // ── Exact code match ─────────────────────────────────────────────

    [Fact]
    public void ExactCode_Matches()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredItemCodes.Add("game:stone-granite");

        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone-granite", "Granite"));
    }

    [Fact]
    public void ExactCode_CaseSensitive()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredItemCodes.Add("game:stone-granite");

        // Item codes are ordinal (case-sensitive).
        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, "game:Stone-Granite", "Granite"));
    }

    [Fact]
    public void ExactCode_NoMatch()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredItemCodes.Add("game:stone-granite");

        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone-basalt", "Basalt"));
    }

    // ── Wildcard match ───────────────────────────────────────────────

    [Fact]
    public void Wildcard_TrailingStar()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredItemCodes.Add("game:stone-*");

        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone-granite", "Granite"));
        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone-basalt",  "Basalt"));
        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, "game:ore-iron",     "Iron Ore"));
    }

    [Fact]
    public void Wildcard_MiddleStar()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredItemCodes.Add("game:*-granite");

        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone-granite", "Granite"));
        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:rock-granite",  "Granite"));
        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone-basalt", "Basalt"));
    }

    [Fact]
    public void Wildcard_CaseInsensitive()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredItemCodes.Add("game:Stone-*");

        // Wildcard regex uses IgnoreCase.
        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone-granite", "Granite"));
    }

    [Fact]
    public void Wildcard_MultipleStars()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredItemCodes.Add("game:*stone*");

        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg,  "game:stone-granite",   ""));
        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg,  "game:cobblestone-slab", ""));
        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, "game:ore-iron",          ""));
    }

    // ── Keyword match ────────────────────────────────────────────────

    [Fact]
    public void Keyword_SubstringMatch()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredKeywords.Add("granite");

        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "some:code", "Polished Granite Slab"));
    }

    [Fact]
    public void Keyword_CaseInsensitive()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredKeywords.Add("GRANITE");

        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "some:code", "polished granite slab"));
    }

    [Fact]
    public void Keyword_NoMatch()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredKeywords.Add("granite");

        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, "some:code", "Basalt Stone"));
    }

    // ── Allowlist mode ───────────────────────────────────────────────

    [Fact]
    public void AllowlistMode_BlocksUnlistedItems()
    {
        var cfg = new LootFilterConfig { AllowlistMode = true };
        cfg.FilteredItemCodes.Add("game:diamond");

        // Diamond is on the list → NOT blocked.
        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, "game:diamond", "Diamond"));
        // Stone is NOT on the list → blocked.
        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone", "Stone"));
    }

    [Fact]
    public void AllowlistMode_KeywordsAlsoInverted()
    {
        var cfg = new LootFilterConfig { AllowlistMode = true };
        cfg.FilteredKeywords.Add("diamond");

        // "Diamond Ore" matches keyword → NOT blocked.
        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, "game:ore-diamond", "Diamond Ore"));
        // "Iron Ore" doesn't match → blocked.
        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:ore-iron", "Iron Ore"));
    }

    // ── Edge cases ───────────────────────────────────────────────────

    [Fact]
    public void NullConfig_ReturnsFalse()
    {
        Assert.False(LootFilterMatchLogic.MatchesFilter(null!, "game:stone", "Stone"));
    }

    [Fact]
    public void EmptyFilter_NeverMatches()
    {
        var cfg = new LootFilterConfig();

        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone", "Stone"));
    }

    [Fact]
    public void EmptyFilter_AllowlistMode_BlocksEverything()
    {
        var cfg = new LootFilterConfig { AllowlistMode = true };

        // Empty allowlist → nothing is allowed → everything blocked.
        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone", "Stone"));
    }

    [Fact]
    public void NullCodeAndName_ReturnsFalse()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredItemCodes.Add("game:stone");

        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, null!, null!));
        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, "", ""));
    }

    [Fact]
    public void EmptyPattern_Skipped()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredItemCodes.Add("");
        cfg.FilteredItemCodes.Add("game:stone");

        // Empty pattern doesn't crash; stone still matches.
        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone", "Stone"));
        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, "game:ore",  "Ore"));
    }

    [Fact]
    public void EmptyKeyword_Skipped()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredKeywords.Add("");
        cfg.FilteredKeywords.Add("stone");

        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg,  "x", "Smooth Stone"));
        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, "x", "Iron Ore"));
    }

    [Fact]
    public void InvalidateCache_DoesNotThrow()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredItemCodes.Add("game:stone-*");

        // Prime the cache.
        LootFilterMatchLogic.MatchesFilter(cfg, "game:stone-granite", "");

        // Should not throw.
        LootFilterMatchLogic.InvalidateCache();

        // Should still work after invalidation.
        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone-granite", ""));
    }

    // ── Combined code + keyword ──────────────────────────────────────

    [Fact]
    public void CodeMatch_TakesPrecedence_OverKeywordMiss()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredItemCodes.Add("game:stone");
        // No keywords — keyword check won't match.

        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone", "Completely Different Name"));
    }

    [Fact]
    public void KeywordMatch_Works_WhenCodeMisses()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredItemCodes.Add("game:diamond");
        cfg.FilteredKeywords.Add("stone");

        // Code doesn't match, but keyword does.
        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:rock", "Smooth Stone"));
    }

    // ── Attribute rules — no live stack (stack = null) ────────────────

    [Fact]
    public void AttributeRule_WithNullStack_NeverMatches()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredAttributes.Add(new AttributeRule
        {
            Field     = "durability%",
            Op        = AttributeOperator.LessThanOrEqual,
            Threshold = 0.25
        });

        // stack = null (GUI browser path) → attribute rules skipped → no match.
        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, "game:sword", "Sword", stack: null));
    }

    // ── Attribute rules — allowlist interaction ───────────────────────

    [Fact]
    public void AttributeRule_AllowlistMode_InvertedCorrectly()
    {
        // In allowlist mode an attribute rule saying "match low-durability items"
        // means those items ARE allowed; everything else is blocked.
        var cfg = new LootFilterConfig { AllowlistMode = true };
        cfg.FilteredAttributes.Add(new AttributeRule
        {
            Field     = "durability%",
            Op        = AttributeOperator.LessThanOrEqual,
            Threshold = 0.25
        });

        // Null stack → attribute rule not evaluated → core returns false →
        // allowlist inversion → MatchesFilter returns true (blocked).
        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone", "Stone", stack: null));
    }

    // ── Attribute rules — empty field ────────────────────────────────

    [Fact]
    public void AttributeRule_EmptyField_Skipped()
    {
        var cfg = new LootFilterConfig();
        cfg.FilteredAttributes.Add(new AttributeRule { Field = "", Op = AttributeOperator.LessThan, Threshold = 10 });
        cfg.FilteredItemCodes.Add("game:stone");

        // Empty field rule is skipped; code match still works.
        Assert.True(LootFilterMatchLogic.MatchesFilter(cfg, "game:stone", "Stone", stack: null));
        Assert.False(LootFilterMatchLogic.MatchesFilter(cfg, "game:ore",   "Ore",   stack: null));
    }
}
