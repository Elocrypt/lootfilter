namespace LootFilter.Tests;

public class PacketTests
{
    private static LootFilterConfig MakeTestConfig()
    {
        var cfg = new LootFilterConfig
        {
            FilteredItemCodes = new() { "game:stone", "game:ore-*", "game:flint" },
            FilteredKeywords  = new() { "dirt", "gravel" },
            AutoDropFiltered  = true,
            AllowlistMode     = true,
            CrouchBypassEnabled = false
        };

        cfg.FilteredAttributes.Add(new AttributeRule
        {
            Field     = "durability%",
            Op        = AttributeOperator.LessThanOrEqual,
            Threshold = 0.25,
            Label     = "≤ 25% durability"
        });

        cfg.FilteredAttributes.Add(new AttributeRule
        {
            Field     = "freshness",
            Op        = AttributeOperator.GreaterThanOrEqual,
            Threshold = 0.9,
            Label     = "Almost spoiled"
        });

        return cfg;
    }

    // ── FilterUpdatePacket ───────────────────────────────────────────

    [Fact]
    public void UpdatePacket_RoundTrip_PreservesAllFields()
    {
        var original = MakeTestConfig();
        var packet   = FilterUpdatePacket.FromConfig(original);
        var restored = packet.ToConfig();

        Assert.Equal(original.FilteredItemCodes, restored.FilteredItemCodes);
        Assert.Equal(original.FilteredKeywords,  restored.FilteredKeywords);
        Assert.Equal(original.AutoDropFiltered,  restored.AutoDropFiltered);
        Assert.Equal(original.AllowlistMode,     restored.AllowlistMode);
        Assert.Equal(original.CrouchBypassEnabled, restored.CrouchBypassEnabled);

        Assert.Equal(original.FilteredAttributes.Count, restored.FilteredAttributes.Count);
        Assert.Equal(original.FilteredAttributes[0].Field,     restored.FilteredAttributes[0].Field);
        Assert.Equal(original.FilteredAttributes[0].Op,        restored.FilteredAttributes[0].Op);
        Assert.Equal(original.FilteredAttributes[0].Threshold, restored.FilteredAttributes[0].Threshold);
        Assert.Equal(original.FilteredAttributes[0].Label,     restored.FilteredAttributes[0].Label);
    }

    [Fact]
    public void UpdatePacket_FromConfig_DefensiveCopy()
    {
        var original = MakeTestConfig();
        var packet   = FilterUpdatePacket.FromConfig(original);

        // Mutating original should not affect the packet.
        original.FilteredItemCodes.Add("game:mutated");
        Assert.DoesNotContain("game:mutated", packet.FilteredItemCodes);
    }

    [Fact]
    public void UpdatePacket_ToConfig_DefensiveCopy()
    {
        var packet = FilterUpdatePacket.FromConfig(MakeTestConfig());
        var cfg    = packet.ToConfig();

        // Mutating the returned config should not affect the packet.
        cfg.FilteredItemCodes.Add("game:mutated");
        Assert.DoesNotContain("game:mutated", packet.FilteredItemCodes);
    }

    [Fact]
    public void UpdatePacket_ToConfig_NullLists_Handled()
    {
        var packet = new FilterUpdatePacket
        {
            FilteredItemCodes   = null!,
            FilteredKeywords    = null!,
            FilteredAttributes  = null!
        };

        var cfg = packet.ToConfig();
        Assert.NotNull(cfg.FilteredItemCodes);
        Assert.NotNull(cfg.FilteredKeywords);
        Assert.NotNull(cfg.FilteredAttributes);
        Assert.Empty(cfg.FilteredItemCodes);
        Assert.Empty(cfg.FilteredKeywords);
        Assert.Empty(cfg.FilteredAttributes);
    }

    [Fact]
    public void UpdatePacket_AttributeRules_DefensiveCopy_From()
    {
        var original = MakeTestConfig();
        var packet   = FilterUpdatePacket.FromConfig(original);

        original.FilteredAttributes.Add(new AttributeRule { Field = "stacksize", Op = AttributeOperator.Equal, Threshold = 1 });
        // The packet captured the state before the mutation.
        Assert.Equal(2, packet.FilteredAttributes.Count);
    }

    [Fact]
    public void UpdatePacket_AttributeRules_DefensiveCopy_To()
    {
        var packet = FilterUpdatePacket.FromConfig(MakeTestConfig());
        var cfg    = packet.ToConfig();

        cfg.FilteredAttributes.Add(new AttributeRule { Field = "stacksize", Op = AttributeOperator.Equal, Threshold = 1 });
        // The packet is unaffected.
        Assert.Equal(2, packet.FilteredAttributes.Count);
    }

    // ── FilterSyncPacket ─────────────────────────────────────────────

    [Fact]
    public void SyncPacket_RoundTrip_PreservesAllFields()
    {
        var original = MakeTestConfig();
        var packet   = FilterSyncPacket.FromConfig(original);
        var restored = packet.ToConfig();

        Assert.Equal(original.FilteredItemCodes, restored.FilteredItemCodes);
        Assert.Equal(original.FilteredKeywords,  restored.FilteredKeywords);
        Assert.Equal(original.AutoDropFiltered,  restored.AutoDropFiltered);
        Assert.Equal(original.AllowlistMode,     restored.AllowlistMode);
        Assert.Equal(original.CrouchBypassEnabled, restored.CrouchBypassEnabled);

        Assert.Equal(original.FilteredAttributes.Count, restored.FilteredAttributes.Count);
        Assert.Equal(original.FilteredAttributes[1].Field,     restored.FilteredAttributes[1].Field);
        Assert.Equal(original.FilteredAttributes[1].Op,        restored.FilteredAttributes[1].Op);
        Assert.Equal(original.FilteredAttributes[1].Threshold, restored.FilteredAttributes[1].Threshold);
    }

    [Fact]
    public void SyncPacket_FromConfig_DefensiveCopy()
    {
        var original = MakeTestConfig();
        var packet   = FilterSyncPacket.FromConfig(original);

        original.FilteredKeywords.Add("mutated");
        Assert.DoesNotContain("mutated", packet.FilteredKeywords);
    }

    [Fact]
    public void SyncPacket_ToConfig_DefensiveCopy()
    {
        var packet = FilterSyncPacket.FromConfig(MakeTestConfig());
        var cfg    = packet.ToConfig();

        cfg.FilteredKeywords.Add("mutated");
        Assert.DoesNotContain("mutated", packet.FilteredKeywords);
    }

    [Fact]
    public void SyncPacket_ToConfig_NullLists_Handled()
    {
        var packet = new FilterSyncPacket
        {
            FilteredItemCodes  = null!,
            FilteredKeywords   = null!,
            FilteredAttributes = null!
        };

        var cfg = packet.ToConfig();
        Assert.NotNull(cfg.FilteredItemCodes);
        Assert.NotNull(cfg.FilteredKeywords);
        Assert.NotNull(cfg.FilteredAttributes);
    }

    // ── Default values ───────────────────────────────────────────────

    [Fact]
    public void UpdatePacket_DefaultConfig_CrouchBypassTrue()
    {
        var cfg    = new LootFilterConfig();
        var packet = FilterUpdatePacket.FromConfig(cfg);

        Assert.True(packet.CrouchBypassEnabled);
        Assert.False(packet.AutoDropFiltered);
        Assert.False(packet.AllowlistMode);
        Assert.Empty(packet.FilteredAttributes);
    }

    [Fact]
    public void SyncPacket_DefaultConfig_CrouchBypassTrue()
    {
        var cfg    = new LootFilterConfig();
        var packet = FilterSyncPacket.FromConfig(cfg);

        Assert.True(packet.CrouchBypassEnabled);
        Assert.Empty(packet.FilteredAttributes);
    }
}
