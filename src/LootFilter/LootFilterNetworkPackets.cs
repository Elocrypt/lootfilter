using System.Collections.Generic;
using ProtoBuf;

namespace LootFilter
{
    /// <summary>
    /// Sent client → server when the player saves filter changes from the GUI
    /// or when a settings toggle changes.
    /// </summary>
    [ProtoContract]
    public class FilterUpdatePacket
    {
        [ProtoMember(1)]
        public List<string> FilteredItemCodes { get; set; } = new List<string>();

        [ProtoMember(2)]
        public List<string> FilteredKeywords { get; set; } = new List<string>();

        [ProtoMember(3)]
        public bool AutoDropFiltered { get; set; }

        [ProtoMember(4)]
        public bool AllowlistMode { get; set; }

        [ProtoMember(5)]
        public bool CrouchBypassEnabled { get; set; } = true;

        /// <summary>Snapshot the given config into a new packet.</summary>
        public static FilterUpdatePacket FromConfig(LootFilterConfig cfg)
        {
            return new FilterUpdatePacket
            {
                FilteredItemCodes = new List<string>(cfg.FilteredItemCodes),
                FilteredKeywords  = new List<string>(cfg.FilteredKeywords),
                AutoDropFiltered  = cfg.AutoDropFiltered,
                AllowlistMode     = cfg.AllowlistMode,
                CrouchBypassEnabled = cfg.CrouchBypassEnabled
            };
        }

        /// <summary>Apply packet contents onto an existing config instance.</summary>
        public LootFilterConfig ToConfig()
        {
            return new LootFilterConfig
            {
                FilteredItemCodes = new List<string>(FilteredItemCodes ?? new List<string>()),
                FilteredKeywords  = new List<string>(FilteredKeywords ?? new List<string>()),
                AutoDropFiltered  = AutoDropFiltered,
                AllowlistMode     = AllowlistMode,
                CrouchBypassEnabled = CrouchBypassEnabled
            };
        }
    }

    /// <summary>
    /// Sent server → client to synchronise the authoritative config.
    /// Delivered on player join and after every confirmed <see cref="FilterUpdatePacket"/>.
    /// </summary>
    [ProtoContract]
    public class FilterSyncPacket
    {
        [ProtoMember(1)]
        public List<string> FilteredItemCodes { get; set; } = new List<string>();

        [ProtoMember(2)]
        public List<string> FilteredKeywords { get; set; } = new List<string>();

        [ProtoMember(3)]
        public bool AutoDropFiltered { get; set; }

        [ProtoMember(4)]
        public bool AllowlistMode { get; set; }

        [ProtoMember(5)]
        public bool CrouchBypassEnabled { get; set; } = true;

        /// <summary>Snapshot the given config into a new packet.</summary>
        public static FilterSyncPacket FromConfig(LootFilterConfig cfg)
        {
            return new FilterSyncPacket
            {
                FilteredItemCodes = new List<string>(cfg.FilteredItemCodes),
                FilteredKeywords  = new List<string>(cfg.FilteredKeywords),
                AutoDropFiltered  = cfg.AutoDropFiltered,
                AllowlistMode     = cfg.AllowlistMode,
                CrouchBypassEnabled = cfg.CrouchBypassEnabled
            };
        }

        /// <summary>Apply packet contents onto an existing config instance.</summary>
        public LootFilterConfig ToConfig()
        {
            return new LootFilterConfig
            {
                FilteredItemCodes = new List<string>(FilteredItemCodes ?? new List<string>()),
                FilteredKeywords  = new List<string>(FilteredKeywords ?? new List<string>()),
                AutoDropFiltered  = AutoDropFiltered,
                AllowlistMode     = AllowlistMode,
                CrouchBypassEnabled = CrouchBypassEnabled
            };
        }
    }
}
