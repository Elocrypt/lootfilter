namespace LootFilter.Tests;

public class PacketTests
{
    private static LootFilterConfig MakeTestConfig()
    {
        return new LootFilterConfig
        {
            FilteredItemCodes = new() { "game:stone", "game:ore-*", "game:flint" },
            FilteredKeywords = new() { "dirt", "gravel" },
            AutoDropFiltered = true,
            AllowlistMode = true,
            CrouchBypassEnabled = false
        };
    }

    // ── FilterUpdatePacket ───────────────────────────────────────────

    [Fact]
    public void UpdatePacket_RoundTrip_PreservesAllFields()
    {
        var original = MakeTestConfig();
        var packet = FilterUpdatePacket.FromConfig(original);
        var restored = packet.ToConfig();

        Assert.Equal(original.FilteredItemCodes, restored.FilteredItemCodes);
        Assert.Equal(original.FilteredKeywords, restored.FilteredKeywords);
        Assert.Equal(original.AutoDropFiltered, restored.AutoDropFiltered);
        Assert.Equal(original.AllowlistMode, restored.AllowlistMode);
        Assert.Equal(original.CrouchBypassEnabled, restored.CrouchBypassEnabled);
    }

    [Fact]
    public void UpdatePacket_FromConfig_DefensiveCopy()
    {
        var original = MakeTestConfig();
        var packet = FilterUpdatePacket.FromConfig(original);

        // Mutating original should not affect the packet.
        original.FilteredItemCodes.Add("game:mutated");
        Assert.DoesNotContain("game:mutated", packet.FilteredItemCodes);
    }

    [Fact]
    public void UpdatePacket_ToConfig_DefensiveCopy()
    {
        var packet = FilterUpdatePacket.FromConfig(MakeTestConfig());
        var cfg = packet.ToConfig();

        // Mutating the returned config should not affect the packet.
        cfg.FilteredItemCodes.Add("game:mutated");
        Assert.DoesNotContain("game:mutated", packet.FilteredItemCodes);
    }

    [Fact]
    public void UpdatePacket_ToConfig_NullLists_Handled()
    {
        var packet = new FilterUpdatePacket
        {
            FilteredItemCodes = null!,
            FilteredKeywords = null!
        };

        var cfg = packet.ToConfig();
        Assert.NotNull(cfg.FilteredItemCodes);
        Assert.NotNull(cfg.FilteredKeywords);
        Assert.Empty(cfg.FilteredItemCodes);
        Assert.Empty(cfg.FilteredKeywords);
    }

    // ── FilterSyncPacket ─────────────────────────────────────────────

    [Fact]
    public void SyncPacket_RoundTrip_PreservesAllFields()
    {
        var original = MakeTestConfig();
        var packet = FilterSyncPacket.FromConfig(original);
        var restored = packet.ToConfig();

        Assert.Equal(original.FilteredItemCodes, restored.FilteredItemCodes);
        Assert.Equal(original.FilteredKeywords, restored.FilteredKeywords);
        Assert.Equal(original.AutoDropFiltered, restored.AutoDropFiltered);
        Assert.Equal(original.AllowlistMode, restored.AllowlistMode);
        Assert.Equal(original.CrouchBypassEnabled, restored.CrouchBypassEnabled);
    }

    [Fact]
    public void SyncPacket_FromConfig_DefensiveCopy()
    {
        var original = MakeTestConfig();
        var packet = FilterSyncPacket.FromConfig(original);

        original.FilteredKeywords.Add("mutated");
        Assert.DoesNotContain("mutated", packet.FilteredKeywords);
    }

    [Fact]
    public void SyncPacket_ToConfig_DefensiveCopy()
    {
        var packet = FilterSyncPacket.FromConfig(MakeTestConfig());
        var cfg = packet.ToConfig();

        cfg.FilteredKeywords.Add("mutated");
        Assert.DoesNotContain("mutated", packet.FilteredKeywords);
    }

    [Fact]
    public void SyncPacket_ToConfig_NullLists_Handled()
    {
        var packet = new FilterSyncPacket
        {
            FilteredItemCodes = null!,
            FilteredKeywords = null!
        };

        var cfg = packet.ToConfig();
        Assert.NotNull(cfg.FilteredItemCodes);
        Assert.NotNull(cfg.FilteredKeywords);
    }

    // ── Default values ───────────────────────────────────────────────

    [Fact]
    public void UpdatePacket_DefaultConfig_CrouchBypassTrue()
    {
        var cfg = new LootFilterConfig();
        var packet = FilterUpdatePacket.FromConfig(cfg);

        Assert.True(packet.CrouchBypassEnabled);
        Assert.False(packet.AutoDropFiltered);
        Assert.False(packet.AllowlistMode);
    }

    [Fact]
    public void SyncPacket_DefaultConfig_CrouchBypassTrue()
    {
        var cfg = new LootFilterConfig();
        var packet = FilterSyncPacket.FromConfig(cfg);

        Assert.True(packet.CrouchBypassEnabled);
    }
}
