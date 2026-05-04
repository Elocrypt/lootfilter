using System;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json;

namespace LootFilter
{
    /// <summary>
    /// Thread-safe, file-backed store for per-player <see cref="LootFilterConfig"/>
    /// instances.  Lives on the server only.  No Vintage Story API dependency —
    /// the caller supplies the root directory at construction time.
    /// </summary>
    public class PerPlayerConfigStore
    {
        private readonly string root;
        private readonly ConcurrentDictionary<string, LootFilterConfig> cache = new();

        /// <param name="configRoot">
        /// Absolute path to the directory where per-player JSON files are stored
        /// (e.g. <c>ModConfig/LootFilter/players</c>).  Created if it does not exist.
        /// </param>
        public PerPlayerConfigStore(string configRoot)
        {
            root = configRoot ?? throw new ArgumentNullException(nameof(configRoot));
            Directory.CreateDirectory(root);
        }

        /// <summary>
        /// Returns the cached config for <paramref name="uid"/>, loading from
        /// disk on first access.  Never returns null.
        /// </summary>
        public LootFilterConfig Get(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return new LootFilterConfig();
            return cache.GetOrAdd(uid, Load);
        }

        /// <summary>
        /// Overwrites the cached entry and persists to disk.
        /// Use after applying a <see cref="FilterUpdatePacket"/>.
        /// </summary>
        public void Put(string uid, LootFilterConfig cfg)
        {
            if (string.IsNullOrEmpty(uid) || cfg == null) return;
            cache[uid] = cfg;
            WriteFile(uid, cfg);
        }

        /// <summary>
        /// Persists the currently-cached config for <paramref name="uid"/> to disk.
        /// No-op if the uid has never been loaded.
        /// </summary>
        public void Save(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return;
            if (!cache.TryGetValue(uid, out LootFilterConfig cfg) || cfg == null) return;
            WriteFile(uid, cfg);
        }

        /// <summary>
        /// Deserialises a config from disk.  Returns a fresh default instance
        /// on any failure (missing file, corrupt JSON, I/O error).
        /// </summary>
        public LootFilterConfig Load(string uid)
        {
            string path = PathFor(uid);
            if (!File.Exists(path)) return new LootFilterConfig();

            try
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<LootFilterConfig>(json)
                       ?? new LootFilterConfig();
            }
            catch
            {
                // Corrupt file — return safe defaults rather than crashing.
                return new LootFilterConfig();
            }
        }

        private void WriteFile(string uid, LootFilterConfig cfg)
        {
            try
            {
                string json = JsonConvert.SerializeObject(cfg, Formatting.Indented);
                File.WriteAllText(PathFor(uid), json);
            }
            catch
            {
                // Swallow I/O errors; the in-memory cache remains authoritative.
                // A future Save() call will retry.
            }
        }

        private string PathFor(string uid) => Path.Combine(root, $"{uid}.json");
    }
}
