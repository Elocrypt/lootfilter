using System.IO;
using Newtonsoft.Json;

namespace LootFilter.Tests;

public class ConfigStoreTests : IDisposable
{
    private readonly string tempDir;

    public ConfigStoreTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "LootFilterTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, recursive: true);
    }

    // ── Get ──────────────────────────────────────────────────────────

    [Fact]
    public void Get_ReturnsDefault_WhenNoFileExists()
    {
        var store = new PerPlayerConfigStore(tempDir);
        var cfg = store.Get("player1");

        Assert.NotNull(cfg);
        Assert.Empty(cfg.FilteredItemCodes);
        Assert.Empty(cfg.FilteredKeywords);
        Assert.False(cfg.AutoDropFiltered);
        Assert.False(cfg.AllowlistMode);
        Assert.True(cfg.CrouchBypassEnabled);
    }

    [Fact]
    public void Get_NullUid_ReturnsDefault()
    {
        var store = new PerPlayerConfigStore(tempDir);
        var cfg = store.Get(null!);

        Assert.NotNull(cfg);
        Assert.Empty(cfg.FilteredItemCodes);
    }

    [Fact]
    public void Get_EmptyUid_ReturnsDefault()
    {
        var store = new PerPlayerConfigStore(tempDir);
        var cfg = store.Get("");

        Assert.NotNull(cfg);
    }

    // ── Put + Get round-trip ─────────────────────────────────────────

    [Fact]
    public void Put_ThenGet_ReturnsSameConfig()
    {
        var store = new PerPlayerConfigStore(tempDir);
        var cfg = new LootFilterConfig
        {
            FilteredItemCodes = new() { "game:stone", "game:ore-*" },
            FilteredKeywords = new() { "flint", "dirt" },
            AutoDropFiltered = true,
            AllowlistMode = false,
            CrouchBypassEnabled = false
        };

        store.Put("player1", cfg);
        var loaded = store.Get("player1");

        Assert.Equal(cfg.FilteredItemCodes, loaded.FilteredItemCodes);
        Assert.Equal(cfg.FilteredKeywords, loaded.FilteredKeywords);
        Assert.Equal(cfg.AutoDropFiltered, loaded.AutoDropFiltered);
        Assert.Equal(cfg.AllowlistMode, loaded.AllowlistMode);
        Assert.Equal(cfg.CrouchBypassEnabled, loaded.CrouchBypassEnabled);
    }

    // ── Persistence to disk ──────────────────────────────────────────

    [Fact]
    public void Put_WritesJsonFile()
    {
        var store = new PerPlayerConfigStore(tempDir);
        var cfg = new LootFilterConfig();
        cfg.FilteredItemCodes.Add("game:stone");

        store.Put("player1", cfg);

        string path = Path.Combine(tempDir, "player1.json");
        Assert.True(File.Exists(path));

        string json = File.ReadAllText(path);
        var deserialized = JsonConvert.DeserializeObject<LootFilterConfig>(json);
        Assert.NotNull(deserialized);
        Assert.Contains("game:stone", deserialized!.FilteredItemCodes);
    }

    [Fact]
    public void FreshStore_LoadsFromExistingFile()
    {
        // Write a file manually.
        var cfg = new LootFilterConfig { AutoDropFiltered = true };
        cfg.FilteredItemCodes.Add("game:diamond");
        string json = JsonConvert.SerializeObject(cfg, Formatting.Indented);
        File.WriteAllText(Path.Combine(tempDir, "player2.json"), json);

        // New store instance should pick it up.
        var store = new PerPlayerConfigStore(tempDir);
        var loaded = store.Get("player2");

        Assert.True(loaded.AutoDropFiltered);
        Assert.Contains("game:diamond", loaded.FilteredItemCodes);
    }

    // ── Corrupt file ─────────────────────────────────────────────────

    [Fact]
    public void Load_CorruptJson_ReturnsDefault()
    {
        File.WriteAllText(Path.Combine(tempDir, "corrupt.json"), "{{not valid json!!");

        var store = new PerPlayerConfigStore(tempDir);
        var cfg = store.Get("corrupt");

        Assert.NotNull(cfg);
        Assert.Empty(cfg.FilteredItemCodes);
    }

    // ── Save (re-persist cached entry) ───────────────────────────────

    [Fact]
    public void Save_PersistsMutatedCachedConfig()
    {
        var store = new PerPlayerConfigStore(tempDir);
        var cfg = store.Get("player3"); // creates default in cache
        cfg.FilteredKeywords.Add("stone");

        store.Save("player3");

        // New store instance should see the saved keyword.
        var store2 = new PerPlayerConfigStore(tempDir);
        var reloaded = store2.Get("player3");
        Assert.Contains("stone", reloaded.FilteredKeywords);
    }

    [Fact]
    public void Save_NullUid_NoOp()
    {
        var store = new PerPlayerConfigStore(tempDir);
        // Should not throw.
        store.Save(null!);
        store.Save("");
    }

    // ── Directory creation ───────────────────────────────────────────

    [Fact]
    public void Constructor_CreatesDirectory()
    {
        string subDir = Path.Combine(tempDir, "nested", "deep");
        _ = new PerPlayerConfigStore(subDir);
        Assert.True(Directory.Exists(subDir));
    }

    // ── Isolation between players ────────────────────────────────────

    [Fact]
    public void DifferentPlayers_HaveIndependentConfigs()
    {
        var store = new PerPlayerConfigStore(tempDir);

        var cfg1 = new LootFilterConfig();
        cfg1.FilteredItemCodes.Add("game:stone");
        store.Put("alice", cfg1);

        var cfg2 = new LootFilterConfig();
        cfg2.FilteredItemCodes.Add("game:diamond");
        store.Put("bob", cfg2);

        Assert.Contains("game:stone", store.Get("alice").FilteredItemCodes);
        Assert.DoesNotContain("game:diamond", store.Get("alice").FilteredItemCodes);
        Assert.Contains("game:diamond", store.Get("bob").FilteredItemCodes);
        Assert.DoesNotContain("game:stone", store.Get("bob").FilteredItemCodes);
    }
}
